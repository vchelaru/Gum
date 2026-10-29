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
    $errorFile = Join-Path ([System.IO.Path]::GetTempPath()) "verify-tests-$PID.err"
    $output = @(& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'verify.ps1') -Files $file -ListOnly 2>$errorFile 6>&1 | ForEach-Object { "$_" })
    $errors = @(Get-Content $errorFile -ErrorAction SilentlyContinue | Where-Object { $_ })
    Remove-Item $errorFile -ErrorAction SilentlyContinue
    $output += $errors
    $actual = @($output | Where-Object { $_ -match '^build\s+' } | ForEach-Object { ($_ -replace '^build\s+', '') -replace '\\', '/' } |
        Where-Object { $_ -notlike 'Runtimes/SokolGum/*' } | Sort-Object)
    $expectedText = ($expected | Sort-Object) -join '; '
    if ($errors.Count -eq 0 -and ($actual -join '; ') -ceq $expectedText) {
        Write-Host "pass  $name"
    } else {
        Write-Host "FAIL  $name`n  expected: $expectedText`n  actual:   $($actual -join '; ')`n  output:`n    $($output -join "`n    ")"
        $script:failures++
    }
}

# A changed file in a test project runs its class; one in a library that is not a test project builds
# it. A test project is IsTestProject true or a Microsoft.NET.Test.Sdk reference, whatever its name.
function Assert-Plan([string]$name, [string]$file, [string]$expected) {
    $output = @(& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'verify.ps1') -Files $file -ListOnly 6>&1 | ForEach-Object { "$_" })
    $actual = @($output | Where-Object { $_ -match '^(test|build)\s+' } | ForEach-Object { (($_ -replace '\s+', ' ').Trim()) -replace '\\', '/' })
    if (($actual -join '; ') -ceq $expected) { Write-Host "pass  $name" }
    else { Write-Host "FAIL  $name`n  expected: $expected`n  actual:   $($actual -join '; ')"; $script:failures++ }
}

Assert-Plan 'file in a test project not named *.Tests runs as a test' `
    'Tests/Gum.Layout.Benchmarks/LayoutTreeShapes.cs' 'test Tests/Gum.Layout.Benchmarks/Gum.Layout.Benchmarks.csproj'

Assert-Plan 'file in a *TestsCommon library builds it' `
    'Tests/MonoGameGum.TestsCommon/StaticRegistrationScope.cs' 'build Tests/MonoGameGum.TestsCommon/MonoGameGum.TestsCommon.csproj'

Assert-Builds 'file linked by a Directory.Build.props builds the projects under that props' `
    'Runtimes/SkiaGum/CustomSetPropertyOnRenderable.cs' `
    @('Runtimes/GumShapes/KniGumShapes.csproj', 'Runtimes/GumShapes/MonoGameGumShapes.csproj', 'Runtimes/SkiaGum/SkiaGum.csproj')

# Building the head csproj alone leaves the plugins out of later Gum.slnx builds (CLAUDE.md).
Assert-Plan 'file in the Avalonia head builds Gum.slnx, not the head csproj' `
    'Tool/Gum.Avalonia/HeadOptions.cs' 'build Gum.slnx'

Assert-Builds 'file linked by a csproj and by a .props another .props imports builds every project that sees it' `
    'MonoGameGum/Input/CursorExtensions.cs' `
    @('MonoGameGum/KniGum/KniGum.csproj', 'MonoGameGum/MonoGameGum.csproj','Runtimes/KniSkiaGum/KniSkiaGum.csproj', 'Runtimes/RaylibGum/RaylibGum.csproj',
      'Runtimes/SilkNetGum/SilkNetGum.csproj', 'Runtimes/StrideGum/StrideGum.csproj',
      'Runtimes/UnityGum/UnityGum.csproj')

Assert-Builds 'file in a folder with two csprojs, linked by nothing, builds both and nothing else' `
    'Runtimes/GumShapes/AssemblyInfo.cs' `
    @('Runtimes/GumShapes/KniGumShapes.csproj', 'Runtimes/GumShapes/MonoGameGumShapes.csproj')

Assert-Builds 'file every linking csproj removes with Compile Remove builds only the projects that keep it' `
    'MonoGameGum/Input/KeyCombo.cs' `
    @('GumCommon/GumCommon.csproj')

# fast-test.ps1 -ListOnly: a test class in a shared folder that test projects link in with
# <Compile Include> runs in every project that includes it; Project:Class picks one.
function Assert-FastTest([string]$name, [string]$request, [string[]]$expected) {
    $output = @(& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'fast-test.ps1') $request -ListOnly 6>&1 | ForEach-Object { "$_" })
    $actual = @($output | Where-Object { $_ -match '^test\s+' } | ForEach-Object { (($_ -split '\s+')[1]) -replace '\\', '/' } | Sort-Object)
    if (($actual -join '; ') -ceq (($expected | Sort-Object) -join '; ')) { Write-Host "pass  $name" }
    else { Write-Host "FAIL  $name`n  expected: $(($expected | Sort-Object) -join '; ')`n  actual:   $($actual -join '; ')`n  output:`n    $($output -join "`n    ")"; $script:failures++ }
}

Assert-FastTest 'fast-test: class in a shared folder runs in every test project that links it' `
    'TestAppDataFolderTests' `
    @('Tests/Gum.Avalonia.Tests/Gum.Avalonia.Tests.csproj', 'Tests/Gum.Presentation.Tests/Gum.Presentation.Tests.csproj')

Assert-FastTest 'fast-test: Project:Class picks one project for a linked class' `
    'Gum.Presentation.Tests:TestAppDataFolderTests' `
    @('Tests/Gum.Presentation.Tests/Gum.Presentation.Tests.csproj')

# A branch is compared with the point it left Base, so files merged into Base since then are not
# treated as changed. Runs verify.ps1 in a scratch repo where Base moved after the branch point.
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) "verify-tests-$PID"
try {
    New-Item -ItemType Directory -Force -Path (Join-Path $scratch 'Tools'), (Join-Path $scratch 'A'), (Join-Path $scratch 'B') | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'verify.ps1'), (Join-Path $PSScriptRoot 'Invoke-Dotnet.ps1'), (Join-Path $PSScriptRoot 'TestProjects.ps1'), (Join-Path $PSScriptRoot 'IncludeIndex.ps1'), (Join-Path $PSScriptRoot 'IncludeIndex.ps1') (Join-Path $scratch 'Tools')
    Set-Content (Join-Path $scratch 'A/A.csproj') '<Project />'
    Set-Content (Join-Path $scratch 'B/B.csproj') '<Project />'
    $git = { git -C $scratch -c user.name=t -c user.email=t@t -c core.autocrlf=false @args 2>&1 | Out-Null }
    & $git init -q -b base
    & $git add -A; & $git commit -q -m root
    & $git checkout -q -b branch
    Set-Content (Join-Path $scratch 'A/OnBranch.cs') 'class OnBranch {}'
    & $git add -A; & $git commit -q -m branch
    & $git checkout -q base
    Set-Content (Join-Path $scratch 'B/MergedToBase.cs') 'class MergedToBase {}'
    & $git add -A; & $git commit -q -m base
    & $git checkout -q branch
    $output = @(& pwsh -NoProfile -File (Join-Path $scratch 'Tools/verify.ps1') -Base base -ListOnly 6>&1 | ForEach-Object { "$_" })
    $actual = @($output | Where-Object { $_ -match '^build\s+' } | ForEach-Object { ($_ -replace '^build\s+', '') -replace '\\', '/' })
    if (($actual -join '; ') -ceq 'A/A.csproj') { Write-Host 'pass  files merged into Base after the branch point are not changed' }
    else { Write-Host "FAIL  files merged into Base after the branch point are not changed`n  expected: A/A.csproj`n  actual:   $($actual -join '; ')"; $failures++ }
}
finally { Remove-Item -Recurse -Force $scratch -ErrorAction SilentlyContinue }

if ($failures -gt 0) { Write-Host "`n$failures failed"; exit 1 }
Write-Host "`nall passed"
