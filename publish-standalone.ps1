#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Publishes azdoutil as a self-contained, single-file executable
.DESCRIPTION
    Creates a standalone executable that includes the .NET runtime and doesn't require .NET to be installed on the target machine.
.PARAMETER Runtime
    Target runtime identifier (default: win-x64). Common values: win-x64, win-x86, linux-x64, osx-x64
.PARAMETER Configuration
    Build configuration (default: Release)
.PARAMETER Framework
    Target framework (default: net10.0). Options: net8.0, net9.0, net10.0
.PARAMETER OutputPath
    Output directory for the published executable (default: ./publish)
.EXAMPLE
    .\publish-standalone.ps1
.EXAMPLE
    .\publish-standalone.ps1 -Runtime win-x86 -Framework net8.0
#>

param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$Framework = "net10.0",
    [string]$OutputPath = "./publish"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Publishing azdoutil as standalone executable" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Runtime:       $Runtime"
Write-Host "  Framework:     $Framework"
Write-Host "  Configuration: $Configuration"
Write-Host "  Output Path:   $OutputPath"
Write-Host ""

# Clean output directory
if (Test-Path $OutputPath) {
    Write-Host "Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

# Publish the project
Write-Host "Publishing project..." -ForegroundColor Yellow
dotnet publish Benday.AzureDevOpsUtil.ConsoleUi/Benday.AzureDevOpsUtil.ConsoleUi.csproj `
    --configuration $Configuration `
    --runtime $Runtime `
    --framework $Framework `
    --self-contained true `
    --output $OutputPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Build successful!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Find the executable
$exeName = if ($Runtime.StartsWith("win")) { "azdoutil.exe" } else { "azdoutil" }
$exePath = Join-Path $OutputPath $exeName

if (Test-Path $exePath) {
    $fileSize = (Get-Item $exePath).Length / 1MB
    Write-Host "Executable created:" -ForegroundColor Cyan
    Write-Host "  Location: $exePath"
    Write-Host "  Size:     $($fileSize.ToString('0.00')) MB"
    Write-Host ""
    Write-Host "This executable can run on any $Runtime machine without .NET installed!" -ForegroundColor Green
} else {
    Write-Host "Warning: Expected executable not found at $exePath" -ForegroundColor Yellow
    Write-Host "Check the output directory for the published files." -ForegroundColor Yellow
}

Write-Host ""
