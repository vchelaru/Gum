using System.Diagnostics;
using System.Globalization;
using System.Text;
using Gum.Wireframe;
using Xunit.Abstractions;

namespace Gum.Layout.Benchmarks;

/// <summary>One measured scenario at one tree size.</summary>
/// <param name="Scenario">"FullLayout" or "LeafEdit".</param>
/// <param name="Size">The shape's size parameter (child count or depth).</param>
/// <param name="Nodes">Total elements in the tree.</param>
/// <param name="CallsPerRun">Average <see cref="GraphicalUiElement.UpdateLayoutCallCount"/> delta per run.</param>
/// <param name="MicrosPerRun">Average wall-clock microseconds per run.</param>
/// <param name="Abandoned">True when the first run blew the time budget and the size was not measured.</param>
public sealed record BenchmarkSample(
    string Scenario, int Size, int Nodes, double CallsPerRun, double MicrosPerRun, bool Abandoned);

/// <summary>
/// Measures how <c>UpdateLayout</c> cost grows with tree size, and prints the growth exponent between
/// consecutive sizes. An exponent near 1 is linear, 2 is quadratic, and a number that keeps climbing as the
/// size grows is exponential. Call counts are deterministic; timings are not, so only the call counts are
/// safe to assert on.
/// </summary>
public sealed class LayoutBenchmarkRunner
{
    private const double MinimumMeasuredMilliseconds = 50;
    private const int MinimumRuns = 5;
    private const int MaximumRuns = 5000;
    private const int WarmupRuns = 3;
    private const int Batches = 5;

    private readonly ITestOutputHelper _output;
    private readonly double _abandonMilliseconds;

    public LayoutBenchmarkRunner(ITestOutputHelper output, double abandonMilliseconds = 1000)
    {
        _output = output;
        _abandonMilliseconds = abandonMilliseconds;
    }

    /// <summary>
    /// Builds the shape at each size, measures a full layout from the root and a one-leaf width edit, and
    /// reports the table. Stops growing the size once a run exceeds the time budget.
    /// </summary>
    public List<BenchmarkSample> RunSeries(string shapeName, int[] sizes, Func<int, TreeUnderTest> build)
    {
        List<BenchmarkSample> samples = new();
        foreach (int size in sizes)
        {
            TreeUnderTest tree = build(size);

            BenchmarkSample full = Measure("FullLayout", size, tree, () => tree.Root.UpdateLayout());

            float leafWidth = tree.Leaf.Width;
            BenchmarkSample edit = Measure("LeafEdit", size, tree, () =>
            {
                leafWidth = leafWidth == 10 ? 11 : 10;
                tree.Leaf.Width = leafWidth;
            });

            samples.Add(full);
            samples.Add(edit);
            if (full.Abandoned || edit.Abandoned)
            {
                break;
            }
        }

        Report(shapeName, samples);
        return samples;
    }

    private BenchmarkSample Measure(string scenario, int size, TreeUnderTest tree, Action run)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        run();
        if (stopwatch.Elapsed.TotalMilliseconds > _abandonMilliseconds)
        {
            return new BenchmarkSample(scenario, size, tree.NodeCount, 0, 0, Abandoned: true);
        }

        for (int i = 1; i < WarmupRuns; i++)
        {
            run();
        }

        // Best of several batches: the minimum is the least noisy estimate of the cost itself.
        double bestMicrosPerRun = double.MaxValue;
        double callsPerRun = 0;
        for (int batch = 0; batch < Batches; batch++)
        {
            int callsBefore = GraphicalUiElement.UpdateLayoutCallCount;
            int runs = 0;
            stopwatch.Restart();
            while (runs < MaximumRuns && (runs < MinimumRuns || stopwatch.Elapsed.TotalMilliseconds < MinimumMeasuredMilliseconds))
            {
                run();
                runs++;
            }
            double microsPerRun = stopwatch.Elapsed.TotalMilliseconds * 1000 / runs;
            bestMicrosPerRun = Math.Min(bestMicrosPerRun, microsPerRun);
            callsPerRun = (double)(GraphicalUiElement.UpdateLayoutCallCount - callsBefore) / runs;
        }

        return new BenchmarkSample(scenario, size, tree.NodeCount, callsPerRun, bestMicrosPerRun, Abandoned: false);
    }

    private void Report(string shapeName, List<BenchmarkSample> samples)
    {
        StringBuilder report = new();
        report.AppendLine($"=== {shapeName} ===");
        report.AppendLine("scenario     size  nodes   calls/run   calls exp    us/run   time exp");

        foreach (string scenario in new[] { "FullLayout", "LeafEdit" })
        {
            BenchmarkSample? previous = null;
            foreach (BenchmarkSample sample in samples.Where(s => s.Scenario == scenario))
            {
                if (sample.Abandoned)
                {
                    report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0,-10} {1,6} {2,6}   abandoned: first run exceeded {3:0} ms",
                        scenario, sample.Size, sample.Nodes, _abandonMilliseconds));
                    continue;
                }

                string callsExponent = Exponent(previous, sample, s => s.CallsPerRun);
                string timeExponent = Exponent(previous, sample, s => s.MicrosPerRun);
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-10} {1,6} {2,6} {3,11:0.0} {4,11} {5,9:0.0} {6,10}",
                    scenario, sample.Size, sample.Nodes, sample.CallsPerRun, callsExponent, sample.MicrosPerRun, timeExponent));
                previous = sample;
            }
        }

        _output.WriteLine(report.ToString());
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, $"layout-benchmark-{shapeName}.txt"), report.ToString());
    }

    private static string Exponent(BenchmarkSample? previous, BenchmarkSample current, Func<BenchmarkSample, double> value)
    {
        if (previous == null || value(previous) <= 0 || value(current) <= 0)
        {
            return "-";
        }
        double exponent = Math.Log(value(current) / value(previous)) / Math.Log((double)current.Size / previous.Size);
        return exponent.ToString("0.00", CultureInfo.InvariantCulture);
    }
}

/// <summary>Resets the layout statics a benchmark could leave behind.</summary>
public abstract class LayoutBenchmarkTestBase : IDisposable
{
    protected LayoutBenchmarkTestBase()
    {
        GraphicalUiElement.IsAllLayoutSuspended = false;
    }

    public void Dispose()
    {
        GraphicalUiElement.IsAllLayoutSuspended = false;
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;
    }
}
