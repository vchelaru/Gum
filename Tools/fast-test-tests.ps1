<#
.SYNOPSIS
Tests which projects and filters Tools/fast-test.ps1 picks for a request, run through
fast-test.ps1 -ListOnly in a scratch repo. No build, a few seconds. Exits 1 on any failure.

.EXAMPLE
pwsh Tools/fast-test-tests.ps1
#>
$ErrorActionPreference = 'Stop'
$failures = 0
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) "fast-test-tests-$PID"

function Assert-Plan([string]$name, [string]$request, [string[]]$expected) {
    $output = @(& pwsh -NoProfile -File (Join-Path $scratch 'Tools/fast-test.ps1') $request -ListOnly 2>&1 6>&1 | ForEach-Object { "$_" })
    $actual = @($output | Where-Object { $_ -match '^(test|REFUSED)\b' } | ForEach-Object { ($_ -replace '\s+', ' ').Trim() })
    if (($actual -join "`n") -ceq ($expected -join "`n")) {
        Write-Host "pass  $name"
    } else {
        Write-Host "FAIL  $name`n  expected:`n    $($expected -join "`n    ")`n  output:`n    $($output -join "`n    ")"
        $script:failures++
    }
}

try {
    foreach ($dir in 'Tools', 'A.Tests', 'B.Tests', 'Outside.Tests') { New-Item -ItemType Directory -Force -Path (Join-Path $scratch $dir) | Out-Null }
    Copy-Item (Join-Path $PSScriptRoot 'fast-test.ps1'), (Join-Path $PSScriptRoot 'Invoke-Dotnet.ps1') (Join-Path $scratch 'Tools')
    foreach ($project in 'A.Tests', 'B.Tests', 'Outside.Tests') { Set-Content (Join-Path $scratch "$project/$project.csproj") '<Project />' }
    Set-Content (Join-Path $scratch 'All.slnx') "<Solution>`n  <Project Path=`"A.Tests/A.Tests.csproj`" />`n</Solution>"
    Set-Content (Join-Path $scratch 'Other.sln') "Project(`"{9A19103F-16F7-4668-BE54-9A1E7A4F7556}`") = `"B.Tests`", `"B.Tests\B.Tests.csproj`", `"{11111111-1111-1111-1111-111111111111}`"`nEndProject"
    Set-Content (Join-Path $scratch 'A.Tests/Dup.cs') "namespace A;`npublic class DupTests`n{`n    public void OnlyInA() { }`n}"
    Set-Content (Join-Path $scratch 'B.Tests/Dup.cs') "namespace B;`npublic class DupTests`n{`n    public void OnlyInB() { }`n}"
    Set-Content (Join-Path $scratch 'Outside.Tests/Outside.cs') "namespace O;`npublic class DupTests { }`npublic class OutsideOnlyTests { }"
    $git = { git -C $scratch -c user.name=t -c user.email=t@t -c core.autocrlf=false @args 2>&1 | Out-Null }
    & $git init -q -b main
    & $git add -A; & $git commit -q -m root
    Set-Content (Join-Path $scratch 'A.Tests/Untracked.cs') "namespace A;`npublic class UntrackedTests { }"

    Assert-Plan 'Class.Method on a class in two projects runs only where the method exists' 'DupTests.OnlyInA' `
        @('test A.Tests/A.Tests.csproj FullyQualifiedName~A.DupTests.OnlyInA')
    Assert-Plan 'Project:Class runs only in that project' 'B.Tests:DupTests' `
        @('test B.Tests/B.Tests.csproj FullyQualifiedName~B.DupTests.|FullyQualifiedName~B.DupTests+')
    Assert-Plan 'bare class in two solution projects runs both, never a project outside every solution' 'DupTests' `
        @('test A.Tests/A.Tests.csproj FullyQualifiedName~A.DupTests.|FullyQualifiedName~A.DupTests+',
          'test B.Tests/B.Tests.csproj FullyQualifiedName~B.DupTests.|FullyQualifiedName~B.DupTests+')
    Assert-Plan 'class in an untracked file is found' 'UntrackedTests' `
        @('test A.Tests/A.Tests.csproj FullyQualifiedName~A.UntrackedTests.|FullyQualifiedName~A.UntrackedTests+')
    Assert-Plan 'class only in a project outside every solution is refused' 'OutsideOnlyTests' `
        @('REFUSED: not a test class in any test project: OutsideOnlyTests')

    # fast-test and verify read the classic "Passed!"/"Failed!" summary; the terminal logger prints
    # "Test summary:" instead, so a run with it on reported NOTESTS for tests that ran.
    $previous = $env:MSBUILDTERMINALLOGGER
    $env:MSBUILDTERMINALLOGGER = 'on'
    try {
        . (Join-Path $PSScriptRoot 'Invoke-Dotnet.ps1')
        $result = Invoke-Dotnet -Command @('msbuild', (Join-Path $scratch 'A.Tests/A.Tests.csproj'), '-nologo', '-getProperty:MSBUILDTERMINALLOGGER') -TimeoutSeconds 60
        $value = ($result.Output | Where-Object { $_ } | Select-Object -Last 1)
        if ($value -eq 'off') { Write-Host 'pass  dotnet runs with the terminal logger off even when the environment turns it on' }
        else { Write-Host "FAIL  dotnet runs with the terminal logger off even when the environment turns it on`n  MSBUILDTERMINALLOGGER was: $value"; $failures++ }
    }
    finally { $env:MSBUILDTERMINALLOGGER = $previous }

    # A NOTESTS result prints the filter and the tail of the output, so its cause shows without a rerun.
    # The scratch copy's Invoke-Dotnet is swapped for a stub that exits 0 with no test summary.
    Set-Content (Join-Path $scratch 'Tools/Invoke-Dotnet.ps1') 'function Invoke-Dotnet { param([string[]]$Command, [int]$TimeoutSeconds) @{ Output = @(''stub line 6569''); Exit = 0; TimedOut = $false } }'
    $output = (& pwsh -NoProfile -File (Join-Path $scratch 'Tools/fast-test.ps1') 'UntrackedTests' -NoBuild 2>&1 6>&1 | ForEach-Object { "$_" }) -join "`n"
    $name = 'NOTESTS prints the filter and the last lines of the output'
    if ($output -match 'NOTESTS' -and $output -match 'filter: FullyQualifiedName~A\.UntrackedTests\.' -and $output -match 'stub line 6569') { Write-Host "pass  $name" }
    else { Write-Host "FAIL  $name`n  output:`n    $($output -replace "`n", "`n    ")"; $failures++ }
}
finally { Remove-Item -Recurse -Force $scratch -ErrorAction SilentlyContinue }

if ($failures -gt 0) { Write-Host "`n$failures failed"; exit 1 }
Write-Host "`nall passed"
exit 0
