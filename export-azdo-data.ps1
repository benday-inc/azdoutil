<#
.SYNOPSIS
    Exports Azure DevOps build definitions, release definitions, and agent pool
    info to JSON files organized by TPC and team project.

.DESCRIPTION
    Workaround script that uses direct REST API calls with a PAT when
    azdoutil cannot be run on the target machine. Works with on-premises
    Azure DevOps Server across multiple Team Project Collections.

.PARAMETER ServerUrl
    Base URL of the Azure DevOps Server (e.g., https://tfs.contoso.com/tfs)

.PARAMETER Pat
    Personal Access Token for authentication

.PARAMETER OutputDir
    Root directory for exported JSON files (default: ./azdo-export)

.PARAMETER CollectionNames
    Optional array of specific TPC names to process. If omitted, discovers all collections.

.EXAMPLE
    .\export-azdo-data.ps1 -ServerUrl "https://tfs.contoso.com/tfs" -Pat "your-pat-here"

.EXAMPLE
    .\export-azdo-data.ps1 -ServerUrl "https://tfs.contoso.com/tfs" -Pat "your-pat-here" -CollectionNames @("DefaultCollection", "ProjectCollection2")
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

# Ensure trailing slash on server URL
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
    # Replace characters that are invalid in file/folder names
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
    # Try the TPC enumeration API (on-prem)
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
        # Fallback: if this is Azure DevOps Services or single-collection,
        # treat the server URL itself as the only collection
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
    Write-Host "  Base URL: $collectionBaseUrl"

    # --------------------------------------------------------
    # 2a. List team projects in this collection
    # --------------------------------------------------------
    Write-Host "`n--- Listing Team Projects ---" -ForegroundColor Yellow

    $projectsUrl = "$collectionBaseUrl/_apis/projects?`$top=10000&api-version=7.0"
    $projectsResponse = Invoke-AzdoApi -Uri $projectsUrl -IgnoreErrors

    if (-not $projectsResponse -or -not $projectsResponse.value -or $projectsResponse.count -eq 0) {
        # Fallback to older API version for older TFS
        $projectsUrl = "$collectionBaseUrl/_apis/projects?`$top=10000&api-version=2.0"
        $projectsResponse = Invoke-AzdoApi -Uri $projectsUrl -IgnoreErrors
    }

    if (-not $projectsResponse -or -not $projectsResponse.value) {
        Write-Warning "  No projects found in collection '$collectionDisplayName'. Skipping."
        continue
    }

    $projects = $projectsResponse.value
    Write-Host "  Found $($projects.Count) project(s)"

    # Save the projects list
    Save-JsonToFile -Path (Join-Path $collectionOutputDir "_projects.json") -Content $projectsResponse

    # --------------------------------------------------------
    # 2b. Agent pools (collection-level, not per-project)
    # --------------------------------------------------------
    Write-Host "`n--- Exporting Agent Pools ---" -ForegroundColor Yellow

    $agentPoolsUrl = "$collectionBaseUrl/_apis/distributedtask/pools?api-version=7.1-preview.1"
    $agentPoolsResponse = Invoke-AzdoApi -Uri $agentPoolsUrl -IgnoreErrors

    if (-not $agentPoolsResponse) {
        # Fallback to older API
        $agentPoolsUrl = "$collectionBaseUrl/_apis/distributedtask/pools?api-version=2.0"
        $agentPoolsResponse = Invoke-AzdoApi -Uri $agentPoolsUrl -IgnoreErrors
    }

    if ($agentPoolsResponse -and $agentPoolsResponse.value) {
        $pools = $agentPoolsResponse.value
        Write-Host "  Found $($pools.Count) agent pool(s)"

        # For each pool, get agents with capabilities
        foreach ($pool in $pools) {
            $poolId = $pool.id
            $poolName = $pool.name
            Write-Host "  Pool: $poolName (Id: $poolId) - fetching agents..."

            $agentsUrl = "$collectionBaseUrl/_apis/distributedtask/pools/$poolId/agents?includeCapabilities=true&api-version=7.1-preview.1"
            $agentsResponse = Invoke-AzdoApi -Uri $agentsUrl -IgnoreErrors

            if (-not $agentsResponse) {
                # Fallback
                $agentsUrl = "$collectionBaseUrl/_apis/distributedtask/pools/$poolId/agents?includeCapabilities=true&api-version=2.0"
                $agentsResponse = Invoke-AzdoApi -Uri $agentsUrl -IgnoreErrors
            }

            if ($agentsResponse) {
                $pool | Add-Member -NotePropertyName "agents" -NotePropertyValue $agentsResponse.value -Force
                $agentCount = if ($agentsResponse.value) { $agentsResponse.value.Count } else { 0 }
                Write-Host "    Found $agentCount agent(s)"
            }
        }

        $agentPoolsDir = Join-Path $collectionOutputDir "_agent-pools"
        Save-JsonToFile -Path (Join-Path $agentPoolsDir "agent-pools.json") -Content $agentPoolsResponse

        # Also save each pool individually for easier browsing
        foreach ($pool in $pools) {
            $safePoolName = Get-SafeFileName $pool.name
            Save-JsonToFile -Path (Join-Path $agentPoolsDir "$safePoolName.json") -Content $pool
        }
    }
    else {
        Write-Warning "  No agent pools found or API not available."
    }

    # --------------------------------------------------------
    # 2c. Process each team project
    # --------------------------------------------------------
    foreach ($project in $projects) {
        $projectName = $project.name
        $safeProjectName = Get-SafeFileName $projectName
        $projectOutputDir = Join-Path $collectionOutputDir $safeProjectName

        Write-Host "`n--- Project: $projectName ---" -ForegroundColor Yellow

        # Build the release base URL (vsrm prefix for Azure DevOps Services)
        if ($collectionBaseUrl -match "^https://dev\.azure\.com/") {
            $releaseBaseUrl = $collectionBaseUrl -replace "^https://dev\.", "https://vsrm.dev."
        }
        else {
            # On-prem: release APIs are on the same server
            $releaseBaseUrl = $collectionBaseUrl
        }

        $encodedProjectName = [Uri]::EscapeDataString($projectName)

        # --------------------------------------------------
        # Build Definitions
        # --------------------------------------------------
        Write-Host "  Exporting build definitions..."

        $buildDefsUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/definitions?api-version=7.1"
        $buildDefsResponse = Invoke-AzdoApi -Uri $buildDefsUrl -IgnoreErrors

        if (-not $buildDefsResponse) {
            # Fallback to older API
            $buildDefsUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/definitions?api-version=2.0"
            $buildDefsResponse = Invoke-AzdoApi -Uri $buildDefsUrl -IgnoreErrors
        }

        if ($buildDefsResponse -and $buildDefsResponse.value -and $buildDefsResponse.count -gt 0) {
            $buildDefs = $buildDefsResponse.value
            Write-Host "    Found $($buildDefs.Count) build definition(s)"

            $buildDefsDir = Join-Path $projectOutputDir "build-definitions"

            # Save the summary list
            Save-JsonToFile -Path (Join-Path $buildDefsDir "_build-definitions-list.json") -Content $buildDefsResponse

            # Export each build definition in detail
            foreach ($buildDef in $buildDefs) {
                $buildId = $buildDef.id
                $buildName = $buildDef.name
                $safeBuildName = Get-SafeFileName $buildName

                Write-Host "    Exporting build: $buildName (Id: $buildId)"

                $buildDetailUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/definitions/$($buildId)?api-version=7.0&includeLatestBuilds=true"
                $buildDetailJson = Invoke-AzdoApiRaw -Uri $buildDetailUrl -IgnoreErrors

                if (-not $buildDetailJson) {
                    # Fallback
                    $buildDetailUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/definitions/$($buildId)?api-version=2.0"
                    $buildDetailJson = Invoke-AzdoApiRaw -Uri $buildDetailUrl -IgnoreErrors
                }

                if ($buildDetailJson) {
                    Save-JsonToFile -Path (Join-Path $buildDefsDir "$safeBuildName.json") -Content $buildDetailJson
                }
                else {
                    Write-Warning "      Could not export build definition: $buildName"
                }
            }
        }
        else {
            Write-Host "    No build definitions found."
        }

        # --------------------------------------------------
        # XAML Build Definitions (legacy)
        # --------------------------------------------------
        Write-Host "  Checking for XAML build definitions..."

        $xamlBuildDefsUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/definitions?api-version=2.2"
        $xamlBuildDefsResponse = Invoke-AzdoApi -Uri $xamlBuildDefsUrl -IgnoreErrors

        if ($xamlBuildDefsResponse -and $xamlBuildDefsResponse.value -and $xamlBuildDefsResponse.count -gt 0) {
            $xamlDefs = $xamlBuildDefsResponse.value

            # Filter to only XAML definitions (type = "xaml" in older API)
            # The 2.2 API returns both XAML and non-XAML, so we save them all
            # but in a separate folder for clarity
            Write-Host "    Found $($xamlDefs.Count) definition(s) from legacy API"

            $xamlBuildDefsDir = Join-Path $projectOutputDir "xaml-build-definitions"
            Save-JsonToFile -Path (Join-Path $xamlBuildDefsDir "_xaml-build-definitions-list.json") -Content $xamlBuildDefsResponse

            foreach ($xamlDef in $xamlDefs) {
                $xamlId = $xamlDef.id
                $xamlName = $xamlDef.name
                $safeXamlName = Get-SafeFileName $xamlName

                $xamlDetailUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/definitions/$($xamlId)?api-version=2.0"
                $xamlDetailJson = Invoke-AzdoApiRaw -Uri $xamlDetailUrl -IgnoreErrors

                if ($xamlDetailJson) {
                    Save-JsonToFile -Path (Join-Path $xamlBuildDefsDir "$safeXamlName.json") -Content $xamlDetailJson
                }
            }
        }
        else {
            Write-Host "    No XAML build definitions found."
        }

        # --------------------------------------------------
        # Release Definitions
        # --------------------------------------------------
        Write-Host "  Exporting release definitions..."

        $releaseDefsUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/definitions?api-version=7.1"
        $releaseDefsResponse = Invoke-AzdoApi -Uri $releaseDefsUrl -IgnoreErrors

        if (-not $releaseDefsResponse) {
            # Fallback to older API version
            $releaseDefsUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/definitions?api-version=3.0-preview.1"
            $releaseDefsResponse = Invoke-AzdoApi -Uri $releaseDefsUrl -IgnoreErrors
        }

        if ($releaseDefsResponse -and $releaseDefsResponse.value -and $releaseDefsResponse.count -gt 0) {
            $releaseDefs = $releaseDefsResponse.value
            Write-Host "    Found $($releaseDefs.Count) release definition(s)"

            $releaseDefsDir = Join-Path $projectOutputDir "release-definitions"

            # Save the summary list
            Save-JsonToFile -Path (Join-Path $releaseDefsDir "_release-definitions-list.json") -Content $releaseDefsResponse

            # Export each release definition in detail
            foreach ($releaseDef in $releaseDefs) {
                $releaseId = $releaseDef.id
                $releaseName = $releaseDef.name
                $safeReleaseName = Get-SafeFileName $releaseName

                Write-Host "    Exporting release: $releaseName (Id: $releaseId)"

                $releaseDetailUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/definitions/$($releaseId)?api-version=7.1"
                $releaseDetailJson = Invoke-AzdoApiRaw -Uri $releaseDetailUrl -IgnoreErrors

                if (-not $releaseDetailJson) {
                    # Fallback
                    $releaseDetailUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/definitions/$($releaseId)?api-version=3.0-preview.1"
                    $releaseDetailJson = Invoke-AzdoApiRaw -Uri $releaseDetailUrl -IgnoreErrors
                }

                if ($releaseDetailJson) {
                    Save-JsonToFile -Path (Join-Path $releaseDefsDir "$safeReleaseName.json") -Content $releaseDetailJson
                }
                else {
                    Write-Warning "      Could not export release definition: $releaseName"
                }
            }
        }
        else {
            Write-Host "    No release definitions found."
        }
    }
}

Write-Host "`n=== Export Complete ===" -ForegroundColor Green
Write-Host "Output directory: $(Resolve-Path $OutputDir -ErrorAction SilentlyContinue)" -ForegroundColor Green
