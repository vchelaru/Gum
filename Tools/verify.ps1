<#
.SYNOPSIS
Focused pre-push check: runs only the test classes this branch added or changed (which also builds
the projects they depend on), builds any changed source project no changed test covers (a .cs outside
every project directory, or a .projitems, builds the csprojs that link or import it), and prints
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

# A source file can reach a project without sitting under its directory: a csproj can link it with
# <Compile Include="..\X.cs" Link=...> (or a glob), and a .projitems includes it for every project
# that imports that .projitems (GumCoreShared.projitems -> the GumCore.* projects). Find-Project
# sees none of that, so a change to such a file used to build nothing. The index below is read from
# the csproj/projitems text on first use, so a run without such a file pays nothing for it.
$script:includeIndex = $null
function Get-RepoKey([string]$fullPath) { Get-Key ([System.IO.Path]::GetRelativePath($repo, $fullPath)) }

function Resolve-MsBuildPath([string]$item, [string]$dir) {
    $path = $item.Replace('$(MSBuildThisFileDirectory)', $dir + '/')
    if ($path -match '\$\(') { return $null }
    $path = $path -replace '\\', '/'
    if (-not [System.IO.Path]::IsPathRooted($path)) { $path = Join-Path $dir $path }
    return [System.IO.Path]::GetFullPath($path)
}

function Add-ToIndex([hashtable]$table, [string]$key, [string]$value) {
    if (-not $table.ContainsKey($key)) { $table[$key] = [System.Collections.Generic.List[string]]::new() }
    $table[$key].Add($value)
}

function Get-IncludeIndex {
    if ($script:includeIndex) { return $script:includeIndex }
    # Exact/Globs: included file (or glob) -> csproj/projitems that include it. Importers: projitems -> csproj.
    $index = @{ Exact = @{}; Globs = [System.Collections.Generic.List[object]]::new(); Importers = @{} }
    $files = @(git ls-files --cached --others --exclude-standard -- '*.csproj' '*.projitems' | Where-Object { $_ -and (Test-Path (Join-Path $repo $_)) })
    foreach ($relative in $files) {
        $full = Join-Path $repo $relative
        $dir = Split-Path -Parent $full
        $text = Get-Content -Raw $full
        foreach ($m in [regex]::Matches($text, '<Import\s+Project="(?<p>[^"]+\.projitems)"')) {
            $target = Resolve-MsBuildPath $m.Groups['p'].Value $dir
            if ($target) { Add-ToIndex $index.Importers (Get-RepoKey $target) $full }
        }
        foreach ($m in [regex]::Matches($text, '<Compile\s+Include="(?<p>[^"]+)"')) {
            $target = Resolve-MsBuildPath $m.Groups['p'].Value $dir
            if (-not $target) { continue }
            $key = Get-RepoKey $target
            if ($key -notmatch '[*?]') { Add-ToIndex $index.Exact $key $relative; continue }
            # Glob: '**\' spans directories, '*' and '?' stay within one.
            $pattern = [regex]::Escape($key).Replace('\*\*\\', '(?:.*\\)?').Replace('\*\*', '.*').Replace('\*', '[^\\]*').Replace('\?', '[^\\]')
            $index.Globs.Add(@{ Regex = [regex]::new("^$pattern$"); Owner = $relative })
        }
    }
    $script:includeIndex = $index
    return $index
}

# Importers of a .projitems, narrowed to one representative where the others only swap platform
# packages around the same source (and need the fna submodule or mobile workloads to build at all).
$representativeImporters = @{ 'gumcoreshared.projitems' = 'GumCore.DesktopGlNet6.csproj' }
function Get-Importers([string]$projitemsKey) {
    $all = @((Get-IncludeIndex).Importers[$projitemsKey])
    $leaf = Split-Path -Leaf $projitemsKey
    if ($representativeImporters.ContainsKey($leaf)) {
        $preferred = @($all | Where-Object { (Split-Path -Leaf $_) -eq $representativeImporters[$leaf] })
        if ($preferred.Count -gt 0) { return $preferred }
    }
    return $all
}

