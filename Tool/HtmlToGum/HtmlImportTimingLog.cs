using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HtmlToGumPlugin;

/// <summary>One measured phase of an HTML import.</summary>
public sealed class ImportTimingPhase
{
    /// <summary>Phase label, e.g. "converter process" or "primary.goto".</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>Wall-clock duration of the phase.</summary>
    [JsonPropertyName("ms")]
    public long Milliseconds { get; set; }
}

/// <summary>Contents of the timings.json convert.ts writes into its staging folder.</summary>
public sealed class ConverterTimings
{
    /// <summary>Converter phases in the order they ran.</summary>
    [JsonPropertyName("phases")]
    public List<ImportTimingPhase> Phases { get; set; } = new();

    /// <summary>Page-size figures (node/instance/image/font counts) a run can be normalized against.</summary>
    [JsonPropertyName("counts")]
    public Dictionary<string, long> Counts { get; set; } = new();

    /// <summary>Total converter wall clock, which exceeds the sum of the phases by untimed glue.</summary>
    [JsonPropertyName("totalMs")]
    public long TotalMilliseconds { get; set; }
}

/// <summary>A single Content → Import → HTML run, converter and plugin phases together.</summary>
public sealed class ImportTimingRun
{
    /// <summary>When the import finished.</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Imported URL or local HTML path.</summary>
    public string Source { get; set; } = "";

    /// <summary>Name of the screen the import produced.</summary>
    public string ScreenName { get; set; } = "";

    /// <summary>Converter viewport width.</summary>
    public int ViewportWidth { get; set; }

    /// <summary>Converter viewport height.</summary>
    public int ViewportHeight { get; set; }

    /// <summary>False when the import ran with --no-responsive, which skips the training passes.</summary>
    public bool Responsive { get; set; }

    /// <summary>Plugin-side phases in the order they ran.</summary>
    public List<ImportTimingPhase> PluginPhases { get; set; } = new();

    /// <summary>Total plugin wall clock, including the converter process it waited on.</summary>
    public long PluginTotalMilliseconds { get; set; }

    /// <summary>Converter phases, or null when the converter did not get far enough to write them.</summary>
    public ConverterTimings? ConverterTimings { get; set; }
}

/// <summary>
/// Collects plugin-side phase durations for one import. Phases are recorded even when the
/// measured work throws, so a failed import still shows where its time went.
/// </summary>
public sealed class ImportPhaseRecorder
{
    private readonly List<ImportTimingPhase> _phases;
    private readonly Func<TimeSpan> _elapsed;
    private long _total;

    /// <summary>Starts recording against a wall clock.</summary>
    public ImportPhaseRecorder() : this(StartStopwatch())
    {
    }

    /// <summary>Starts recording against <paramref name="elapsed"/>, which reports time since the run began.</summary>
    public ImportPhaseRecorder(Func<TimeSpan> elapsed)
    {
        _phases = new List<ImportTimingPhase>();
        _elapsed = elapsed;
        _total = 0;
    }

    private static Func<TimeSpan> StartStopwatch()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        return () => stopwatch.Elapsed;
    }

    /// <summary>Recorded phases, in the order they completed.</summary>
    public IReadOnlyList<ImportTimingPhase> Phases => _phases;

    /// <summary>
    /// Milliseconds from construction to the end of the most recently recorded phase. Stops at
    /// the last phase rather than reading the clock, so time the user spends on a modal dialog
    /// afterwards doesn't land in the run's total.
    /// </summary>
    public long Total => _total;

    /// <summary>Runs <paramref name="work"/>, recording its duration as <paramref name="name"/>.</summary>
    public void Measure(string name, Action work)
    {
        Measure(name, () =>
        {
            work();
            return true;
        });
    }

    /// <inheritdoc cref="Measure(string, Action)"/>
    public T Measure<T>(string name, Func<T> work)
    {
        TimeSpan start = _elapsed();
        try
        {
            return work();
        }
        finally
        {
            Record(name, start);
        }
    }

    /// <inheritdoc cref="Measure(string, Action)"/>
    public async Task<T> MeasureAsync<T>(string name, Func<Task<T>> work)
    {
        TimeSpan start = _elapsed();
        try
        {
            return await work().ConfigureAwait(true);
        }
        finally
        {
            Record(name, start);
        }
    }

    private void Record(string name, TimeSpan start)
    {
        TimeSpan end = _elapsed();
        _total = (long)end.TotalMilliseconds;
        _phases.Add(new ImportTimingPhase
        {
            Name = name,
            Milliseconds = (long)(end - start).TotalMilliseconds,
        });
    }
}

/// <summary>
/// Formats and appends the per-import timing log. HTML import spans a Chromium converter
/// process and a stretch of UI-thread work; without a per-phase record a slow import can only
/// be guessed at.
/// </summary>
public static class HtmlImportTimingLog
{
    private const string ConverterTimingsFileName = "timings.json";

    /// <summary>Log the plugin appends one entry to per import, next to its import-prefs.json.</summary>
    public static string LogPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HtmlToGumPlugin", "import-timings.log");

    /// <summary>Reads the converter's timings.json out of its staging folder; null if it wasn't written.</summary>
    public static ConverterTimings? TryReadConverterTimings(string stageDir)
    {
        try
        {
            string path = Path.Combine(stageDir, ConverterTimingsFileName);
            return File.Exists(path) ? ParseConverterTimings(File.ReadAllText(path)) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Parses converter timings JSON, returning null rather than throwing on malformed input.</summary>
    public static ConverterTimings? ParseConverterTimings(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ConverterTimings>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Renders one run as an aligned block so phases line up when runs are compared.</summary>
    public static string Format(ImportTimingRun run)
    {
        StringBuilder sb = new();
        sb.AppendLine(
            $"=== {run.TimestampUtc:yyyy-MM-dd HH:mm:ss}Z  screen={run.ScreenName}  " +
            $"viewport={run.ViewportWidth}x{run.ViewportHeight}  " +
            $"responsive={(run.Responsive ? "on" : "off")}");
        sb.AppendLine($"    source={run.Source}");

        if (run.ConverterTimings is null)
        {
            sb.AppendLine($"{"converter",-11}(no {ConverterTimingsFileName} — converter did not finish)");
        }
        else
        {
            foreach (ImportTimingPhase phase in run.ConverterTimings.Phases)
            {
                AppendPhase(sb, "converter", phase.Name, phase.Milliseconds);
            }
            AppendPhase(sb, "converter", "(total)", run.ConverterTimings.TotalMilliseconds);
        }

        foreach (ImportTimingPhase phase in run.PluginPhases)
        {
            AppendPhase(sb, "plugin", phase.Name, phase.Milliseconds);
        }
        AppendPhase(sb, "plugin", "(total)", run.PluginTotalMilliseconds);

        if (run.ConverterTimings is { Counts.Count: > 0 })
        {
            string counts = string.Join(" ", run.ConverterTimings.Counts.Select(c => $"{c.Key}={c.Value}"));
            sb.AppendLine($"{"counts",-11}{counts}");
        }

        return sb.ToString();
    }

    /// <summary>Appends an entry to the log. Best-effort — a failure here never fails an import.</summary>
    public static void Append(string logPath, string entry)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, entry + Environment.NewLine);
        }
        catch
        {
            // Timing log is diagnostic only.
        }
    }

    private static void AppendPhase(StringBuilder sb, string origin, string name, long milliseconds)
    {
        sb.AppendLine($"{origin,-11}{name,-30}{milliseconds,8} ms");
    }
}
