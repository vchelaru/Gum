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
    foreach ($dir in 'Tools', 'A.Tests', 'B.Tests', 'Outside.Tests', 'Lib.TestsCommon', 'GumToolUnitTests') { New-Item -ItemType Directory -Force -Path (Join-Path $scratch $dir) | Out-Null }
    Copy-Item (Join-Path $PSScriptRoot 'fast-test.ps1'), (Join-Path $PSScriptRoot 'Invoke-Dotnet.ps1'), (Join-Path $PSScriptRoot 'TestProjects.ps1') (Join-Path $scratch 'Tools')
    # A test project is one with IsTestProject true or a Microsoft.NET.Test.Sdk reference; a library
    # named *Tests* (shared test scaffolding) is not.
    $isTestProject = '<Project><PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup></Project>'
    $testSdk = '<Project><ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" /></ItemGroup></Project>'
    Set-Content (Join-Path $scratch 'A.Tests/A.Tests.csproj') $isTestProject
    Set-Content (Join-Path $scratch 'B.Tests/B.Tests.csproj') $testSdk
    Set-Content (Join-Path $scratch 'Outside.Tests/Outside.Tests.csproj') $isTestProject
    Set-Content (Join-Path $scratch 'GumToolUnitTests/GumToolUnitTests.csproj') $testSdk
    Set-Content (Join-Path $scratch 'Lib.TestsCommon/Lib.TestsCommon.csproj') '<Project><ItemGroup><PackageReference Include="xunit" /></ItemGroup></Project>'
    Set-Content (Join-Path $scratch 'All.slnx') "<Solution>`n  <Project Path=`"A.Tests/A.Tests.csproj`" />`n  <Project Path=`"Lib.TestsCommon/Lib.TestsCommon.csproj`" />`n</Solution>"
    Set-Content (Join-Path $scratch 'Other.sln') ("Project(`"{9A19103F-16F7-4668-BE54-9A1E7A4F7556}`") = `"B.Tests`", `"B.Tests\B.Tests.csproj`", `"{11111111-1111-1111-1111-111111111111}`"`nEndProject`n" +
        "Project(`"{9A19103F-16F7-4668-BE54-9A1E7A4F7556}`") = `"GumToolUnitTests`", `"GumToolUnitTests\GumToolUnitTests.csproj`", `"{22222222-2222-2222-2222-222222222222}`"`nEndProject")
    Set-Content (Join-Path $scratch 'Lib.TestsCommon/Common.cs') "namespace L;`npublic class CommonOnlyTests { }"
    Set-Content (Join-Path $scratch 'GumToolUnitTests/Frozen.cs') "namespace F;`npublic class DupTests { }`npublic class FrozenOnlyTests { }"
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
    Assert-Plan 'Project:Class runs a project outside every solution when named' 'Outside.Tests:OutsideOnlyTests' `
        @('test Outside.Tests/Outside.Tests.csproj FullyQualifiedName~O.OutsideOnlyTests.|FullyQualifiedName~O.OutsideOnlyTests+')
    Assert-Plan 'class only in a solution library named *Tests* that is not a test project is refused' 'CommonOnlyTests' `
        @('REFUSED: not a test class in any test project: CommonOnlyTests')
    Assert-Plan 'class only in the frozen WPF head tests is refused unless the project is named' 'FrozenOnlyTests' `
        @('REFUSED: not a test class in any test project: FrozenOnlyTests')
    Assert-Plan 'Project:Class runs the frozen WPF head tests when named' 'GumToolUnitTests:FrozenOnlyTests' `
        @('test GumToolUnitTests/GumToolUnitTests.csproj FullyQualifiedName~F.FrozenOnlyTests.|FullyQualifiedName~F.FrozenOnlyTests+')

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

    # A failed test prints its whole message, including a Shouldly "because" past the expected/actual
    # lines, but not its stack trace.
    $failedRun = @(
        '  Failed A.UntrackedTests.Fails [42 ms]', '  Error Message:', '   Shouldly.ShouldAssertException : 2', '    should be', '3',
        '    but was', '2', '', '    Additional Info:', '    because 6724', '  Stack Trace:', '     at stack frame 6724',
        'Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1')
    Set-Content (Join-Path $scratch 'Tools/Invoke-Dotnet.ps1') ('function Invoke-Dotnet { param([string[]]$Command, [int]$TimeoutSeconds) @{ Output = @(' +
        (($failedRun | ForEach-Object { "'$_'" }) -join ',') + '); Exit = 1; TimedOut = $false } }')
    $output = (& pwsh -NoProfile -File (Join-Path $scratch 'Tools/fast-test.ps1') 'UntrackedTests' -NoBuild 2>&1 6>&1 | ForEach-Object { "$_" }) -join "`n"
    $name = 'a failed test prints its whole message, including the because text, without the stack trace'
    if ($output -match 'because 6724' -and $output -match 'Additional Info:' -and $output -notmatch 'stack frame 6724') { Write-Host "pass  $name" }
    else { Write-Host "FAIL  $name`n  output:`n    $($output -replace "`n", "`n    ")"; $failures++ }

    # One project's failure never skips another project's steps: a failed test leaves the next
    # project's tests to run, and a failed build skips only that project's own tests.
    function Assert-Continues([string]$name, [string]$failingStep, [string[]]$expected) {
        Set-Content (Join-Path $scratch 'Tools/Invoke-Dotnet.ps1') (@'
function Invoke-Dotnet {
    param([string[]]$Command, [int]$TimeoutSeconds)
    $step = "$($Command[0]) $(Split-Path -Leaf $Command[1])"
    if ($step -eq 'FAILING_STEP') { return @{ Output = @('Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1'); Exit = 1; TimedOut = $false } }
    @{ Output = @('Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1'); Exit = 0; TimedOut = $false }
}
'@ -replace 'FAILING_STEP', $failingStep)
        $output = @(& pwsh -NoProfile -File (Join-Path $scratch 'Tools/fast-test.ps1') 'DupTests' 2>&1 6>&1 | ForEach-Object { "$_" })
        $exit = $LASTEXITCODE
        $actual = @($output | Where-Object { $_ -match '^(ok|FAILED|NOTESTS|TIMEDOUT|skipped)\s' -or $_ -match '^FAST-TEST' } |
            ForEach-Object { ($_ -replace '\s+\d+s\s+', ' ' -replace '\s+', ' ').Trim() })
        if (($actual -join "`n") -ceq ($expected -join "`n") -and $exit -eq 1) { Write-Host "pass  $name" }
        else { Write-Host "FAIL  $name (exit $exit)`n  expected:`n    $($expected -join "`n    ")`n  output:`n    $($output -join "`n    ")"; $script:failures++ }
    }
    Assert-Continues 'a failed test in one project still runs the next project''s tests' 'test A.Tests.csproj' `
        @('ok build A.Tests/A.Tests.csproj', 'FAILED test A.Tests/A.Tests.csproj',
          'ok build B.Tests/B.Tests.csproj', 'ok test B.Tests/B.Tests.csproj', 'FAST-TEST FAILED')
    Assert-Continues 'a failed build skips only that project''s tests' 'build A.Tests.csproj' `
        @('FAILED build A.Tests/A.Tests.csproj', 'skipped test A.Tests/A.Tests.csproj: its build failed',
          'ok build B.Tests/B.Tests.csproj', 'ok test B.Tests/B.Tests.csproj', 'FAST-TEST FAILED')
}
finally { Remove-Item -Recurse -Force $scratch -ErrorAction SilentlyContinue }

if ($failures -gt 0) { Write-Host "`n$failures failed"; exit 1 }
Write-Host "`nall passed"
exit 0
