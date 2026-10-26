<#
.SYNOPSIS
Builds once, then runs exactly the named test classes (or Class.Method) with --no-build, printing
the elapsed time of every step. Names that are not a test class (a namespace, a typo, a whole
project) are refused: CI runs the broad suites, a local run is only for the code under work.

.PARAMETER Tests
Test class names, or Class.Method, comma separated or as separate arguments. Project:Class (or
Project:Class.Method, Project being the csproj name) picks one project when a class name is in several,
and is the only way to run a test project no repo solution includes.

.PARAMETER NoBuild
Skip the build step (the projects were already built by a previous run).

.PARAMETER ListOnly
Print the projects and filters that would run, without building or testing.

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
    [switch]$ListOnly,
    [int]$BuildTimeoutSeconds = 300,
    [int]$TestTimeoutSeconds = 240
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo
. (Join-Path $PSScriptRoot 'Invoke-Dotnet.ps1')
. (Join-Path $PSScriptRoot 'TestProjects.ps1')

$requests = @($Tests | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

# Only test projects a repo solution includes are candidates, so a class in an orphan project (one
# no solution or CI builds) never triggers its build. A library named *Tests* is not a test project,
# and the frozen WPF head's tests run only when named as Project:Class.
$solutionProjects = @{}
foreach ($solution in @(Get-ChildItem -File -Path (Join-Path $repo '*'), (Join-Path $repo 'Tests/*') -Include '*.sln', '*.slnx' -ErrorAction SilentlyContinue)) {
    $solutionDir = Split-Path -Parent $solution.FullName
    $text = Get-Content -Raw $solution.FullName
    $paths = if ($solution.Extension -eq '.slnx') { [regex]::Matches($text, '<Project\s+Path="([^"]+\.csproj)"') }
             else { [regex]::Matches($text, '"([^"]+\.csproj)"') }
    foreach ($match in $paths) {
        $full = [System.IO.Path]::GetFullPath((Join-Path $solutionDir ($match.Groups[1].Value -replace '\\', '/')))
        $solutionProjects[([System.IO.Path]::GetRelativePath($repo, $full) -replace '\\', '/')] = $true
    }
}
$testProjects = @($solutionProjects.Keys | Where-Object {
        [System.IO.Path]::GetFileNameWithoutExtension($_) -notin $frozenTestProjects -and (Test-IsTestProject (Join-Path $repo $_))
    } | Sort-Object)

# A request is [Project:]Class[.Method]; Project is the csproj name without extension.
$parsed = foreach ($request in $requests) {
    $projectName = $null
    $rest = $request
    if ($request -match '^(?<project>[^:]+):(?<rest>.+)$') { $projectName = $Matches.project; $rest = $Matches.rest }
    $parts = $rest -split '\.', 2
    [pscustomobject]@{ Request = $request; Project = $projectName; Class = $parts[0]; Method = if ($parts.Count -gt 1) { $parts[1] } else { $null } }
}
# A project named explicitly by Project:Class is a candidate even when no repo solution includes it
# (the FRB pixel harness, the KniGumInCode Playwright tests), since naming it is the opt-in.
foreach ($name in @($parsed | Where-Object { $_.Project } | ForEach-Object { $_.Project } | Sort-Object -Unique)) {
    if ($testProjects | Where-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) -eq $name }) { continue }
    $testProjects += @(git ls-files -- "$name.csproj" "*/$name.csproj")
}
$classNames = $parsed | ForEach-Object { $_.Class } | Sort-Object -Unique
$classPattern = "class[[:space:]]+($($classNames -join '|'))([^A-Za-z0-9_]|$)"

# class name -> list of @{ Project; Namespace; Files }. git grep is used because a PowerShell scan of
# every test project's sources takes half a minute; --untracked finds a new file not yet added.
$found = @{}
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$projectDirs = $testProjects | ForEach-Object { @{ Project = $_; Dir = ((Split-Path -Parent $_) -replace '\\', '/') + '/' } }
foreach ($line in (git grep --untracked -nIE $classPattern -- '*.cs')) {
    if ($line -notmatch '^(?<file>[^:]+):\d+:.*class\s+(?<name>\w+)') { continue }
    $file = $Matches.file
    $name = $Matches.name
    if ($name -notin $classNames) { continue }
    $owner = $projectDirs | Where-Object { $file.StartsWith($_.Dir) } | Sort-Object { $_.Dir.Length } -Descending | Select-Object -First 1
    if (-not $owner) { continue }
    $namespaceMatch = Select-String -Path (Join-Path $repo $file) -Pattern '^\s*namespace\s+([\w.]+)' -List
    $namespace = if ($namespaceMatch) { $namespaceMatch.Matches[0].Groups[1].Value } else { '' }
    if (-not $found.ContainsKey($name)) { $found[$name] = @() }
    $existing = $found[$name] | Where-Object { $_.Project -eq $owner.Project -and $_.Namespace -eq $namespace }
    if ($existing) { $existing.Files += $file }
    else { $found[$name] += @{ Project = $owner.Project; Namespace = $namespace; Files = @($file) } }
}
Write-Host ("resolved      {0,4:N0}s  {1} request(s) against {2} test project(s)" -f $sw.Elapsed.TotalSeconds, $requests.Count, $testProjects.Count)

