#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Lists the functionality-inventory IDs that no non-skipped test tags with [Trait("Feature", "<ID>")].

.DESCRIPTION
    Reads the IDs from Direction/avalonia-migration/functionality-inventory.md and the Feature traits
    from every test file under Tests/. A trait counts only when the attribute block it sits in has no
    Skip. Prints the untested count per area, then the untested IDs, then any traited ID the
    inventory does not list (a typo, or an item that needs adding).

.PARAMETER Detail
    Also print each untested item's inventory line.

.EXAMPLE
    pwsh Tools/e2e-coverage.ps1
#>
param(
    [switch]$Detail
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$inventoryPath = Join-Path $repoRoot 'Direction/avalonia-migration/functionality-inventory.md'

# Inventory items: "- TREE-043 F2 renames element. tested: ..."
$inventory = [ordered]@{}
foreach ($line in Get-Content $inventoryPath) {
    if ($line -match '^- ([A-Z]+-\d{3}) (.*)$') {
        $inventory[$Matches[1]] = $Matches[2]
    }
}

# Feature traits of non-skipped tests. Attributes stack on the lines above a member; the block ends
# at the first line that is not an attribute, so a Skip anywhere in it drops every trait in it.
$tagged = @{}
$testFiles = Get-ChildItem -Path (Join-Path $repoRoot 'Tests') -Recurse -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
foreach ($file in $testFiles) {
    $blockFeatures = @()
    $blockSkipped = $false
    foreach ($line in Get-Content $file.FullName) {
        $trimmed = $line.Trim()
        if ($trimmed.StartsWith('//')) {
            continue
        }
        if ($trimmed.StartsWith('[')) {
            if ($trimmed -match 'Skip\s*=') {
                $blockSkipped = $true
            }
            foreach ($match in [regex]::Matches($trimmed, 'Trait\(\s*"Feature"\s*,\s*"([A-Z]+-\d{3})"\s*\)')) {
                $blockFeatures += $match.Groups[1].Value
            }
            continue
        }
        if (-not $blockSkipped) {
            foreach ($id in $blockFeatures) {
                $tagged[$id] = $true
            }
        }
        $blockFeatures = @()
        $blockSkipped = $false
    }
}

$untested = @($inventory.Keys | Where-Object { -not $tagged.ContainsKey($_) })
$areaOf = { param($id) $id.Substring(0, $id.IndexOf('-')) }

Write-Host "Inventory items: $($inventory.Count); tagged by a non-skipped test: $($inventory.Count - $untested.Count); untested: $($untested.Count)"
Write-Host ''
Write-Host 'Untested per area:'
$inventory.Keys | Group-Object { & $areaOf $_ } | ForEach-Object {
    $area = $_.Name
    $missing = @($untested | Where-Object { (& $areaOf $_) -eq $area }).Count
    '  {0,-6} {1,3} of {2,3}' -f $area, $missing, $_.Count
} | Write-Host

Write-Host ''
Write-Host 'Untested IDs:'
foreach ($group in ($untested | Group-Object { & $areaOf $_ })) {
    if ($Detail) {
        foreach ($id in $group.Group) {
            Write-Host "  $id $($inventory[$id])"
        }
    }
    else {
        Write-Host "  $($group.Name): $($group.Group -join ', ')"
    }
}

$unknown = @($tagged.Keys | Where-Object { -not $inventory.Contains($_) } | Sort-Object)
if ($unknown.Count -gt 0) {
    Write-Host ''
    Write-Host "Tagged but not in the inventory: $($unknown -join ', ')"
}
