using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Gum.Diagnostics;

/// <summary>
/// Diagnostic log for diagnosing slow, frozen or failing edits. In a Debug build it is always on and
/// writes to <see cref="DefaultPath"/>, <c>{temp folder}/Gum/Logs/tool-yyyyMMdd-HHmmss.log</c> named by the
/// time the tool started (Windows: <c>%TEMP%\Gum\Logs</c>), one file per session, so history is kept; a
/// Release build is off. The GUM_TOOL_LOG environment variable, when it
/// names an absolute file path, writes there instead in either build, moving that path's previous file
/// to <c>.previous</c>. Nothing prunes old logs; delete files older than a month by hand. Every line is flushed as it is written, so a
/// freeze leaves the log up to the stall. Unhandled and reported exceptions are written with their
/// stack (<see cref="WriteException"/>, called by the crash reporter). When off, every call returns at
/// once and allocates nothing. Lines look like
/// <c>   1234.5 t07 +   12.3ms  stage detail</c>: milliseconds since the first entry, thread id,
/// stage duration. Pass counts through <c>count</c> rather than building a detail string; guard any
/// string building with <see cref="IsEnabled"/>.
/// </summary>
public static class ToolLog
{
    private const string EnableVariable = "GUM_TOOL_LOG";

    private static readonly object _lock = new object();
    // Declared before _writer: static fields initialize in order and Open() reads DefaultPath.
    private static readonly string _sessionFileName = $"tool-{Process.GetCurrentProcess().StartTime:yyyyMMdd-HHmmss}.log";
    private static readonly StreamWriter? _writer = Open();
    private static readonly long _startTimestamp = Stopwatch.GetTimestamp();

    /// <summary>True when GUM_TOOL_LOG named a writable file.</summary>
    public static bool IsEnabled => _writer != null;

    /// <summary>Times the work between this call and the returned scope's disposal; one line is written on disposal.</summary>
    /// <param name="stage">What is running, such as "SetVariable".</param>
    /// <param name="count">An optional count shown after the stage (instances, rows); -1 for none.</param>
    /// <param name="detail">An optional constant or already-built text.</param>
    public static Scope Begin(string stage, int count = -1, string? detail = null)
    {
        if (_writer == null)
        {
            return default;
        }
        return new Scope(stage, count, detail, Stopwatch.GetTimestamp());
    }

    /// <summary>Writes one line without a duration.</summary>
    public static void Write(string stage, int count = -1, string? detail = null)
    {
        if (_writer == null)
        {
            return;
        }
        WriteLine(stage, count, detail, durationMilliseconds: -1);
    }

    /// <summary>The folder holding one log per tool session: <c>Gum/Logs</c> in the user's temp folder.</summary>
    public static string LogFolder => Path.Combine(Path.GetTempPath(), "Gum", "Logs");

    /// <summary>The file Debug builds write to: this session's <c>tool-yyyyMMdd-HHmmss.log</c> (the time the tool started) in <see cref="LogFolder"/>.</summary>
    public static string DefaultPath => Path.Combine(LogFolder, _sessionFileName);

    /// <summary>Writes an exception with its stack, as an <c>error</c> line. Never throws.</summary>
    public static void WriteException(string context, Exception exception)
    {
        if (_writer == null)
        {
            return;
        }
        WriteLine("error", -1, context + ": " + exception, durationMilliseconds: -1);
    }

    private static StreamWriter? Open() =>
        Open(Environment.GetEnvironmentVariable(EnableVariable), DiagnosticLogFile.IsDebugBuild, DefaultPath);

    /// <summary>Opens the log at the resolved path, or returns null when off or unopenable. Internal so tests can choose the build configuration and path.</summary>
    internal static StreamWriter? Open(string? environmentPath, bool isDebugBuild, string defaultPath)
    {
        // A relative GUM_TOOL_LOG is ignored (it would depend on the working directory).
        string? explicitPath = !string.IsNullOrWhiteSpace(environmentPath) && Path.IsPathRooted(environmentPath) ? environmentPath : null;
        string? path = DiagnosticLogFile.ResolvePath(explicitPath, defaultPath, isDebugBuild);
        if (path == null)
        {
            return null;
        }

        try
        {
            // Shared read so the file can be tailed while the tool runs.
            // The default file is unique to this session, so history is kept and nothing is rotated. A path
            // the user chose through GUM_TOOL_LOG keeps the old behavior of moving the previous run aside.
            return new StreamWriter(DiagnosticLogFile.Open(path, rotate: explicitPath != null)) { AutoFlush = true };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void WriteLine(string stage, int count, string? detail, double durationMilliseconds)
    {
        double elapsed = (Stopwatch.GetTimestamp() - _startTimestamp) * 1000.0 / Stopwatch.Frequency;
        int thread = Environment.CurrentManagedThreadId;
        lock (_lock)
        {
            try
            {
                _writer!.Write(elapsed.ToString("F1").PadLeft(9));
                _writer.Write(" t");
                _writer.Write(thread.ToString("D2"));
                if (durationMilliseconds >= 0)
                {
                    _writer.Write(" +");
                    _writer.Write(durationMilliseconds.ToString("F1").PadLeft(8));
                    _writer.Write("ms  ");
                }
                else
                {
                    _writer.Write("            ");
                }
                _writer.Write(stage);
                if (count >= 0)
                {
                    _writer.Write(" x");
                    _writer.Write(count);
                }
                if (detail != null)
                {
                    _writer.Write(' ');
                    _writer.Write(detail);
                }
                _writer.WriteLine();
            }
            catch (Exception)
            {
                // A diagnostic must never fail the tool.
            }
        }
    }

    /// <summary>The open timing of one stage; disposing it writes the line. The default value (log off) does nothing.</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly string? _stage;
        private readonly int _count;
        private readonly string? _detail;
        private readonly long _start;

        internal Scope(string stage, int count, string? detail, long start)
        {
            _stage = stage;
            _count = count;
            _detail = detail;
            _start = start;
        }

        public void Dispose()
        {
            if (_stage == null)
            {
                return;
            }
            double milliseconds = (Stopwatch.GetTimestamp() - _start) * 1000.0 / Stopwatch.Frequency;
            WriteLine(_stage, _count, _detail, milliseconds);
        }
    }
}
