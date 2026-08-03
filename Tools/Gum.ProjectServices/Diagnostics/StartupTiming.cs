using System;
using System.Diagnostics;
using System.IO;

namespace Gum.Diagnostics;

/// <summary>
/// Process-wide facade over <see cref="StartupTimingLog"/> that attributes cold-start cost to
/// individual initialization steps. Disabled (a no-op, no file IO) unless the GUM_STARTUP_TIMING
/// environment variable is set to "1" - which logs to %TEMP%\gum_startup_timing.log - or to an
/// explicit file path.
/// </summary>
public static class StartupTiming
{
    private const string EnableVariable = "GUM_STARTUP_TIMING";
    private const string DefaultFileName = "gum_startup_timing.log";

    private static readonly object _fileLock = new object();
    private static readonly Stopwatch _stopwatch;
    private static readonly StartupTimingLog _log;
    private static readonly string? _filePath;
    private static readonly double _millisecondsBeforeFirstMark;

    /// <inheritdoc cref="StartupTimingLog.IsEnabled"/>
    public static bool IsEnabled => _log.IsEnabled;

    /// <summary>
    /// Milliseconds since the OS process started, including runtime startup before any mark was
    /// recorded. Use this for the enclosing total that the individual marks must add up to.
    /// </summary>
    public static double MillisecondsSinceProcessStart =>
        _millisecondsBeforeFirstMark + _stopwatch.ElapsedMilliseconds;

    static StartupTiming()
    {
        _stopwatch = Stopwatch.StartNew();

        string? setting = Environment.GetEnvironmentVariable(EnableVariable);
        if (string.IsNullOrWhiteSpace(setting) || setting == "0")
        {
            _filePath = null;
            _millisecondsBeforeFirstMark = 0;
            _log = new StartupTimingLog(sink: null, () => _stopwatch.ElapsedMilliseconds);
            return;
        }

        _filePath = setting is "1" or "true"
            ? Path.Combine(Path.GetTempPath(), DefaultFileName)
            : setting;
        _log = new StartupTimingLog(Append, () => _stopwatch.ElapsedMilliseconds);

        try
        {
            _millisecondsBeforeFirstMark = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
        }
        catch (Exception)
        {
            // Process start time is unavailable under some platforms/permissions; the relative
            // marks are still useful without it.
            _millisecondsBeforeFirstMark = 0;
        }

        Append($"--- Gum startup {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---");
        Append($"{_millisecondsBeforeFirstMark,8:0} ms  (runtime startup before first mark)");
    }

    /// <inheritdoc cref="StartupTimingLog.Mark"/>
    public static void Mark(string label) => _log.Mark(label);

    /// <inheritdoc cref="StartupTimingLog.MarkOnce"/>
    public static void MarkOnce(string label) => _log.MarkOnce(label);

    /// <inheritdoc cref="StartupTimingLog.Log"/>
    public static void Log(string message) => _log.Log(message);

    /// <inheritdoc cref="StartupTimingLog.Time"/>
    public static IDisposable Time(string label) => _log.Time(label);

    private static void Append(string line)
    {
        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_filePath!, line + Environment.NewLine);
            }
        }
        catch (IOException)
        {
            // Diagnostics must never take the app down.
        }
    }
}
