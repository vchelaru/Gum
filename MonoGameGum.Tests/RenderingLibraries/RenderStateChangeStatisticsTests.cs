using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Unit tests for <see cref="RenderStateChangeStatistics"/> — the per-frame counter of
/// ShapeBatch (Apos.Shapes) begins. Device-free: the counter is a plain accumulator, so the
/// increment/reset contract can be pinned without a MonoGame device. The wiring that drives
/// it (ShapeRenderer recording begins, Renderer.Draw resetting each frame) is covered
/// elsewhere.
/// </summary>
public class RenderStateChangeStatisticsTests : BaseTestClass
{
    [Fact]
    public void RecordShapeBatchBegin_Twice_CountIsTwo()
    {
        RenderStateChangeStatistics statistics = new();

        statistics.RecordShapeBatchBegin();
        statistics.RecordShapeBatchBegin();

        statistics.ShapeBatchBeginCount.ShouldBe(2);
    }

    [Fact]
    public void Reset_AfterRecording_ZeroesCount()
    {
        RenderStateChangeStatistics statistics = new();
        statistics.RecordShapeBatchBegin();

        statistics.Reset();

        statistics.ShapeBatchBeginCount.ShouldBe(0);
    }

    [Fact]
    public void ShapeBatchBeginCount_OnNewInstance_IsZero()
    {
        RenderStateChangeStatistics statistics = new();

        statistics.ShapeBatchBeginCount.ShouldBe(0);
    }
}
