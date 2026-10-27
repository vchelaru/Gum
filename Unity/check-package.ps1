# Checks a built Unity package folder (default: Unity/com.vchelaru.gum after build-unity-package.ps1,
# or the staging folder publish-unity-package.ps1 writes):
#  1. Every file and folder has a .meta. A package installed from git is read-only, so Unity can't write
#     the missing ones and logs "has no meta file, but it's in an immutable folder" on every import.
#     Folders ending in ~ and dot-files are ignored by Unity and need none.
#  2. Every assembly a managed Plugins/ DLL references is either in Plugins/, shipped by
#     SkiaGameRendering, or part of Unity's .NET Standard 2.1 profile. A missing one fails the
#     assembly load ("Assembly ... will not be loaded").
param(
    [string]$Path = (Join-Path $PSScriptRoot 'com.vchelaru.gum')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path $Path).Path
$problems = @()

function Test-Ignored([string]$fullPath) {
    $relative = $fullPath.Substring($root.Length).TrimStart([char]92, [char]47).Replace([string][char]92, '/')
    foreach ($segment in $relative -split '/') {
        if ($segment.EndsWith('~') -or $segment.StartsWith('.')) { return $true }
    }
    return $false
}

foreach ($item in Get-ChildItem $root -Recurse -Force) {
    if ($item.Extension -eq '.meta' -or (Test-Ignored $item.FullName)) { continue }
    if (-not (Test-Path -LiteralPath "$($item.FullName).meta")) {
        $problems += "No .meta file: $($item.FullName.Substring($root.Length).TrimStart([char]92, [char]47))"
    }
}

$providedByUnityOrSkiaGameRendering = @(
    'netstandard', 'SkiaSharp', 'Microsoft.CSharp', 'System.Buffers', 'System.Memory',
    'System.Numerics.Vectors', 'System.Threading.Tasks.Extensions'
)
$plugins = Join-Path $root 'Plugins'
if (Test-Path $plugins) {
    $dlls = Get-ChildItem $plugins -Filter *.dll
    $shipped = $dlls | ForEach-Object { $_.BaseName }
    foreach ($dll in $dlls) {
        $stream = [IO.File]::OpenRead($dll.FullName)
        $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($reader)
            foreach ($handle in $metadata.AssemblyReferences) {
                $name = $metadata.GetString($metadata.GetAssemblyReference($handle).Name)
                if ($shipped -notcontains $name -and $providedByUnityOrSkiaGameRendering -notcontains $name) {
                    $problems += "$($dll.Name) references $name, which the package does not ship."
                }
            }
        } finally {
            $reader.Dispose()
            $stream.Dispose()
        }
    }
}

if ($problems.Count -gt 0) {
    $problems | Sort-Object -Unique | ForEach-Object { Write-Host $_ }
    throw "Unity package check failed: $($problems.Count) problem(s)."
}
Write-Host "Unity package check passed: $root"
