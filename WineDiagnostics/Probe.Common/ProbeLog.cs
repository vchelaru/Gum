using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Probe.Common;

/// <summary>
/// Minimal logging + run-wrapper shared by every Wine diagnostic probe. Each probe calls
/// <see cref="Run"/> from its entry point; the wrapper writes a structured log (header, steps,
/// exceptions, and a final RESULT line) to both the console and a per-probe log file so a macOS
/// user can attach the output to a bug report.
/// </summary>
/// <remarks>
/// Static by design: this is a throwaway diagnostic tool, not part of Gum's DI architecture, so
/// the usual "every helper gets an I-prefixed interface" rule in <c>code-style.md</c> does not apply.
/// </remarks>
public static class ProbeLog
{
    private static string _logFilePath = "";
    private static bool _failed;
    private static Exception? _firstError;

    /// <summary>
    /// Runs <paramref name="body"/> inside a try/catch, writing a structured log around it.
    /// Returns 0 if the probe completed without recording a failure, otherwise 1 - return this
    /// directly from Main so the runner script can read the result from the process exit code.
    /// </summary>
    public static int Run(string probeName, Action body)
    {
        Begin(probeName);
        try
        {
            body();
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
        }
        return End();
    }

    /// <summary>Logs a single named step within a probe.</summary>
    public static void Step(string message)
    {
        Write("  STEP : " + message);
    }

    /// <summary>Logs a key/value diagnostic detail (adapter name, feature level, version, etc.).</summary>
    public static void Info(string key, string value)
    {
        Write("  INFO : " + key + " = " + value);
    }

    /// <summary>
    /// Records a failure - an exception caught by the probe, or one surfaced from a UI message
    /// loop via an unhandled-exception hook. The first error is remembered and the final RESULT
    /// line will report FAIL.
    /// </summary>
    public static void RecordFailure(Exception ex)
    {
        _failed = true;
        if (_firstError == null)
        {
            _firstError = ex;
        }
        Write("  ERROR: " + ex.GetType().FullName + ": " + ex.Message);
        Write(ex.ToString());
    }

    private static void Begin(string probeName)
    {
        string? configured = Environment.GetEnvironmentVariable("PROBE_LOG_DIR");
        string directory = string.IsNullOrWhiteSpace(configured) ? AppContext.BaseDirectory : configured;
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch
        {
            // If the configured directory can't be created, fall back to the working directory.
            directory = ".";
        }
        _logFilePath = Path.Combine(directory, probeName + ".log");

        Write("============================================================");
        Write("PROBE: " + probeName);
        Write("TIME : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        Write("OS   : " + RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")");
        Write("PROC : " + RuntimeInformation.ProcessArchitecture);
        Write("FX   : " + RuntimeInformation.FrameworkDescription);
        Write("============================================================");
    }

    private static int End()
    {
        Write(_failed ? "RESULT: FAIL" : "RESULT: PASS");
        Write("");
        return _failed ? 1 : 0;
    }

    private static void Write(string line)
    {
        try
        {
            Console.WriteLine(line);
        }
        catch
        {
            // Ignore - WinExe probes may have no attached console.
        }
        try
        {
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never throw and mask the actual probe result.
        }
    }
}
