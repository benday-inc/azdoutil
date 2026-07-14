[CmdletBinding()]

param([Parameter(HelpMessage='Uninstall before installing')]
    [ValidateNotNullOrEmpty()]
    [switch]
    $reinstall)

if ($reinstall -eq $true)
{
    &.\uninstall.ps1
}

dotnet build

$pathToDebugFolder = Join-Path $PSScriptRoot 'Benday.AzureDevOpsUtil.ConsoleUi\bin\Debug'

Write-Host "Installing cms from $pathToDebugFolder"

dotnet tool install --global --add-source "$pathToDebugFolder" azdoutil