# Fills Unity/com.vchelaru.gum/Plugins/ with the DLLs the Unity package ships: UnityGum's
# netstandard2.1 build, everything it depends on, and the HarfBuzzSharp native. Unity doesn't consume
# NuGet, so they come out of the build output and the NuGet cache. Windows x64 only so far.
#
# The DLLs are build output, so they are not committed (Plugins/ is gitignored). Run this before
# opening Samples/UnityGum, and before publish-unity-package.ps1.
#
# SkiaSharp.dll and its native are left out: the SkiaGameRendering package ships them, and Unity
# refuses two copies of one assembly. So are assemblies Unity's .NET Standard 2.1 profile already has.
param(
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$package = Join-Path $PSScriptRoot 'com.vchelaru.gum'
$plugins = Join-Path $package 'Plugins'
$native = Join-Path $plugins 'x86_64'
$project = Join-Path $repo 'Runtimes/UnityGum/UnityGum.csproj'
$nuget = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $HOME '.nuget/packages' }

$excluded = @(
    'SkiaSharp.dll',
    'System.Buffers.dll',
    'System.Memory.dll',
    'System.Numerics.Vectors.dll',
    'System.Runtime.CompilerServices.Unsafe.dll',
    'System.Threading.Tasks.Extensions.dll',
    'Microsoft.CSharp.dll',
    'System.Data.DataSetExtensions.dll'
)

dotnet build $project -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "UnityGum build failed." }

$output = Join-Path $repo "Runtimes/UnityGum/bin/$Configuration/netstandard2.1"

if (Test-Path $plugins) { Remove-Item -LiteralPath $plugins -Recurse -Force }
New-Item -ItemType Directory -Force $native | Out-Null

Get-ChildItem $output -Filter *.dll |
    Where-Object { $excluded -notcontains $_.Name } |
    Copy-Item -Destination $plugins

# HarfBuzzSharp's native library, which NuGet would normally copy for the runtime identifier.
$assets = Get-Content (Join-Path $repo 'Runtimes/UnityGum/obj/project.assets.json') -Raw | ConvertFrom-Json
$libraries = $assets.libraries.PSObject.Properties.Name
$harfBuzz = $libraries | Where-Object { $_ -like 'HarfBuzzSharp.NativeAssets.Win32/*' } | Select-Object -First 1
if (-not $harfBuzz) { throw "HarfBuzzSharp.NativeAssets.Win32 is not in UnityGum's dependency graph." }
Copy-Item (Join-Path $nuget "$($harfBuzz.ToLower())/runtimes/win-x64/native/libHarfBuzzSharp.dll") $native

# A package installed from git is read-only, so Unity can't write .meta files for it and ignores any
# file without one. Each DLL gets a .meta whose GUID is derived from its path, so it is stable from
# build to build, with import settings of Editor and Windows x64 only (the same as SkiaGameRendering).
$md5 = [System.Security.Cryptography.MD5]::Create()
function Get-StableGuid([string]$key) {
    $bytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("com.vchelaru.gum/$key"))
    ($bytes | ForEach-Object { $_.ToString('x2') }) -join ''
}

function Write-FolderMeta([string]$path, [string]$key) {
    @"
fileFormatVersion: 2
guid: $(Get-StableGuid $key)
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@ | Set-Content "$path.meta"
}

function Write-ManagedMeta([string]$path, [string]$key) {
    @"
fileFormatVersion: 2
guid: $(Get-StableGuid $key)
PluginImporter:
  externalObjects: {}
  serializedVersion: 3
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
    Any:
      enabled: 0
      settings: {}
    Editor:
      enabled: 1
      settings:
        DefaultValueInitialized: true
    Win64:
      enabled: 1
      settings: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@ | Set-Content "$path.meta"
}

function Write-NativeMeta([string]$path, [string]$key) {
    @"
fileFormatVersion: 2
guid: $(Get-StableGuid $key)
PluginImporter:
  externalObjects: {}
  serializedVersion: 3
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
    Any:
      enabled: 0
      settings: {}
    Editor:
      enabled: 1
      settings:
        CPU: x86_64
        DefaultValueInitialized: true
        OS: Windows
    Linux64:
      enabled: 0
      settings:
        CPU: x86_64
    OSXUniversal:
      enabled: 0
      settings:
        CPU: x86_64
    Win:
      enabled: 0
      settings:
        CPU: None
    Win64:
      enabled: 1
      settings:
        CPU: x86_64
  userData:
  assetBundleName:
  assetBundleVariant:
"@ | Set-Content "$path.meta"
}

Write-FolderMeta $plugins 'Plugins'
Write-FolderMeta $native 'Plugins/x86_64'
Get-ChildItem $plugins -Filter *.dll | ForEach-Object { Write-ManagedMeta $_.FullName "Plugins/$($_.Name)" }
Get-ChildItem $native -Filter *.dll | ForEach-Object { Write-NativeMeta $_.FullName "Plugins/x86_64/$($_.Name)" }

# The binaries carry their own licenses, which redistribution has to include: Gum's own, plus each
# NuGet package's license as its .nuspec declares it.
Copy-Item (Join-Path $repo 'LICENSE') (Join-Path $package 'LICENSE.md')
$notices = Join-Path $package 'ThirdPartyNotices~'
if (Test-Path $notices) { Remove-Item -LiteralPath $notices -Recurse -Force }
New-Item -ItemType Directory -Force $notices | Out-Null
$lines = @('Third-party packages whose DLLs ship in Plugins/, with the license each declares.', '')
foreach ($library in $libraries | Sort-Object) {
    $id, $version = $library -split '/'
    $folder = Join-Path $nuget "$($id.ToLower())/$version"
    $nuspec = Get-ChildItem $folder -Filter *.nuspec -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $nuspec) { continue }
    [xml]$spec = Get-Content $nuspec.FullName
    $metadata = $spec.package.metadata
    $license = if ($metadata.license) { $metadata.license.'#text' } elseif ($metadata.licenseUrl) { $metadata.licenseUrl } else { 'see the package' }
    $lines += "$id $version : $license"
    foreach ($file in Get-ChildItem $folder -File | Where-Object { $_.Name -match '^(LICENSE|THIRD-PARTY-NOTICES)' }) {
        Copy-Item $file.FullName (Join-Path $notices "$id-$($file.Name)")
    }
}
$lines | Set-Content (Join-Path $notices 'PACKAGES.txt')

Write-Host "Unity package binaries written to $plugins"
