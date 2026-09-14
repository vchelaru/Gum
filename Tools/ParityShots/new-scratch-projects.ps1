# Copies a sample project twice (one copy per head), so a run never touches Samples\ itself:
# opening a project can re-save it. Prints the two .gumx paths to pass to the heads.
param(
    [string]$Sample = (Join-Path $PSScriptRoot "..\..\Samples\GameUiSamples\Content\GumProject"),
    [string]$Root = (Join-Path $env:TEMP "GumParity")
)
$ErrorActionPreference = "Stop"
foreach ($head in @("wpf", "avalonia")) {
    $target = Join-Path $Root $head
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item -Path (Join-Path $Sample "*") -Destination $target -Recurse -Force
    $gumx = Get-ChildItem $target -Filter *.gumx | Select-Object -First 1
    "$head`t$($gumx.FullName)"
}
