<#
.SYNOPSIS
Builds once, then runs exactly the named test classes (or Class.Method) with --no-build, printing
the elapsed time of every step. Names that are not a test class (a namespace, a typo, a whole
project) are refused: CI runs the broad suites, a local run is only for the code under work.

.PARAMETER Tests
Test class names, or Class.Method, comma separated or as separate arguments.

.PARAMETER NoBuild
Skip the build step (the projects were already built by a previous run).

.PARAMETER BuildTimeoutSeconds
Hard limit for each build. Default 300.

.PARAMETER TestTimeoutSeconds
Hard limit for each test run. Default 240.

.EXAMPLE
pwsh Tools/fast-test.ps1 LayoutShallowUpdateWrapTests,ButtonTests
pwsh Tools/fast-test.ps1 ButtonTests.Click_RaisesClicked
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0, ValueFromRemainingArguments)]
    [string[]]$Tests,
    [switch]$NoBuild,
    [int]$BuildTimeoutSeconds = 300,
    [int]$TestTimeoutSeconds = 240
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo
. (Join-Path $PSScriptRoot 'Invoke-Dotnet.ps1')

$requests = @($Tests | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$testProjects = @(git ls-files '*Tests*.csproj' | Where-Object { $_ })
$classNames = $requests | ForEach-Object { ($_ -split '\.')[0] } | Sort-Object -Unique
$classPattern = "class[[:space:]]+($($classNames -join '|'))([^A-Za-z0-9_]|$)"

# class name -> list of @{ Project; Namespace }. git grep is used because a PowerShell scan of every
# test project's sources takes half a minute.
$found = @{}
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$projectDirs = $testProjects | ForEach-Object { @{ Project = $_; Dir = ((Split-Path -Parent $_) -replace '\\', '/') + '/' } }
foreach ($line in (git grep -nIE $classPattern -- '*.cs')) {
    if ($line -notmatch '^(?<file>[^:]+):\d+:.*class\s+(?<name>\w+)') { continue }
    $file = $Matches.file
    $name = $Matches.name
    if ($name -notin $classNames) { continue }
    $owner = $projectDirs | Where-Object { $file.StartsWith($_.Dir) } | Sort-Object { $_.Dir.Length } -Descending | Select-Object -First 1
    if (-not $owner) { continue }
    $namespaceMatch = Select-String -Path (Join-Path $repo $file) -Pattern '^\s*namespace\s+([\w.]+)' -List
    $namespace = if ($namespaceMatch) { $namespaceMatch.Matches[0].Groups[1].Value } else { '' }
    if (-not $found.ContainsKey($name)) { $found[$name] = @() }
    if (-not ($found[$name] | Where-Object { $_.Project -eq $owner.Project -and $_.Namespace -eq $namespace })) {
        $found[$name] += @{ Project = $owner.Project; Namespace = $namespace }
    }
}
Write-Host ("resolved      {0,4:N0}s  {1} request(s) against {2} test project(s)" -f $sw.Elapsed.TotalSeconds, $requests.Count, $testProjects.Count)

$unknown = @($requests | Where-Object { -not $found.ContainsKey(($_ -split '\.')[0]) })
if ($unknown.Count -gt 0) {
    Write-Host "REFUSED: not a test class in any test project: $($unknown -join ', ')"
    Write-Host "Name test classes (optionally Class.Method), never a namespace, a folder or a whole project. CI runs the broad suites."
    exit 2
}

# project -> filter terms
$filters = @{}
foreach ($request in $requests) {
    $parts = $request -split '\.', 2
    foreach ($location in $found[$parts[0]]) {
        $qualified = if ($location.Namespace) { "$($location.Namespace).$($parts[0])" } else { $parts[0] }
        $terms = if ($parts.Count -gt 1) { @("FullyQualifiedName~$qualified.$($parts[1])") }
                 else { @("FullyQualifiedName~$qualified.", "FullyQualifiedName~$qualified+") }
        if (-not $filters.ContainsKey($location.Project)) { $filters[$location.Project] = @() }
        $filters[$location.Project] += $terms
    }
}

$failed = $false

function Invoke-Timed {
    param([string]$Name, [string[]]$Command, [int]$TimeoutSeconds)
    $clock = [System.Diagnostics.Stopwatch]::StartNew()
    $result = Invoke-Dotnet -Command $Command -TimeoutSeconds $TimeoutSeconds
    $clock.Stop()
    $output = $result.Output
    $ranNoTests = ($Command[0] -eq 'test') -and -not ($output | Where-Object { $_ -match '^(Passed|Failed)!' })
    $status = if ($result.TimedOut) { 'TIMEDOUT' } elseif ($result.Exit -ne 0) { 'FAILED' } elseif ($ranNoTests) { 'NOTESTS' } else { 'ok' }
    Write-Host ("{0,-8} {1,4:N0}s  {2}" -f $status, $clock.Elapsed.TotalSeconds, $Name)
    if ($result.TimedOut) { Write-Host "  killed after ${TimeoutSeconds}s with no result. Do not rerun it unchanged: narrow the class list or look for a spinning dotnet process." }
    if ($ranNoTests -and -not $result.TimedOut -and $result.Exit -eq 0) { Write-Host "  the filter matched no tests" }
    $output | Where-Object { $_ -match ': error ' } | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
    for ($i = 0; $i -lt $output.Count; $i++) {
        $line = $output[$i]
        if ($line -match '^(Passed|Failed)!' -or $line -match '^\s+Failed ') { Write-Host "  $($line.Trim())" }
        if ($line -match '^\s+Error Message:') {
            $output[($i + 1)..[Math]::Min($i + 3, $output.Count - 1)] | ForEach-Object { Write-Host "    $($_.Trim())" }
        }
    }
    if ($status -ne 'ok') { $script:failed = $true }
}

foreach ($project in $filters.Keys | Sort-Object) {
    $full = Join-Path $repo $project
    $extra = @("-p:SolutionDir=$repo\")
    if (-not $NoBuild) {
        Invoke-Timed -Name "build $project" -Command (@('build', $full, '-nologo', '-v:q') + $extra) -TimeoutSeconds $BuildTimeoutSeconds
        if ($failed) { break }
    }
    $filter = ($filters[$project] | Sort-Object -Unique) -join '|'
    Invoke-Timed -Name "test  $project" -Command (@('test', $full, '--no-build', '-nologo', '-v:m', '--filter', $filter) + $extra) -TimeoutSeconds $TestTimeoutSeconds
}

if ($failed) { Write-Host "`nFAST-TEST FAILED"; exit 1 }
Write-Host "`nFAST-TEST OK"
