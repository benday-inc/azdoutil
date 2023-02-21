[CmdletBinding()]

param([Parameter(HelpMessage='Uninstall before installing')]
    [ValidateNotNullOrEmpty()]
    [switch]
    $reinstall)

if ($reinstall -eq $true)
{
    &.\uninstall.ps1
}

dotnet tool install --global --add-source .\Benday.AzureDevOpsUtil.ConsoleUi\bin\Debug azdoutil
