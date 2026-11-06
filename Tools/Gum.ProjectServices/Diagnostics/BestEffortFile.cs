using System;
using System.IO;
using System.Threading;

namespace Gum.Diagnostics;

/// <summary>
/// Runs a diagnostic file operation (marker, sentinel) that must never stop startup. A file another
/// running Gum holds open is retried briefly, then given up on silently.
/// </summary>
public static class BestEffortFile
{
    private const int Attempts = 3;
    private const int DelayMilliseconds = 50;

    /// <summary>Runs <paramref name="action"/>; returns false if it still failed after the retries.</summary>
    public static bool TryRun(Action action) => TryRun(action, () => Thread.Sleep(DelayMilliseconds));

    /// <summary>Runs <paramref name="action"/>, calling <paramref name="waitBeforeRetry"/> between attempts instead of sleeping.</summary>
    public static bool TryRun(Action action, Action waitBeforeRetry)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                if (attempt == Attempts)
                {
                    System.Diagnostics.Debug.WriteLine("Best-effort file operation gave up: " + exception.Message);
                    return false;
                }

                waitBeforeRetry();
            }
        }
    }
}