# Project (full path) -> why it compiles this file, for a file Find-Project could not place.
# Empty when nothing compiles the file.
function Find-IndirectProjects([string]$file) {
    $index = Get-IncludeIndex
    $found = [ordered]@{}
    if ($file -like '*.projitems') {
        foreach ($importer in Get-Importers (Get-Key $file)) { $found[$importer] = "imports $(Split-Path -Leaf $file)" }
        return $found
    }
    $key = Get-Key $file
    $owners = [System.Collections.Generic.List[string]]::new()
    if ($index.Exact.ContainsKey($key)) { $owners.AddRange($index.Exact[$key]) }
    foreach ($glob in $index.Globs) { if ($glob.Regex.IsMatch($key)) { $owners.Add($glob.Owner) } }
    foreach ($owner in $owners) {
        if ($owner -like '*.csproj') { $found[(Join-Path $repo $owner)] = 'Compile Include' }
        else {
            foreach ($importer in Get-Importers (Get-Key $owner)) { $found[$importer] = "imports $(Split-Path -Leaf $owner)" }
        }
    }
    return $found
}

# First ProjectReference/Import (followed through ProjectReferences) missing on this machine, such as
# the ../FlatRedBall sibling or the fna submodule, else $null. A project that cannot build here is
# reported as unverified instead of failing.
function Find-MissingDependency([string]$csproj, [System.Collections.Generic.HashSet[string]]$seen) {
    if (-not $seen.Add($csproj)) { return $null }
    $dir = Split-Path -Parent $csproj
    foreach ($m in [regex]::Matches((Get-Content -Raw $csproj), '<(?:ProjectReference\s+Include|Import\s+Project)="(?<p>[^"]+)"')) {
        $path = Resolve-MsBuildPath $m.Groups['p'].Value $dir
        if (-not $path) { continue }
        if (-not (Test-Path $path)) { return [System.IO.Path]::GetRelativePath($repo, $path) }
        $missing = if ($path -like '*.csproj') { Find-MissingDependency $path $seen }
        if ($missing) { return $missing }
    }
    return $null
}

# Changed test classes grouped by test project (file name == class name by convention), and
# changed source projects. A changed .cs with no project directory, and a changed .projitems, are
# resolved through the include index instead.
$testFilters = @{}
$sourceProjects = [System.Collections.Generic.HashSet[string]]::new()
$indirectProjects = [ordered]@{}
$unbuilt = [System.Collections.Generic.List[string]]::new()
foreach ($file in ($changedFiles | Where-Object { $_ -like '*.cs' -or $_ -like '*.projitems' })) {
    $project = if ($file -like '*.cs') { Find-Project $file }
    if (-not $project) {
        $via = Find-IndirectProjects $file
        if ($via.Count -eq 0) { $unbuilt.Add("$file (no project directory and no csproj or .projitems includes it)"); continue }
        foreach ($indirect in $via.Keys) {
            if (-not $indirectProjects.Contains($indirect)) { $indirectProjects[$indirect] = [System.Collections.Generic.List[string]]::new() }
            $indirectProjects[$indirect].Add("$file ($($via[$indirect]))")
        }
        continue
    }
    if ($project -match '\.Tests\.csproj$|GumToolUnitTests\.csproj$') {
        $className = [System.IO.Path]::GetFileNameWithoutExtension($file)
        if (-not $testFilters.ContainsKey($project)) { $testFilters[$project] = @() }
        $testFilters[$project] += "FullyQualifiedName~$className"
    }
    else {
        [void]$sourceProjects.Add($project)
    }
}

# SokolGum projects compile against a Sokol.NET clone that is not a submodule and that most checkouts
# lack (see Runtimes/SokolGum/README.md), so without it their build always fails. Skip them then.
$hasSokolCheckout = Test-Path (Join-Path $repo 'Sokol.NET/src/sokol')
function Test-NeedsSokolCheckout([string]$relativeProject) {
    ($relativeProject -replace '\\', '/') -match '^(Runtimes/Sokol|Runtimes/SokolGum|Tests/SokolGum\.Tests|Samples/SokolGum[^/]*)/'
}
function Skip-WithoutSokol([string]$name, [string]$relativeProject) {
    if ($hasSokolCheckout -or -not (Test-NeedsSokolCheckout $relativeProject)) { return $false }
    Write-Host ("skipped      $name`: needs the Sokol.NET clone (Runtimes/SokolGum/README.md), unverified")
    return $true
}

$failed = $false
$warningsOnChangedLines = [System.Collections.Generic.HashSet[string]]::new()

