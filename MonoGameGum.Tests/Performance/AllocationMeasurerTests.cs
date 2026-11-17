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
        GC.KeepAlive(sink);

        result.TotalBytes.ShouldBe(0);
    }
}

/// <summary>
/// The measured iterations run as several windows and the lowest wins, so a real allocation still fails and a
/// one-off burst does not (#8190).
/// </summary>
public class AllocationMeasurerWindowTests
{
    private static byte[]? _sink;

    [Fact]
    public void Measure_RealAllocationEveryIteration_ReportedByEveryWindow()
    {
        string? noise = null;
        AllocationMeasurer.NoiseSink = line => noise = line;
        try
        {
            AllocationResult result = AllocationMeasurer.Measure(
                () => _sink = new byte[1000], warmupIterations: 10, measuredIterations: 300);

            result.BytesPerIteration.ShouldBeGreaterThanOrEqualTo(1000);
            noise.ShouldBeNull();
        }
        finally
        {
            AllocationMeasurer.NoiseSink = null;
        }
    }

    [Fact]
    public void Measure_BurstInOneWindow_ReadsZeroAndReportsNoise()
    {
        string? noise = null;
        AllocationMeasurer.NoiseSink = line => noise = line;
        try
        {
            int call = 0;
            AllocationResult result = AllocationMeasurer.Measure(
                () =>
                {
                    call++;
                    if (call == 10 + 150) // inside the second of three windows
                    {
                        _sink = new byte[4096];
                    }
                },
                warmupIterations: 10,
                measuredIterations: 300);

            result.TotalBytes.ShouldBe(0);
            noise.ShouldNotBeNull();
            noise!.ShouldStartWith("ALLOCATION-NOISE:");
            noise.ShouldContain("Measure_BurstInOneWindow_ReadsZeroAndReportsNoise");
        }
        finally
        {
            AllocationMeasurer.NoiseSink = null;
        }
    }
}
