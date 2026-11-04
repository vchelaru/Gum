<#
.SYNOPSIS
Tests which .nupkg files Tools/Get-PublishablePackages.ps1 lists in a scratch folder. Exits 1 on any failure.

.EXAMPLE
pwsh Tools/Get-PublishablePackages-tests.ps1
#>
$ErrorActionPreference = 'Stop'
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) "publishable-tests-$PID"
try {
    New-Item -ItemType Directory -Force -Path $scratch | Out-Null
    $files = @(
        'Gum.KniSkia.2026.11.4.1.nupkg',                            # held: stable
        'Gum.KniSkia.2026.11.4.1-preview.1.nupkg',                  # listed: preview
        'Gum.KniSkia.2026.11.4.1.snupkg',                           # never listed: not a .nupkg
        'Gum.Themes.Default.KniSkia.2026.11.4.1.nupkg',             # held: stable
        'Gum.Themes.Default.KniSkia.2026.11.4.1-preview.2.nupkg',   # listed: preview
        'Gum.KniSkiaFoo.2026.11.4.1.nupkg',                         # listed: a different package
        'Gum.Themes.Default.KniSkiaFoo.2026.11.4.1.nupkg',          # listed: a different package
        'MonoGameGum.2026.11.4.1.nupkg',                            # listed
        'MonoGameGum.2026.11.4.1-preview.1.nupkg')                  # listed
    foreach ($f in $files) { New-Item -ItemType File -Path (Join-Path $scratch $f) | Out-Null }

    $expected = @(
        'Gum.KniSkia.2026.11.4.1-preview.1.nupkg',
        'Gum.KniSkiaFoo.2026.11.4.1.nupkg',
        'Gum.Themes.Default.KniSkia.2026.11.4.1-preview.2.nupkg',
        'Gum.Themes.Default.KniSkiaFoo.2026.11.4.1.nupkg',
        'MonoGameGum.2026.11.4.1-preview.1.nupkg',
        'MonoGameGum.2026.11.4.1.nupkg') | Sort-Object
    $actual = @(& (Join-Path $PSScriptRoot 'Get-PublishablePackages.ps1') -PackageDirectory $scratch | ForEach-Object { $_.Name } | Sort-Object)

    if (($actual -join "`n") -ceq ($expected -join "`n")) { Write-Host 'all passed'; exit 0 }
    Write-Host "FAIL`n  expected:`n    $($expected -join "`n    ")`n  actual:`n    $($actual -join "`n    ")"
    exit 1
}
finally { Remove-Item -Recurse -Force $scratch -ErrorAction SilentlyContinue }
