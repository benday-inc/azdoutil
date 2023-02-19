[CmdletBinding()]

param([Parameter(HelpMessage='Uninstall before installing')]
    [ValidateNotNullOrEmpty()]
    [switch]
    $uninstall)

if ($uninstall -eq $true)
{
    &.\uninstall.ps1
}

dotnet tool install --global --add-source .\Benday.AzureDevOpsUtil.ConsoleUi\bin\Debug azdoutil
