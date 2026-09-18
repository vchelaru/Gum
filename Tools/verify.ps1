<#
.SYNOPSIS
Focused pre-push check: runs only the test classes this branch added or changed (which also builds
the projects they depend on), builds any changed source project no changed test covers, and prints
per-step status, errors, test summaries, and warnings on lines this branch changed (only projects
that actually recompiled report warnings; an up-to-date project is skipped by MSBuild). CI runs the
full matrix; this is the fast loop, not a copy of CI.

.PARAMETER Base
Git ref to diff against. Default origin/main.

.EXAMPLE
pwsh Tools/verify.ps1
#>
[CmdletBinding()]
param(
    [string]$Base = 'origin/main'
)

$ErrorActionPreference = 'Continue'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

function Get-Key([string]$path) { ($path -replace '/', '\').ToLowerInvariant() }

# Changed line ranges per file (lower-cased, backslash-relative path -> list of [start, end]).
# A warning counts as "new" when it sits on a line this branch added or changed; a file with
# hundreds of pre-existing nullable warnings would otherwise drown the one you introduced.
$changedLines = @{}
$changedFiles = @(git diff --name-only $Base 2>$null | Where-Object { $_ })
foreach ($file in $changedFiles) {
    $ranges = @()
    # Group names avoid 'count': on the $Matches hashtable that resolves to Hashtable.Count.
    foreach ($hunk in @(git diff -U0 $Base -- $file 2>$null | Where-Object { $_ -match '^@@ .* \+(?<first>\d+)(,(?<len>\d+))? @@' })) {
        [void]($hunk -match '^@@ .* \+(?<first>\d+)(,(?<len>\d+))? @@')
        [int]$first = $Matches['first']
        [int]$len = if ($Matches.ContainsKey('len')) { $Matches['len'] } else { 1 }
        if ($len -gt 0) { $ranges += , @($first, ($first + $len - 1)) }
    }
    $changedLines[(Get-Key $file)] = $ranges
}
$untracked = @(git ls-files --others --exclude-standard | Where-Object { $_ })
foreach ($file in $untracked) {
    $changedLines[(Get-Key $file)] = @(, @(1, [int]::MaxValue))
}
$changedFiles += $untracked

function Find-Project([string]$file) {
    $dir = Split-Path -Parent (Join-Path $repo $file)
    while ($dir -and $dir.Length -ge $repo.Length) {
        $csproj = Get-ChildItem -Path $dir -Filter *.csproj -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($csproj) { return $csproj.FullName }
        $dir = Split-Path -Parent $dir
    }
    return $null
}

# Changed test classes grouped by test project (file name == class name by convention), and
# changed source projects.
$testFilters = @{}
$sourceProjects = [System.Collections.Generic.HashSet[string]]::new()
foreach ($file in ($changedFiles | Where-Object { $_ -like '*.cs' })) {
    $project = Find-Project $file
    if (-not $project) { continue }
    if ($project -match '\.Tests\.csproj$|GumToolUnitTests\.csproj$') {
        $className = [System.IO.Path]::GetFileNameWithoutExtension($file)
        if (-not $testFilters.ContainsKey($project)) { $testFilters[$project] = @() }
        $testFilters[$project] += "FullyQualifiedName~$className"
    }
    else {
        [void]$sourceProjects.Add($project)
    }
}

$failed = $false
$warningsOnChangedLines = [System.Collections.Generic.HashSet[string]]::new()

function Invoke-Step {
    param([string]$Name, [string[]]$Command)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $output = & dotnet @Command 2>&1 | ForEach-Object { "$_" }
    $exit = $LASTEXITCODE
    $sw.Stop()
    $status = if ($exit -eq 0) { 'ok' } else { 'FAILED' }
    Write-Host ("{0,-7} {1,6:N0}s  {2}" -f $status, $sw.Elapsed.TotalSeconds, $Name)

    $output | Where-Object { $_ -match ': error ' } | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
    for ($i = 0; $i -lt $output.Count; $i++) {
        $line = $output[$i]
        if ($line -match '^(Passed|Failed)!' -or $line -match '^\s+Failed ') { Write-Host "  $($line.Trim())" }
        if ($line -match '^\s+Error Message:') {
            # The assertion text follows on the next lines; show enough to know what failed.
            $output[($i + 1)..[Math]::Min($i + 3, $output.Count - 1)] | ForEach-Object { Write-Host "    $($_.Trim())" }
        }
    }

    foreach ($line in $output) {
        if ($line -notmatch '^\s*(?<file>.+?)\((?<line>\d+),\d+\): warning ') { continue }
        $file = Get-Key $Matches.file
        $lineNumber = [int]$Matches.line
        $relative = if ($file.StartsWith($repo.ToLowerInvariant() + '\')) { $file.Substring($repo.Length + 1) } else { $file }
        if (-not $changedLines.ContainsKey($relative)) { continue }
        foreach ($range in $changedLines[$relative]) {
            if ($lineNumber -ge $range[0] -and $lineNumber -le $range[1]) {
                [void]$warningsOnChangedLines.Add(($line.Trim() -replace '\s*\[.*\]$', ''))
                break
            }
        }
    }

    if ($exit -ne 0) { $script:failed = $true }
}

if ($testFilters.Count -eq 0 -and $sourceProjects.Count -eq 0) {
    Write-Host "No changed .cs files vs $Base"
}

foreach ($project in $testFilters.Keys | Sort-Object) {
    $relativeProject = $project.Substring($repo.Length + 1)
    $filter = ($testFilters[$project] | Sort-Object -Unique) -join '|'
    $command = @('test', $project, '-nologo', '-v:m', '--filter', $filter)
    if ($project -match 'GumToolUnitTests\.csproj$') {
        # The frozen WPF head's plugin post-builds use $(SolutionDir) in their copy steps, which is
        # undefined when building this csproj directly (see CLAUDE.md "Running focused WPF-head
        # unit tests").
        $command += "-p:SolutionDir=$repo\"
    }
    Invoke-Step -Name "test $relativeProject ($filter)" -Command $command
}

foreach ($project in $sourceProjects | Sort-Object) {
    $relativeProject = $project.Substring($repo.Length + 1)
    Invoke-Step -Name "build $relativeProject" -Command @('build', $project, '-nologo', '-v:q')
}

if ($warningsOnChangedLines.Count -gt 0) {
    Write-Host "`nWarnings on lines changed vs ${Base}:"
    $warningsOnChangedLines | Sort-Object | ForEach-Object { Write-Host "  $_" }
}
else {
    Write-Host "`nNo warnings on lines changed vs $Base"
}

if ($failed) { Write-Host "`nVERIFY FAILED"; exit 1 }
Write-Host "`nVERIFY OK"
