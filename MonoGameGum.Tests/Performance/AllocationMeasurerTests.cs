using MonoGameGum.TestsCommon;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Self-tests for <see cref="AllocationMeasurer"/> — proves the measurement mechanism itself
/// reports zero for a no-op and tracks a known allocation, so the runtime allocation scenarios
/// that depend on it can be trusted.
/// </summary>
public class AllocationMeasurerTests
{
    [Fact]
    public void Measure_KnownAllocation_ReportsAtLeastAllocatedSize()
    {
        const int arraySize = 10_000;

        AllocationResult result = AllocationMeasurer.Measure(
            () => GC.KeepAlive(new byte[arraySize]),
            warmupIterations: 10,
            measuredIterations: 100);

        result.BytesPerIteration.ShouldBeGreaterThanOrEqualTo(arraySize);
    }

    [Fact]
    public void Measure_NoOpAction_ReportsZeroAllocations()
    {
        AllocationResult result = AllocationMeasurer.Measure(
            () => { },
            warmupIterations: 10,
            measuredIterations: 1000);

        result.TotalBytes.ShouldBe(0);
    }
}

/// <summary>
/// Finalizers of garbage left by earlier tests run on the finalizer thread and can hand work to the
/// measuring thread (a resource-release queue the next frame drains). If that lands in the measured
/// window, the window reads non-zero for code that allocates nothing (#8190).
/// </summary>
[Collection("AllocationMeasurerPendingFinalizer")]
public class AllocationMeasurerPendingFinalizerTests
{
    private static readonly System.Collections.Concurrent.ConcurrentQueue<int> ReleaseQueue = new();
    private static readonly System.Threading.ManualResetEventSlim FinalizerGate = new(false);

    private sealed class Releasable
    {
        // Stands in for a GraphicsResource: its finalizer queues a release the next frame performs.
        ~Releasable()
        {
            FinalizerGate.Wait(TimeSpan.FromMilliseconds(500));
            ReleaseQueue.Enqueue(1);
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void CreateGarbage() => GC.KeepAlive(new Releasable());

    [Fact]
    public void Measure_FinalizerQueuedBeforeMeasuring_DoesNotLeakIntoWindow()
    {
        while (ReleaseQueue.TryDequeue(out _)) { }
        FinalizerGate.Reset();
        CreateGarbage();
        GC.Collect(); // finalizer thread now holds the object, blocked on the gate
        string? noise = null;
        AllocationMeasurer.NoiseSink = line => noise = line;
        AllocationMeasurer.RecordsRuntimeEvents = false;
        int call = 0;
        byte[]? sink = null;
        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                call++;
                if (call == 102)
                {
                    // First measured call: let the lagging finalizer finish inside the window.
                    FinalizerGate.Set();
                    long deadline = Environment.TickCount64 + 2000;
                    while (ReleaseQueue.IsEmpty && Environment.TickCount64 < deadline)
                    {
                        System.Threading.Thread.Yield();
                    }
                }
                if (ReleaseQueue.TryDequeue(out _))
                {
                    sink = new byte[4096]; // the release work, charged to the measuring thread
                }
            },
            warmupIterations: 100,
            measuredIterations: 200);
        AllocationMeasurer.NoiseSink = null;
        AllocationMeasurer.RecordsRuntimeEvents = null;
        GC.KeepAlive(sink);

        result.TotalBytes.ShouldBe(0);
        noise.ShouldBeNull("the pre-window drain finishes the finalizer, so the first window is clean and no retry runs");
    }
}

/// <summary>
/// A window that allocated is re-run (up to two more full-size windows) and the lowest wins, so a real allocation
/// still fails, periodic or not, and a one-off burst does not (#8190).
/// </summary>
public class AllocationMeasurerWindowTests
{
    private static byte[]? _sink;

    private static AllocationResult MeasureCapturing(Action action, int measured, out string? noise,
        [System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        string? captured = null;
        AllocationMeasurer.NoiseSink = line => captured = line;
        AllocationMeasurer.RecordsRuntimeEvents = false;
        try
        {
            AllocationResult result = AllocationMeasurer.Measure(action, warmupIterations: 10, measuredIterations: measured, testName: testName);
            noise = captured;
            return result;
        }
        finally
        {
            AllocationMeasurer.NoiseSink = null;
            AllocationMeasurer.RecordsRuntimeEvents = null;
        }
    }

    [Fact]
    public void Measure_RealAllocationEveryIteration_FailsInEveryWindow()
    {
        AllocationResult result = MeasureCapturing(() => _sink = new byte[1000], 300, out string? noise);

        result.BytesPerIteration.ShouldBeGreaterThanOrEqualTo(1000);
        noise.ShouldBeNull();
    }

    [Theory]
    [InlineData(50)]
    [InlineData(400)]
    [InlineData(600)]
    public void Measure_PeriodicAllocation_StillFails(int period)
    {
        int call = 0;
        AllocationResult result = MeasureCapturing(
            () =>
            {
                call++;
                if (call % period == 0)
                {
                    _sink = new byte[4096];
                }
            },
            1000,
            out string? noise);

        result.TotalBytes.ShouldBeGreaterThan(0);
        noise.ShouldBeNull();
    }

    [Fact]
    public void Measure_BurstInFirstWindowWithCleanRetry_ReadsZeroAndReportsNoise()
    {
        int call = 0;
        AllocationResult result = MeasureCapturing(
            () =>
            {
                call++;
                if (call == 10 + 150) // inside the first measured window only
                {
                    _sink = new byte[4096];
                }
            },
            300,
            out string? noise);

        result.TotalBytes.ShouldBe(0);
        noise.ShouldNotBeNull();
        noise!.ShouldStartWith("ALLOCATION-NOISE:");
        noise.ShouldContain("Measure_BurstInFirstWindowWithCleanRetry_ReadsZeroAndReportsNoise");
    }
}
