#!/bin/bash
#
# Probes a few Azure DevOps TFVC API shapes that azdoutil's
# assess-tfvc-migration depends on and that cannot be settled from docs.
#
# Read-only. Every call is a GET.
#
# Usage:
#   export AZDO_URL="https://dev.azure.com/yourorg"
#     ...or on-prem: export AZDO_URL="https://tfs.contoso.com/DefaultCollection"
#   export AZDO_PAT="your-pat"          # omit on-prem to try Windows auth
#
#   ./probe-tfvc-api.sh "MyProject" "$/MyProject/Main"
#
# The TFVC path should be somewhere with a decent amount of history.

set -u

PROJECT="${1:-}"
TFVC_PATH="${2:-}"

if [ -z "$PROJECT" ] || [ -z "$TFVC_PATH" ]; then
    echo "usage: $0 <team-project> <tfvc-path>"
    echo "example: $0 MyProject '\$/MyProject/Main'"
    exit 1
fi

if [ -z "${AZDO_URL:-}" ]; then
    echo "Set AZDO_URL first. Example:"
    echo "  export AZDO_URL=\"https://dev.azure.com/yourorg\""
    exit 1
fi

BASE="${AZDO_URL%/}"
API="7.0"

if [ -n "${AZDO_PAT:-}" ]; then
    AUTH=(-u ":${AZDO_PAT}")
else
    echo "(no AZDO_PAT set - trying integrated Windows auth)"
    AUTH=(--ntlm --negotiate -u :)
fi

# 90 days ago, in both the ISO form azdoutil currently sends and the
# US form the Microsoft docs use in their samples.
if date -v-90d >/dev/null 2>&1; then
    ISO_CUTOFF=$(date -u -v-90d +"%Y-%m-%dT%H:%M:%SZ")
    US_CUTOFF=$(date -u -v-90d +"%m-%d-%Y")
    CUTOFF_HUMAN=$(date -u -v-90d +"%Y-%m-%d")
else
    ISO_CUTOFF=$(date -u -d "90 days ago" +"%Y-%m-%dT%H:%M:%SZ")
    US_CUTOFF=$(date -u -d "90 days ago" +"%m-%d-%Y")
    CUTOFF_HUMAN=$(date -u -d "90 days ago" +"%Y-%m-%d")
fi

HAS_JQ=0
if command -v jq >/dev/null 2>&1; then
    HAS_JQ=1
fi

urlencode() {
    printf '%s' "$1" | od -An -tx1 | tr ' ' '\n' | grep -v '^$' | while read -r c; do
        case "$c" in
            30|31|32|33|34|35|36|37|38|39|41|42|43|44|45|46|47|48|49|4a|4b|4c|4d|4e|4f|50|51|52|53|54|55|56|57|58|59|5a|61|62|63|64|65|66|67|68|69|6a|6b|6c|6d|6e|6f|70|71|72|73|74|75|76|77|78|79|7a|2d|5f|2e|7e)
                printf '%b' "\\x$c" ;;
            *)
                printf '%%%s' "$(printf '%s' "$c" | tr 'a-f' 'A-F')" ;;
        esac
    done
}

PATH_ENC=$(urlencode "$TFVC_PATH")

call() {
    local label="$1"
    local url="$2"

    echo ""
    echo "--- $label"
    echo "    $url"

    local body
    local status

    body=$(curl -sS "${AUTH[@]}" -w $'\n__STATUS__%{http_code}' "$url" 2>&1)
    status=$(printf '%s' "$body" | sed -n 's/.*__STATUS__//p')
    body=$(printf '%s' "$body" | sed 's/__STATUS__[0-9]*$//')

    echo "    HTTP $status"

    if [ "$status" != "200" ]; then
        echo "    body: $(printf '%s' "$body" | head -c 400)"
        return
    fi

    printf '%s' "$body"
    echo ""
}

summarize_changesets() {
    local label="$1"
    local url="$2"

    echo ""
    echo "--- $label"
    echo "    $url"

    local body
    local status

    body=$(curl -sS "${AUTH[@]}" -w $'\n__STATUS__%{http_code}' "$url" 2>&1)
    status=$(printf '%s' "$body" | sed -n 's/.*__STATUS__//p')
    body=$(printf '%s' "$body" | sed 's/__STATUS__[0-9]*$//')

    echo "    HTTP $status"

    if [ "$status" != "200" ]; then
        echo "    body: $(printf '%s' "$body" | head -c 400)"
        return
    fi

    if [ "$HAS_JQ" = "1" ]; then
        echo "    count:  $(printf '%s' "$body" | jq -r '.count // "?"')"
        echo "    dates:  $(printf '%s' "$body" | jq -r '[.value[].createdDate] | sort | "oldest=" + (.[0] // "none") + "  newest=" + (.[-1] // "none")')"
        echo "    order:  $(printf '%s' "$body" | jq -r '[.value[].changesetId] | join(", ")')"
    else
        echo "    (jq not found - raw response, first 600 chars)"
        printf '%s' "$body" | head -c 600
        echo ""
    fi
}

