<#
.SYNOPSIS
Tests for which projects Tools/verify.ps1 builds for a changed file, run against this repo's real
csproj/props layout through verify.ps1 -ListOnly. Exits 1 on any failure.

.EXAMPLE
pwsh Tools/verify-tests.ps1
#>
$ErrorActionPreference = 'Stop'
$failures = 0

function Assert-Builds([string]$name, [string]$file, [string[]]$expected) {
    $output = @(& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'verify.ps1') -Files $file -ListOnly 6>&1 | ForEach-Object { "$_" })
    $actual = @($output | Where-Object { $_ -match '^build\s+' } | ForEach-Object { ($_ -replace '^build\s+', '') -replace '\\', '/' } |
        Where-Object { $_ -notlike 'Runtimes/SokolGum/*' } | Sort-Object)
    $expectedText = ($expected | Sort-Object) -join '; '
    if (($actual -join '; ') -ceq $expectedText) {
        Write-Host "pass  $name"
    } else {
        Write-Host "FAIL  $name`n  expected: $expectedText`n  actual:   $($actual -join '; ')`n  output:`n    $($output -join "`n    ")"
        $script:failures++
    }
}

Assert-Builds 'file linked by a Directory.Build.props builds the projects under that props' `
    'Runtimes/SkiaGum/CustomSetPropertyOnRenderable.cs' `
    @('Runtimes/GumShapes/KniGumShapes.csproj', 'Runtimes/GumShapes/MonoGameGumShapes.csproj', 'Runtimes/SkiaGum/SkiaGum.csproj')

Assert-Builds 'file linked by a csproj and by a .props another .props imports builds every project that sees it' `
    'MonoGameGum/Input/CursorExtensions.cs' `
    @('MonoGameGum/KniGum/KniGum.csproj', 'MonoGameGum/MonoGameGum.csproj','Runtimes/KniSkiaGum/KniSkiaGum.csproj', 'Runtimes/RaylibGum/RaylibGum.csproj',
      'Runtimes/SilkNetGum/SilkNetGum.csproj', 'Runtimes/StrideGum/StrideGum.csproj',
      'Runtimes/UnityGum/UnityGum.csproj')

Assert-Builds 'file nothing links builds only its own project' `
    'Runtimes/GumShapes/AssemblyInfo.cs' `
    @('Runtimes/GumShapes/KniGumShapes.csproj')

if ($failures -gt 0) { Write-Host "`n$failures failed"; exit 1 }
Write-Host "`nall passed"
