using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Gum.Mvvm.Benchmarks.Wasm;

/// <summary>Run settings; the page URL can override them: ?rounds=5&amp;target=150&amp;types=int,string.</summary>
public sealed record Options(int Rounds, double TargetMs, string[] Types)
{
    public static Options Default { get; } = new(5, 150, new[] { "int", "string" });

    public static Options Parse(string query)
    {
        Options options = Default;
        foreach (string pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
            switch (parts[0])
            {
                case "rounds" when int.TryParse(value, CultureInfo.InvariantCulture, out int rounds) && rounds > 0:
                    options = options with { Rounds = rounds };
                    break;
                case "target" when double.TryParse(value, CultureInfo.InvariantCulture, out double target) && target > 0:
                    options = options with { TargetMs = target };
                    break;
                case "types" when value.Length > 0:
                    options = options with { Types = value.Split(',', StringSplitOptions.RemoveEmptyEntries) };
                    break;
            }
        }
        return options;
    }
}

sealed class Subscriber
{
    private long _count;

    public void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => _count++;
}

/// <summary>
/// The desktop benchmark's cases (get, set same, set new, set new + subscriber, set new + [DependsOn]) for the old
/// by-name API, keyed, ref-field, ref-field + key and property-object styles, run on the Mono interpreter.
/// Case names are "style.op".
/// </summary>
public static class WasmBench
{
    public static readonly string[] Styles = { "plain", "old", "keyed", "ref", "ref+key", "obj" };
    public static readonly string[] Ops = { "get", "setSame", "setNew", "setNewSub", "setNewDep" };

    private static readonly Subscriber _subscriber = new();

    public static async Task RunAndPublishAsync(IJSRuntime js)
    {
        try
        {
            string query = await js.InvokeAsync<string>("gumBench.query");
            Options options = Options.Parse(query);
            string json = await RunAsync(options, js);
            await js.InvokeVoidAsync("gumBench.done", json, BuildHtml(json));
        }
        catch (Exception exception)
        {
            await js.InvokeVoidAsync("gumBench.fail", exception.ToString());
        }
    }

    private static void Add(List<BenchCase> cases, string type, string name, Action<int> body)
    {
        cases.Add(new BenchCase(type, name, body));
    }

