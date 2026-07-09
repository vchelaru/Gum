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

    [Fact]
    public void MeasureMinimum_AllAttemptsAllocate_ReportsNonZero()
    {
        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () => GC.KeepAlive(new byte[64]),
            attempts: 3,
            warmupIterations: 2,
            measuredIterations: 5);

        result.TotalBytes.ShouldBeGreaterThan(0,
            "because a genuine per-iteration allocation must still be caught even after taking the minimum across attempts");
    }

    [Fact]
    public void MeasureMinimum_OnlyFirstAttemptAllocates_ReportsZero()
    {
        const int warmupIterations = 2;
        const int measuredIterations = 5;
        const int firstAttemptInvocations = warmupIterations + measuredIterations;

        int invocationCount = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                invocationCount++;
                if (invocationCount <= firstAttemptInvocations)
                {
                    // Simulate a one-off environmental blip (e.g. JIT tier-up not settling within
                    // the warmup window) that only shows up on the first of several attempts.
                    GC.KeepAlive(new byte[64]);
                }
            },
            attempts: 3,
            warmupIterations: warmupIterations,
            measuredIterations: measuredIterations);

        result.TotalBytes.ShouldBe(0,
            "because MeasureMinimum should return the cleanest of several attempts, not one polluted by a one-off blip");
    }
}
