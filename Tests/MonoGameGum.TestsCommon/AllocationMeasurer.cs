using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

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
    public static AllocationResult Measure(
        Action action,
        int warmupIterations = 100,
        int measuredIterations = 1000,
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
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

        Drain();

        // #8190: with GUM_ALLOCATION_EVENTS=1 (the Mesa CI steps) the runtime's own events (JIT, type loads, exceptions)
        // are recorded from here, so a window that allocated can say what ran inside it. Started before any window.
        bool explain = Environment.GetEnvironmentVariable("GUM_ALLOCATION_EVENTS") == "1";
        using RuntimeEventRecorder recorder = explain ? new RuntimeEventRecorder() : null!;
        long testThreadId = RuntimeEventRecorder.CurrentOsThreadId();
        List<(DateTime Start, DateTime End)> windowTimes = new();

        for (int i = 0; i < warmupIterations; i++)
        {
            action();
        }

        // One full-size window, exactly as before. If it allocated nothing it is the answer and nothing more runs.
        // If it allocated, up to two more full-size windows run, each after a fresh drain, and the lowest wins: code
        // that allocates does so in every window (periodic or not), so a real allocation still fails at the same
        // bound, while a one-off burst (a GC, a finalizer, a driver thread handing work to this thread) lands in one
        // window and is dropped. The extra cost is paid only when a window allocated.
        List<long> windowBytes = new();
        List<long[]> windowCalls = new();
        for (int window = 0; window < MaxWindows; window++)
        {
            if (window > 0)
            {
                Drain();
            }
            long[] perCall = new long[measuredIterations];
            DateTime windowStartTime = DateTime.UtcNow;
            long windowStart = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < measuredIterations; i++)
            {
                long callStart = GC.GetAllocatedBytesForCurrentThread();
                action();
                perCall[i] = GC.GetAllocatedBytesForCurrentThread() - callStart;
            }
            long bytes = GC.GetAllocatedBytesForCurrentThread() - windowStart;
            windowTimes.Add((windowStartTime, DateTime.UtcNow));
            windowBytes.Add(bytes);
            windowCalls.Add(perCall);
            if (bytes == 0)
            {
                break;
            }
        }

        if (windowBytes.Count > 1 && windowBytes.Min() == 0 && windowBytes.Max() > 0)
        {
            ReportDisagreement(testName, filePath, windowBytes, windowCalls);
        }

        // The events of the first window that allocated, on the console (a failing test's output shows it) and through
        // NoiseSink. Reported also when every window allocated, which is the failure case.
        if (explain && windowBytes.Max() > 0)
        {
            int worst = windowBytes.IndexOf(windowBytes.Max());
            string events = "ALLOCATION-EVENTS: " + Path.GetFileNameWithoutExtension(filePath) + "." + testName + " window w" + worst + ": "
                + recorder.Describe(windowTimes[worst].Start.AddMilliseconds(-2), windowTimes[worst].End.AddMilliseconds(2), testThreadId);
            (NoiseSink ?? Console.WriteLine)(events);
        }

        return new AllocationResult(windowBytes.Min(), measuredIterations);
    }

    private const int MaxWindows = 3;

    /// <summary>
    /// Receives the <c>ALLOCATION-NOISE:</c> line when measured windows disagree, on the measuring thread. Unset, the
    /// line goes to the console. The hook for richer diagnostics (the runtime event recorder) is
    /// <see cref="DisagreementHook"/>.
    /// </summary>
    [ThreadStatic]
    public static Action<string>? NoiseSink;

    /// <summary>
    /// Called with the line and the per-call bytes of every measured iteration when the windows disagree, so a
    /// recorder can attach runtime events for the allocating window (the diagnostics PR plugs in here).
    /// </summary>
    [ThreadStatic]
    public static Action<string, long[]>? DisagreementHook;

    private static void ReportDisagreement(string testName, string filePath, List<long> windowBytes, List<long[]> windowCalls)
    {
        string file = System.IO.Path.GetFileNameWithoutExtension(filePath);
        string windows = string.Join(", ", windowBytes.Select((bytes, i) => $"w{i}={bytes}B"));
        string allocating = string.Join(" | ", windowCalls.Select((calls, window) =>
            $"w{window}: " + string.Join(" ", calls.Select((bytes, i) => (Call: i, Bytes: bytes))
                .Where(c => c.Bytes != 0).Take(20).Select(c => $"#{c.Call}:{c.Bytes}"))));
        string line = $"ALLOCATION-NOISE: {file}.{testName} windows [{windows}]; allocating calls (first 20 per window): {allocating}";
        (NoiseSink ?? Console.WriteLine)(line);
        DisagreementHook?.Invoke(line, windowCalls.OrderByDescending(calls => calls.Sum()).First());
    }

    /// <summary>
    /// Bytes allocated so far on the current thread. The only place a test reads the counter, so a test that
    /// needs a per-phase reading inside a measured action, or one that runs its own warm-up and window, still
    /// goes through this class. A test with its own warm-up calls <see cref="Drain"/> before that warm-up.
    /// </summary>
    public static long ThreadBytes => GC.GetAllocatedBytesForCurrentThread();

    /// <summary>
    /// Finishes the finalizers queued by earlier tests. Garbage from earlier tests can still be waiting for
    /// finalization, and a finalizer that runs inside a window can hand work to this thread, such as a resource
    /// release the next frame performs, which reads as an allocation in code that allocates nothing (#8190).
    /// Call it before the warm-up so the warm-up frames absorb the work handed over. The second pass takes what
    /// the finalizers released.
    /// </summary>
    public static void Drain()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    // Generous for the few hundred frames a window runs (each allocates a few tens of KB).
    private const long NoGcRegionBytes = 64_000_000;

    /// <summary>
    /// Like <see cref="Measure"/> for workloads whose per-frame reading must not depend on what ran before the
    /// test or on how often the garbage collector runs (#7860). Runs <paramref name="frame"/> with the frame
    /// numbers <c>0 .. warmupIterations + measuredIterations - 1</c> and returns the bytes allocated in frames
    /// <c>warmupIterations ..</c> (the measured window).
    /// </summary>
    /// <remarks>
    /// RichTextKit creates <c>SKFont</c> objects it never disposes (2 to 21 per frame for the text cases). SkiaSharp
    /// registers each one in a <c>Dictionary&lt;IntPtr, WeakReference&gt;</c> until a finalizer removes it, and that
    /// dictionary doubles whenever the number of live handles passes its capacity. The capacity never shrinks, so a
    /// resize (115 KB and more) lands in whichever measured window first reaches a new peak, and which window that is
    /// depends on how many handles earlier tests left and how often a collection ran. The measurement therefore (1) runs
    /// the identical frames once inside a no-GC region so the table reaches the capacity the window needs, (2) lets
    /// finalizers empty it again, and (3) measures the same frames, again inside a no-GC region, so the reading does not
    /// depend on collection frequency either. Every pass replays the same frame numbers, so each window draws the same
    /// strings. The figure is the steady-state managed bytes per frame between collections.
    /// </remarks>
    public static AllocationResult MeasureReplayed(Action<int> frame, int warmupIterations = 50, int measuredIterations = 300)
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
        Drain();

        for (int i = 0; i < warmupIterations; i++)
        {
            frame(i);
        }
        Drain();

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
