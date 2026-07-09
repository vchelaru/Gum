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

    /// <summary>
    /// Runs <see cref="Measure"/> <paramref name="attempts"/> times and returns the attempt with
    /// the fewest total bytes allocated. Use this for zero-allocation regression guards: a one-off
    /// environmental blip (e.g. JIT tier-up not settling within the warmup window on a slower CI
    /// runner) can occasionally pollute a single attempt without reflecting a real per-iteration
    /// allocation, while a genuine regression allocates on every attempt and so still surfaces as
    /// the minimum.
    /// </summary>
    /// <param name="action">The operation to measure. Must be synchronous and single-threaded.</param>
    /// <param name="attempts">The number of independent measurement attempts to run.</param>
    /// <param name="warmupIterations">Runs performed before measuring in each attempt, to settle JIT and lazy init.</param>
    /// <param name="measuredIterations">Runs performed inside the measured window of each attempt.</param>
    /// <returns>The attempt with the fewest total bytes allocated.</returns>
    public static AllocationResult MeasureMinimum(Action action, int attempts = 3, int warmupIterations = 100, int measuredIterations = 1000)
    {
        if (attempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attempts), "Must run at least one attempt.");
        }

        AllocationResult best = Measure(action, warmupIterations, measuredIterations);
        for (int i = 1; i < attempts; i++)
        {
            AllocationResult attempt = Measure(action, warmupIterations, measuredIterations);
            if (attempt.TotalBytes < best.TotalBytes)
            {
                best = attempt;
            }
        }

        return best;
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
