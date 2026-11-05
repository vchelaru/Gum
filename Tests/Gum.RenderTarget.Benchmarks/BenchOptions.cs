using System;
using System.IO;

namespace Gum.RenderTarget.Benchmarks;

/// <summary>Command line: <c>--out dir --hardware text --warmup n --frames n --timeout seconds --label text</c>.</summary>
public sealed class BenchOptions
{
    public string OutputFolder { get; private set; } = Path.Combine(Path.GetTempPath(), "outline-bench");

    public string Hardware { get; private set; } = "";

    public string Label { get; private set; } = "";

    public int WarmupFrames { get; private set; } = 120;

    public int MeasuredFrames { get; private set; } = 600;

    /// <summary>Hard limit for the whole run; the app writes what it has and exits when it passes.</summary>
    public double TimeoutSeconds { get; private set; } = 540;

    public static BenchOptions Parse(string[] args)
    {
        BenchOptions options = new BenchOptions();
        for (int i = 0; i < args.Length - 1; i += 2)
        {
            string value = args[i + 1];
            switch (args[i])
            {
                case "--out": options.OutputFolder = value; break;
                case "--hardware": options.Hardware = value; break;
                case "--label": options.Label = value; break;
                case "--warmup": options.WarmupFrames = int.Parse(value); break;
                case "--frames": options.MeasuredFrames = int.Parse(value); break;
                case "--timeout": options.TimeoutSeconds = double.Parse(value); break;
                default: throw new ArgumentException("Unknown option " + args[i]);
            }
        }

        return options;
    }
}
