<#
.SYNOPSIS
Opens every element of every sample project in the Avalonia Gum tool and captures a screenshot of each.

.DESCRIPTION
For each .gumx under Samples\ (bin/obj skipped, byte-identical projects swept once), every Screen,
Component and Standard element is opened in the built Avalonia head with
  --exit-after <n> --select <element> --screenshot <png> --user-data <folder>
against a copy of the project under -OutRoot, never the checked-in sample.

-Reference also renders each Screen and Component through `gumcli screenshot` (MonoGame, the renderer
the WPF tool's canvas uses), so each Avalonia canvas can be read next to what WPF would draw.

Output under -OutRoot:
  shots\<sample>\<element>.png   full-window screenshots from the Avalonia head
  ref\<sample>\<element>.png     gumcli MonoGame renders (-Reference)
  logs\<sample>\<element>.log    the head's stderr, with GUM_ECHO_OUTPUT=1 (every Output tab line)
  sheets\sheet-NNN.png           contact sheets: the canvas region of each shot, beside its reference
  results.csv, summary.md        exit code, duration and flagged log lines per element

Build first: dotnet build Gum.slnx (and Tools\Gum.Cli\Gum.Cli.csproj for -Reference).

.EXAMPLE
pwsh Tools/SampleSweep/sweep.ps1 -Reference
pwsh Tools/SampleSweep/sweep.ps1 -Samples GameUiSamples -Elements Controls/DialogBox
pwsh Tools/SampleSweep/sweep.ps1 -SkipRun      # rebuild summary and sheets from the last run
pwsh Tools/SampleSweep/sweep.ps1 -Resume       # finish an interrupted sweep
#>
[CmdletBinding()]
param(
    [string]$OutRoot = (Join-Path $env:TEMP 'gum-sample-sweep'),
    [string]$Configuration = 'Debug',
    [double]$ExitAfter = 8,
    [int]$Parallel = 4,
    # Substrings matched against the sample id (e.g. GameUiSamples, MVVM).
    [string[]]$Samples,
    # Exact element names (e.g. Controls/DialogBox, Text).
    [string[]]$Elements,
    # Sweep an element even when the same file was already swept in another sample.
    [switch]$NoDedupe,
    [switch]$Reference,
    # Run only the elements with no result yet under -OutRoot (an interrupted sweep).
    [switch]$Resume,
    [switch]$SkipRun
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$headExe = Join-Path $repo "Tool\Gum.Avalonia\bin\$Configuration\net10.0\Gum.exe"
$gumcli = Join-Path $repo "Tools\Gum.Cli\bin\$Configuration\net10.0\gumcli.exe"
if (-not $SkipRun -and -not (Test-Path $headExe)) { throw "Build the head first: $headExe not found" }
if ($Reference -and -not $SkipRun -and -not (Test-Path $gumcli)) { throw "Build gumcli first: $gumcli not found" }

New-Item -ItemType Directory -Force $OutRoot | Out-Null

function Get-SafeName([string]$name) { $name -replace '[\\/:*?"<>|]', '_' }

# --- Discover projects and elements ---------------------------------------------------------------
$seenProjects = @{}
$seenElements = @{}
$jobs = [System.Collections.Generic.List[object]]::new()
$projects = [System.Collections.Generic.List[object]]::new()

$gumxFiles = Get-ChildItem (Join-Path $repo 'Samples') -Recurse -Filter *.gumx |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | Sort-Object FullName

foreach ($gumx in $gumxFiles) {
    $hash = (Get-FileHash $gumx.FullName).Hash
    $relDir = $gumx.DirectoryName.Substring($repo.Length + 'Samples\'.Length + 1)
    $sampleId = (($relDir -split '\\') | Where-Object { $_ -ne 'Content' }) -join '_'
    if ($seenProjects.ContainsKey($hash)) {
        Write-Host "skip $sampleId (same .gumx as $($seenProjects[$hash]))"
        continue
    }
    $seenProjects[$hash] = $sampleId
    if ($Samples -and -not ($Samples | Where-Object { $sampleId -like "*$_*" })) { continue }

    $project = [pscustomobject]@{ Id = $sampleId; Dir = $gumx.DirectoryName; GumxName = $gumx.Name }
    $projects.Add($project)

    [xml]$xml = Get-Content -Raw $gumx.FullName
    $kinds = @(
        @{ Tag = 'ScreenReference'; Folder = 'Screens'; Ext = 'gusx'; Kind = 'Screen' },
        @{ Tag = 'ComponentReference'; Folder = 'Components'; Ext = 'gucx'; Kind = 'Component' },
        @{ Tag = 'StandardElementReference'; Folder = 'Standards'; Ext = 'gutx'; Kind = 'Standard' })
    foreach ($k in $kinds) {
        foreach ($ref in @($xml.GumProjectSave.($k.Tag))) {
            if ($null -eq $ref) { continue }
            $name = $ref.Name
            if ($Elements -and $Elements -notcontains $name) { continue }
            $file = Join-Path $gumx.DirectoryName "$($k.Folder)\$name.$($k.Ext)"
            $fileHash = if (Test-Path $file) { (Get-FileHash $file).Hash } else { 'missing' }
            $key = "$($k.Kind)|$name|$fileHash"
            $dupOf = $null
            if (-not $NoDedupe -and $fileHash -ne 'missing' -and $seenElements.ContainsKey($key)) {
                $dupOf = $seenElements[$key]
            }
            else {
                $seenElements[$key] = "$sampleId/$name"
            }
            $jobs.Add([pscustomobject]@{
                Sample = $sampleId; Kind = $k.Kind; Name = $name; Safe = (Get-SafeName $name)
                GumxName = $gumx.Name; DupOf = $dupOf; FileMissing = ($fileHash -eq 'missing')
            })
        }
    }
}

$toRun = @($jobs | Where-Object { -not $_.DupOf })
if ($Resume) {
    $toRun = @($toRun | Where-Object { -not (Test-Path (Join-Path $OutRoot "logs\$($_.Sample)\$($_.Safe).result.json")) })
}
Write-Host "$($projects.Count) project(s), $($jobs.Count) element(s), $($toRun.Count) to run after dedupe"

# --- Run the head -------------------------------------------------------------------------------
if (-not $SkipRun) {
    # One project copy per lane, so parallel runs never share a folder the tool may re-save into.
    foreach ($p in $projects) {
        for ($lane = 0; $lane -lt $Parallel; $lane++) {
            $dest = Join-Path $OutRoot "projects\$($p.Id)\lane$lane"
            if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
            New-Item -ItemType Directory -Force $dest | Out-Null
            Copy-Item -Recurse -Force (Join-Path $p.Dir '*') $dest
        }
    }

    $lanes = 0..($Parallel - 1) | ForEach-Object { $l = $_; , @($toRun | Where-Object { ([array]::IndexOf($toRun, $_) % $Parallel) -eq $l }) }
    $laneIndex = 0
    $laneInputs = foreach ($laneJobs in $lanes) { [pscustomobject]@{ Lane = $laneIndex; Jobs = $laneJobs }; $laneIndex++ }

    $laneInputs | ForEach-Object -ThrottleLimit $Parallel -Parallel {
        $lane = $_.Lane
        $outRoot = $using:OutRoot
        $exe = $using:headExe
        $exitAfter = $using:ExitAfter
        $userData = Join-Path $outRoot "userdata\lane$lane"
        New-Item -ItemType Directory -Force $userData | Out-Null
        foreach ($job in $_.Jobs) {
            $gumxPath = Join-Path $outRoot "projects\$($job.Sample)\lane$lane\$($job.GumxName)"
            $shot = Join-Path $outRoot "shots\$($job.Sample)\$($job.Safe).png"
            $log = Join-Path $outRoot "logs\$($job.Sample)\$($job.Safe).log"
            $result = Join-Path $outRoot "logs\$($job.Sample)\$($job.Safe).result.json"
            New-Item -ItemType Directory -Force (Split-Path $shot), (Split-Path $log) | Out-Null
            Remove-Item -Force $shot, $log -ErrorAction SilentlyContinue

            $psi = [System.Diagnostics.ProcessStartInfo]::new($exe)
            foreach ($a in @($gumxPath, '--exit-after', "$exitAfter", '--select', $job.Name, '--screenshot', $shot, '--user-data', $userData)) {
                $psi.ArgumentList.Add($a)
            }
            $psi.RedirectStandardError = $true
            $psi.RedirectStandardOutput = $true
            $psi.UseShellExecute = $false
            $psi.Environment['GUM_ECHO_OUTPUT'] = '1'
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $proc = [System.Diagnostics.Process]::Start($psi)
            $errTask = $proc.StandardError.ReadToEndAsync()
            $outTask = $proc.StandardOutput.ReadToEndAsync()
            $timedOut = -not $proc.WaitForExit([int](($exitAfter + 90) * 1000))
            if ($timedOut) { $proc.Kill($true); $proc.WaitForExit() }
            $sw.Stop()
            Set-Content -Path $log -Value ($errTask.Result + $outTask.Result)
            [pscustomobject]@{
                ExitCode = if ($timedOut) { 'timeout' } else { $proc.ExitCode }
                Seconds = [math]::Round($sw.Elapsed.TotalSeconds, 1)
            } | ConvertTo-Json | Set-Content $result
            Write-Host "[lane $lane] $($job.Sample) $($job.Name): exit $(if ($timedOut) { 'timeout' } else { $proc.ExitCode })"
        }
    }

    if ($Reference) {
        $refJobs = @($jobs | Where-Object {
            -not $_.DupOf -and $_.Kind -ne 'Standard' -and
            -not (Test-Path (Join-Path $OutRoot "ref\$($_.Sample)\$($_.Safe).png")) })
        $refJobs | ForEach-Object -ThrottleLimit $Parallel -Parallel {
            $job = $_
            $outRoot = $using:OutRoot
            $out = Join-Path $outRoot "ref\$($job.Sample)\$($job.Safe).png"
            New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
            # A lane-0 copy is safe to read now: the head runs above have all exited.
            $gumxPath = Join-Path $outRoot "projects\$($job.Sample)\lane0\$($job.GumxName)"
            $text = & $using:gumcli screenshot $gumxPath $job.Name --output $out 2>&1 | Out-String
            Set-Content (Join-Path $outRoot "logs\$($job.Sample)\$($job.Safe).ref.log") "exit $LASTEXITCODE`n$text"
        }
    }
}

# --- Summarize ----------------------------------------------------------------------------------
# Lines worth a look in the Output echo; the args echo and routine load chatter are excluded.
$flagPattern = '(?i)exception|startup failed|\berror\b|failed|could not|unable|not found|missing'
$ignorePattern = '(?i)^\[Gum output\] (--|[A-Z]:\\)|0 Errors|^(Game|Input|Graphics)Factory not found\.$'
$rows = foreach ($job in $jobs) {
    $src = if ($job.DupOf) { $null } else { $job }
    $exit = ''; $secs = ''; $flags = @(); $shotExists = $false; $refExit = ''
    if ($src) {
        $resultPath = Join-Path $OutRoot "logs\$($job.Sample)\$($job.Safe).result.json"
        if (Test-Path $resultPath) { $r = Get-Content -Raw $resultPath | ConvertFrom-Json; $exit = $r.ExitCode; $secs = $r.Seconds }
        $logPath = Join-Path $OutRoot "logs\$($job.Sample)\$($job.Safe).log"
        if (Test-Path $logPath) {
            $flags = @(Get-Content $logPath | Where-Object { $_ -match $flagPattern -and $_ -notmatch $ignorePattern } | Select-Object -Unique)
        }
        $shotExists = Test-Path (Join-Path $OutRoot "shots\$($job.Sample)\$($job.Safe).png")
        $refLog = Join-Path $OutRoot "logs\$($job.Sample)\$($job.Safe).ref.log"
        if (Test-Path $refLog) { $refExit = ((Get-Content $refLog -TotalCount 1) -replace '^exit ', '') }
    }
    [pscustomobject]@{
        Sample = $job.Sample; Kind = $job.Kind; Name = $job.Name; DupOf = $job.DupOf; FileMissing = $job.FileMissing
        ExitCode = $exit; Seconds = $secs; Screenshot = $shotExists; RefExit = $refExit; Flags = ($flags -join ' || ')
    }
}
$rows | Export-Csv -NoTypeInformation (Join-Path $OutRoot 'results.csv')

$ran = @($rows | Where-Object { -not $_.DupOf })
$bad = @($ran | Where-Object { "$($_.ExitCode)" -ne '0' -or -not $_.Screenshot -or $_.FileMissing })
$flagged = @($ran | Where-Object { $_.Flags })
$md = [System.Text.StringBuilder]::new()
[void]$md.AppendLine("# Sample sweep`n")
[void]$md.AppendLine("$($rows.Count) elements, $($ran.Count) run, $($rows.Count - $ran.Count) skipped as duplicates.`n")
[void]$md.AppendLine("## Non-zero exit, timeout, missing screenshot or missing file ($($bad.Count))`n")
foreach ($b in $bad) { [void]$md.AppendLine("- $($b.Sample) $($b.Kind) ``$($b.Name)``: exit $($b.ExitCode), screenshot $($b.Screenshot), file missing $($b.FileMissing)") }
[void]$md.AppendLine("`n## Flagged Output lines ($($flagged.Count))`n")
foreach ($f in $flagged) { [void]$md.AppendLine("- $($f.Sample) ``$($f.Name)``: $($f.Flags)") }
Set-Content (Join-Path $OutRoot 'summary.md') $md.ToString()

# --- Contact sheets -----------------------------------------------------------------------------
# Each cell is the Editor canvas cropped from the full-window shot (fractions of the default
# 1280x720 window), with the gumcli MonoGame render beside it when one exists.
Add-Type -AssemblyName System.Drawing
$sheetDir = Join-Path $OutRoot 'sheets'
if (Test-Path $sheetDir) { Remove-Item -Recurse -Force $sheetDir }
New-Item -ItemType Directory -Force $sheetDir | Out-Null

# Canvas crop in the default 1280x720 window, and where world (0,0) sits inside it at 100% zoom.
$cropX = 578; $cropY = 80; $cropW = 690; $cropH = 435; $originX = 31; $originY = 31
$cellW = 460; $cellH = 290; $labelH = 18; $rowsPerSheet = 4
$scale = $cellW / $cropW
$font = [System.Drawing.Font]::new('Segoe UI', 9)

# Slots are half a row; an element with a reference render takes a whole row (tool left, gumcli right).
$placed = [System.Collections.Generic.List[object]]::new()
$slot = 0
foreach ($c in @($ran | Where-Object { $_.Screenshot })) {
    $safe = Get-SafeName $c.Name
    $refPath = Join-Path $OutRoot "ref\$($c.Sample)\$safe.png"
    $hasRef = Test-Path $refPath
    if ($hasRef -and $slot % 2 -eq 1) { $slot++ }
    $placed.Add([pscustomobject]@{ Cell = $c; Slot = $slot; Ref = $(if ($hasRef) { $refPath }) })
    $slot += $(if ($hasRef) { 2 } else { 1 })
}

$slotsPerSheet = $rowsPerSheet * 2
$sheetCount = [math]::Ceiling($slot / $slotsPerSheet)
for ($sheetIndex = 0; $sheetIndex -lt $sheetCount; $sheetIndex++) {
    $onSheet = @($placed | Where-Object { [math]::Floor($_.Slot / $slotsPerSheet) -eq $sheetIndex })
    if ($onSheet.Count -eq 0) { continue }
    $rowCount = [math]::Floor((($onSheet[-1].Slot % $slotsPerSheet)) / 2) + 1
    $sheet = [System.Drawing.Bitmap]::new($cellW * 2, ($cellH + $labelH) * $rowCount)
    $g = [System.Drawing.Graphics]::FromImage($sheet)
    $g.Clear([System.Drawing.Color]::FromArgb(40, 40, 40))
    $g.InterpolationMode = 'HighQualityBicubic'
    foreach ($p in $onSheet) {
        $c = $p.Cell
        $local = $p.Slot % $slotsPerSheet
        $x = ($local % 2) * $cellW
        $y = [math]::Floor($local / 2) * ($cellH + $labelH)
        $label = "$($c.Sample) | $($c.Kind) $($c.Name) | exit $($c.ExitCode)"
        if ($p.Ref) { $label += '   (right: gumcli MonoGame, same scale)' }
        $g.DrawString($label, $font, [System.Drawing.Brushes]::White, $x + 2, $y + 1)
        $shot = [System.Drawing.Image]::FromFile((Join-Path $OutRoot "shots\$($c.Sample)\$(Get-SafeName $c.Name).png"))
        $sx = $shot.Width / 1280; $sy = $shot.Height / 720
        $src = [System.Drawing.Rectangle]::new([int]($cropX * $sx), [int]($cropY * $sy), [int]($cropW * $sx), [int]($cropH * $sy))
        $g.DrawImage($shot, [System.Drawing.Rectangle]::new($x, $y + $labelH, $cellW, $cellH), $src, 'Pixel')
        $shot.Dispose()
        if ($p.Ref) {
            $ref = [System.Drawing.Image]::FromFile($p.Ref)
            $cell = [System.Drawing.Rectangle]::new($cellW, $y + $labelH, $cellW, $cellH)
            $g.FillRectangle([System.Drawing.Brushes]::DimGray, $cell)
            $g.SetClip($cell)
            $g.DrawImage($ref, [single]($cellW + $originX * $scale), [single]($y + $labelH + $originY * $scale),
                [single]($ref.Width * $scale), [single]($ref.Height * $scale))
            $g.ResetClip()
            $ref.Dispose()
        }
    }
    $g.Dispose()
    $sheet.Save((Join-Path $sheetDir ('sheet-{0:D3}.png' -f $sheetIndex)))
    $sheet.Dispose()
}
Write-Host "Summary: $(Join-Path $OutRoot 'summary.md'); $sheetCount sheet(s) in $sheetDir"
