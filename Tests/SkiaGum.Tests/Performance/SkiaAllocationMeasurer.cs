using MonoGameGum.TestsCommon;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// <see cref="AllocationMeasurer.MeasureMinimum"/> for Skia workloads whose per-frame reading must not depend on
/// what ran before the test or on how often the garbage collector runs (#7860).
/// </summary>
/// <remarks>
/// RichTextKit creates <c>SKFont</c> objects it never disposes (2 to 21 per frame for the text cases). SkiaSharp
/// registers each one in a <c>Dictionary&lt;IntPtr, WeakReference&gt;</c> until a finalizer removes it, and that
/// dictionary doubles whenever the number of live handles passes its capacity. The capacity never shrinks, so a
/// resize (115 KB and more) lands in whichever measured window first reaches a new peak, and which window that is
/// depends on how many handles earlier tests left and how often a collection ran. Each attempt therefore (1) runs
/// the identical frames once inside a no-GC region so the table reaches the capacity the window needs, (2) lets
/// finalizers empty it again, and (3) measures the same frames, again inside a no-GC region, so the reading does not
/// depend on collection frequency either. Every pass replays the same frame numbers, so each window draws the same
/// strings. The figure is the steady-state managed bytes per frame between collections.
/// </remarks>
internal static class SkiaAllocationMeasurer
{
    // Generous for the few hundred frames a window runs (each allocates a few tens of KB).
    private const long NoGcRegionBytes = 64_000_000;

    /// <summary>
    /// Runs <paramref name="frame"/> with the frame numbers <c>0 .. warmupIterations + measuredIterations - 1</c>
    /// on every attempt, and returns the attempt with the fewest bytes allocated in frames
    /// <c>warmupIterations ..</c> (the measured window).
    /// </summary>
    public static AllocationResult MeasureMinimum(
        Action<int> frame,
        int attempts = 3,
        int warmupIterations = 50,
        int measuredIterations = 300)
    {
        AllocationResult best = default;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            AllocationResult result = MeasureOnce(frame, warmupIterations, measuredIterations);
            if (attempt == 0 || result.TotalBytes < best.TotalBytes)
            {
                best = result;
            }
        }
        return best;
    }

    private static AllocationResult MeasureOnce(Action<int> frame, int warmupIterations, int measuredIterations)
    {
        int total = warmupIterations + measuredIterations;

        // Peak pass: the table grows to the capacity a window needs.
        bool inRegion = StartNoGcRegion();
        try
        {
            for (int i = 0; i < total; i++)
            {
                frame(i);
            }
        }
        finally
        {
            EndNoGcRegion(inRegion);
        }
        Settle();

        for (int i = 0; i < warmupIterations; i++)
        {
            frame(i);
        }
        Settle();

        inRegion = StartNoGcRegion();
        long before;
        long after;
        try
        {
            before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = warmupIterations; i < total; i++)
            {
                frame(i);
            }
            after = GC.GetAllocatedBytesForCurrentThread();
        }
        finally
        {
            EndNoGcRegion(inRegion);
        }

        return new AllocationResult(after - before, measuredIterations);
    }

    // Collects and lets finalizers remove the handles of the SKFonts the previous frames left behind.
    private static void Settle()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static bool StartNoGcRegion()
    {
        try
        {
            return GC.TryStartNoGCRegion(NoGcRegionBytes);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static void EndNoGcRegion(bool started)
    {
        if (started && System.Runtime.GCSettings.LatencyMode == System.Runtime.GCLatencyMode.NoGCRegion)
        {
            GC.EndNoGCRegion();
        }
    }
}
