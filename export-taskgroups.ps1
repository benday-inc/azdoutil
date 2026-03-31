<#
.SYNOPSIS
    Exports Azure DevOps task groups to JSON files organized by TPC and team project.

.DESCRIPTION
    Companion script to export-azdo-data.ps1. Exports all task group definitions
    from each team project in each Team Project Collection, writing them into
    the same folder structure (TPC/Project/task-groups/).

.PARAMETER ServerUrl
    Base URL of the Azure DevOps Server (e.g., https://tfs.contoso.com/tfs)

.PARAMETER Pat
    Personal Access Token for authentication

.PARAMETER OutputDir
    Root directory for exported JSON files (default: ./azdo-export)
    Uses the same root as export-azdo-data.ps1 so files land in the same tree.

.PARAMETER CollectionNames
    Optional array of specific TPC names to process. If omitted, discovers all collections.

.EXAMPLE
    .\export-taskgroups.ps1 -ServerUrl "https://tfs.contoso.com/tfs" -Pat "your-pat-here"

.EXAMPLE
    .\export-taskgroups.ps1 -ServerUrl "https://tfs.contoso.com/tfs" -Pat "your-pat-here" -CollectionNames @("DefaultCollection")
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ServerUrl,

    [Parameter(Mandatory = $true)]
    [string]$Pat,

    [Parameter(Mandatory = $false)]
    [string]$OutputDir = "./azdo-export",

    [Parameter(Mandatory = $false)]
    [string[]]$CollectionNames
)

$ErrorActionPreference = "Stop"

# --- Auth header ---
$base64Token = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Pat"))
$headers = @{ Authorization = "Basic $base64Token" }

$ServerUrl = $ServerUrl.TrimEnd('/')

function Invoke-AzdoApi {
    param(
        [string]$Uri,
        [switch]$IgnoreErrors
    )

    try {
        $response = Invoke-RestMethod -Uri $Uri -Headers $headers -Method Get -ContentType "application/json"
        return $response
    }
    catch {
        if ($IgnoreErrors) {
            Write-Warning "  API call failed: $Uri -- $($_.Exception.Message)"
            return $null
        }
        throw
    }
}

function Invoke-AzdoApiRaw {
    param(
        [string]$Uri,
        [switch]$IgnoreErrors
    )

    try {
        $response = Invoke-WebRequest -Uri $Uri -Headers $headers -Method Get -ContentType "application/json"
        return $response.Content
    }
    catch {
        if ($IgnoreErrors) {
            Write-Warning "  API call failed: $Uri -- $($_.Exception.Message)"
            return $null
        }
        throw
    }
}

function Save-JsonToFile {
    param(
        [string]$Path,
        [object]$Content
    )

    $dir = Split-Path -Parent $Path
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    if ($Content -is [string]) {
        $Content | Out-File -FilePath $Path -Encoding utf8
    }
    else {
        $Content | ConvertTo-Json -Depth 50 | Out-File -FilePath $Path -Encoding utf8
    }

    Write-Host "  Saved: $Path"
}

function Get-SafeFileName {
    param([string]$Name)
    return ($Name -replace '[\\/:*?"<>|]', '_')
}

# ============================================================
# 1. Discover Team Project Collections
# ============================================================
Write-Host "`n=== Discovering Team Project Collections ===" -ForegroundColor Cyan

if ($CollectionNames) {
    Write-Host "Using provided collection names: $($CollectionNames -join ', ')"
    $collections = $CollectionNames | ForEach-Object {
        [PSCustomObject]@{ name = $_ }
    }
}
else {
    $collectionsUrl = "$ServerUrl/_apis/projectCollections?api-version=2.0"
    Write-Host "Querying: $collectionsUrl"
    $collectionsResponse = Invoke-AzdoApi -Uri $collectionsUrl -IgnoreErrors

    if ($collectionsResponse -and $collectionsResponse.value) {
        $collections = $collectionsResponse.value
        Write-Host "Found $($collections.Count) collection(s):"
        foreach ($c in $collections) {
            Write-Host "  - $($c.name)"
        }
    }
    else {
        Write-Warning "Could not enumerate collections. Treating server URL as a single collection."
        $collections = @([PSCustomObject]@{ name = "" })
    }
}

