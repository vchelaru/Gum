<#
.SYNOPSIS
Runs real Gum projects through gumcli's headless load, check, save, codegen and font paths and
prints a table of project, step and result.

.DESCRIPTION
Every project is copied to a work folder first; the originals are never touched. Steps, in order:

  check       gumcli check. Fails when the project has errors.
  refs        gumcli check-references. Fails on unpropagated variable references.
  resave      gumcli resave --raw (serializers only), then a comparison of every file in the
              copy. A file that lost or changed a line fails. A file that only differs in
              formatting, line order, or newly written default-valued fields (it was saved by an
              older Gum) is reported as a note.
  toolsave    gumcli resave, which loads the way the tool does, including the load-time back-fill
              of standard-element defaults. Files the first save rewrites are reported as a note,
              since the back-fill is by design. A second save that rewrites anything again fails:
              the save is not stable.
  codegen     gumcli codegen. Skipped when the project has no code settings and auto-detection
              finds no .csproj. Fails when a written file has a backslash in its name (a Windows
              path separator used off Windows).
  fonts       Deletes the project's cached .fnt files, then gumcli fonts (KernSmith off Windows).
              A font substituted because it is not installed is reported as a note.

After the steps, every file left in the copy goes into manifest.txt (path and hash, line endings
ignored; generated fonts by name only). Tools/project-sweep-compare.ps1 compares the manifests
from runs on different OSes.

What gets copied: the project's folder, or, when ProjectCodeSettings.codsj points CodeProjectRoot
outside it, the folder that holds both, so generated code lands inside the copy. bin, obj and .git
folders are skipped.

Runs on Windows, macOS and Linux (PowerShell 7).

.PARAMETER Project
Explicit .gumx/.gumj files. When omitted, projects are discovered under -SearchRoot.

.PARAMETER SearchRoot
Folders to search for projects. In a git checkout only tracked projects are used, so build output
is ignored; otherwise a recursive search that skips bin and obj. Default: this repo.

.PARAMETER WorkRoot
Where the copies, logs and reports go. It is emptied first. Default: <temp>/gum-project-sweep.

.PARAMETER Gumcli
Path to a built gumcli.dll. Default: builds Tools/Gum.Cli in Release and uses that.

.PARAMETER Steps
Subset of steps to run. Default: all.

.PARAMETER KnownFailures
JSON file mapping a project label (as printed in the report) to the steps expected to fail and
why: deliberate test fixtures and failures already filed as issues. A listed failure is reported
as "known" and does not fail the run; a listed step that passes is reported as a note so the
entry gets removed. Default: Tools/project-sweep-known.json.

.EXAMPLE
pwsh Tools/project-sweep.ps1

.EXAMPLE
pwsh -Command "./Tools/project-sweep.ps1 -SearchRoot ., ../FlatRedBall -WorkRoot /tmp/sweep"

Array parameters need -Command (or a call from a pwsh session); pwsh -File passes them as one string.

.NOTES
Exit code 0 when no step failed (pass, note, known and skip are all fine), 1 otherwise. Writes report.md
and results.json to -WorkRoot, plus one log per project and step under <WorkRoot>/<nn>/logs.
#>
[CmdletBinding()]
param(
    [string[]]$Project,
    [string[]]$SearchRoot,
    [string]$WorkRoot = (Join-Path ([System.IO.Path]::GetTempPath()) 'gum-project-sweep'),
    [string]$Gumcli,
    [ValidateSet('check', 'refs', 'resave', 'toolsave', 'codegen', 'fonts')]
    [string[]]$Steps = @('check', 'refs', 'resave', 'toolsave', 'codegen', 'fonts'),
    [string]$KnownFailures = (Join-Path $PSScriptRoot 'project-sweep-known.json')
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $SearchRoot) { $SearchRoot = @($repo) }

$excludedFolders = @('bin', 'obj', '.git', '.vs', 'node_modules')

function Test-ExcludedPath([string]$relativePath) {
    foreach ($segment in ($relativePath -split '[\\/]')) {
        if ($excludedFolders -contains $segment) { return $true }
    }
    return $false
}

