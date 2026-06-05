using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// GPU-backed isolation tests for <see cref="BatchDrawCallCounter"/>. They exercise the owned-
/// <c>RenderBatch</c> interop directly (load / pin / activate / bank / deactivate) against the
/// hidden raylib window opened by the test harness, independent of the full renderer walk.
/// </summary>
public class BatchDrawCallCounterTests : BaseTestClass
{
    [Fact]
    public void BeginPass_EmptyPass_RecordsZeroDrawCalls()
    {
        BatchDrawCallCounter counter = new();
        RenderStateChangeStatistics statistics = new();

        BeginDrawing();
        counter.BeginPass(statistics);
        counter.EndPass();
        EndDrawing();

        statistics.DrawCallCount.ShouldBe(0);
    }

    [Fact]
    public void Bank_SingleRectangle_RecordsOneDrawCall()
    {
        BatchDrawCallCounter counter = new();
        RenderStateChangeStatistics statistics = new();

        BeginDrawing();
        counter.BeginPass(statistics);
        DrawRectangle(0, 0, 10, 10, Color.Red);
        counter.EndPass();
        EndDrawing();

        statistics.DrawCallCount.ShouldBe(1);
    }

    [Fact]
    public void Bank_TwoTexturelessShapesSameMode_CoalesceToOneDrawCall()
    {
        BatchDrawCallCounter counter = new();
        RenderStateChangeStatistics statistics = new();

        BeginDrawing();
        counter.BeginPass(statistics);
        DrawRectangle(0, 0, 10, 10, Color.Red);
        DrawRectangle(20, 20, 10, 10, Color.Blue);
        counter.EndPass();
        EndDrawing();

        statistics.DrawCallCount.ShouldBe(1);
    }
}
