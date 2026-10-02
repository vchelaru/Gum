# Fills Unity/com.vchelaru.gum/Plugins/ with the DLLs the Unity package ships: UnityGum's
# netstandard2.1 build, everything it depends on, and the HarfBuzzSharp native for each player
# platform. Unity doesn't consume NuGet, so they come out of the build output and the NuGet cache.
# Platforms: Windows x64 and macOS universal.
#
# The DLLs are build output, so they are not committed (Plugins/ is gitignored). Run this before
# opening Samples/UnityGum, and before publish-unity-package.ps1.
#
# SkiaSharp.dll and its natives are left out: the SkiaGameRendering package ships them for the same
# platforms, and Unity refuses two copies of one assembly. So are assemblies Unity's .NET Standard 2.1
# profile already has. HarfBuzzSharp is Gum's dependency, not SkiaGameRendering's, so it ships here.
param(
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'

$repo = Split-Path $PSScriptRoot -Parent
$package = Join-Path $PSScriptRoot 'com.vchelaru.gum'
$plugins = Join-Path $package 'Plugins'
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
New-Item -ItemType Directory -Force $plugins | Out-Null

Get-ChildItem $output -Filter *.dll |
    Where-Object { $excluded -notcontains $_.Name } |
    Copy-Item -Destination $plugins

# HarfBuzzSharp's native library per platform, which NuGet would normally copy for the runtime
# identifier. UnityGum.csproj downloads these packages (PackageDownload) at the managed version.
$assets = Get-Content (Join-Path $repo 'Runtimes/UnityGum/obj/project.assets.json') -Raw | ConvertFrom-Json
$libraries = $assets.libraries.PSObject.Properties.Name
$harfBuzz = $libraries | Where-Object { $_ -like 'HarfBuzzSharp/*' } | Select-Object -First 1
if (-not $harfBuzz) { throw "HarfBuzzSharp is not in UnityGum's dependency graph." }
$harfBuzzVersion = ($harfBuzz -split '/')[1]

# Dest is the folder under Plugins/. Platform and CPU are Unity's plugin import settings; EditorOS
# also enables the native in the Editor on that OS.
$natives = @(
    @{ Package = 'HarfBuzzSharp.NativeAssets.Win32'; Source = 'runtimes/win-x64/native/libHarfBuzzSharp.dll'; Dest = 'x86_64'; Platform = 'Win64'; CPU = 'x86_64'; EditorOS = 'Windows' },
    @{ Package = 'HarfBuzzSharp.NativeAssets.macOS'; Source = 'runtimes/osx/native/libHarfBuzzSharp.dylib'; Dest = 'macOS'; Platform = 'OSXUniversal'; CPU = 'AnyCPU'; EditorOS = 'OSX' }
)
foreach ($entry in $natives) {
    $source = Join-Path $nuget "$($entry.Package.ToLower())/$harfBuzzVersion/$($entry.Source)"
    if (-not (Test-Path $source)) {
        throw "$source is missing. UnityGum.csproj's PackageDownload versions must match HarfBuzzSharp $harfBuzzVersion."
    }
    $destination = Join-Path $plugins $entry.Dest
    New-Item -ItemType Directory -Force $destination | Out-Null
    Copy-Item $source $destination
    $entry.Path = Join-Path $destination (Split-Path $entry.Source -Leaf)
}

# A package installed from git is read-only, so Unity can't write .meta files for it and ignores any
# file without one. Each plugin gets a .meta whose GUID is derived from its path, so it is stable from
# build to build. Managed DLLs are enabled on every supported platform, each native on its own one.
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
    OSXUniversal:
      enabled: 1
      settings: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@ | Set-Content "$path.meta"
}

# Enabled only for its one player platform and CPU, plus the Editor on the matching OS.
function Write-NativeMeta([string]$path, [string]$key, [hashtable]$native) {
    $editorEnabled = if ($native.EditorOS) { 1 } else { 0 }
    $editorOS = if ($native.EditorOS) { $native.EditorOS } else { 'AnyOS' }
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
      enabled: $editorEnabled
      settings:
        CPU: $($native.CPU)
        DefaultValueInitialized: true
        OS: $editorOS
    $($native.Platform):
      enabled: 1
      settings:
        CPU: $($native.CPU)
  userData:
  assetBundleName:
  assetBundleVariant:
"@ | Set-Content "$path.meta"
}

function Get-PluginKey([string]$path) {
    'Plugins' + $path.Substring($plugins.Length).Replace('\', '/')
}

Write-FolderMeta $plugins 'Plugins'
Get-ChildItem $plugins -Directory -Recurse |
    ForEach-Object { Write-FolderMeta $_.FullName (Get-PluginKey $_.FullName) }
Get-ChildItem $plugins -Filter *.dll | ForEach-Object { Write-ManagedMeta $_.FullName "Plugins/$($_.Name)" }
foreach ($entry in $natives) { Write-NativeMeta $entry.Path (Get-PluginKey $entry.Path) $entry }

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
