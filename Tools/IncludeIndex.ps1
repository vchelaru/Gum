# Shared by fast-test.ps1 and verify.ps1: which csprojs compile a source file without it sitting under
# their directory (Compile Include links, .projitems, imported .props). Needs $repo (the repo root) set by the caller.

function Get-Key([string]$path) { ($path -replace '/', '\').ToLowerInvariant() }

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
