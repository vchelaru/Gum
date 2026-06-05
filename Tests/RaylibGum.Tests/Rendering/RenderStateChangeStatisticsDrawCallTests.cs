using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="RenderStateChangeStatistics.DrawCallCount"/> accumulator added
/// for the raylib draw-call metric. The counter is a plain accumulator, so the add/reset contract
/// is device-free. The wiring that drives it (the raylib <c>Renderer</c> banking the owned
/// <c>RenderBatch</c>'s draw counter each frame) is covered by the GPU-backed renderer tests.
/// </summary>
public class RenderStateChangeStatisticsDrawCallTests : BaseTestClass
{
    [Fact]
    public void AddDrawCalls_Twice_Accumulates()
    {
        RenderStateChangeStatistics statistics = new();

        statistics.AddDrawCalls(3);
        statistics.AddDrawCalls(2);

        statistics.DrawCallCount.ShouldBe(5);
    }

    [Fact]
    public void DrawCallCount_OnNewInstance_IsZero()
    {
        RenderStateChangeStatistics statistics = new();

        statistics.DrawCallCount.ShouldBe(0);
    }

    [Fact]
    public void Reset_AfterAdding_ZeroesDrawCallCount()
    {
        RenderStateChangeStatistics statistics = new();
        statistics.AddDrawCalls(4);

        statistics.Reset();

        statistics.DrawCallCount.ShouldBe(0);
    }
}