function Find-Projects([string]$root) {
    $root = (Resolve-Path $root).Path
    $tracked = $null
    if (Test-Path (Join-Path $root '.git')) {
        $tracked = @(git -C $root ls-files -- '*.gumx' '*.gumj' 2>$null)
        if ($LASTEXITCODE -ne 0) { $tracked = $null }
    }

    if ($null -ne $tracked) {
        return $tracked | Where-Object { $_ -and -not (Test-ExcludedPath $_) } |
            ForEach-Object { [System.IO.Path]::GetFullPath((Join-Path $root $_)) }
    }

    return Get-ChildItem -Path $root -Recurse -File -Include '*.gumx', '*.gumj' |
        Where-Object { -not (Test-ExcludedPath ([System.IO.Path]::GetRelativePath($root, $_.FullName))) } |
        ForEach-Object { $_.FullName }
}

# The folder to copy: the project's own folder, widened to include CodeProjectRoot when the code
# settings point outside it (e.g. "../../" to reach the .csproj folder).
function Get-CopyRoot([string]$projectFile) {
    $projectDir = Split-Path -Parent $projectFile
    $settingsFile = Join-Path $projectDir 'ProjectCodeSettings.codsj'
    if (-not (Test-Path $settingsFile)) { return $projectDir }
    try {
        $codeRoot = (Get-Content -Raw $settingsFile | ConvertFrom-Json).CodeProjectRoot
    } catch { return $projectDir }
    if ([string]::IsNullOrWhiteSpace($codeRoot) -or [System.IO.Path]::IsPathRooted($codeRoot)) { return $projectDir }

    $separator = [System.IO.Path]::DirectorySeparatorChar
    $codeDir = [System.IO.Path]::GetFullPath((Join-Path $projectDir ($codeRoot -replace '\\', '/'))).TrimEnd('\', '/')
    $common = $projectDir.TrimEnd('\', '/')
    while ($common -and -not ($codeDir + $separator).StartsWith($common + $separator)) {
        $common = Split-Path -Parent $common
    }
    if (-not $common) { return $projectDir }
    return $common
}

function Copy-Tree([string]$source, [string]$destination) {
    [System.IO.Directory]::CreateDirectory($destination) | Out-Null
    foreach ($directory in [System.IO.Directory]::GetDirectories($source)) {
        $name = [System.IO.Path]::GetFileName($directory)
        if ($excludedFolders -contains $name) { continue }
        Copy-Tree $directory ([System.IO.Path]::Combine($destination, $name))
    }
    foreach ($file in [System.IO.Directory]::GetFiles($source)) {
        [System.IO.File]::Copy($file, [System.IO.Path]::Combine($destination, [System.IO.Path]::GetFileName($file)))
    }
}

function Get-FileHashes([string]$root) {
    $hashes = @{}
    foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File -Force) {
        $relative = [System.IO.Path]::GetRelativePath($root, $file.FullName) -replace '\\', '/'
        $hashes[$relative] = [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes($file.FullName)))
    }
    return $hashes
}

function Get-ChangedFiles($before, $after) {
    return @($after.Keys | Where-Object { $before[$_] -ne $after[$_] } | Sort-Object)
}

function Format-FileList([string[]]$files) {
    $shown = ($files | Select-Object -First 3) -join ', '
    $more = if ($files.Count -gt 3) { " (+$($files.Count - 3) more)" } else { '' }
    return "$($files.Count) file(s): $shown$more"
}

# A saved file as a flat list of "path = value" facts, so two files that hold the same data compare
# equal regardless of formatting: XML attributes and child elements are the same fact (the compact
# and verbose formats), and comments, xmlns and xsi:type are ignored. JSON is flattened the same way.
# The XML walk and the multiset comparison are C#: as PowerShell functions they took minutes over
# the corpus, most of the sweep's run time.
Add-Type -TypeDefinition @'
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

