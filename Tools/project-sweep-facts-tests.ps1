<#
.SYNOPSIS
Tests for the saved-file comparison in Tools/project-sweep-facts.ps1. Exits 1 on any failure.

.EXAMPLE
pwsh Tools/project-sweep-facts-tests.ps1
#>
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'project-sweep-facts.ps1')

$folder = Join-Path ([System.IO.Path]::GetTempPath()) "project-sweep-facts-tests-$PID"
New-Item -ItemType Directory -Force -Path $folder | Out-Null
$failures = 0

function Get-Removed([string]$original, [string]$saved, [switch]$AllowCircleRadiusMigration, [string]$extension = 'gutx') {
    $originalFile = Join-Path $folder "original.$extension"
    $savedFile = Join-Path $folder "saved.$extension"
    Set-Content -LiteralPath $originalFile -Value $original
    Set-Content -LiteralPath $savedFile -Value $saved
    return @(Get-RemovedFacts $originalFile $savedFile '' -AllowCircleRadiusMigration:$AllowCircleRadiusMigration)
}

function Assert-Removed([string]$name, $actual, [string[]]$expected) {
    $actualText = ($actual | Sort-Object) -join "`n"
    $expectedText = ($expected | Sort-Object) -join "`n"
    if ($actualText -ceq $expectedText) {
        Write-Host "pass  $name"
    } else {
        Write-Host "FAIL  $name`n  expected: $($expected -join '; ')`n  actual:   $($actual -join '; ')"
        $script:failures++
    }
}

function New-VariableXml([string]$name, [string]$value) {
    return "<Variable><Type>float</Type><Name>$name</Name><Value>$value</Value><SetsValue>true</SetsValue></Variable>"
}

function New-Circle([string[]]$variables) {
    return "<StandardElementSave><Name>Circle</Name><State><Name>Default</Name>$($variables -join '')</State></StandardElementSave>"
}

# A screen whose Circle instance sets its size in the compact format (Name as an attribute).
function New-Screen([hashtable]$values) {
    $variables = foreach ($key in $values.Keys | Sort-Object) {
        "<Variable Type=`"float`" Name=`"$key`" SetsValue=`"true`"><Value>$($values[$key])</Value></Variable>"
    }
    return "<ScreenSave><Name>Main</Name><State><Name>Default</Name>$($variables -join '')</State></ScreenSave>"
}

$legacyCircle = New-Circle @((New-VariableXml 'Radius' '16'), (New-VariableXml 'Width' '10'), (New-VariableXml 'X' '5'))
$migratedCircle = New-Circle @((New-VariableXml 'Width' '32'), (New-VariableXml 'X' '5'), (New-VariableXml 'Height' '32'))

Assert-Removed 'standard Radius migrated to Width = Height = 2r is accepted' `
    (Get-Removed $legacyCircle $migratedCircle -AllowCircleRadiusMigration) @()

Assert-Removed 'the migration is not accepted without -AllowCircleRadiusMigration (raw save)' `
    (Get-Removed $legacyCircle $migratedCircle) @(
        'StandardElementSave/State/Variable/Name = Radius', 'StandardElementSave/State/Variable/Value = 16', 'StandardElementSave/State/Variable/Value = 10')

Assert-Removed 'a Height that is not 2r is still a changed value' `
    (Get-Removed $legacyCircle (New-Circle @((New-VariableXml 'Width' '32'), (New-VariableXml 'X' '5'), (New-VariableXml 'Height' '30'))) -AllowCircleRadiusMigration) @(
        'StandardElementSave/State/Variable/Name = Radius', 'StandardElementSave/State/Variable/Value = 16', 'StandardElementSave/State/Variable/Value = 10')

Assert-Removed 'another value lost alongside the migration is still reported' `
    (Get-Removed $legacyCircle (New-Circle @((New-VariableXml 'Width' '32'), (New-VariableXml 'Height' '32'))) -AllowCircleRadiusMigration) @(
        'StandardElementSave/State/Variable/Name = X', 'StandardElementSave/State/Variable/Value = 5')

Assert-Removed 'a Radius kept by the save is not treated as migrated' `
    (Get-Removed $legacyCircle (New-Circle @((New-VariableXml 'Radius' '16'), (New-VariableXml 'Width' '32'), (New-VariableXml 'X' '5'), (New-VariableXml 'Height' '32'))) -AllowCircleRadiusMigration) @(
        'StandardElementSave/State/Variable/Value = 10')

$legacyGradient = New-Circle @((New-VariableXml 'GradientInnerRadius' '16'))
Assert-Removed 'only the Radius segment migrates: a lost GradientInnerRadius is reported' `
    (Get-Removed $legacyGradient (New-Circle @((New-VariableXml 'GradientInnerWidth' '32'), (New-VariableXml 'GradientInnerHeight' '32'))) -AllowCircleRadiusMigration) @(
        'StandardElementSave/State/Variable/Name = GradientInnerRadius', 'StandardElementSave/State/Variable/Value = 16')

