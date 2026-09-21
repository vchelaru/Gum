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
    public void IsActive_AfterEndPass_IsFalse()
    {
        BatchDrawCallCounter counter = new();
        RenderStateChangeStatistics statistics = new();

        BeginDrawing();
        counter.BeginPass(statistics);
        counter.EndPass();
        EndDrawing();

        counter.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void IsActive_BetweenBeginPassAndEndPass_IsTrue()
    {
        BatchDrawCallCounter counter = new();
        RenderStateChangeStatistics statistics = new();

        BeginDrawing();
        counter.BeginPass(statistics);

        // The GL window used by this test harness is already up from earlier tests in the process
        // (the batch is initialized lazily, once, for the process lifetime — see BatchDrawCallCounter's
        // class remarks), so BeginPass is expected to activate counting here.
        counter.IsActive.ShouldBeTrue();

        counter.EndPass();
        EndDrawing();
    }
}
