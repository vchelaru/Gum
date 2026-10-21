using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Gum.Mvvm.Benchmarks;

// Usage: dotnet run -c Release --project Tests/Gum.Mvvm.Benchmarks -- [--quick] [--runs <n>] [--type <name>] [--out <file>]
bool quick = args.Contains("--quick");
string? typeFilter = ValueOf("--type");
string? outputPath = ValueOf("--out");
Settings settings = quick ? Settings.Quick : Settings.Full;
if (ValueOf("--runs") is string runsText)
{
    settings = settings with { Runs = int.Parse(runsText, CultureInfo.InvariantCulture) };
}

bool isDebugBuild = false;
#if DEBUG
isDebugBuild = true;
#endif
if (isDebugBuild)
{
    Console.Error.WriteLine("Built in Debug: the numbers are meaningless. Use -c Release.");
    return 1;
}

if (System.Diagnostics.Debugger.IsAttached)
{
    Console.Error.WriteLine("A debugger is attached: the numbers are meaningless.");
    return 1;
}

try
{
    System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
}
catch (Exception)
{
    // Priority is only a noise reduction; run without it.
}

List<BenchCase> cases = new();
Subscriber subscriber = new();

void Record(string type, string caseName, Action<int> body)
{
    cases.Add(new BenchCase(type, caseName, body));
}

