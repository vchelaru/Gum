#requires -Version 7.0
<#
.SYNOPSIS
    Publishes every Wine diagnostic probe as a Windows (win-x64) executable and bundles them into a
    single zip for transfer to a macOS machine.

.DESCRIPTION
    Run this on Windows. By default the probes are published framework-dependent, so under Wine they
    use the SAME .NET 8 desktop runtime that the Gum tool uses (dotnetdesktop8 installed in the wine
    prefix). That is the most faithful test. Pass -SelfContained to instead bundle the runtime with
    each probe (larger, but removes the prefix's runtime as a variable).

.EXAMPLE
    pwsh ./publish.ps1
    pwsh ./publish.ps1 -SelfContained
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained,
    [string]$OutputRoot = "$PSScriptRoot/dist"
)

$ErrorActionPreference = "Stop"

$projects = @(
    "Probe0.Runtime",
    "Probe1.Wpf",
    "Probe2.WinForms",
    "Probe3.WindowsFormsHost",
    "Probe4.SkiaCpu",
    "Probe5.Direct3D11",
    "Probe6.KniDx11",
    "Probe7.MonoGameDesktopGL",
    "Probe8.MonoGameWindowsDX"
)

$selfContainedFlag = $SelfContained.IsPresent ? "true" : "false"

if (Test-Path $OutputRoot) {
    Remove-Item -Recurse -Force $OutputRoot
}

foreach ($project in $projects) {
    $csproj = Join-Path $PSScriptRoot "$project/$project.csproj"
    $outDir = Join-Path $OutputRoot $project
    Write-Host "Publishing $project ..." -ForegroundColor Cyan
    dotnet publish $csproj -c $Configuration -r $Runtime --self-contained $selfContainedFlag -o $outDir
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed for $project"
    }
}

# Ship the runner + README alongside the binaries so the macOS user has everything in one place.
Copy-Item (Join-Path $PSScriptRoot "run_mac_diagnostics.sh") (Join-Path $OutputRoot "run_mac_diagnostics.sh")
Copy-Item (Join-Path $PSScriptRoot "README.md") (Join-Path $OutputRoot "README.md")

$zipPath = Join-Path $PSScriptRoot "gum-mac-diagnostics.zip"
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}
Compress-Archive -Path "$OutputRoot/*" -DestinationPath $zipPath

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  Binaries: $OutputRoot"
Write-Host "  Bundle:   $zipPath"
Write-Host ""
Write-Host "Copy the bundle to the Mac, unzip it, then run:" -ForegroundColor Yellow
Write-Host "  chmod +x run_mac_diagnostics.sh && ./run_mac_diagnostics.sh"
