using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.RenderTarget.Benchmarks;

/// <summary>Mean, median and 95th percentile (nearest rank) of a set of samples.</summary>
public readonly record struct FrameStats(double Mean, double Median, double P95)
{
    public static FrameStats From(IReadOnlyList<double> samples)
    {
        if (samples.Count == 0)
        {
            return new FrameStats(0, 0, 0);
        }

        double[] sorted = samples.OrderBy(x => x).ToArray();
        double median = sorted.Length % 2 == 1
            ? sorted[sorted.Length / 2]
            : (sorted[(sorted.Length / 2) - 1] + sorted[sorted.Length / 2]) / 2.0;
        int rank = (int)Math.Ceiling(0.95 * sorted.Length);
        return new FrameStats(sorted.Average(), median, sorted[Math.Max(0, rank - 1)]);
    }
}
