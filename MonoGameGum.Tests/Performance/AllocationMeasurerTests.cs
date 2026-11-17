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