Assert-Removed 'instance Radius in the compact format overwrites the old Width and Height' `
    (Get-Removed (New-Screen @{ 'CircleInstance.Radius' = '52'; 'CircleInstance.Width' = '55'; 'CircleInstance.Height' = '104' }) `
        (New-Screen @{ 'CircleInstance.Width' = '104'; 'CircleInstance.Height' = '104' }) -AllowCircleRadiusMigration -extension 'gusx') @()

Assert-Removed 'instance migration only accepts the matching prefix' `
    (Get-Removed (New-Screen @{ 'CircleInstance.Radius' = '52' }) `
        (New-Screen @{ 'OtherInstance.Width' = '104'; 'OtherInstance.Height' = '104' }) -AllowCircleRadiusMigration -extension 'gusx') @(
        'ScreenSave/State/Variable/Name = CircleInstance.Radius', 'ScreenSave/State/Variable/Value = 52')

$legacyJson = '{ "Name": "Circle", "States": [ { "Name": "Default", "Variables": [ { "Name": "Radius", "Type": "float", "ValueAsFloat": 16 }, { "Name": "X", "Type": "float", "ValueAsFloat": 5 } ] } ] }'
$migratedJson = '{ "Name": "Circle", "States": [ { "Name": "Default", "Variables": [ { "Name": "Width", "Type": "float", "ValueAsFloat": 32 }, { "Name": "Height", "Type": "float", "ValueAsFloat": 32 }, { "Name": "X", "Type": "float", "ValueAsFloat": 5 } ] } ] }'
Assert-Removed 'JSON Radius migration is accepted' (Get-Removed $legacyJson $migratedJson -AllowCircleRadiusMigration -extension 'gutj') @()
Assert-Removed 'JSON migration to the wrong size is reported' `
    (Get-Removed $legacyJson ($migratedJson -replace '"ValueAsFloat": 32 }, \{ "Name": "Height"', '"ValueAsFloat": 30 }, { "Name": "Height"') -AllowCircleRadiusMigration -extension 'gutj') @(
        '/States/Variables/Name = Radius', '/States/Variables/ValueAsFloat = 16')

$exposedWidth = "<ScreenSave><Name>Main</Name><State><Name>Default</Name><Variable Type=`"float`" Name=`"CircleInstance.Radius`"><Value>8</Value></Variable><Variable Type=`"float`" Name=`"CircleInstance.Width`" ExposedAsName=`"Size`"><Value>3</Value></Variable></State></ScreenSave>"
Assert-Removed 'the migration keeps the old Width''s other fields: a lost ExposedAsName is reported' `
    (Get-Removed $exposedWidth (New-Screen @{ 'CircleInstance.Width' = '16'; 'CircleInstance.Height' = '16' }) -AllowCircleRadiusMigration -extension 'gusx') @(
        'ScreenSave/State/Variable/ExposedAsName = Size')

# Two categories with a state named Big: the migration in one must not excuse a change in the other.
function New-JsonCategories([string]$first, [string]$second) {
    return "{ `"Name`": `"C`", `"Categories`": [ { `"Name`": `"A`", `"States`": [ { `"Name`": `"Big`", `"Variables`": [ $first ] } ] }, { `"Name`": `"B`", `"States`": [ { `"Name`": `"Big`", `"Variables`": [ $second ] } ] } ] }"
}
Assert-Removed 'JSON states with the same name in different categories are kept apart' `
    (Get-Removed (New-JsonCategories '{ "Name": "Circle.Radius", "ValueAsFloat": 8 }' '{ "Name": "Circle.Width", "ValueAsFloat": 7 }') `
        (New-JsonCategories '{ "Name": "Circle.Width", "ValueAsFloat": 16 }, { "Name": "Circle.Height", "ValueAsFloat": 16 }' '{ "Name": "Circle.Width", "ValueAsFloat": 9 }') `
        -AllowCircleRadiusMigration -extension 'gucj') @('/Categories/States/Variables/ValueAsFloat = 7')

Remove-Item -Recurse -Force $folder
if ($failures -gt 0) { Write-Host "$failures failure(s)"; exit 1 }
Write-Host 'All passed.'