void RunType<T, TVm, TPlain>(
    string type, Func<BenchViewModel, TVm> viewModelAccessor, Func<PlainModel, TPlain> plainAccessor, T a, T b)
    where TVm : struct, IAccessor<T>
    where TPlain : struct, IAccessor<T>
{
    if (typeFilter != null && !type.Contains(typeFilter, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    TPlain plain = plainAccessor(new PlainModel());
    TVm viewModel = viewModelAccessor(new BenchViewModel());

    BenchViewModel subscribed = new();
    subscribed.PropertyChanged += subscriber.OnPropertyChanged;
    TVm withSubscriber = viewModelAccessor(subscribed);

    TVm withDependent = viewModelAccessor(new BenchViewModelWithDependents());

    Record(type, "plain.get", n => Loops.Get<T, TPlain>(plain, n));
    Record(type, "plain.setSame", n => Loops.SetSame(plain, a, n));
    Record(type, "plain.setNew", n => Loops.SetNew(plain, a, b, n));

    Record(type, "vm.get", n => Loops.Get<T, TVm>(viewModel, n));
    Record(type, "vm.setSame", n => Loops.SetSame(viewModel, a, n));
    Record(type, "vm.setNew", n => Loops.SetNew(viewModel, a, b, n));
    Record(type, "vm.setNewSub", n => Loops.SetNew(withSubscriber, a, b, n));
    Record(type, "vm.setNewDep", n => Loops.SetNew(withDependent, a, b, n));
    Record(type, "vm.setQuiet", n => Loops.SetQuiet(viewModel, a, b, n));
}

RunType("string", m => new VmText(m), m => new PlainText(m), Values.TextA, Values.TextB);
RunType("ObservableCollection<int>", m => new VmItems(m), m => new PlainItems(m), Values.ItemsA, Values.ItemsB);
RunType("int[]", m => new VmArray(m), m => new PlainArray(m), Values.ArrayA, Values.ArrayB);
RunType("class", m => new VmReference(m), m => new PlainReference(m), Values.ReferenceA, Values.ReferenceB);
RunType("int", m => new VmInt(m), m => new PlainInt(m), 1, 2);
RunType("float", m => new VmFloat(m), m => new PlainFloat(m), 1f, 2f);
RunType("bool", m => new VmBool(m), m => new PlainBool(m), false, true);
RunType("double", m => new VmDouble(m), m => new PlainDouble(m), 1d, 2d);
RunType("enum", m => new VmEnum(m), m => new PlainEnum(m), BenchEnum.First, BenchEnum.Second);
RunType("struct 8 B", m => new VmSmall(m), m => new PlainSmall(m), Values.SmallA, Values.SmallB);
RunType("struct 32 B", m => new VmLarge(m), m => new PlainLarge(m), Values.LargeA, Values.LargeB);
RunType("struct 16 B, no IEquatable", m => new VmUnequatable(m), m => new PlainUnequatable(m),
    Values.UnequatableA, Values.UnequatableB);

// ChangeAndNotify over a backing field: the notification cost without the property dictionary.
if (typeFilter == null)
{
    foreach ((string type, Func<FieldBackedViewModel, Action<int>> setNew) in new (string, Func<FieldBackedViewModel, Action<int>>)[]
    {
        ("float", m => n => Loops.SetNew(new FieldFloat(m), 1f, 2f, n)),
        ("string", m => n => Loops.SetNew(new FieldText(m), Values.TextA, Values.TextB, n)),
    })
    {
        FieldBackedViewModel quiet = new();
        FieldBackedViewModel subscribed = new();
        subscribed.PropertyChanged += subscriber.OnPropertyChanged;
        Record(type, "field.setNew", setNew(quiet));
        Record(type, "field.setNewSub", setNew(subscribed));
    }

    const int modelCount = 100;
    BenchViewModel[] viewModels = Enumerable.Range(0, modelCount).Select(_ => new BenchViewModel()).ToArray();
    PlainModel[] plainModels = Enumerable.Range(0, modelCount).Select(_ => new PlainModel()).ToArray();
    Record("frame", "plain.frame", n => Sink<long>.Escape = Loops.FrameReadPlain(plainModels, n));
    Record("frame", "vm.frame", n => Sink<long>.Escape = Loops.FrameReadViewModels(viewModels, n));
}

Harness.RunAll(cases, settings, message => Console.Error.WriteLine(message));
Dictionary<(string Type, string Case), Result> results = cases.ToDictionary(c => (c.Type, c.Name), Harness.Summarize);
string report = BuildReport(results);
Console.WriteLine(report);
if (outputPath != null)
{
    File.WriteAllText(outputPath, report);
}

return 0;

string? ValueOf(string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string Ns(Result r) => r.Ns.ToString(r.Ns < 10 ? "0.0" : "0", CultureInfo.InvariantCulture);

static string Bytes(Result r) => Math.Round(r.Bytes).ToString("0", CultureInfo.InvariantCulture);

static string Cpu()
{
    string? fromEnvironment = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
    if (File.Exists("/proc/cpuinfo"))
    {
        string? line = File.ReadLines("/proc/cpuinfo").FirstOrDefault(l => l.StartsWith("model name"));
        return line?.Split(':', 2)[1].Trim() ?? "unknown";
    }

    if (OperatingSystem.IsWindows())
    {
        using Microsoft.Win32.RegistryKey? key =
            Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        if (key?.GetValue("ProcessorNameString") is string name)
        {
            return name.Trim();
        }
    }

    return fromEnvironment ?? "unknown";
}

string BuildReport(Dictionary<(string Type, string Case), Result> results)
{
    string[] types = results.Keys.Select(k => k.Type).Where(t => t != "frame").Distinct().ToArray();
    StringBuilder sb = new();

    sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd}  Mode: {(quick ? "quick" : "full")}");
    sb.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}, Release, {RuntimeInformation.ProcessArchitecture}");
    sb.AppendLine($"OS: {RuntimeInformation.OSDescription}");
    sb.AppendLine($"CPU: {Cpu()}, {Environment.ProcessorCount} logical cores");
    sb.AppendLine($"Each case: {settings.WarmupMs} ms warmup, median of {settings.Runs} runs of about {settings.TargetRunMs} ms, runs interleaved across cases.");
    sb.AppendLine();

    sb.AppendLine("ns/op (lower is better). Plain = auto-property. VM = Gum.Mvvm.ViewModel Get/Set.");
    sb.AppendLine();
    sb.AppendLine("| Type | Plain get | VM get | Plain set | VM set same | VM set new | VM set new + 1 subscriber | VM set new + [DependsOn] | VM SetWithoutNotifying |");
    sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
    foreach (string type in types)
    {
        sb.AppendLine($"| {type} | {Ns(results[(type, "plain.get")])} | {Ns(results[(type, "vm.get")])} | " +
            $"{Ns(results[(type, "plain.setNew")])} | {Ns(results[(type, "vm.setSame")])} | {Ns(results[(type, "vm.setNew")])} | " +
            $"{Ns(results[(type, "vm.setNewSub")])} | {Ns(results[(type, "vm.setNewDep")])} | {Ns(results[(type, "vm.setQuiet")])} |");
    }

    sb.AppendLine();
    sb.AppendLine("Bytes allocated per op, VM cases (every plain case allocates 0 B except where listed below).");
    sb.AppendLine();
    sb.AppendLine("| Type | VM get | VM set same | VM set new | VM set new + 1 subscriber | VM set new + [DependsOn] | VM SetWithoutNotifying |");
    sb.AppendLine("|---|---|---|---|---|---|---|");
    foreach (string type in types)
    {
        sb.AppendLine($"| {type} | {Bytes(results[(type, "vm.get")])} | {Bytes(results[(type, "vm.setSame")])} | " +
            $"{Bytes(results[(type, "vm.setNew")])} | {Bytes(results[(type, "vm.setNewSub")])} | " +
            $"{Bytes(results[(type, "vm.setNewDep")])} | {Bytes(results[(type, "vm.setQuiet")])} |");
    }

    List<string> plainAllocations = results
        .Where(r => r.Key.Case.StartsWith("plain.") && r.Value.Bytes >= 0.5)
        .Select(r => $"{r.Key.Type} {r.Key.Case} {Bytes(r.Value)} B")
        .ToList();
    sb.AppendLine();
    sb.AppendLine(plainAllocations.Count == 0
        ? "Plain cases allocated 0 B/op."
        : $"Plain cases that allocated: {string.Join(", ", plainAllocations)}.");

    if (results.ContainsKey(("float", "field.setNew")))
    {
        sb.AppendLine();
        sb.AppendLine("Baseline for notifying sets without the dictionary: ChangeAndNotify(ref field, value), the existing API, set new value.");
        sb.AppendLine();
        sb.AppendLine("| Type | No subscriber ns (B) | 1 subscriber ns (B) | VM no subscriber ns (B) | VM 1 subscriber ns (B) |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (string type in new[] { "float", "string" })
        {
            Result field = results[(type, "field.setNew")];
            Result fieldSub = results[(type, "field.setNewSub")];
            Result vm = results[(type, "vm.setNew")];
            Result vmSub = results[(type, "vm.setNewSub")];
            sb.AppendLine($"| {type} | {Ns(field)} ({Bytes(field)}) | {Ns(fieldSub)} ({Bytes(fieldSub)}) | {Ns(vm)} ({Bytes(vm)}) | {Ns(vmSub)} ({Bytes(vmSub)}) |");
        }

        Result plainFrame = results[("frame", "plain.frame")];
        Result vmFrame = results[("frame", "vm.frame")];
        const int readsPerFrame = 100 * 12;
        sb.AppendLine();
        sb.AppendLine($"Frame-style read: {readsPerFrame} property reads per frame (100 models x 12 properties, mixed types).");
        sb.AppendLine();
        sb.AppendLine("| | ns per read | microseconds per frame | B per frame |");
        sb.AppendLine("|---|---|---|---|");
        sb.AppendLine($"| Plain | {(plainFrame.Ns / readsPerFrame).ToString("0.00", CultureInfo.InvariantCulture)} | {(plainFrame.Ns / 1000).ToString("0.0", CultureInfo.InvariantCulture)} | {Bytes(plainFrame)} |");
        sb.AppendLine($"| VM | {(vmFrame.Ns / readsPerFrame).ToString("0.00", CultureInfo.InvariantCulture)} | {(vmFrame.Ns / 1000).ToString("0.0", CultureInfo.InvariantCulture)} | {Bytes(vmFrame)} |");
    }

    double[] spreads = results.Values.Select(r => r.SpreadFraction).Order().ToArray();
    sb.AppendLine();
    sb.AppendLine($"Noise: the middle half of the runs spans {spreads[spreads.Length / 2]:P0} of the median for the typical case " +
        $"and {spreads[^1]:P0} for the worst. Rerun on an idle machine if that is high; the ratios between rows are more reliable than the absolute values.");

    return sb.ToString();
}

sealed class Subscriber
{
    private long _count;

    public void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _count++;
    }
}