    private static void AddType<T, TVm, TPlain>(
        List<BenchCase> cases, string type, Func<BenchViewModel, TVm> viewModelAccessor,
        Func<PlainModel, TPlain> plainAccessor, T a, T b)
        where TVm : struct, IAccessor<T>
        where TPlain : struct, IAccessor<T>
    {
        TPlain plain = plainAccessor(new PlainModel());
        Add(cases, type, "plain.get", n => Loops.Get<T, TPlain>(plain, n));
        Add(cases, type, "plain.setSame", n => Loops.SetSame(plain, a, n));
        Add(cases, type, "plain.setNew", n => Loops.SetNew(plain, a, b, n));

        // Old by-name API.
        TVm viewModel = viewModelAccessor(new BenchViewModel());
        BenchViewModel subscribed = new();
        subscribed.PropertyChanged += _subscriber.OnPropertyChanged;
        TVm withSubscriber = viewModelAccessor(subscribed);
        TVm withDependent = viewModelAccessor(new BenchViewModelWithDependents());
        Add(cases, type, "old.get", n => Loops.Get<T, TVm>(viewModel, n));
        Add(cases, type, "old.setSame", n => Loops.SetSame(viewModel, a, n));
        Add(cases, type, "old.setNew", n => Loops.SetNew(viewModel, a, b, n));
        Add(cases, type, "old.setNewSub", n => Loops.SetNew(withSubscriber, a, b, n));
        Add(cases, type, "old.setNewDep", n => Loops.SetNew(withDependent, a, b, n));

        // Keyed style (#6395).
        ViewModelProperty<T> key = KeyedBenchViewModel.KeyFor<T>(type);
        KeyedBenchViewModel keyedSubscribed = new();
        keyedSubscribed.PropertyChanged += _subscriber.OnPropertyChanged;
        KeyedAccessor<T> keyedAccessor = new(new KeyedBenchViewModel(), key);
        KeyedAccessor<T> keyedSubAccessor = new(keyedSubscribed, key);
        KeyedAccessor<T> keyedDepAccessor = new(new KeyedBenchViewModelWithDependents(), key);
        Add(cases, type, "keyed.get", n => Loops.Get<T, KeyedAccessor<T>>(keyedAccessor, n));
        Add(cases, type, "keyed.setSame", n => Loops.SetSame(keyedAccessor, a, n));
        Add(cases, type, "keyed.setNew", n => Loops.SetNew(keyedAccessor, a, b, n));
        Add(cases, type, "keyed.setNewSub", n => Loops.SetNew(keyedSubAccessor, a, b, n));
        Add(cases, type, "keyed.setNewDep", n => Loops.SetNew(keyedDepAccessor, a, b, n));

        // Ref-field style (#6397): Set(ref _v, value).
        RefVm<T> refSubscribed = new(a);
        refSubscribed.PropertyChanged += _subscriber.OnPropertyChanged;
        RefAccessor<T> refAccessor = new(new RefVm<T>(a));
        RefAccessor<T> refSubAccessor = new(refSubscribed);
        RefAccessor<T> refDepAccessor = new(new RefVmWithDependent<T>(a));
        Add(cases, type, "ref.get", n => Loops.Get<T, RefAccessor<T>>(refAccessor, n));
        Add(cases, type, "ref.setSame", n => Loops.SetSame(refAccessor, a, n));
        Add(cases, type, "ref.setNew", n => Loops.SetNew(refAccessor, a, b, n));
        Add(cases, type, "ref.setNewSub", n => Loops.SetNew(refSubAccessor, a, b, n));
        Add(cases, type, "ref.setNewDep", n => Loops.SetNew(refDepAccessor, a, b, n));

        // Ref-field + key: differs from "ref" only when an event is raised, so get/setSame/setNew are not run.
        RefKeyVm<T> refKeySubscribed = new(a);
        refKeySubscribed.PropertyChanged += _subscriber.OnPropertyChanged;
        RefKeyAccessor<T> refKeySubAccessor = new(refKeySubscribed);
        RefKeyAccessor<T> refKeyDepAccessor = new(new RefKeyVmWithDependent<T>(a));
        Add(cases, type, "ref+key.setNewSub", n => Loops.SetNew(refKeySubAccessor, a, b, n));
        Add(cases, type, "ref+key.setNewDep", n => Loops.SetNew(refKeyDepAccessor, a, b, n));

        // Property object (ViewModelField<T>).
        ObjVm<T> objSubscribed = new(a);
        objSubscribed.PropertyChanged += _subscriber.OnPropertyChanged;
        ObjAccessor<T> objAccessor = new(new ObjVm<T>(a));
        ObjAccessor<T> objSubAccessor = new(objSubscribed);
        ObjAccessor<T> objDepAccessor = new(new ObjVmWithDependent<T>(a));
        Add(cases, type, "obj.get", n => Loops.Get<T, ObjAccessor<T>>(objAccessor, n));
        Add(cases, type, "obj.setSame", n => Loops.SetSame(objAccessor, a, n));
        Add(cases, type, "obj.setNew", n => Loops.SetNew(objAccessor, a, b, n));
        Add(cases, type, "obj.setNewSub", n => Loops.SetNew(objSubAccessor, a, b, n));
        Add(cases, type, "obj.setNewDep", n => Loops.SetNew(objDepAccessor, a, b, n));
    }

    private static List<BenchCase> BuildCases(Options options)
    {
        List<BenchCase> cases = new();
        bool Selected(string type) => options.Types.Contains(type, StringComparer.OrdinalIgnoreCase);
        if (Selected("int"))
        {
            AddType(cases, "int", m => new VmInt(m), m => new PlainInt(m), 1, 2);
        }
        if (Selected("string"))
        {
            AddType(cases, "string", m => new VmText(m), m => new PlainText(m), Values.TextA, Values.TextB);
        }
        if (Selected("float"))
        {
            AddType(cases, "float", m => new VmFloat(m), m => new PlainFloat(m), 1f, 2f);
        }
        if (Selected("class"))
        {
            AddType(cases, "class", m => new VmReference(m), m => new PlainReference(m), Values.ReferenceA, Values.ReferenceB);
        }
        return cases;
    }

