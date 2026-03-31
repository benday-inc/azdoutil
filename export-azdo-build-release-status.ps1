<#
.SYNOPSIS
    Reports the last run status of all build and release definitions across
    Azure DevOps collections and team projects.

.DESCRIPTION
    For each build and release definition, reports:
    1. Last time it ran
    2. Outcome of the last run
    3. Last time it succeeded or partially succeeded
    4. Which agent pool it used

    Outputs a CSV file and console summary.

.PARAMETER ServerUrl
    Base URL of the Azure DevOps Server (e.g., https://tfs.contoso.com/tfs)

.PARAMETER Pat
    Personal Access Token for authentication

.PARAMETER OutputDir
    Root directory for exported files (default: ./azdo-status-report)

.PARAMETER CollectionNames
    Optional array of specific TPC names to process. If omitted, discovers all collections.

.EXAMPLE
    .\export-azdo-build-release-status.ps1 -ServerUrl "https://tfs.contoso.com/tfs" -Pat "your-pat-here"

.EXAMPLE
    .\export-azdo-build-release-status.ps1 -ServerUrl "https://tfs.contoso.com/tfs" -Pat "your-pat-here" -CollectionNames @("DefaultCollection")
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ServerUrl,

    [Parameter(Mandatory = $true)]
    [string]$Pat,

    [Parameter(Mandatory = $false)]
    [string]$OutputDir = "./azdo-status-report",

    [Parameter(Mandatory = $false)]
    [string[]]$CollectionNames
)

$ErrorActionPreference = "Stop"

# --- Auth header ---
$base64Token = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$Pat"))
$headers = @{ Authorization = "Basic $base64Token" }

# Ensure no trailing slash on server URL
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

function Get-SafeFileName {
    param([string]$Name)
    return ($Name -replace '[\\/:*?"<>|]', '_')
}