# ============================================================
# 2. Process each collection
# ============================================================
foreach ($collection in $collections) {
    $collectionName = $collection.name

    if ([string]::IsNullOrEmpty($collectionName)) {
        $collectionBaseUrl = $ServerUrl
        $collectionDisplayName = "DefaultCollection"
    }
    else {
        $collectionBaseUrl = "$ServerUrl/$collectionName"
        $collectionDisplayName = $collectionName
    }

    $safeCollectionName = Get-SafeFileName $collectionDisplayName
    $collectionOutputDir = Join-Path $OutputDir $safeCollectionName

    Write-Host "`n=== Processing Collection: $collectionDisplayName ===" -ForegroundColor Cyan

    # --------------------------------------------------------
    # List team projects
    # --------------------------------------------------------
    $projectsUrl = "$collectionBaseUrl/_apis/projects?`$top=10000&api-version=7.0"
    $projectsResponse = Invoke-AzdoApi -Uri $projectsUrl -IgnoreErrors

    if (-not $projectsResponse -or -not $projectsResponse.value) {
        $projectsUrl = "$collectionBaseUrl/_apis/projects?`$top=10000&api-version=2.0"
        $projectsResponse = Invoke-AzdoApi -Uri $projectsUrl -IgnoreErrors
    }

    if (-not $projectsResponse -or -not $projectsResponse.value) {
        Write-Warning "  No projects found in collection '$collectionDisplayName'. Skipping."
        continue
    }

    $projects = $projectsResponse.value
    Write-Host "  Found $($projects.Count) project(s)"

    # --------------------------------------------------------
    # Export task groups for each project
    # --------------------------------------------------------
    foreach ($project in $projects) {
        $projectName = $project.name
        $safeProjectName = Get-SafeFileName $projectName
        $projectOutputDir = Join-Path $collectionOutputDir $safeProjectName

        $encodedProjectName = [Uri]::EscapeDataString($projectName)

        Write-Host "`n--- Task Groups: $projectName ---" -ForegroundColor Yellow

        # Task Groups API endpoint
        $taskGroupsUrl = "$collectionBaseUrl/$encodedProjectName/_apis/distributedtask/taskgroups?api-version=7.1-preview.1"
        $taskGroupsResponse = Invoke-AzdoApi -Uri $taskGroupsUrl -IgnoreErrors

        if (-not $taskGroupsResponse) {
            # Fallback to older API version
            $taskGroupsUrl = "$collectionBaseUrl/$encodedProjectName/_apis/distributedtask/taskgroups?api-version=5.0-preview.1"
            $taskGroupsResponse = Invoke-AzdoApi -Uri $taskGroupsUrl -IgnoreErrors
        }

        if (-not $taskGroupsResponse) {
            # Even older fallback
            $taskGroupsUrl = "$collectionBaseUrl/$encodedProjectName/_apis/distributedtask/taskgroups?api-version=4.0-preview.1"
            $taskGroupsResponse = Invoke-AzdoApi -Uri $taskGroupsUrl -IgnoreErrors
        }

        if ($taskGroupsResponse -and $taskGroupsResponse.value -and $taskGroupsResponse.count -gt 0) {
            $taskGroups = $taskGroupsResponse.value
            Write-Host "    Found $($taskGroups.Count) task group(s)"

            $taskGroupsDir = Join-Path $projectOutputDir "task-groups"

            # Save the summary list
            Save-JsonToFile -Path (Join-Path $taskGroupsDir "_task-groups-list.json") -Content $taskGroupsResponse

            # Export each task group individually
            foreach ($taskGroup in $taskGroups) {
                $tgId = $taskGroup.id
                $tgName = $taskGroup.name
                $safeTgName = Get-SafeFileName $tgName

                Write-Host "    Exporting task group: $tgName (Id: $tgId)"

                # Get the individual task group detail (includes all versions/revisions)
                $tgDetailUrl = "$collectionBaseUrl/$encodedProjectName/_apis/distributedtask/taskgroups/$($tgId)?api-version=7.1-preview.1"
                $tgDetailJson = Invoke-AzdoApiRaw -Uri $tgDetailUrl -IgnoreErrors

                if (-not $tgDetailJson) {
                    $tgDetailUrl = "$collectionBaseUrl/$encodedProjectName/_apis/distributedtask/taskgroups/$($tgId)?api-version=5.0-preview.1"
                    $tgDetailJson = Invoke-AzdoApiRaw -Uri $tgDetailUrl -IgnoreErrors
                }

                if ($tgDetailJson) {
                    Save-JsonToFile -Path (Join-Path $taskGroupsDir "$safeTgName.json") -Content $tgDetailJson
                }
                else {
                    Write-Warning "      Could not export task group: $tgName"
                }
            }
        }
        else {
            Write-Host "    No task groups found."
        }
    }
}

Write-Host "`n=== Task Group Export Complete ===" -ForegroundColor Green
Write-Host "Output directory: $(Resolve-Path $OutputDir -ErrorAction SilentlyContinue)" -ForegroundColor Green
