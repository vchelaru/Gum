# Packs the Unity package, binaries included, into Unity/.artifacts/ as both a folder and an npm-style
# .tgz ("Add package from tarball" in Unity). With -Push, also commits that folder as the only content
# of the upm branch and tags it upm/v<Version>, which is what users install:
#   https://github.com/vchelaru/Gum.git#upm  (or #upm/v<Version>)
# Run Unity/build-unity-package.ps1 first. Modeled on SkiaGameRendering's eng/publish-unity-package.ps1.
param(
    [Parameter(Mandatory)][string]$Version,
    [switch]$Push
)
$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$package = Join-Path $PSScriptRoot 'com.vchelaru.gum'
$out = Join-Path $PSScriptRoot '.artifacts'
$staging = Join-Path $out 'package'

if (-not (Test-Path (Join-Path $package 'Plugins/UnityGum.dll'))) {
    throw "Plugins/ is empty. Run Unity/build-unity-package.ps1 first."
}

if (Test-Path $out) { Remove-Item -LiteralPath $out -Recurse -Force }
New-Item -ItemType Directory -Force $staging | Out-Null
Get-ChildItem $package -Force | Copy-Item -Destination $staging -Recurse

$manifestPath = Join-Path $staging 'package.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$manifest.version = $Version
$manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath

# Relative paths: GNU tar (Git for Windows) reads a drive letter's colon as a remote host.
$tgzName = "com.vchelaru.gum-$Version.tgz"
$tgz = Join-Path $out $tgzName
Push-Location $out
try {
    tar -czf $tgzName package
    if ($LASTEXITCODE -ne 0) { throw "tar failed." }
} finally {
    Pop-Location
}
Write-Host "Packed $tgz"

if (-not $Push) { return }

$branch = 'upm'
$tag = "upm/v$Version"
$tree = Join-Path $out 'upm-branch'

# A published version is never overwritten.
git -C $repo ls-remote --exit-code --tags origin "refs/tags/$tag" | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "$tag is already published; skipping the push."
    return
}

git -C $repo fetch origin $branch 2>$null
if ($LASTEXITCODE -eq 0) {
    git -C $repo worktree add -B $branch $tree FETCH_HEAD
} else {
    git -C $repo worktree add --orphan -b $branch $tree
}
if ($LASTEXITCODE -ne 0) { throw "Could not check out the $branch branch." }

try {
    Get-ChildItem $tree -Force | Where-Object { $_.Name -ne '.git' } | Remove-Item -Recurse -Force
    Get-ChildItem $staging -Force | Copy-Item -Destination $tree -Recurse
    git -C $tree add -A
    git -C $tree commit -m "com.vchelaru.gum $Version"
    if ($LASTEXITCODE -ne 0) { throw "Commit failed." }
    git -C $tree tag $tag
    git -C $tree push origin $branch $tag
    if ($LASTEXITCODE -ne 0) { throw "Push failed." }
} finally {
    git -C $repo worktree remove --force $tree
}
