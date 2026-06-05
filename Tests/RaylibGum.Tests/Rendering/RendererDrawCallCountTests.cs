using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// End-to-end tests that drive a real raylib render pass via <see cref="GumService.Default"/> and
/// assert the authoritative draw-call count the owned <c>RenderBatch</c> reports through
/// <see cref="Renderer.RenderStateChangeStatistics"/>. Counts are asserted as deltas against a
/// baseline frame, and content is added as test-root children (which <c>BaseTestClass</c> clears
/// and which reliably detach) so each test is isolated and leaves no residue for later tests.
/// </summary>
public class RendererDrawCallCountTests : BaseTestClass
{
    private static int DrawAndCountDrawCalls()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();
        return Renderer.Self.RenderStateChangeStatistics.DrawCallCount;
    }

    [Fact]
    public void Draw_AddingColoredRectangle_AddsExactlyOneDrawCall()
    {
        int baseline = DrawAndCountDrawCalls();

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 50;
        rectangle.Height = 50;
        GumService.Default.Root.Children.Add(rectangle);
        GumService.Default.Root.UpdateLayout();

        int withRectangle = DrawAndCountDrawCalls();

        GumService.Default.Root.Children.Clear();

        (withRectangle - baseline).ShouldBe(1);
    }

    [Fact]
    public void Draw_ClippedContainerWithChildRectangle_CountsChildAcrossScissorFlush()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;
        container.ClipsChildren = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 50;
        rectangle.Height = 50;
        container.Children.Add(rectangle);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        int withClippedChild = DrawAndCountDrawCalls();

        GumService.Default.Root.Children.Clear();

        // The container clips, so the child draws between a BeginScissorMode/EndScissorMode pair —
        // each of which flushes the batch. The child's single draw must still be banked exactly once.
        (withClippedChild - baseline).ShouldBe(1);
    }

    [Fact]
    public void Draw_Twice_DoesNotAccumulateDrawCallCount()
    {
        int first = DrawAndCountDrawCalls();
        int second = DrawAndCountDrawCalls();

        second.ShouldBe(first);
    }
}
