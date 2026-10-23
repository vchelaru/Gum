<#
.SYNOPSIS
Focused pre-push check: runs only the test classes this branch added or changed (which also builds
the projects they depend on), builds any changed source project no changed test covers (a changed .cs
or .projitems also builds every csproj that links or imports it, directly or through a .props), and prints
per-step status, errors, test summaries, and warnings on lines this branch changed (only projects
that actually recompiled report warnings; an up-to-date project is skipped by MSBuild). CI runs the
full matrix; this is the fast loop, not a copy of CI.

.PARAMETER Base
Git ref to diff against. Default origin/main.

.PARAMETER Files
Treat these repo-relative files as the changed set (every line changed) instead of diffing against Base.

.PARAMETER ListOnly
Print the test and build steps the changed files resolve to, then exit without running them.

.EXAMPLE
pwsh Tools/verify.ps1
#>
[CmdletBinding()]
param(
    [string]$Base = 'origin/main',
    [string[]]$Files,
    [switch]$ListOnly
)

$ErrorActionPreference = 'Continue'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

function Get-Key([string]$path) { ($path -replace '/', '\').ToLowerInvariant() }

# Changed line ranges per file (lower-cased, backslash-relative path -> list of [start, end]).
# A warning counts as "new" when it sits on a line this branch added or changed; a file with
# hundreds of pre-existing nullable warnings would otherwise drown the one you introduced.
# The diff starts where this branch left Base, so commits merged into Base since then are not
# treated as this branch's changes.
$changedLines = @{}
$diffBase = git merge-base HEAD $Base 2>$null
if (-not $diffBase) { $diffBase = $Base }
$changedFiles = if ($Files) { @($Files | ForEach-Object { $_ -replace '\\', '/' }) } else { @(git diff --name-only $diffBase 2>$null | Where-Object { $_ }) }
foreach ($file in $changedFiles) {
    if ($Files) { $changedLines[(Get-Key $file)] = @(, @(1, [int]::MaxValue)); continue }
    $ranges = @()
    # Group names avoid 'count': on the $Matches hashtable that resolves to Hashtable.Count.
    foreach ($hunk in @(git diff -U0 $diffBase -- $file 2>$null | Where-Object { $_ -match '^@@ .* \+(?<first>\d+)(,(?<len>\d+))? @@' })) {
        [void]($hunk -match '^@@ .* \+(?<first>\d+)(,(?<len>\d+))? @@')
        [int]$first = $Matches['first']
        [int]$len = if ($Matches.ContainsKey('len')) { $Matches['len'] } else { 1 }
        if ($len -gt 0) { $ranges += , @($first, ($first + $len - 1)) }
    }
    $changedLines[(Get-Key $file)] = $ranges
}
$untracked = if ($Files) { @() } else { @(git ls-files --others --exclude-standard | Where-Object { $_ }) }
foreach ($file in $untracked) {
    $changedLines[(Get-Key $file)] = @(, @(1, [int]::MaxValue))
}
$changedFiles += $untracked

# Every csproj in the nearest folder above the file: a folder can hold several (Runtimes/GumShapes has
# a MonoGame and a KNI project), and each compiles the file through its default glob.
function Find-Projects([string]$file) {
    $dir = Split-Path -Parent (Join-Path $repo $file)
    while ($dir -and $dir.Length -ge $repo.Length) {
        $csprojs = @(Get-ChildItem -Path $dir -Filter *.csproj -File -ErrorAction SilentlyContinue | ForEach-Object FullName)
        if ($csprojs.Count -gt 0) { return $csprojs }
        $dir = Split-Path -Parent $dir
    }
    return @()
}

# A source file can reach a project without sitting under its directory: a csproj can link it with
# <Compile Include="..\X.cs" Link=...> (or a glob), and a .projitems includes it for every project
# that imports that .projitems (GumCoreShared.projitems -> the GumCore.* projects), and so does a
# .props/.targets the csproj imports, directly, through another .props, or as the nearest
# Directory.Build.props/.targets above it (Runtimes/GumShapes/Directory.Build.props links SkiaGum
# files into the GumShapes projects). Find-Project sees none of that. The index below is read from
# the csproj/projitems/props/targets text on first use.
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

# Adds one <Compile Include> to the index. $owner is the including csproj/projitems relative to the
# repo, followed by "|<props file name>" when the link comes from a .props/.targets it imports.
function Add-CompileInclude($index, [string]$target, [string]$owner) {
    $key = Get-RepoKey $target
    if ($key -notmatch '[*?]') { Add-ToIndex $index.Exact $key $owner; return }
    $index.Globs.Add(@{ Regex = (ConvertTo-KeyRegex $key); Owner = $owner })
}

# Glob: '**\' spans directories, '*' and '?' stay within one.
function ConvertTo-KeyRegex([string]$key) {
    $pattern = [regex]::Escape($key).Replace('\*\*\\', '(?:.*\\)?').Replace('\*\*', '.*').Replace('\*', '[^\\]*').Replace('\?', '[^\\]')
    return [regex]::new("^$pattern$")
}

# Records a <Compile Remove> (exact path or glob) for a csproj; Test-Removed checks a file against them.
function Add-CompileRemove($index, [string]$target, [string]$csproj) {
    $csprojKey = Get-Key $csproj
    if (-not $index.Removed.ContainsKey($csprojKey)) { $index.Removed[$csprojKey] = [System.Collections.Generic.List[regex]]::new() }
    $index.Removed[$csprojKey].Add((ConvertTo-KeyRegex (Get-RepoKey $target)))
}

function Test-Removed([string]$csproj, [string]$fileKey) {
    $removes = (Get-IncludeIndex).Removed[(Get-Key $csproj)]
    if (-not $removes) { return $false }
    return [bool]($removes | Where-Object { $_.IsMatch($fileKey) } | Select-Object -First 1)
}

function Get-IncludeIndex {
    if ($script:includeIndex) { return $script:includeIndex }
    # Exact/Globs: included file (or glob) -> csproj/projitems that include it. Importers: projitems -> csproj.
    # Removed: csproj -> <Compile Remove> patterns from it and the .props/.targets it sees.
    $index = @{ Exact = @{}; Globs = [System.Collections.Generic.List[object]]::new(); Importers = @{}; Removed = @{} }
    $files = @(git ls-files --cached --others --exclude-standard -- '*.csproj' '*.projitems' '*.props' '*.targets' | Where-Object { $_ -and (Test-Path (Join-Path $repo $_)) })
    $text = @{}
    foreach ($relative in $files) { $text[(Join-Path $repo $relative)] = Get-Content -Raw (Join-Path $repo $relative) }
    $propsImport = '<Import\s+Project="(?<p>[^"]+\.(?:props|targets))"'
    foreach ($relative in $files | Where-Object { $_ -like '*.csproj' -or $_ -like '*.projitems' }) {
        $full = Join-Path $repo $relative
        $dir = Split-Path -Parent $full
        foreach ($m in [regex]::Matches($text[$full], '<Import\s+Project="(?<p>[^"]+\.projitems)"')) {
            $target = Resolve-MsBuildPath $m.Groups['p'].Value $dir
            if ($target) { Add-ToIndex $index.Importers (Get-RepoKey $target) $full }
        }
        foreach ($m in [regex]::Matches($text[$full], '<Compile\s+Include="(?<p>[^"]+)"')) {
            $target = Resolve-MsBuildPath $m.Groups['p'].Value $dir
            if ($target) { Add-CompileInclude $index $target $relative }
        }
        if ($relative -notlike '*.csproj') { continue }
        foreach ($m in [regex]::Matches($text[$full], '<Compile\s+Remove="(?<p>[^"]+)"')) {
            $target = Resolve-MsBuildPath $m.Groups['p'].Value $dir
            if ($target) { Add-CompileRemove $index $target $full }
        }

        # The .props/.targets this csproj sees: the nearest Directory.Build.props/.targets above it,
        # its own imports, and whatever those import in turn.
        $pending = [System.Collections.Generic.Queue[string]]::new()
        foreach ($name in 'Directory.Build.props', 'Directory.Build.targets') {
            $up = $dir
            while ($up -and $up.Length -ge $repo.Length) {
                $candidate = Join-Path $up $name
                if ($text.ContainsKey($candidate)) { $pending.Enqueue($candidate); break }
                $up = Split-Path -Parent $up
            }
        }
        foreach ($m in [regex]::Matches($text[$full], $propsImport)) {
            $target = Resolve-MsBuildPath $m.Groups['p'].Value $dir
            if ($target) { $pending.Enqueue($target) }
        }
        $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        while ($pending.Count -gt 0) {
            $props = $pending.Dequeue()
            if (-not $text.ContainsKey($props) -or -not $seen.Add($props)) { continue }
            $propsDir = Split-Path -Parent $props
            foreach ($m in [regex]::Matches($text[$props], $propsImport)) {
                $target = Resolve-MsBuildPath $m.Groups['p'].Value $propsDir
                if ($target) { $pending.Enqueue($target) }
            }
            # An item path in an imported file is relative to the project, not to the imported file;
            # only $(MSBuildThisFileDirectory) points at the imported file's folder.
            foreach ($m in [regex]::Matches($text[$props], '<Compile\s+(?<kind>Include|Remove)="(?<p>[^"]+)"')) {
                $item = $m.Groups['p'].Value.Replace('$(MSBuildThisFileDirectory)', $propsDir + '/')
                $target = Resolve-MsBuildPath $item $dir
                if (-not $target) { continue }
                if ($m.Groups['kind'].Value -eq 'Remove') { Add-CompileRemove $index $target $full }
                else { Add-CompileInclude $index $target "$relative|$(Split-Path -Leaf $props)" }
            }
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

# Project (full path) -> why it compiles this file through a link, minus projects that
# <Compile Remove> it. Empty when no link reaches the file.
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
        $project, $props = $owner -split '\|', 2
        if ($props) { $found[(Join-Path $repo $project)] = "Compile Include in $props" }
        elseif ($owner -like '*.csproj') { $found[(Join-Path $repo $owner)] = 'Compile Include' }
        else {
            foreach ($importer in Get-Importers (Get-Key $owner)) { $found[$importer] = "imports $(Split-Path -Leaf $owner)" }
        }
    }
    foreach ($project in @($found.Keys)) { if (Test-Removed $project $key) { $found.Remove($project) } }
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

. (Join-Path $PSScriptRoot 'TestProjects.ps1')

# Changed test classes grouped by test project (file name == class name by convention), and
# changed source projects. Every changed .cs and .projitems is also resolved through the include
# index, so a file linked into other projects builds those too.
$testFilters = @{}
$sourceProjects = [System.Collections.Generic.HashSet[string]]::new()
$indirectProjects = [ordered]@{}
$unbuilt = [System.Collections.Generic.List[string]]::new()
foreach ($file in ($changedFiles | Where-Object { $_ -like '*.cs' -or $_ -like '*.projitems' })) {
    $projects = if ($file -like '*.cs') { @(Find-Projects $file | Where-Object { -not (Test-Removed $_ (Get-Key $file)) }) } else { @() }
    $via = Find-IndirectProjects $file
    if ($projects.Count -eq 0 -and $via.Count -eq 0) { $unbuilt.Add("$file (no project directory and no csproj, .projitems or .props includes it)"); continue }
    foreach ($indirect in $via.Keys | Where-Object { $projects -notcontains $_ }) {
        if (-not $indirectProjects.Contains($indirect)) { $indirectProjects[$indirect] = [System.Collections.Generic.List[string]]::new() }
        $indirectProjects[$indirect].Add("$file ($($via[$indirect]))")
    }
    foreach ($project in $projects) {
        if (Test-IsTestProject $project) {
            $className = [System.IO.Path]::GetFileNameWithoutExtension($file)
            if (-not $testFilters.ContainsKey($project)) { $testFilters[$project] = @() }
            $testFilters[$project] += "FullyQualifiedName~$className"
        }
        else {
            [void]$sourceProjects.Add($project)
        }
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

. (Join-Path $PSScriptRoot 'Invoke-Dotnet.ps1')

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
foreach ($project in @($sourceProjects)) {
    # A folder's second csproj can be a variant that needs a missing dependency (SkiaInGum.FNA needs fna).
    $missing = Find-MissingDependency $project ([System.Collections.Generic.HashSet[string]]::new())
    if (-not $missing) { continue }
    $relativeProject = $project.Substring($repo.Length + 1)
    Write-Host ("skipped      build $relativeProject`: needs $missing, which is missing here, unverified")
    $unverified.Add("$relativeProject (needs $missing)")
    [void]$sourceProjects.Remove($project)
}
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

# The Avalonia head builds through Gum.slnx: built by its csproj alone, its plugins stay missing from
# later Gum.slnx builds until a --no-incremental rebuild (CLAUDE.md "Building and Testing").
$headProject = Join-Path $repo 'Tool/Gum.Avalonia/Gum.Avalonia.csproj'
function Get-BuildTarget([string]$project) {
    if ((Get-Key $project) -eq (Get-Key $headProject)) { return Join-Path $repo 'Gum.slnx' }
    return $project
}
$buildTargets = @($sourceProjects | ForEach-Object { Get-BuildTarget $_ } | Sort-Object -Unique)

if ($ListOnly) {
    foreach ($project in $testFilters.Keys | Sort-Object) { Write-Host "test         $($project.Substring($repo.Length + 1))" }
    foreach ($project in $buildTargets) { Write-Host "build        $($project.Substring($repo.Length + 1))" }
    foreach ($entry in $unbuilt) { Write-Host "NOT BUILT    $entry" }
    exit 0
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

foreach ($project in $buildTargets) {
    $relativeProject = $project.Substring($repo.Length + 1)
    if (Skip-WithoutSokol "build $relativeProject" $relativeProject) { continue }
    # Same $(SolutionDir) post-build copy as above: the frozen WPF head's plugin projects need it
    # whenever they are built by csproj, not only through the test project.
    $command = @('build', $project, '-nologo', '-v:q', "-p:SolutionDir=$repo\")
    # Only the .gumj Preview window needs the GumPreview variants a Debug Gum.slnx build publishes.
    if ($project -like '*.slnx') { $command += '-p:SkipPreviewVariantPublish=true' }
    Invoke-Step -Name "build $relativeProject" -Command $command
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
