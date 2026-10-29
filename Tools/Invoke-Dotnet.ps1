# Dot-sourced by Tools/verify.ps1 and Tools/fast-test.ps1.
#
# Runs dotnet with a hard time limit. A hung test host (for example a raylib or MonoGame test that opens its
# window off the main thread on macOS, which spins at 100% CPU) otherwise stalls the whole run silently; on timeout the process tree is
# killed and the step is reported as TIMED OUT, which is not the same as a failing test.
function Invoke-Dotnet {
    param([string[]]$Command, [int]$TimeoutSeconds)
    $psi = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
    foreach ($argument in $Command) { $psi.ArgumentList.Add($argument) }
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    # Callers parse the classic console summary ("Passed!"/"Failed!"); the terminal logger, if an
    # environment turns it on, prints "Test summary:" instead and every run would read as NOTESTS.
    $psi.Environment['MSBUILDTERMINALLOGGER'] = 'off'
    $process = [System.Diagnostics.Process]::Start($psi)
    $standardOutput = $process.StandardOutput.ReadToEndAsync()
    $standardError = $process.StandardError.ReadToEndAsync()
    $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
    if ($timedOut) { $process.Kill($true); $process.WaitForExit() }
    $lines = (($standardOutput.Result + "`n" + $standardError.Result) -split "`r?`n") | ForEach-Object { "$_" }
    return @{ Output = $lines; Exit = $(if ($timedOut) { -1 } else { $process.ExitCode }); TimedOut = $timedOut }
}
