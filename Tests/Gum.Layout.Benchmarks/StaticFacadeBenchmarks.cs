using System.Diagnostics;
using System.Text;
using Gum.Managers;
using RenderingLibrary.Content;
using ToolsUtilities;
using Xunit.Abstractions;

namespace Gum.Layout.Benchmarks;

/// <summary>
/// Cost of reading the statics that became per-service facades (#6066): <c>ObjectFinder.Self</c>,
/// <c>LoaderManager.Self</c> and <c>FileManager.RelativeDirectory</c>. Uses only APIs that exist both
/// before and after the facades, so the same file runs on either side of the change. Prints nanoseconds
/// per read (best of several batches) and writes <c>facade-benchmark.txt</c> next to the test binary.
/// </summary>
public class StaticFacadeBenchmarks
{
    private const int Reads = 20_000_000;
    private const int Batches = 7;

    private readonly ITestOutputHelper _output;

    public StaticFacadeBenchmarks(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ReadCost()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("facade read cost, ns per read (best of " + Batches + " batches of " + Reads + ")");
        report.AppendLine("ObjectFinder.Self        " + Measure(() => ObjectFinder.Self).ToString("F2"));
        report.AppendLine("LoaderManager.Self       " + Measure(() => LoaderManager.Self).ToString("F2"));
        report.AppendLine("FileManager.RelativeDir  " + Measure(() => FileManager.RelativeDirectory).ToString("F2"));
        report.AppendLine("baseline (empty lambda)  " + Measure(() => (object?)null).ToString("F2"));
        AppendContextReports(report);

        _output.WriteLine(report.ToString());
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "facade-benchmark.txt"), report.ToString());
    }

    private static double Measure(Func<object?> read)
    {
        object? sink = null;
        for (int i = 0; i < 1_000_000; i++)
        {
            sink = read();
        }

        double best = double.MaxValue;
        for (int batch = 0; batch < Batches; batch++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < Reads; i++)
            {
                sink = read();
            }
            stopwatch.Stop();
            best = Math.Min(best, stopwatch.Elapsed.TotalMilliseconds * 1_000_000d / Reads);
        }
        GC.KeepAlive(sink);
        return best;
    }

    // After the facades exist, repeat with a live context (adopting) and with a second, non-adopting
    // context activated. Reflection keeps this file compiling against the code before the facades.
    private static void AppendContextReports(StringBuilder report)
    {
        Type? contextType = typeof(ObjectFinder).Assembly.GetType("Gum.GumContext");
        if (contextType == null)
        {
            report.AppendLine("(no GumContext in this build)");
            return;
        }

        object first = Activator.CreateInstance(contextType, nonPublic: true)!;
        object second = Activator.CreateInstance(contextType, nonPublic: true)!;
        System.Reflection.BindingFlags any = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        contextType.GetMethod("Register", any)!.Invoke(first, null);
        contextType.GetMethod("Register", any)!.Invoke(second, null);
        try
        {
            report.AppendLine("-- first (adopting) context live, no scope");
            AppendReads(report);

            IDisposable scope = (IDisposable)contextType.GetMethod("Activate", Type.EmptyTypes)!.Invoke(second, null)!;
            try
            {
                report.AppendLine("-- second (non-adopting) context active");
                AppendReads(report);
            }
            finally
            {
                scope.Dispose();
            }
        }
        finally
        {
            contextType.GetMethod("Release", any)!.Invoke(second, null);
            contextType.GetMethod("Release", any)!.Invoke(first, null);
        }
    }

    private static void AppendReads(StringBuilder report)
    {
        report.AppendLine("ObjectFinder.Self        " + Measure(() => ObjectFinder.Self).ToString("F2"));
        report.AppendLine("LoaderManager.Self       " + Measure(() => LoaderManager.Self).ToString("F2"));
        report.AppendLine("FileManager.RelativeDir  " + Measure(() => FileManager.RelativeDirectory).ToString("F2"));
    }
}
