#nullable enable
using System.Diagnostics;
using System.Text;

namespace GumSamples.Benchmark;

/// <summary>
/// Drives <see cref="BenchmarkScreen"/> for a fixed number of frames and prints one markdown row of CPU and
/// frame milliseconds (#8052). No window or rendering code: the host supplies Update, Draw and Sync
/// (wait for the GPU, e.g. glFinish). Fixed 1/60 s step and a scripted ListBox scroll so every runtime draws
/// the same frames.
/// </summary>
internal static class BenchmarkRunner
{
    public const int WarmupFrames = 600; // a laptop GPU idles at a low clock; 30 frames measured the ramp-up (#8052)
    public const int MeasuredFrames = 300;

    /// <summary>A Debug build measures unoptimized code, not Gum, so a benchmark refuses to run in one.</summary>
    public static void RequireRelease()
    {
#if DEBUG
        Console.Error.WriteLine("Benchmark refused: this is a Debug build. Run: dotnet build -c Release, then the Release exe.");
        Environment.Exit(2);
#endif
    }

    public static string Run(string runtimeName, BenchmarkScreen screen, Action<double> update, Action draw, Action sync,
        string? outputPath)
    {
        const double step = 1.0 / 60;
        double firstFrameMs = 0;
        double[] updateMs = new double[MeasuredFrames];
        double[] drawMs = new double[MeasuredFrames];
        double[] totalMs = new double[MeasuredFrames];

        double toMs = 1000.0 / Stopwatch.Frequency;
        long drawAllocBytes = 0; int gen0Start = 0, gen2Start = 0; long totalAllocStart = 0; // SPIKE #8116 runtime
        for (int frame = 0; frame < WarmupFrames + MeasuredFrames; frame++)
        {
            // Scroll sweeps the whole list once per run, back and forth.
            int measured = frame - WarmupFrames;
            double phase = Math.Max(measured, 0) / (double)MeasuredFrames;
            screen.SetScroll(phase < 0.5 ? phase * 2 : (1 - phase) * 2);

            long start = Stopwatch.GetTimestamp();
            update(step * frame);
            if (measured == 0) { gen0Start = GC.CollectionCount(0); gen2Start = GC.CollectionCount(2); totalAllocStart = GC.GetTotalAllocatedBytes(false); }
            long afterUpdate = Stopwatch.GetTimestamp();
            long allocBefore = GC.GetAllocatedBytesForCurrentThread();
            draw();
            if (measured >= 0) drawAllocBytes += GC.GetAllocatedBytesForCurrentThread() - allocBefore;
            long afterDraw = Stopwatch.GetTimestamp();
            sync();
            long end = Stopwatch.GetTimestamp();

            if (frame == 0)
            {
                firstFrameMs = (end - start) * toMs;
            }
            if (measured >= 0)
            {
                updateMs[measured] = (afterUpdate - start) * toMs;
                drawMs[measured] = (afterDraw - afterUpdate) * toMs;
                totalMs[measured] = (end - start) * toMs;
            }
        }

        double firstHalf = Median(totalMs[..(MeasuredFrames / 2)]);
        double secondHalf = Median(totalMs[(MeasuredFrames / 2)..]);
        bool unstable = Math.Abs(firstHalf - secondHalf) > 0.1 * Math.Min(firstHalf, secondHalf);

        StringBuilder text = new StringBuilder();
        text.AppendLine($"<!-- runtime: dynamicCode={System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported} serverGC={System.Runtime.GCSettings.IsServerGC} " +
            $"latency={System.Runtime.GCSettings.LatencyMode} drawAllocBytesPerFrame={drawAllocBytes / (double)MeasuredFrames:F1} " +
            $"allThreadsAllocBytesPerFrame={(GC.GetTotalAllocatedBytes(false) - totalAllocStart) / (double)MeasuredFrames:F1} " +
            $"gen0={GC.CollectionCount(0) - gen0Start} gen2={GC.CollectionCount(2) - gen2Start} -->");
        text.AppendLine("| runtime | update ms (med / p95) | draw ms: CPU record + flush submit (med / p95) | frame incl. glFinish wait ms (med / p95) | first frame ms | halves differ >10% |");
        text.AppendLine("|---|---|---|---|---|---|");
        text.AppendLine($"| {runtimeName} | {Median(updateMs):F2} / {Percentile95(updateMs):F2} | {Median(drawMs):F2} / {Percentile95(drawMs):F2} | " +
            $"{Median(totalMs):F2} / {Percentile95(totalMs):F2} | {firstFrameMs:F1} | {(unstable ? "YES, rerun" : "no")} |");
        Console.Write(text.ToString());
        if (outputPath != null)
        {
            File.AppendAllText(outputPath, text.ToString());
            Console.WriteLine("Appended to " + outputPath);
        }
        return text.ToString();
    }

    private static double Median(double[] values)
    {
        double[] sorted = (double[])values.Clone();
        Array.Sort(sorted);
        return sorted[sorted.Length / 2];
    }

    private static double Percentile95(double[] values)
    {
        double[] sorted = (double[])values.Clone();
        Array.Sort(sorted);
        return sorted[(int)(sorted.Length * 0.95)];
    }
}