public static class ProjectSweepFacts
{
    public static void AddXmlFacts(XmlElement element, string path, List<string> facts)
    {
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (attribute.Name.StartsWith("xmlns") || attribute.Prefix == "xsi") continue;
            facts.Add(path + "/" + attribute.LocalName + " = " + attribute.Value);
        }
        var children = new List<XmlElement>();
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlElement childElement) children.Add(childElement);
        }
        if (children.Count == 0)
        {
            if (element.Attributes.Count == 0 || !string.IsNullOrEmpty(element.InnerText)) facts.Add(path + " = " + element.InnerText);
            return;
        }
        foreach (var child in children) AddXmlFacts(child, path + "/" + child.LocalName, facts);
    }

    public static string HashWithoutCarriageReturns(string file)
    {
        var bytes = System.IO.File.ReadAllBytes(file);
        var kept = System.Array.FindAll(bytes, b => b != 13);
        return System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(kept));
    }

    // Facts in the original missing from the saved list, counted as a multiset.
    public static List<string> Removed(List<string> original, List<string> saved, string ignore)
    {
        var remaining = new Dictionary<string, int>();
        foreach (var fact in saved) remaining[fact] = remaining.TryGetValue(fact, out var n) ? n + 1 : 1;
        // Case-insensitive, as PowerShell's -match was.
        var defaults = new Regex(" = (false|0|null|)$", RegexOptions.IgnoreCase);
        var ignoreRegex = string.IsNullOrEmpty(ignore) ? null : new Regex(ignore, RegexOptions.IgnoreCase);
        var removed = new List<string>();
        foreach (var fact in original)
        {
            if (remaining.TryGetValue(fact, out var count) && count > 0) { remaining[fact] = count - 1; continue; }
            if (defaults.IsMatch(fact)) continue;
            if (ignoreRegex != null && ignoreRegex.IsMatch(fact)) continue;
            removed.Add(fact);
        }
        return removed;
    }
}
'@

function Add-JsonFacts($node, [string]$path, [System.Collections.Generic.List[string]]$facts) {
    if ($node -is [System.Collections.IDictionary]) {
        foreach ($key in $node.Keys) { Add-JsonFacts $node[$key] "$path/$key" $facts }
    } elseif ($node -is [System.Collections.IList]) {
        if ($node.Count -eq 0) { $facts.Add("$path = ") }
        foreach ($item in $node) { Add-JsonFacts $item $path $facts }
    } elseif ($null -eq $node) {
        $facts.Add("$path = null")
    } elseif ($node -is [bool]) {
        $facts.Add("$path = $($node.ToString().ToLowerInvariant())")
    } else {
        $facts.Add("$path = $([System.Convert]::ToString($node, [System.Globalization.CultureInfo]::InvariantCulture))")
    }
}

function Get-Facts([string]$file) {
    $facts = [System.Collections.Generic.List[string]]::new()
    $text = [System.IO.File]::ReadAllText($file)
    if ($text.TrimStart([char]0xFEFF).TrimStart().StartsWith('<')) {
        $document = [System.Xml.XmlDocument]::new()
        $document.LoadXml($text.TrimStart([char]0xFEFF))
        [ProjectSweepFacts]::AddXmlFacts($document.DocumentElement, $document.DocumentElement.LocalName, $facts)
    } else {
        Add-JsonFacts ($text | ConvertFrom-Json -AsHashtable) '' $facts
    }
    # The comma keeps PowerShell from unrolling the list into an array.
    return , $facts
}

# Facts (as a multiset) in the original that are gone from the saved copy, ignoring default values
# (false, 0, null, empty) that a newer serializer omits. Reordering and additions are not removals.
function Get-RemovedFacts([string]$original, [string]$saved, [string]$ignore) {
    if (-not (Test-Path -LiteralPath $original)) { return @() }
    try {
        return @([ProjectSweepFacts]::Removed((Get-Facts $original), (Get-Facts $saved), $ignore))
    } catch {
        return @("(could not compare: $($_.Exception.Message))")
    }
}

function Get-ErrorLines($output, [int]$count = 3) {
    return (@($output | Where-Object { $_ -match '(?i)error' }) | Select-Object -First $count) -join '; '
}

