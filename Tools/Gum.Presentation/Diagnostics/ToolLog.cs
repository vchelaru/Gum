using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Gum.Diagnostics;

/// <summary>
/// Opt-in diagnostic log for diagnosing slow or frozen edits. Off (every call returns at once and
/// allocates nothing) unless the GUM_TOOL_LOG environment variable names an absolute file path;
/// the file is replaced on each run and every line is flushed as it is written, so a freeze
/// leaves the log up to the stall. Lines look like
/// <c>   1234.5 t07 +   12.3ms  stage detail</c>: milliseconds since the first entry, thread id,
/// stage duration. Pass counts through <c>count</c> rather than building a detail string; guard any
/// string building with <see cref="IsEnabled"/>.
/// </summary>
public static class ToolLog
{
    private const string EnableVariable = "GUM_TOOL_LOG";

    private static readonly object _lock = new object();
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

    private static StreamWriter? Open()
    {
        string? path = Environment.GetEnvironmentVariable(EnableVariable);
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
        {
            return null;
        }

        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            // Shared read so the file can be tailed while the tool runs.
            FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            return new StreamWriter(stream) { AutoFlush = true };
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