# Collect all results
$allResults = [System.Collections.ArrayList]::new()

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

    Write-Host "`n=== Processing Collection: $collectionDisplayName ===" -ForegroundColor Cyan

    # --------------------------------------------------------
    # List team projects
    # --------------------------------------------------------
    $projectsUrl = "$collectionBaseUrl/_apis/projects?`$top=10000&api-version=7.0"
    $projectsResponse = Invoke-AzdoApi -Uri $projectsUrl -IgnoreErrors

    if (-not $projectsResponse -or -not $projectsResponse.value -or $projectsResponse.count -eq 0) {
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
    # Process each team project
    # --------------------------------------------------------
    foreach ($project in $projects) {
        $projectName = $project.name
        $encodedProjectName = [Uri]::EscapeDataString($projectName)

        # Build the release base URL (vsrm prefix for Azure DevOps Services)
        if ($collectionBaseUrl -match "^https://dev\.azure\.com/") {
            $releaseBaseUrl = $collectionBaseUrl -replace "^https://dev\.", "https://vsrm.dev."
        }
        else {
            $releaseBaseUrl = $collectionBaseUrl
        }

        Write-Host "`n--- Project: $projectName ---" -ForegroundColor Yellow

        # ==================================================
        # BUILDS
        # ==================================================
        Write-Host "  Querying build definitions..."

        $buildDefsUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/definitions?api-version=7.1"
        $buildDefsResponse = Invoke-AzdoApi -Uri $buildDefsUrl -IgnoreErrors

        if (-not $buildDefsResponse) {
            $buildDefsUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/definitions?api-version=2.0"
            $buildDefsResponse = Invoke-AzdoApi -Uri $buildDefsUrl -IgnoreErrors
        }

        if ($buildDefsResponse -and $buildDefsResponse.value -and $buildDefsResponse.count -gt 0) {
            $buildDefs = $buildDefsResponse.value
            Write-Host "    Found $($buildDefs.Count) build definition(s)"

            foreach ($buildDef in $buildDefs) {
                $defId = $buildDef.id
                $defName = $buildDef.name

                Write-Host "    Checking build: $defName (Id: $defId)"

                # --- Last build (any result) ---
                $lastBuildUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/builds?definitions=$defId&`$top=1&queryOrder=finishTimeDescending&api-version=7.1"
                $lastBuildResponse = Invoke-AzdoApi -Uri $lastBuildUrl -IgnoreErrors

                if (-not $lastBuildResponse) {
                    $lastBuildUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/builds?definitions=$defId&`$top=1&queryOrder=finishTimeDescending&api-version=2.0"
                    $lastBuildResponse = Invoke-AzdoApi -Uri $lastBuildUrl -IgnoreErrors
                }

                $lastRunTime = ""
                $lastRunResult = ""
                $lastRunAgentPool = ""

                if ($lastBuildResponse -and $lastBuildResponse.value -and $lastBuildResponse.count -gt 0) {
                    $lastBuild = $lastBuildResponse.value[0]
                    $lastRunTime = $lastBuild.finishTime
                    $lastRunResult = $lastBuild.result
                    if (-not $lastRunResult -and $lastBuild.status) {
                        $lastRunResult = "status:$($lastBuild.status)"
                    }

                    # Agent pool from the build's queue
                    if ($lastBuild.queue -and $lastBuild.queue.pool) {
                        $lastRunAgentPool = $lastBuild.queue.pool.name
                    }
                    elseif ($lastBuild.queue) {
                        $lastRunAgentPool = $lastBuild.queue.name
                    }
                }

                # --- Last successful or partially succeeded build ---
                $lastSuccessUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/builds?definitions=$defId&`$top=1&queryOrder=finishTimeDescending&resultFilter=succeeded,partiallySucceeded&api-version=7.1"
                $lastSuccessResponse = Invoke-AzdoApi -Uri $lastSuccessUrl -IgnoreErrors

                if (-not $lastSuccessResponse) {
                    $lastSuccessUrl = "$collectionBaseUrl/$encodedProjectName/_apis/build/builds?definitions=$defId&`$top=1&queryOrder=finishTimeDescending&resultFilter=succeeded,partiallySucceeded&api-version=2.0"
                    $lastSuccessResponse = Invoke-AzdoApi -Uri $lastSuccessUrl -IgnoreErrors
                }

                $lastSuccessTime = ""

                if ($lastSuccessResponse -and $lastSuccessResponse.value -and $lastSuccessResponse.count -gt 0) {
                    $lastSuccessTime = $lastSuccessResponse.value[0].finishTime
                }

                $row = [PSCustomObject]@{
                    Collection        = $collectionDisplayName
                    Project           = $projectName
                    Type              = "Build"
                    DefinitionName    = $defName
                    DefinitionId      = $defId
                    LastRunTime       = $lastRunTime
                    LastRunResult     = $lastRunResult
                    LastSuccessTime   = $lastSuccessTime
                    AgentPool         = $lastRunAgentPool
                }

                $allResults.Add($row) | Out-Null

                Write-Host "      Last run: $lastRunTime | Result: $lastRunResult | Last success: $lastSuccessTime | Pool: $lastRunAgentPool"
            }
        }
        else {
            Write-Host "    No build definitions found."
        }

        # ==================================================
        # RELEASES
        # ==================================================
        Write-Host "  Querying release definitions..."

        $releaseDefsUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/definitions?api-version=7.1"
        $releaseDefsResponse = Invoke-AzdoApi -Uri $releaseDefsUrl -IgnoreErrors

        if (-not $releaseDefsResponse) {
            $releaseDefsUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/definitions?api-version=3.0-preview.1"
            $releaseDefsResponse = Invoke-AzdoApi -Uri $releaseDefsUrl -IgnoreErrors
        }

        if ($releaseDefsResponse -and $releaseDefsResponse.value -and $releaseDefsResponse.count -gt 0) {
            $releaseDefs = $releaseDefsResponse.value
            Write-Host "    Found $($releaseDefs.Count) release definition(s)"

            foreach ($releaseDef in $releaseDefs) {
                $defId = $releaseDef.id
                $defName = $releaseDef.name

                Write-Host "    Checking release: $defName (Id: $defId)"

                # --- Last release (any status) ---
                $lastReleaseUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/releases?definitionId=$defId&`$top=1&queryOrder=descending&api-version=7.1"
                $lastReleaseResponse = Invoke-AzdoApi -Uri $lastReleaseUrl -IgnoreErrors

                if (-not $lastReleaseResponse) {
                    $lastReleaseUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/releases?definitionId=$defId&`$top=1&queryOrder=descending&api-version=3.0-preview.1"
                    $lastReleaseResponse = Invoke-AzdoApi -Uri $lastReleaseUrl -IgnoreErrors
                }

                $lastRunTime = ""
                $lastRunResult = ""
                $lastRunAgentPool = ""
                $lastSuccessTime = ""

                if ($lastReleaseResponse -and $lastReleaseResponse.value -and $lastReleaseResponse.count -gt 0) {
                    $lastRelease = $lastReleaseResponse.value[0]
                    $lastRunTime = $lastRelease.modifiedOn
                    if (-not $lastRunTime) { $lastRunTime = $lastRelease.createdOn }

                    # Get the full release details to see environment statuses and agent info
                    $releaseDetailUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/releases/$($lastRelease.id)?api-version=7.1"
                    $releaseDetail = Invoke-AzdoApi -Uri $releaseDetailUrl -IgnoreErrors

                    if (-not $releaseDetail) {
                        $releaseDetailUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/releases/$($lastRelease.id)?api-version=3.0-preview.1"
                        $releaseDetail = Invoke-AzdoApi -Uri $releaseDetailUrl -IgnoreErrors
                    }

                    if ($releaseDetail -and $releaseDetail.environments) {
                        # Determine overall result from environments
                        $envStatuses = $releaseDetail.environments | ForEach-Object { $_.status }
                        if ($envStatuses -contains "rejected") {
                            $lastRunResult = "rejected"
                        }
                        elseif ($envStatuses -contains "canceled" -or $envStatuses -contains "cancelled") {
                            $lastRunResult = "canceled"
                        }
                        elseif ($envStatuses -contains "inProgress") {
                            $lastRunResult = "inProgress"
                        }
                        elseif ($envStatuses -contains "partiallySucceeded") {
                            $lastRunResult = "partiallySucceeded"
                        }
                        elseif (($envStatuses | Where-Object { $_ -eq "succeeded" }).Count -eq $envStatuses.Count) {
                            $lastRunResult = "succeeded"
                        }
                        else {
                            $lastRunResult = ($envStatuses | Select-Object -Unique) -join ";"
                        }

                        # Try to get agent pool from the last completed environment's deploy phases
                        foreach ($env in $releaseDetail.environments) {
                            if ($env.deployPhasesSnapshot) {
                                foreach ($phase in $env.deployPhasesSnapshot) {
                                    if ($phase.deploymentInput -and $phase.deploymentInput.queueId) {
                                        # We have a queue ID but need the name;
                                        # check if there's an agentSpecification or name
                                        if ($phase.name) {
                                            $lastRunAgentPool = $phase.name
                                        }
                                    }
                                }
                            }
                            # Also check release deploy steps for agent info
                            if ($env.deploySteps) {
                                foreach ($step in $env.deploySteps) {
                                    if ($step.releaseDeployPhases) {
                                        foreach ($rdp in $step.releaseDeployPhases) {
                                            if ($rdp.deploymentJobs) {
                                                foreach ($job in $rdp.deploymentJobs) {
                                                    if ($job.job -and $job.job.agentName) {
                                                        $lastRunAgentPool = $job.job.agentName
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else {
                        $lastRunResult = $lastRelease.status
                    }
                }

                # --- Last successful release ---
                $lastSuccessReleaseUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/releases?definitionId=$defId&`$top=50&queryOrder=descending&api-version=7.1"
                $lastSuccessReleaseResponse = Invoke-AzdoApi -Uri $lastSuccessReleaseUrl -IgnoreErrors

                if (-not $lastSuccessReleaseResponse) {
                    $lastSuccessReleaseUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/releases?definitionId=$defId&`$top=50&queryOrder=descending&api-version=3.0-preview.1"
                    $lastSuccessReleaseResponse = Invoke-AzdoApi -Uri $lastSuccessReleaseUrl -IgnoreErrors
                }

                if ($lastSuccessReleaseResponse -and $lastSuccessReleaseResponse.value) {
                    foreach ($rel in $lastSuccessReleaseResponse.value) {
                        # Get release detail to check if all environments succeeded
                        $relDetailUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/releases/$($rel.id)?api-version=7.1"
                        $relDetail = Invoke-AzdoApi -Uri $relDetailUrl -IgnoreErrors

                        if (-not $relDetail) {
                            $relDetailUrl = "$releaseBaseUrl/$encodedProjectName/_apis/release/releases/$($rel.id)?api-version=3.0-preview.1"
                            $relDetail = Invoke-AzdoApi -Uri $relDetailUrl -IgnoreErrors
                        }

                        if ($relDetail -and $relDetail.environments) {
                            $envStatuses = $relDetail.environments | ForEach-Object { $_.status }
                            $allSucceeded = ($envStatuses | Where-Object { $_ -eq "succeeded" -or $_ -eq "partiallySucceeded" }).Count -eq $envStatuses.Count
                            if ($allSucceeded -and $envStatuses.Count -gt 0) {
                                $lastSuccessTime = $relDetail.modifiedOn
                                if (-not $lastSuccessTime) { $lastSuccessTime = $relDetail.createdOn }
                                break
                            }
                        }
                    }
                }

                $row = [PSCustomObject]@{
                    Collection        = $collectionDisplayName
                    Project           = $projectName
                    Type              = "Release"
                    DefinitionName    = $defName
                    DefinitionId      = $defId
                    LastRunTime       = $lastRunTime
                    LastRunResult     = $lastRunResult
                    LastSuccessTime   = $lastSuccessTime
                    AgentPool         = $lastRunAgentPool
                }

                $allResults.Add($row) | Out-Null

                Write-Host "      Last run: $lastRunTime | Result: $lastRunResult | Last success: $lastSuccessTime | Agent: $lastRunAgentPool"
            }
        }
        else {
            Write-Host "    No release definitions found."
        }
    }
}

# ============================================================
# 3. Output results
# ============================================================
Write-Host "`n=== Summary ===" -ForegroundColor Green

if ($allResults.Count -eq 0) {
    Write-Host "No build or release definitions found." -ForegroundColor Yellow
}
else {
    # Console table
    $allResults | Format-Table -AutoSize -Property Collection, Project, Type, DefinitionName, LastRunTime, LastRunResult, LastSuccessTime, AgentPool

    # Ensure output directory exists
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    # Export to CSV
    $csvPath = Join-Path $OutputDir "build-release-status.csv"
    $allResults | Export-Csv -Path $csvPath -NoTypeInformation -Encoding utf8
    Write-Host "CSV exported to: $csvPath" -ForegroundColor Green

    # Export to JSON
    $jsonPath = Join-Path $OutputDir "build-release-status.json"
    $allResults | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonPath -Encoding utf8
    Write-Host "JSON exported to: $jsonPath" -ForegroundColor Green
}

Write-Host "`n=== Report Complete ===" -ForegroundColor Green
