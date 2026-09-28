using System;
using System.Threading.Tasks;

namespace Gum.Avalonia.Diagnostics;

/// <summary>
/// Runs the head's startup steps and turns an exception from any of them into a reported failure:
/// stderr, and the main window's failure panel.
/// </summary>
public sealed class HeadStartupRun
{
    private readonly StartupFailureReporter _failureReporter;

    /// <summary>Creates a run that reports failures through <paramref name="failureReporter"/>.</summary>
    public HeadStartupRun(StartupFailureReporter failureReporter)
    {
        _failureReporter = failureReporter;
    }

    /// <summary>
    /// Runs <paramref name="steps"/>; returns their outcome, or
    /// <see cref="UnattendedStartupOutcome.Failed"/> after reporting what they threw.
    /// </summary>
    public async Task<UnattendedStartupOutcome> RunAsync(Func<Task<UnattendedStartupOutcome>> steps)
    {
        try
        {
            return await steps();
        }
        catch (Exception exception)
        {
            _failureReporter.Report(exception);
            return UnattendedStartupOutcome.Failed;
        }
    }
}