# request -> the locations it runs in
$targets = @{}
foreach ($item in $parsed) {
    $locations = @($found[$item.Class] | Where-Object { $_ })
    if ($item.Project) { $locations = @($locations | Where-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Project) -eq $item.Project }) }
    if ($item.Method -and $locations.Count -gt 1) {
        # A class in several projects: run Class.Method only where the method is declared, so the
        # others do not report NOTESTS. An inherited method is declared nowhere; keep every project then.
        $methodName = ($item.Method -split '[.(]')[0]
        $declaring = @($locations | Where-Object { Select-String -Path ($_.Files | ForEach-Object { Join-Path $repo $_ }) -Pattern "\b$([regex]::Escape($methodName))\s*\(" -Quiet })
        if ($declaring.Count -gt 0) { $locations = $declaring }
    }
    $targets[$item.Request] = $locations
}

$unknown = @($parsed | Where-Object { $targets[$_.Request].Count -eq 0 } | ForEach-Object { $_.Request })
if ($unknown.Count -gt 0) {
    Write-Host "REFUSED: not a test class in any test project: $($unknown -join ', ')"
    Write-Host "Name test classes (optionally Project:Class or Class.Method), never a namespace, a folder or a whole project. CI runs the broad suites. Only projects in a repo solution count, unless named as Project:Class."
    exit 2
}

# project -> filter terms
$filters = @{}
foreach ($item in $parsed) {
    $locations = $targets[$item.Request]
    if ($locations.Count -gt 1) { Write-Host "$($item.Request) is in $($locations.Count) projects, running it in: $(($locations | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Project) }) -join ', ') (Project:Class picks one)" }
    foreach ($location in $locations) {
        $qualified = if ($location.Namespace) { "$($location.Namespace).$($item.Class)" } else { $item.Class }
        $terms = if ($item.Method) { @("FullyQualifiedName~$qualified.$($item.Method)") }
                 else { @("FullyQualifiedName~$qualified.", "FullyQualifiedName~$qualified+") }
        if (-not $filters.ContainsKey($location.Project)) { $filters[$location.Project] = @() }
        $filters[$location.Project] += $terms
    }
}

if ($ListOnly) {
    foreach ($project in $filters.Keys | Sort-Object) { Write-Host "test  $project  $(($filters[$project] | Sort-Object -Unique) -join '|')" }
    exit 0
}

$failed = $false
$results = [System.Collections.Generic.List[string]]::new()

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
    if ($ranNoTests -and -not $result.TimedOut -and $result.Exit -eq 0) {
        # No "Passed!"/"Failed!" summary: either the filter matched nothing or the output format changed.
        # Show both so the cause is visible without a rerun.
        Write-Host "  no test summary in the output; filter: $($Command[[array]::IndexOf($Command, '--filter') + 1])"
        Write-Host "  last lines of dotnet test output:"
        $output | Where-Object { $_.Trim() } | Select-Object -Last 20 | ForEach-Object { Write-Host "    $_" }
    }
    $output | Where-Object { $_ -match ': error ' } | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
    for ($i = 0; $i -lt $output.Count; $i++) {
        $line = $output[$i]
        if ($line -match '^(Passed|Failed)!' -or $line -match '^\s+Failed ') { Write-Host "  $($line.Trim())" }
        # The whole message up to the stack trace: a Shouldly "because" text comes after the
        # expected/actual lines, under "Additional Info:".
        if ($line -match '^\s+Error Message:') {
            for ($j = $i + 1; $j -lt $output.Count; $j++) {
                $messageLine = $output[$j]
                if ($messageLine -match '^\s+(Stack Trace:|Standard Output Messages:|Failed |Passed |Skipped )' -or $messageLine -match '^(Passed|Failed)!') { break }
                if ($messageLine.Trim()) { Write-Host "    $($messageLine.Trim())" }
            }
        }
    }
    $script:results.Add(("{0,-8} {1}" -f $status, $Name))
    if ($status -ne 'ok') { $script:failed = $true }
    return $status
}

foreach ($project in $filters.Keys | Sort-Object) {
    $full = Join-Path $repo $project
    $extra = @("-p:SolutionDir=$repo\")
    if (-not $NoBuild) {
        # A failed build skips only this project's tests; the other projects still build and run.
        $buildStatus = Invoke-Timed -Name "build $project" -Command (@('build', $full, '-nologo', '-v:q') + $extra) -TimeoutSeconds $BuildTimeoutSeconds
        if ($buildStatus -ne 'ok') {
            Write-Host "skipped        test  $project`: its build failed"
            $results.Add("skipped  test  $project")
            continue
        }
    }
    $filter = ($filters[$project] | Sort-Object -Unique) -join '|'
    $null = Invoke-Timed -Name "test  $project" -Command (@('test', $full, '--no-build', '-nologo', '-v:m', '--filter', $filter) + $extra) -TimeoutSeconds $TestTimeoutSeconds
}

if ($results.Count -gt 1) { Write-Host "`nResults:"; $results | ForEach-Object { Write-Host "  $_" } }
if ($failed) { Write-Host "`nFAST-TEST FAILED"; exit 1 }
Write-Host "`nFAST-TEST OK"
