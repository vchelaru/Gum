<#
.SYNOPSIS
Compares the manifest.txt files that Tools/project-sweep.ps1 wrote on different OSes and fails when
the sweep left different files behind: a generated file with another name, path or content, or a
save that wrote something else.

.PARAMETER Manifest
Two or more manifest.txt files. Each is labelled by its parent folder's name in the output.

.EXAMPLE
pwsh -Command "./Tools/project-sweep-compare.ps1 -Manifest win/manifest.txt, linux/manifest.txt"

.NOTES
Exit code 0 when every manifest lists the same files with the same content, 1 otherwise. The
manifests already ignore line endings, and list generated fonts by name only.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]]$Manifest
)

$ErrorActionPreference = 'Stop'

$runs = [ordered]@{}
foreach ($file in $Manifest) {
    $name = Split-Path -Leaf (Split-Path -Parent (Resolve-Path $file).Path)
    $entries = @{}
    foreach ($line in [System.IO.File]::ReadAllLines((Resolve-Path $file).Path)) {
        if (-not $line) { continue }
        $cut = $line.LastIndexOf('|')
        $entries[$line.Substring(0, $cut)] = $line.Substring($cut + 1)
    }
    $runs[$name] = $entries
}

$names = @($runs.Keys)
$allKeys = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($entries in $runs.Values) { foreach ($key in $entries.Keys) { $allKeys.Add($key) | Out-Null } }

$differences = [System.Collections.Generic.List[string]]::new()
foreach ($key in $allKeys) {
    # A plain loop, since a pipeline drops the $null of a run that lacks the file.
    $hashes = [object[]]::new($names.Count)
    for ($i = 0; $i -lt $names.Count; $i++) { $hashes[$i] = $runs[$names[$i]][$key] }
    if ($hashes -notcontains $null -and @($hashes | Select-Object -Unique).Count -eq 1) { continue }
    $cells = for ($i = 0; $i -lt $names.Count; $i++) {
        $value = if ($null -eq $hashes[$i]) { 'missing' } else { $hashes[$i].Substring(0, 8) }
        "$($names[$i])=$value"
    }
    $differences.Add("$($key -replace '\|', ': ')  ($($cells -join ', '))")
}

if ($differences.Count -eq 0) {
    Write-Host "The sweep left identical files on $($names -join ', ') ($($allKeys.Count) files)."
    exit 0
}

Write-Host "$($differences.Count) file(s) differ between $($names -join ', '):"
$differences | ForEach-Object { Write-Host "  $_" }
exit 1