# Runs dotnet with a hard time limit. A hung test host (for example RaylibGum.Tests on macOS, whose
# host spins at 100% CPU) otherwise stalls the whole run silently; on timeout the process tree is
# killed and the step is reported as TIMED OUT, which is not the same as a failing test.
function Invoke-Dotnet {
    param([string[]]$Command, [int]$TimeoutSeconds)
    $psi = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
    foreach ($argument in $Command) { $psi.ArgumentList.Add($argument) }
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($psi)
    $standardOutput = $process.StandardOutput.ReadToEndAsync()
    $standardError = $process.StandardError.ReadToEndAsync()
    $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
    if ($timedOut) { $process.Kill($true); $process.WaitForExit() }
    $lines = (($standardOutput.Result + "`n" + $standardError.Result) -split "`r?`n") | ForEach-Object { "$_" }
    return @{ Output = $lines; Exit = $(if ($timedOut) { -1 } else { $process.ExitCode }); TimedOut = $timedOut }
}

function Invoke-Step {
    param([string]$Name, [string[]]$Command, [int]$TimeoutSeconds = 600)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $result = Invoke-Dotnet -Command $Command -TimeoutSeconds $TimeoutSeconds
    $output = $result.Output
    $exit = $result.Exit
    $sw.Stop()
    $status = if ($result.TimedOut) { 'TIMEDOUT' } elseif ($exit -eq 0) { 'ok' } else { 'FAILED' }
    Write-Host ("{0,-7} {1,6:N0}s  {2}" -f $status, $sw.Elapsed.TotalSeconds, $Name)

    if ($result.TimedOut) { Write-Host "  killed after ${TimeoutSeconds}s with no result: a hung test host or build, not a failing test. Check the process list for a spinning dotnet." }
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

if ($testFilters.Count -eq 0 -and $sourceProjects.Count -eq 0 -and $indirectProjects.Count -eq 0 -and $unbuilt.Count -eq 0) {
    Write-Host "No changed .cs or .projitems files vs $Base"
}

# Files with no project directory build whatever includes them. A project that cannot build on this
# machine (missing FlatRedBall sibling or fna submodule) is listed as unverified, not failed.
$unverified = [System.Collections.Generic.List[string]]::new()
foreach ($project in $indirectProjects.Keys) {
    $relativeProject = $project.Substring($repo.Length + 1)
    $reasons = ($indirectProjects[$project] | Select-Object -First 3) -join '; '
    if ($indirectProjects[$project].Count -gt 3) { $reasons += "; and $($indirectProjects[$project].Count - 3) more" }
    $missing = Find-MissingDependency $project ([System.Collections.Generic.HashSet[string]]::new())
    if ($missing) {
        Write-Host ("skipped      build $relativeProject`: needs $missing, which is missing here, unverified")
        $unverified.Add("$relativeProject (needs $missing) for $reasons")
    }
    elseif (-not $sourceProjects.Contains($project)) {
        Write-Host "indirect     $relativeProject <- $reasons"
        [void]$sourceProjects.Add($project)
    }
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
    if ($IsMacOS -and $project -match 'RaylibGum\.Tests\.csproj$') {
        # Its test host hangs at 100% CPU on macOS (the raylib window cannot be created there), so
        # these tests only run in CI on Windows.
        Write-Host ("skipped      test $relativeProject ($filter): hangs on macOS, CI only")
        continue
    }
    if (Skip-WithoutSokol "test $relativeProject ($filter)" $relativeProject) { continue }
    Invoke-Step -Name "test $relativeProject ($filter)" -Command $command -TimeoutSeconds 300
}

foreach ($project in $sourceProjects | Sort-Object) {
    $relativeProject = $project.Substring($repo.Length + 1)
    if (Skip-WithoutSokol "build $relativeProject" $relativeProject) { continue }
    # Same $(SolutionDir) post-build copy as above: the frozen WPF head's plugin projects need it
    # whenever they are built by csproj, not only through the test project.
    Invoke-Step -Name "build $relativeProject" -Command @('build', $project, '-nologo', '-v:q', "-p:SolutionDir=$repo\")
}

foreach ($entry in $unbuilt) { Write-Host "NOT BUILT    $entry" }

if ($warningsOnChangedLines.Count -gt 0) {
    Write-Host "`nWarnings on lines changed vs ${Base}:"
    $warningsOnChangedLines | Sort-Object | ForEach-Object { Write-Host "  $_" }
}
else {
    Write-Host "`nNo warnings on lines changed vs $Base"
}

if ($failed) { Write-Host "`nVERIFY FAILED"; exit 1 }
$notCovered = $unbuilt.Count + $unverified.Count
if ($notCovered -gt 0) { Write-Host "`nVERIFY OK, but $notCovered changed file(s)/project(s) were not built (see NOT BUILT and skipped above)"; exit 0 }
Write-Host "`nVERIFY OK"
