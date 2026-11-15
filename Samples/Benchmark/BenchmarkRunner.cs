#nullable enable
using System.Diagnostics;
using System.Text;

namespace GumSamples.Benchmark;

/// <summary>Reads the GPU time of the work between <see cref="Begin"/> and <see cref="End"/>. A host that cannot measure it passes null.</summary>
internal interface IGpuTimer
{
    void Begin();
    void End();
    /// <summary>Milliseconds the GPU spent on the last Begin/End pair, after waiting for it to finish.</summary>
    double ReadMilliseconds();
}

/// <summary>
/// Drives <see cref="BenchmarkScreen"/> for a fixed number of frames and prints one markdown row of CPU and
/// GPU milliseconds per frame (#8052). No window or rendering code: the host supplies Update, Draw and Sync
/// (wait for the GPU, e.g. glFinish). Fixed 1/60 s step and a scripted ListBox scroll so every runtime draws
/// the same frames.
/// </summary>
internal static class BenchmarkRunner
{
    public const int WarmupFrames = 30;
    public const int MeasuredFrames = 300;

    public static string Run(string runtimeName, BenchmarkScreen screen, Action<double> update, Action draw, Action sync,
        IGpuTimer? gpuTimer, string? outputPath)
    {
        const double step = 1.0 / 60;
        double firstFrameMs = 0;
        double[] updateMs = new double[MeasuredFrames];
        double[] drawMs = new double[MeasuredFrames];
        double[] totalMs = new double[MeasuredFrames];
        double[] gpuMs = new double[MeasuredFrames];

        double toMs = 1000.0 / Stopwatch.Frequency;
        for (int frame = 0; frame < WarmupFrames + MeasuredFrames; frame++)
        {
            // Scroll sweeps the whole list once per run, back and forth.
            int measured = frame - WarmupFrames;
            double phase = Math.Max(measured, 0) / (double)MeasuredFrames;
            screen.SetScroll(phase < 0.5 ? phase * 2 : (1 - phase) * 2);

            long start = Stopwatch.GetTimestamp();
            update(step * frame);
            long afterUpdate = Stopwatch.GetTimestamp();
            gpuTimer?.Begin();
            draw();
            long afterDraw = Stopwatch.GetTimestamp();
            gpuTimer?.End();
            sync();
            long end = Stopwatch.GetTimestamp();
            double gpu = gpuTimer?.ReadMilliseconds() ?? double.NaN;

            if (frame == 0)
            {
                firstFrameMs = (end - start) * toMs;
            }
            if (measured >= 0)
            {
                updateMs[measured] = (afterUpdate - start) * toMs;
                drawMs[measured] = (afterDraw - afterUpdate) * toMs;
                totalMs[measured] = (end - start) * toMs;
                gpuMs[measured] = gpu;
            }
        }

        double firstHalf = Median(totalMs[..(MeasuredFrames / 2)]);
        double secondHalf = Median(totalMs[(MeasuredFrames / 2)..]);
        bool unstable = Math.Abs(firstHalf - secondHalf) > 0.1 * Math.Min(firstHalf, secondHalf);

        string gpuText = gpuTimer == null ? "n/a" : $"{Median(gpuMs):F2} / {Percentile95(gpuMs):F2}";
        StringBuilder text = new StringBuilder();
        text.AppendLine("| runtime | update ms (med / p95) | draw ms (med / p95) | frame incl. GPU wait ms (med / p95) | GPU ms (med / p95) | first frame ms | halves differ >10% |");
        text.AppendLine("|---|---|---|---|---|---|---|");
        text.AppendLine($"| {runtimeName} | {Median(updateMs):F2} / {Percentile95(updateMs):F2} | {Median(drawMs):F2} / {Percentile95(drawMs):F2} | " +
            $"{Median(totalMs):F2} / {Percentile95(totalMs):F2} | {gpuText} | {firstFrameMs:F1} | {(unstable ? "YES, rerun" : "no")} |");
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