echo "=============================================================="
echo " azdoutil TFVC API probe"
echo " collection: $BASE"
echo " project:    $PROJECT"
echo " path:       $TFVC_PATH"
echo " cutoff:     $CUTOFF_HUMAN  (90 days ago)"
echo "=============================================================="
echo ""
echo "### 1. THE QUESTION THAT MATTERS: does fromDate filter, and in what format?"
echo ""
echo "Compare the three blocks below. In blocks B and C, every returned"
echo "changeset should be dated on or after $CUTOFF_HUMAN."
echo "If a block shows changesets OLDER than that, its date format was"
echo "ignored by the server and the filter silently did nothing."

summarize_changesets \
    "A. baseline, no date filter (expect a spread of old and new)" \
    "${BASE}/${PROJECT}/_apis/tfvc/changesets?searchCriteria.itemPath=${PATH_ENC}&\$top=10&api-version=${API}"

summarize_changesets \
    "B. ISO 8601 cutoff - what azdoutil sends today" \
    "${BASE}/${PROJECT}/_apis/tfvc/changesets?searchCriteria.itemPath=${PATH_ENC}&\$top=10&searchCriteria.fromDate=${ISO_CUTOFF}&api-version=${API}"

summarize_changesets \
    "C. MM-DD-YYYY cutoff - the format the Microsoft doc samples use" \
    "${BASE}/${PROJECT}/_apis/tfvc/changesets?searchCriteria.itemPath=${PATH_ENC}&\$top=10&searchCriteria.fromDate=${US_CUTOFF}&api-version=${API}"

echo ""
echo ""
echo "### 2. EXTRA: does a one-level item listing carry isBranch and size,"
echo "###    and does it include the folder that was asked for?"

if [ "$HAS_JQ" = "1" ]; then
    echo ""
    echo "--- items, recursionLevel=OneLevel"
    curl -sS "${AUTH[@]}" \
        "${BASE}/${PROJECT}/_apis/tfvc/items?scopePath=${PATH_ENC}&recursionLevel=OneLevel&api-version=${API}" \
        | jq '{count: .count, first_three: [.value[:3][] | {path, isFolder, isBranch, size, changeDate}]}'
else
    call "items, recursionLevel=OneLevel (first 800 chars)" \
        "${BASE}/${PROJECT}/_apis/tfvc/items?scopePath=${PATH_ENC}&recursionLevel=OneLevel&api-version=${API}" \
        | head -c 800
fi

echo ""
echo ""
echo "### 3. EXTRA: what a TFVC-connected build definition's workspace"
echo "###    mappings actually look like on this server."
echo ""
echo "Finding a classic TFVC build definition in $PROJECT..."

DEFS=$(curl -sS "${AUTH[@]}" \
    "${BASE}/${PROJECT}/_apis/build/definitions?api-version=7.1" 2>&1)

if [ "$HAS_JQ" = "1" ]; then
    DEF_ID=""

    for id in $(printf '%s' "$DEFS" | jq -r '.value[]?.id' 2>/dev/null | head -25); do
        TYPE=$(curl -sS "${AUTH[@]}" \
            "${BASE}/${PROJECT}/_apis/build/definitions/${id}?api-version=7.1" \
            | jq -r '.repository.type // ""' 2>/dev/null)

        if [ "$TYPE" = "TfsVersionControl" ]; then
            DEF_ID="$id"
            break
        fi
    done

    if [ -n "$DEF_ID" ]; then
        echo "Found TFVC-connected definition id $DEF_ID"
        curl -sS "${AUTH[@]}" \
            "${BASE}/${PROJECT}/_apis/build/definitions/${DEF_ID}?includeLatestBuilds=true&api-version=7.1" \
            | jq '{name, repositoryType: .repository.type, propertyKeys: (.repository.properties | keys), tfvcMapping: .repository.properties.tfvcMapping, latestCompletedBuildFinishTime: .latestCompletedBuild.finishTime}'
    else
        echo "No TFVC-connected build definition found in the first 25 definitions."
        echo "(That is a fine answer too - it just means this project has none.)"
    fi
else
    echo "(jq not found - skipping section 3)"
fi

echo ""
echo "=============================================================="
echo " done"
echo "=============================================================="
