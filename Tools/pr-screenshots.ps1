#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Renders a PR's screenshot tests on the branch (after) and on a base commit (before), uploads the
    PNGs to the pr-screenshots branch and puts a before/after table in the PR body.

.DESCRIPTION
    Screenshot tests live in Tests/Gum.Avalonia.Tests, tagged [Trait("Category", "Screenshot")] and
    run through PrScreenshot.Run (see Tests/Gum.Avalonia.Tests/Harness/README.md, "PR screenshots").
    They skip unless GUM_SCREENSHOT_DIR is set, which this script does.

    "After" runs in this checkout. "Before" runs in a sibling worktree detached at -Base, reused
    between runs so its build stays warm; the screenshot helpers and every test file that uses
    PrScreenshot.Category are copied into it from this checkout, so they must compile against the
    base. PNGs go to pr-<num>/before-<name>.png and after-<name>.png on the never-merged
    pr-screenshots branch through the GitHub contents API. The PR body's table sits between
    <!-- pr-screenshots:start/end --> markers, so a rerun replaces it.

.PARAMETER Pr
    The pull request number.

.PARAMETER Filter
    Which screenshot tests to run: a test class or method name, or a full dotnet test filter
    expression (anything containing '=' or '~'). Always combined with Category=Screenshot.

.PARAMETER Base
    The commit the "before" side renders. Default origin/main.

.PARAMETER NoBefore
    Skip the "before" side, for UI that does not exist on the base.

.PARAMETER DryRun
    Render both sides and write the proposed PR body, but upload nothing and leave the PR alone.

.EXAMPLE
    pwsh Tools/pr-screenshots.ps1 -Pr 5341 -Filter ManagePluginsScreenshotTests
.EXAMPLE
    pwsh Tools/pr-screenshots.ps1 -Pr 5341 -Filter ManagePluginsScreenshotTests -DryRun
