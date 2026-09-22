# Purpose: Run Gum.Avalonia.Tests, retrying only the tests that failed, and leave TestResults/
#          holding one truthful full-suite .trx for the workflow's test reporter.
# Usage:   pwsh .github/scripts/run-avalonia-head-tests.ps1
#
# Avalonia's headless xunit host resolves Dispatcher.UIThread via an unsynchronized
# `s_uiThread ??= ...` (Avalonia.Base/Threading/Dispatcher.cs), so a test-session setup race can
# orphan the compositor for one test in a while - a still-open Avalonia bug (see
# Tests/Gum.Avalonia.Tests/Animations/README.md's Gotchas), not a Gum defect and not fixable here.
# The poisoning is scoped to that one test's isolated session, not to one Window, so only a fresh
# `dotnet test` process gets an independent roll - `s_uiThread` resets on process start.
#
# Retrying the whole assembly does not scale: ~90 tests each roll the dice independently, so the
# per-run failure odds compound with every test added, and each attempt costs a full ~90s. This
# retries only the exact tests the .trx says failed (a couple of seconds), so cost and odds stay
# tied to the number of flaky tests rather than the assembly's size. A genuine failure reproduces
# on every attempt and still fails the step.
#
# The reporter globs TestResults/*.trx and fails the job on any Failed result it finds there, so
# a retry that leaves the first attempt's .trx in that folder turns the job red even after the
# retry passes. Retry runs therefore write to TestResults/retries/ (out of the non-recursive glob)
# and their outcomes are merged back into the first attempt's report, which keeps the published
# report a full-suite result instead of a stale failure or a filtered handful of tests.

$ErrorActionPreference = 'Stop'

$resultsDirectory = 'TestResults'
$retryDirectory = Join-Path $resultsDirectory 'retries'
$baselineTrx = Join-Path $resultsDirectory 'avalonia-head.trx'
$maxAttempts = 4

function Read-Trx($path)
{
    [xml]$document = Get-Content -LiteralPath $path
    $namespaces = New-Object System.Xml.XmlNamespaceManager($document.NameTable)
    $namespaces.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    return [PSCustomObject]@{ Document = $document; Namespaces = $namespaces }
}

function Get-TestNames($trx, $outcome)
{
    return @($trx.Document.SelectNodes("//t:UnitTestResult[@outcome='$outcome']", $trx.Namespaces) |
        ForEach-Object { $_.testName })
}

# Flips the baseline report's entries for tests that passed on a later, filtered attempt, so the
# published report shows each test's final outcome rather than its first roll.
function Merge-RetryOutcomes($baselinePath, $retryPath)
{
    $baseline = Read-Trx $baselinePath
    $passedOnRetry = @{}
    foreach ($name in Get-TestNames (Read-Trx $retryPath) 'Passed')
    {
        $passedOnRetry[$name] = $true
    }

    $mergedCount = 0
    foreach ($result in $baseline.Document.SelectNodes("//t:UnitTestResult[@outcome='Failed']", $baseline.Namespaces))
    {
        if (-not $passedOnRetry.ContainsKey($result.testName)) { continue }

        $result.SetAttribute('outcome', 'Passed')
        $output = $result.SelectSingleNode('t:Output', $baseline.Namespaces)
        if ($output) { $result.RemoveChild($output) | Out-Null }
        $mergedCount++
    }
    if ($mergedCount -eq 0) { return }

    $counters = $baseline.Document.SelectSingleNode('//t:ResultSummary/t:Counters', $baseline.Namespaces)
    $remainingFailures = [int]$counters.failed - $mergedCount
    $counters.SetAttribute('failed', [string]$remainingFailures)
    $counters.SetAttribute('passed', [string]([int]$counters.passed + $mergedCount))
    if ($remainingFailures -eq 0)
    {
        $counters.ParentNode.SetAttribute('outcome', 'Completed')
    }

    $baseline.Document.Save((Resolve-Path -LiteralPath $baselinePath).ProviderPath)
}

$filterArgs = @()
for ($attempt = 1; $attempt -le $maxAttempts; $attempt++)
{
    $isRetry = $attempt -gt 1
    $trxName = if ($isRetry) { "avalonia-head-retry$attempt.trx" } else { Split-Path $baselineTrx -Leaf }
    $trxDirectory = if ($isRetry) { $retryDirectory } else { $resultsDirectory }
    $trxPath = Join-Path $trxDirectory $trxName

    dotnet test Tests/Gum.Avalonia.Tests/Gum.Avalonia.Tests.csproj --configuration Release `
        --logger "trx;LogFileName=$trxName" --results-directory $trxDirectory @filterArgs
    $exitCode = $LASTEXITCODE

    # A nonzero exit with no .trx, or none naming a failed test, is a build or host crash rather
    # than a test failure - re-running a filtered subset would not tell us anything new.
    if ($exitCode -ne 0 -and -not (Test-Path -LiteralPath $trxPath)) { exit $exitCode }

    if ($isRetry) { Merge-RetryOutcomes $baselineTrx $trxPath }
    if ($exitCode -eq 0) { exit 0 }

    $failedTests = Get-TestNames (Read-Trx $trxPath) 'Failed'
    if ($failedTests.Count -eq 0) { exit $exitCode }

    if ($attempt -eq $maxAttempts)
    {
        Write-Host "::error::Still failing after $maxAttempts attempts: $($failedTests -join ', ')"
        exit $exitCode
    }

    Write-Host "::warning::$($failedTests.Count) test(s) failed on attempt $attempt; retrying just those in case this was Avalonia's known headless dispatcher-setup race: $($failedTests -join ', ')"
    $filterArgs = @('--filter', (($failedTests | ForEach-Object { "FullyQualifiedName=$_" }) -join '|'))
}
