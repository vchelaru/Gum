using System.Diagnostics;

namespace Gum.Mvvm.Benchmarks;

/// <summary>Median ns/op and bytes/op, and the interquartile range of the runs as a fraction of the median.</summary>
public readonly record struct Result(double Ns, double Bytes, double SpreadFraction);

/// <summary>How long each case warms up and how many timed runs it takes the median of.</summary>
public sealed record Settings(int WarmupMs, int Runs, double TargetRunMs)
{
    public static Settings Full { get; } = new(WarmupMs: 100, Runs: 9, TargetRunMs: 25);

    /// <summary>For checking that the benchmark runs; the numbers are noisier.</summary>
    public static Settings Quick { get; } = new(WarmupMs: 20, Runs: 3, TargetRunMs: 5);
}

/// <summary>One measured loop: a name, the loop body, and the runs collected so far.</summary>
public sealed class BenchCase(string type, string name, Action<int> body)
{
    public string Type { get; } = type;
    public string Name { get; } = name;
    public Action<int> Body { get; } = body;
    public int Iterations { get; set; }
    public List<double> Nanoseconds { get; } = new();
    public List<double> Bytes { get; } = new();
}

/// <summary>
/// Fixed-iteration harness: each run is one loop of N operations inside a single timed block, after a
/// warmup. Runs are interleaved across all cases (every case runs once per pass) so a burst of other
/// activity on the machine spreads over many cases instead of ruining one. The result is the median.
/// Allocation comes from <see cref="GC.GetAllocatedBytesForCurrentThread"/>.
/// </summary>
public static class Harness
{
    private const int WarmupChunk = 20_000;
    private const int MinimumIterations = 100_000;
    private const int MaximumIterations = 200_000_000;

    public static void RunAll(List<BenchCase> cases, Settings settings, Action<string>? progress = null)
    {
        foreach (BenchCase benchCase in cases)
        {
            Prepare(benchCase, settings);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        for (int pass = 0; pass < settings.Runs; pass++)
        {
            foreach (BenchCase benchCase in cases)
            {
                RunOnce(benchCase);
            }
            progress?.Invoke($"pass {pass + 1}/{settings.Runs}");
        }
    }

    private static void Prepare(BenchCase benchCase, Settings settings)
    {
        Stopwatch warmup = Stopwatch.StartNew();
        while (warmup.ElapsedMilliseconds < settings.WarmupMs)
        {
            benchCase.Body(WarmupChunk);
        }

        long calibrationStart = Stopwatch.GetTimestamp();
        benchCase.Body(WarmupChunk * 5);
        double calibrationNs = (Stopwatch.GetTimestamp() - calibrationStart) * 1e9 / Stopwatch.Frequency;
        double nsPerOperation = Math.Max(calibrationNs / (WarmupChunk * 5), 0.01);
        benchCase.Iterations = (int)Math.Clamp(settings.TargetRunMs * 1e6 / nsPerOperation, MinimumIterations, MaximumIterations);
    }

    private static void RunOnce(BenchCase benchCase)
    {
        int iterations = benchCase.Iterations;
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        benchCase.Body(iterations);
        long ticks = Stopwatch.GetTimestamp() - start;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        benchCase.Nanoseconds.Add(ticks * 1e9 / Stopwatch.Frequency / iterations);
        benchCase.Bytes.Add((double)allocated / iterations);
    }

    public static Result Summarize(BenchCase benchCase)
    {
        double[] nanoseconds = benchCase.Nanoseconds.Order().ToArray();
        double[] bytes = benchCase.Bytes.Order().ToArray();
        double medianNs = nanoseconds[nanoseconds.Length / 2];
        double interquartile = nanoseconds[nanoseconds.Length * 3 / 4] - nanoseconds[nanoseconds.Length / 4];
        double spread = medianNs > 0 ? interquartile / medianNs : 0;
        return new Result(medianNs, bytes[bytes.Length / 2], spread);
    }
}

/// <summary>
/// Where a loop leaves its values so the JIT cannot discard the reads and writes. An instance field, not
/// a static, because a static in a generic class costs a runtime lookup for every reference type.
/// </summary>
public sealed class Sink<T>
{
    public T? Value;

    /// <summary>Keeps the last sink reachable so nothing above can be optimized away.</summary>
    public static object? Escape;
}

public static class Loops
{
    public static void Get<T, TAccessor>(TAccessor accessor, int n) where TAccessor : struct, IAccessor<T>
    {
        Sink<T> sink = new();
        for (int i = 0; i < n; i++)
        {
            sink.Value = accessor.Get();
        }
        Sink<T>.Escape = sink;
    }

    /// <summary>Writes the value the property already holds, so nothing changes.</summary>
    public static void SetSame<T, TAccessor>(TAccessor accessor, T value, int n) where TAccessor : struct, IAccessor<T>
    {
        accessor.Set(value);
        for (int i = 0; i < n; i++)
        {
            accessor.Set(value);
        }
        Sink<T>.Escape = accessor.Get();
    }

    /// <summary>Alternates between two values, so every write after the first changes the property.</summary>
    public static void SetNew<T, TAccessor>(TAccessor accessor, T a, T b, int n) where TAccessor : struct, IAccessor<T>
    {
        accessor.Set(a);
        for (int i = 0; i < n; i++)
        {
            accessor.Set((i & 1) == 0 ? b : a);
        }
        Sink<T>.Escape = accessor.Get();
    }

    public static void SetQuiet<T, TAccessor>(TAccessor accessor, T a, T b, int n) where TAccessor : struct, IAccessor<T>
    {
        accessor.SetQuiet(a);
        for (int i = 0; i < n; i++)
        {
            accessor.SetQuiet((i & 1) == 0 ? b : a);
        }
        Sink<T>.Escape = accessor.Get();
    }

    /// <summary>Reads every property of every model once per frame, the way a per-frame update would.</summary>
    public static long FrameReadViewModels(BenchViewModel[] models, int frames)
    {
        long sum = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            for (int i = 0; i < models.Length; i++)
            {
                BenchViewModel m = models[i];
                sum += m.TextValue.Length + m.ItemsValue.Count + m.ArrayValue.Length + m.ReferenceValue.Value
                    + m.IntValue + (long)m.FloatValue + (m.BoolValue ? 1 : 0) + (long)m.DoubleValue
                    + (int)m.EnumValue + m.SmallValue.X + (long)m.LargeValue.A + (long)m.UnequatableValue.A;
            }
        }
        return sum;
    }

    public static long FrameReadPlain(PlainModel[] models, int frames)
    {
        long sum = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            for (int i = 0; i < models.Length; i++)
            {
                PlainModel m = models[i];
                sum += m.TextValue.Length + m.ItemsValue.Count + m.ArrayValue.Length + m.ReferenceValue.Value
                    + m.IntValue + (long)m.FloatValue + (m.BoolValue ? 1 : 0) + (long)m.DoubleValue
                    + (int)m.EnumValue + m.SmallValue.X + (long)m.LargeValue.A + (long)m.UnequatableValue.A;
            }
        }
        return sum;
    }
}