#>
param(
    [Parameter(Mandatory)][int]$Pr,
    [Parameter(Mandatory)][string]$Filter,
    [string]$Base = 'origin/main',
    [switch]$NoBefore,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$testProject = 'Tests/Gum.Avalonia.Tests/Gum.Avalonia.Tests.csproj'
$screenshotBranch = 'pr-screenshots'
$outRoot = Join-Path ([IO.Path]::GetTempPath()) "gum-pr-screenshots\pr-$Pr"
$baseWorktree = Join-Path (Split-Path -Parent $repo) 'gum-pr-screenshots-base'
$markerStart = '<!-- pr-screenshots:start -->'
$markerEnd = '<!-- pr-screenshots:end -->'

$testFilter = if ($Filter -match '[=~]') { $Filter } else { "FullyQualifiedName~$Filter" }
$testFilter = "Category=Screenshot&($testFilter)"

function Invoke-Native {
    param([string]$What, [scriptblock]$Command)
    # To the host, not the pipeline, or it would join a function's return value.
    & $Command | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$What failed (exit $LASTEXITCODE)." }
}

# Runs the screenshot tests of the checkout at $root and returns the PNGs they wrote.
function Invoke-Screenshots([string]$root, [string]$outDir) {
    if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
    New-Item -ItemType Directory -Force $outDir | Out-Null
    $env:GUM_SCREENSHOT_DIR = $outDir
    try {
        Invoke-Native "Screenshot tests in $root" { dotnet test (Join-Path $root $testProject) -nologo -v:q --filter $testFilter }
    }
    finally {
        Remove-Item Env:GUM_SCREENSHOT_DIR -ErrorAction SilentlyContinue
    }
    $pngs = @(Get-ChildItem -Path $outDir -Filter *.png -File)
    if ($pngs.Count -eq 0) { throw "No screenshot was written by the tests matching '$testFilter' in $root." }
    # The comma keeps a single PNG an array; PowerShell would unroll it to a lone FileInfo.
    return ,$pngs
}

# The base checkout: a reused sibling worktree detached at $Base, with this branch's screenshot
# helpers and screenshot tests copied in.
function Initialize-BaseWorktree([string]$baseSha) {
    if (Test-Path $baseWorktree) {
        # A leftover plain folder (a failed worktree remove) would let git -C walk up to an
        # enclosing checkout, and the forced checkout below would throw away its changes.
        $topLevel = git -C $baseWorktree rev-parse --show-toplevel 2>$null
        if ($LASTEXITCODE -ne 0 -or [IO.Path]::GetFullPath("$topLevel".Trim()).TrimEnd('\', '/') -ne [IO.Path]::GetFullPath($baseWorktree).TrimEnd('\', '/')) {
            throw "$baseWorktree exists but is not a git worktree of its own; delete it and rerun."
        }
        Invoke-Native 'git checkout (base worktree)' { git -C $baseWorktree checkout -q --detach --force $baseSha }
        # Drops files an earlier run copied in; bin/obj are ignored, so the build stays warm.
        Invoke-Native 'git clean (base worktree)' { git -C $baseWorktree clean -fdq -- Tests/Gum.Avalonia.Tests }
    }
    else {
        Invoke-Native 'git worktree add' { git -C $repo worktree add -q --detach $baseWorktree $baseSha }
    }

    $testsRoot = Join-Path $repo 'Tests/Gum.Avalonia.Tests'
    $copied = @(
        Get-ChildItem -Path (Join-Path $testsRoot 'Harness') -Include PrScreenshot.cs, ScreenshotWindow.cs, UserPathGuard.cs -File -Recurse
        Get-ChildItem -Path $testsRoot -Filter *.cs -File -Recurse |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
            Where-Object { Select-String -Path $_.FullName -Pattern 'PrScreenshot.Category' -SimpleMatch -Quiet }
    ) | Sort-Object FullName -Unique
    foreach ($file in $copied) {
        $relative = [IO.Path]::GetRelativePath($repo, $file.FullName)
        $destination = Join-Path $baseWorktree $relative
        New-Item -ItemType Directory -Force (Split-Path -Parent $destination) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
    }
    Write-Host "Base worktree $baseWorktree at $baseSha, with $($copied.Count) screenshot file(s) copied in."
}

# Uploads one PNG to the screenshots branch; returns the new blob sha (used to bust image caches).
function Publish-Png([string]$repoName, [string]$localPath, [string]$remotePath) {
    $existingSha = gh api "repos/$repoName/contents/${remotePath}?ref=$screenshotBranch" --jq .sha 2>$null
    if ($LASTEXITCODE -ne 0) { $existingSha = $null }
    $request = [ordered]@{
        message = "PR #$Pr screenshot $([IO.Path]::GetFileName($remotePath))"
        branch  = $screenshotBranch
        content = [Convert]::ToBase64String([IO.File]::ReadAllBytes($localPath))
    }
    if ($existingSha) { $request.sha = "$existingSha".Trim() }
    # Through a file: a PNG's base64 overruns the Windows command-line limit.
    $requestFile = Join-Path $outRoot 'upload-request.json'
    $request | ConvertTo-Json -Compress | Set-Content -LiteralPath $requestFile -Encoding utf8NoBOM
    $blobSha = gh api -X PUT "repos/$repoName/contents/$remotePath" --input $requestFile --jq .content.sha
    if ($LASTEXITCODE -ne 0) { throw "Uploading $remotePath failed." }
    Remove-Item -LiteralPath $requestFile
    return "$blobSha".Trim()
}

function Get-ImageCell([string]$repoName, [hashtable]$shas, [string]$remotePath, [bool]$exists, [string]$alt) {
    if (-not $exists) { return '_none_' }
    $url = "https://raw.githubusercontent.com/$repoName/$screenshotBranch/$remotePath"
    if ($shas.ContainsKey($remotePath)) { $url += "?v=$($shas[$remotePath].Substring(0, 7))" }
    return "![$alt]($url)"
}

# Puts $section in place of the marked block, or before the closing "Closes #"/generated-by lines.
function Merge-Body([string]$body, [string]$section) {
    $pattern = "(?s)$([regex]::Escape($markerStart)).*?$([regex]::Escape($markerEnd))"
    if ($body -match $pattern) { return [regex]::Replace($body, $pattern, { param($m) $section }) }
    $lines = [Collections.Generic.List[string]]::new([string[]]($body -split "`n"))
    $anchor = $lines.FindIndex([Predicate[string]] { param($line) $line -match '^(Closes|Fixes|Resolves) #\d+' -or $line -match 'Generated with \[Claude Code\]' })
    if ($anchor -lt 0) { return ($body.TrimEnd() + "`n`n" + $section + "`n") }
    $lines.Insert($anchor, '')
    $lines.Insert($anchor, $section)
    return ($lines -join "`n")
}

$repoName = (gh repo view --json nameWithOwner -q .nameWithOwner)
if ($LASTEXITCODE -ne 0) { throw 'gh repo view failed; is gh signed in?' }

Write-Host "After: $repo"
# No @() around the call: the function already returns an array, and @() would nest it.
$afterPngs = Invoke-Screenshots $repo (Join-Path $outRoot 'after')

$beforePngs = @()
if (-not $NoBefore) {
    if ($Base -like 'origin/*') { Invoke-Native 'git fetch' { git -C $repo fetch -q origin } }
    $baseSha = (git -C $repo rev-parse --verify "$Base^{commit}")
    if ($LASTEXITCODE -ne 0) { throw "Unknown base '$Base'." }
    Initialize-BaseWorktree $baseSha
    Write-Host "Before: $baseWorktree"
    $beforePngs = Invoke-Screenshots $baseWorktree (Join-Path $outRoot 'before')
}

$names = @($afterPngs + $beforePngs | ForEach-Object BaseName | Sort-Object -Unique)
$shas = @{}
if (-not $DryRun) {
    foreach ($side in @(@{ Name = 'before'; Pngs = $beforePngs }, @{ Name = 'after'; Pngs = $afterPngs })) {
        foreach ($png in $side.Pngs) {
            $remotePath = "pr-$Pr/$($side.Name)-$($png.Name)"
            $shas[$remotePath] = Publish-Png $repoName $png.FullName $remotePath
            Write-Host "Uploaded $remotePath"
        }
    }
}

$rows = foreach ($name in $names) {
    $before = Get-ImageCell $repoName $shas "pr-$Pr/before-$name.png" ($beforePngs.BaseName -contains $name) 'before'
    $after = Get-ImageCell $repoName $shas "pr-$Pr/after-$name.png" ($afterPngs.BaseName -contains $name) 'after'
    "| ``$name`` | $before | $after |"
}
$baseNote = if ($NoBefore) { '' } else { "; before is ``$Base``" }
$section = @(
    $markerStart
    '## Screenshots'
    ''
    "Rendered headlessly by ``Tools/pr-screenshots.ps1 -Filter $Filter``$baseNote."
    ''
    '| | Before | After |'
    '|---|---|---|'
    $rows
    $markerEnd
) -join "`n"

# gh hands a multi-line body to PowerShell as an array of lines.
$currentBody = (gh pr view $Pr --json body -q .body 2>$null) -join "`n"
if ($LASTEXITCODE -ne 0) {
    if (-not $DryRun) { throw "PR #$Pr not found." }
    $currentBody = ''
}
$newBody = Merge-Body $currentBody $section
$bodyFile = Join-Path $outRoot 'body.md'
Set-Content -LiteralPath $bodyFile -Value $newBody -Encoding utf8NoBOM -NoNewline

if ($DryRun) {
    Write-Host "Dry run: nothing uploaded, PR #$Pr untouched. PNGs: $outRoot\{before,after}; proposed body: $bodyFile"
    exit 0
}
Invoke-Native 'gh pr edit' { gh pr edit $Pr --body-file $bodyFile }
Write-Host "PR #$Pr body updated with $($names.Count) screenshot row(s)."