function Invoke-Gumcli([string]$logFile, [string[]]$arguments) {
    $output = & dotnet $Gumcli @arguments 2>&1 | ForEach-Object { "$_" }
    $code = $LASTEXITCODE
    Set-Content -LiteralPath $logFile -Value (@("> gumcli $($arguments -join ' ')", "exit $code") + $output)
    return [pscustomobject]@{ ExitCode = $code; Output = $output }
}

# --- gumcli -----------------------------------------------------------------------------------
if (-not $Gumcli) {
    Write-Host 'Building gumcli (Release)...'
    dotnet build (Join-Path $repo 'Tools/Gum.Cli/Gum.Cli.csproj') -c Release -v q -nologo -clp:ErrorsOnly | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'gumcli build failed.' }
    $Gumcli = Get-ChildItem -Path (Join-Path $repo 'Tools/Gum.Cli/bin/Release') -Recurse -Filter 'gumcli.dll' |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $Gumcli -or -not (Test-Path $Gumcli)) { throw "gumcli not found: $Gumcli" }

# --- projects ---------------------------------------------------------------------------------
if ($Project) {
    $projects = @($Project | ForEach-Object { (Resolve-Path $_).Path })
} else {
    $projects = @($SearchRoot | ForEach-Object { Find-Projects $_ } | Sort-Object -Unique)
}
if ($projects.Count -eq 0) { throw 'No projects found.' }

