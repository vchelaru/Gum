namespace MonoGameGum.TestsCommon;

/// <summary>
/// Measures managed heap allocations produced by an operation, for use in runtime allocation
/// regression tests (issue #1934). Wraps <see cref="System.GC.GetAllocatedBytesForCurrentThread"/>
/// with a warmup phase so JIT, tiered compilation, and first-run lazy initialization do not
/// pollute the measured window.
/// </summary>
/// <remarks>
/// The measured operation MUST run synchronously on the calling thread. The underlying counter is
/// per-thread, so any <c>await</c> that resumes on a different thread makes the delta meaningless.
/// Because the counter tracks cumulative allocation (not live-heap size), a garbage collection
/// occurring inside the measured window does not affect the result.
/// </remarks>
public static class AllocationMeasurer
{
    /// <summary>
    /// Runs <paramref name="action"/> for a warmup phase, then measures the total managed bytes
    /// allocated on the current thread across <paramref name="measuredIterations"/> runs.
    /// </summary>
    /// <param name="action">The operation to measure. Must be synchronous and single-threaded.</param>
    /// <param name="warmupIterations">Runs performed before measuring, to settle JIT and lazy init.</param>
    /// <param name="measuredIterations">Runs performed inside the measured window.</param>
    /// <returns>The allocation result, including total bytes and bytes per iteration.</returns>
    public static AllocationResult Measure(Action action, int warmupIterations = 100, int measuredIterations = 1000)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        if (warmupIterations < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warmupIterations), "Warmup iterations cannot be negative.");
        }
        if (measuredIterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(measuredIterations), "Must run at least one measured iteration.");
        }

        for (int i = 0; i < warmupIterations; i++)
        {
            action();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < measuredIterations; i++)
        {
            action();
        }
        long after = GC.GetAllocatedBytesForCurrentThread();

        return new AllocationResult(after - before, measuredIterations);
    }
}

/// <summary>
/// The outcome of an <see cref="AllocationMeasurer.Measure"/> call.
/// </summary>
public readonly struct AllocationResult
{
    /// <summary>Total managed bytes allocated across all measured iterations.</summary>
    public long TotalBytes { get; }

    /// <summary>Number of iterations run inside the measured window.</summary>
    public int Iterations { get; }

    /// <summary>Average managed bytes allocated per iteration (one "frame" of the measured operation).</summary>
    public double BytesPerIteration => (double)TotalBytes / Iterations;

    /// <summary>Initializes a new <see cref="AllocationResult"/>.</summary>
    /// <param name="totalBytes">Total managed bytes allocated across all measured iterations.</param>
    /// <param name="iterations">Number of iterations run inside the measured window.</param>
    public AllocationResult(long totalBytes, int iterations)
    {
        TotalBytes = totalBytes;
        Iterations = iterations;
    }
}
