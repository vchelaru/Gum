using System;
using System.Diagnostics;
using System.IO;

namespace Gum.Diagnostics;

/// <summary>
/// TEMPORARY instrumentation for issue #4283 (evaluating a WPF displayer control warm-up pass).
/// Writes wall-clock-since-start timestamps to a fixed log file so before/after startup timing
/// can be compared without asking anyone to relay console output. Remove once #4283 is resolved.
/// </summary>
internal static class StartupPerfLog
{
    private static readonly string LogFilePath = Path.Combine(Path.GetTempPath(), "gum_startup_timing.log");
    private static readonly Stopwatch Stopwatch = new();

    /// <summary>
    /// Starts the stopwatch and truncates the log file. Must be called once, as early as possible
    /// in <c>Main</c>, before anything else runs.
    /// </summary>
    public static void Start()
    {
        Stopwatch.Restart();
        File.WriteAllText(LogFilePath, string.Empty);
    }

    /// <summary>
    /// Appends a timestamped line (milliseconds since <see cref="Start"/>) to the log file.
    /// Never throws - a diagnostic write must not be able to affect real app behavior.
    /// </summary>
    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFilePath, $"{Stopwatch.ElapsedMilliseconds,6}ms | {message}{Environment.NewLine}");
        }
        catch
        {
            // Best-effort diagnostic logging only.
        }
    }
}