    /// <summary>Grows the loop until it takes a measurable time (this is also the warmup), then sizes a run to the target.</summary>
    private static void Calibrate(BenchCase benchCase, double targetMs)
    {
        int n = 64;
        double ms;
        while (true)
        {
            long start = Stopwatch.GetTimestamp();
            benchCase.Body(n);
            ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (ms >= 25 || n >= 1 << 28)
            {
                break;
            }
            n *= 4;
        }

        // One more timed run now that the code is warm.
        long warmStart = Stopwatch.GetTimestamp();
        benchCase.Body(n);
        ms = Math.Max(Stopwatch.GetElapsedTime(warmStart).TotalMilliseconds, 0.001);
        benchCase.Iterations = (int)Math.Clamp(n * targetMs / ms, 16, 1 << 28);
    }

    private static bool AllocationIsTracked()
    {
        try
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            object[] sink = new object[64];
            for (int i = 0; i < sink.Length; i++)
            {
                sink[i] = new byte[128];
            }
            GC.KeepAlive(sink);
            return GC.GetAllocatedBytesForCurrentThread() - before >= 64 * 128;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<string> RunAsync(Options options, IJSRuntime js)
    {
        long runStart = Stopwatch.GetTimestamp();
        List<BenchCase> cases = BuildCases(options);
        bool trackAllocation = AllocationIsTracked();

        for (int i = 0; i < cases.Count; i++)
        {
            Calibrate(cases[i], options.TargetMs);
            await js.InvokeVoidAsync("gumBench.progress", $"Calibrating {i + 1}/{cases.Count}: {cases[i].Type} {cases[i].Name}");
        }

        for (int round = 0; round < options.Rounds; round++)
        {
            // Alternate the order every round so a slow patch does not always land on the same cases.
            for (int i = 0; i < cases.Count; i++)
            {
                BenchCase benchCase = cases[round % 2 == 0 ? i : cases.Count - 1 - i];
                RunOnce(benchCase, trackAllocation);
                if (i % 4 == 3)
                {
                    await js.InvokeVoidAsync("gumBench.progress", $"Round {round + 1}/{options.Rounds}: {i + 1}/{cases.Count} cases");
                }
            }
        }

        double seconds = Stopwatch.GetElapsedTime(runStart).TotalSeconds;
        return BuildJson(options, cases, trackAllocation, seconds);
    }

    private static void RunOnce(BenchCase benchCase, bool trackAllocation)
    {
        int iterations = benchCase.Iterations;
        long allocatedBefore = trackAllocation ? GC.GetAllocatedBytesForCurrentThread() : 0;
        long start = Stopwatch.GetTimestamp();
        benchCase.Body(iterations);
        double elapsedNs = Stopwatch.GetElapsedTime(start).TotalMilliseconds * 1e6;
        long allocated = trackAllocation ? GC.GetAllocatedBytesForCurrentThread() - allocatedBefore : 0;
        benchCase.Nanoseconds.Add(elapsedNs / iterations);
        benchCase.Bytes.Add((double)allocated / iterations);
    }

    private static string BuildJson(Options options, List<BenchCase> cases, bool trackAllocation, double seconds)
    {
        bool debug = false;
#if DEBUG
        debug = true;
#endif
        using MemoryStream stream = new();
        using (Utf8JsonWriter w = new(stream, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteStartObject("meta");
            w.WriteString("runtime", RuntimeInformation.FrameworkDescription);
            w.WriteString("os", RuntimeInformation.OSDescription);
            w.WriteString("architecture", RuntimeInformation.ProcessArchitecture.ToString());
            w.WriteString("configuration", debug ? "Debug" : "Release");
            w.WriteBoolean("dynamicCodeCompiled", System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled);
            w.WriteNumber("rounds", options.Rounds);
            w.WriteNumber("targetMsPerRound", options.TargetMs);
            w.WriteBoolean("allocationTracked", trackAllocation);
            w.WriteNumber("durationSeconds", Math.Round(seconds, 1));
            w.WriteString("unit", "median ns/op over the rounds; bytes/op null when allocation is not tracked");
            w.WriteEndObject();

            w.WriteStartArray("results");
            foreach (BenchCase c in cases)
            {
                Result result = Harness.Summarize(c);
                int dot = c.Name.IndexOf('.');
                w.WriteStartObject();
                w.WriteString("type", c.Type);
                w.WriteString("style", c.Name[..dot]);
                w.WriteString("op", c.Name[(dot + 1)..]);
                w.WriteNumber("nsPerOp", Math.Round(result.Ns, 2));
                if (trackAllocation)
                {
                    w.WriteNumber("bytesPerOp", Math.Round(result.Bytes, 1));
                }
                else
                {
                    w.WriteNull("bytesPerOp");
                }
                w.WriteNumber("iterationsPerRound", c.Iterations);
                w.WriteNumber("spreadFraction", Math.Round(result.SpreadFraction, 3));
                w.WriteStartArray("roundNs");
                foreach (double ns in c.Nanoseconds)
                {
                    w.WriteNumberValue(Math.Round(ns, 2));
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string BuildHtml(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement meta = root.GetProperty("meta");
        List<JsonElement> results = root.GetProperty("results").EnumerateArray().ToList();
        bool tracked = meta.GetProperty("allocationTracked").GetBoolean();

        StringBuilder sb = new();
        sb.Append("<p>")
            .Append(System.Net.WebUtility.HtmlEncode(
                $"{meta.GetProperty("runtime").GetString()}, {meta.GetProperty("configuration").GetString()}, " +
                $"dynamic code compiled: {meta.GetProperty("dynamicCodeCompiled").GetBoolean()} (false = interpreter). " +
                $"{meta.GetProperty("rounds").GetInt32()} alternating rounds of about {meta.GetProperty("targetMsPerRound").GetDouble():0} ms per case, " +
                $"median. Total {meta.GetProperty("durationSeconds").GetDouble():0.0} s."))
            .Append("</p>");

        string[] types = results.Select(r => r.GetProperty("type").GetString()!).Distinct().ToArray();

        sb.Append("<h2>ns/op (lower is better; x = ratio to old by-name)</h2>");
        AppendTable(sb, results, types, "nsPerOp", ratio: true);
        if (tracked)
        {
            sb.Append("<h2>bytes/op</h2>");
            AppendTable(sb, results, types, "bytesPerOp", ratio: false);
        }
        sb.Append("<p>old = by-name Get/Set. keyed = GetValue/SetValue (#6395). ref = Set(ref _v, value). " +
            "ref+key = Set(ref _v, value, key); equal to ref unless an event is raised, so only the subscriber and [DependsOn] rows are run. " +
            "obj = ViewModelField&lt;T&gt;. plain = auto-property (only the non-notifying rows).</p>");
        return sb.ToString();
    }

    private static void AppendTable(StringBuilder sb, List<JsonElement> results, string[] types, string field, bool ratio)
    {
        sb.Append("<table><thead><tr><th>type</th><th>op</th>");
        foreach (string style in Styles)
        {
            sb.Append("<th>").Append(System.Net.WebUtility.HtmlEncode(style)).Append("</th>");
        }
        sb.Append("</tr></thead><tbody>");

        foreach (string type in types)
        {
            foreach (string op in Ops)
            {
                sb.Append("<tr><td>").Append(type).Append("</td><td>").Append(op).Append("</td>");
                double? old = Find(results, type, "old", op, field);
                foreach (string style in Styles)
                {
                    double? value = Find(results, type, style, op, field);
                    if (value == null)
                    {
                        sb.Append(style == "ref+key" && op is "get" or "setSame" or "setNew" ? "<td>=</td>" : "<td>-</td>");
                        continue;
                    }
                    string text = Format(value.Value);
                    if (ratio && old is > 0 && style != "old")
                    {
                        text += $" <small>x{value.Value / old.Value:0.00}</small>";
                    }
                    sb.Append("<td>").Append(text).Append("</td>");
                }
                sb.Append("</tr>");
            }
        }
        sb.Append("</tbody></table>");
    }

    private static string Format(double value) =>
        value < 10 ? value.ToString("0.0", CultureInfo.InvariantCulture) : value.ToString("0", CultureInfo.InvariantCulture);

    private static double? Find(List<JsonElement> results, string type, string style, string op, string field)
    {
        foreach (JsonElement r in results)
        {
            if (r.GetProperty("type").GetString() == type && r.GetProperty("style").GetString() == style &&
                r.GetProperty("op").GetString() == op)
            {
                JsonElement value = r.GetProperty(field);
                return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
            }
        }
        return null;
    }
}