# Labels are relative to the search root that holds the project, prefixed with that root's folder
# name when it is not this repo.
$roots = @($repo) + @($SearchRoot | ForEach-Object { (Resolve-Path $_).Path.TrimEnd('\', '/') })
function Get-Label([string]$file) {
    foreach ($root in $roots) {
        $relative = [System.IO.Path]::GetRelativePath($root, $file)
        if (-not $relative.StartsWith('..') -and -not [System.IO.Path]::IsPathRooted($relative)) {
            $prefix = if ($root -eq $repo) { '' } else { (Split-Path -Leaf $root) + ':' }
            return $prefix + ($relative -replace '\\', '/')
        }
    }
    return $file
}

$known = @{}
if ($KnownFailures -and (Test-Path $KnownFailures)) {
    $known = Get-Content -Raw $KnownFailures | ConvertFrom-Json -AsHashtable
}

if (Test-Path $WorkRoot) { Remove-Item -Recurse -Force $WorkRoot }
New-Item -ItemType Directory -Force -Path $WorkRoot | Out-Null
$WorkRoot = (Resolve-Path $WorkRoot).Path

$results = [System.Collections.Generic.List[object]]::new()
$manifest = [System.Collections.Generic.List[string]]::new()
$index = 0
foreach ($projectFile in $projects) {
    $index++
    $copyRoot = Get-CopyRoot $projectFile
    $label = Get-Label $projectFile
    # Short folder names keep deep projects under Windows' 260-character path limit.
    $slot = Join-Path $WorkRoot ('{0:D2}' -f $index)
    $copyDir = Join-Path $slot 'p'
    $pristineDir = Join-Path $slot 'orig'
    $logDir = Join-Path $slot 'logs'
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    Copy-Tree $copyRoot $copyDir
    Copy-Tree $copyRoot $pristineDir
    Set-Content -LiteralPath (Join-Path $slot 'project.txt') -Value $label
    $copiedProject = Join-Path $copyDir ([System.IO.Path]::GetRelativePath($copyRoot, $projectFile))
    Write-Host ("[{0}/{1}] {2}" -f $index, $projects.Count, $label)

    foreach ($step in $Steps) {
        $log = Join-Path $logDir "$step.log"
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $result = 'pass'
        $detail = ''
        switch ($step) {
            'check' {
                $run = Invoke-Gumcli $log @('check', $copiedProject)
                if ($run.ExitCode -ne 0) { $result = 'FAIL'; $detail = Get-ErrorLines $run.Output }
            }
            'refs' {
                $run = Invoke-Gumcli $log @('check-references', $copiedProject)
                if ($run.ExitCode -ne 0) {
                    $result = 'FAIL'
                    $detail = (@($run.Output | Where-Object { $_ -and $_ -notmatch '^gumcli v' }) | Select-Object -First 2) -join '; '
                }
            }
            'resave' {
                $before = Get-FileHashes $copyDir
                $run = Invoke-Gumcli $log @('resave', '--raw', $copiedProject)
                $changed = Get-ChangedFiles $before (Get-FileHashes $copyDir)
                $lossy = @()
                Add-Content -LiteralPath $log -Value (@('', 'changed files:') + $changed)
                foreach ($file in $changed) {
                    $removed = @(Get-RemovedFacts (Join-Path $pristineDir $file) (Join-Path $copyDir $file))
                    if ($removed.Count -gt 0) {
                        $lossy += $file
                        Add-Content -LiteralPath $log -Value (@('', "values removed or changed in ${file}:") + ($removed | Select-Object -First 20))
                    }
                }
                if ($run.ExitCode -ne 0) {
                    $result = 'FAIL'; $detail = "exit $($run.ExitCode): " + (Get-ErrorLines $run.Output 1)
                } elseif ($lossy.Count -gt 0) {
                    $result = 'FAIL'; $detail = 'removed or changed content in ' + (Format-FileList $lossy)
                } elseif ($changed.Count -gt 0) {
                    $result = 'note'; $detail = 'format or added defaults only in ' + (Format-FileList $changed)
                }
            }
            'toolsave' {
                $before = Get-FileHashes $copyDir
                $first = Invoke-Gumcli $log @('resave', $copiedProject)
                $afterFirst = Get-FileHashes $copyDir
                $rewritten = Get-ChangedFiles $before $afterFirst
                $second = Invoke-Gumcli (Join-Path $logDir 'toolsave-2.log') @('resave', $copiedProject)
                $unstable = Get-ChangedFiles $afterFirst (Get-FileHashes $copyDir)
                Add-Content -LiteralPath $log -Value (@('', 'rewritten by the first save:') + $rewritten + @('', 'rewritten again by the second save:') + $unstable)
                $lossy = @()
                foreach ($file in $rewritten) {
                    # The back-fill refreshes a standard variable's Category (its property-grid group)
                    # from the current defaults, so a recategorized variable is expected, not lost data.
                    $removed = @(Get-RemovedFacts (Join-Path $pristineDir $file) (Join-Path $copyDir $file) '^StandardElementSave/State/Variable/Category = ')
                    if ($removed.Count -gt 0) {
                        $lossy += $file
                        Add-Content -LiteralPath $log -Value (@('', "values removed or changed in ${file} (against the original):") + ($removed | Select-Object -First 20))
                    }
                }
                if ($first.ExitCode -ne 0 -or $second.ExitCode -ne 0) {
                    $result = 'FAIL'; $detail = "exit $($first.ExitCode)/$($second.ExitCode)"
                } elseif ($lossy.Count -gt 0) {
                    $result = 'FAIL'; $detail = 'removed or changed content in ' + (Format-FileList $lossy)
                } elseif ($unstable.Count -gt 0) {
                    $result = 'FAIL'; $detail = 'second save rewrote ' + (Format-FileList $unstable)
                } elseif ($rewritten.Count -gt 0) {
                    $result = 'note'; $detail = 'back-fill rewrote ' + (Format-FileList $rewritten)
                }
            }
            'codegen' {
                $before = Get-FileHashes $copyDir
                $run = Invoke-Gumcli $log @('codegen', $copiedProject)
                # Off Windows a path joined with "\" is not a folder: it becomes one file whose name
                # holds the backslashes, next to where the real file should have gone.
                $misnamed = @(Get-ChildItem -LiteralPath $copyDir -Recurse -File -Force |
                    Where-Object { $_.Name.Contains('\') } |
                    ForEach-Object { [System.IO.Path]::GetRelativePath($copyDir, $_.FullName) })
                Add-Content -LiteralPath $log -Value (@('', 'files written:') + (Get-ChangedFiles $before (Get-FileHashes $copyDir)))
                if ($run.ExitCode -eq 2 -and ($run.Output -match 'auto-detection failed')) {
                    $result = 'skip'; $detail = 'no code project'
                } elseif ($run.ExitCode -ne 0) {
                    $result = 'FAIL'; $detail = Get-ErrorLines $run.Output
                } elseif ($misnamed.Count -gt 0) {
                    $result = 'FAIL'; $detail = 'wrote files with a backslash in the name: ' + (Format-FileList $misnamed)
                }
            }
            'fonts' {
                # Deletes the cached fonts first, so every font the project uses is generated on this
                # OS (KernSmith off Windows). The checked-in cache also holds fonts nothing uses any
                # more, so which fonts come back is compared across OSes (the manifest), not here.
                $fontCache = Join-Path (Split-Path -Parent $copiedProject) 'FontCache'
                if (Test-Path -LiteralPath $fontCache) {
                    Get-ChildItem -LiteralPath $fontCache -Filter '*.fnt' -File | Remove-Item -Force
                }
                $run = Invoke-Gumcli $log @('fonts', $copiedProject)
                $substituted = @($run.Output | Where-Object { $_ -match 'is not installed on this machine' } | Sort-Object -Unique)
                if ($run.ExitCode -ne 0) {
                    $result = 'FAIL'; $detail = Get-ErrorLines $run.Output
                } elseif ($substituted.Count -gt 0) {
                    $result = 'note'; $detail = $substituted -join ' '
                }
            }
        }
        $knownReason = if ($known.ContainsKey($label)) { $known[$label][$step] } else { $null }
        if ($knownReason) {
            if ($result -eq 'FAIL') {
                $result = 'known'; $detail = "$knownReason. $detail"
            } else {
                $result = 'note'; $detail = "listed in known failures ($knownReason) but did not fail; remove the entry. $detail"
            }
        }
        if ($detail.Length -gt 300) { $detail = $detail.Substring(0, 300) + '...' }
        $seconds = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
        $results.Add([pscustomobject]@{ Project = $label; Step = $step; Result = $result; Seconds = $seconds; Detail = $detail; Log = $log })
        Write-Host ("    {0,-9} {1} ({2}s) {3}" -f $step, $result, $seconds, $detail)
    }

    # What the steps left behind, for comparing runs on different OSes (Tools/project-sweep-compare.ps1).
    # Carriage returns are dropped before hashing, since git checks text out with CRLF on Windows.
    # Generated fonts are listed without their content: bmfont.exe and KernSmith draw glyphs
    # differently by design, but every OS must generate the same set of fonts.
    foreach ($file in Get-ChildItem -LiteralPath $copyDir -Recurse -File -Force) {
        $relative = [System.IO.Path]::GetRelativePath($copyDir, $file.FullName) -replace '\\', '/'
        if ($relative -match '(^|/)FontCache/') {
            if ($relative.EndsWith('.fnt')) { $manifest.Add("$label|$relative|generated") }
            continue
        }
        $manifest.Add("$label|$relative|$([ProjectSweepFacts]::HashWithoutCarriageReturns($file.FullName))")
    }
}

# --- report -----------------------------------------------------------------------------------
$lines = @('| Project | Step | Result | Seconds | Detail |', '|---|---|---|---|---|')
foreach ($r in $results) {
    $lines += "| $($r.Project) | $($r.Step) | $($r.Result) | $($r.Seconds) | $($r.Detail -replace '\|', '\|') |"
}
$reportFile = Join-Path $WorkRoot 'report.md'
Set-Content -LiteralPath $reportFile -Value $lines
$results | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $WorkRoot 'results.json')
$manifest | Sort-Object | Set-Content -LiteralPath (Join-Path $WorkRoot 'manifest.txt')

$failed = @($results | Where-Object { $_.Result -eq 'FAIL' })
Write-Host ''
Write-Host "$($projects.Count) project(s), $($results.Count) step(s), $($failed.Count) failure(s). Report: $reportFile"
if ($failed.Count -gt 0) { exit 1 }
exit 0
