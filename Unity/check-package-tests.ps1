# Self-test for check-package.ps1: builds tiny packages in a temp folder and checks what it reports.
# Run: pwsh Unity/check-package-tests.ps1 (exits nonzero on failure; CI's pack-unity job runs it).
$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'check-package.ps1'
$failures = 0

function New-Package {
    $dir = Join-Path ([IO.Path]::GetTempPath()) ("gum-pkg-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path (Join-Path $dir 'Runtime') | Out-Null
    foreach ($file in 'Runtime.meta', 'Runtime/a.cs', 'Runtime/a.cs.meta', 'package.json', 'package.json.meta') {
        Set-Content (Join-Path $dir $file) 'x'
    }
    $dir
}

function Test-Case([string]$name, [scriptblock]$arrange, [string]$expectedMessage) {
    $dir = New-Package
    try {
        & $arrange $dir
        $output = & pwsh -NoProfile -File $script -Path $dir 2>&1 | Out-String
        $failed = $LASTEXITCODE -ne 0
        if ($expectedMessage) {
            $ok = $failed -and $output.Contains($expectedMessage)
        } else {
            $ok = -not $failed
        }
        if ($ok) { Write-Host "ok   $name" } else { Write-Host "FAIL $name`n$output"; $script:failures++ }
    } finally {
        Remove-Item -LiteralPath $dir -Recurse -Force
    }
}

Test-Case 'complete package passes' { param($d) } ''
Test-Case 'file without .meta is reported by relative path' { param($d) Remove-Item "$d/Runtime/a.cs.meta" } 'No .meta file: Runtime/a.cs'
Test-Case 'orphan .meta is reported' { param($d) Remove-Item "$d/Runtime/a.cs" } 'Orphan .meta file (its asset is missing): Runtime/a.cs.meta'

if ($failures -gt 0) { throw "$failures check-package test(s) failed." }
