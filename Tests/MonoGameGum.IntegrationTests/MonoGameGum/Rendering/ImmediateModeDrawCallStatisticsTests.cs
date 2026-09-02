using System.Linq;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pins the immediate-mode entry point (<see cref="Renderer.Begin"/> / <see cref="Renderer.Draw(IRenderableIpso)"/> /
/// <see cref="Renderer.End"/> — what <c>GumBatch</c> wraps) for two bugs reported against it:
/// <see cref="RenderStateChangeStatistics.DrawCallCount"/> was never computed on this path at all
/// (only <see cref="Renderer.Draw(SystemManagers)"/> took the <c>GraphicsDevice.Metrics.DrawCount</c>
/// delta), and both it and <see cref="SpriteRenderer.LastFrameDrawStates"/> were never reset on this
/// path either, so they grew for the life of the process instead of describing one frame. Both are
/// now driven off the same once-per-host-frame boundary <see cref="SystemManagers.Activity"/> already
/// uses for other bookkeeping (<c>NotifyHostFrameAdvanced</c>), so multiple <c>Begin</c>/<c>End</c>
/// cycles in one host frame (one per camera, plus a screen-level overlay pass — the reported FRB2
/// shape) accumulate into one frame's total instead of resetting mid-frame or leaking across frames.
/// </summary>
public class ImmediateModeDrawCallStatisticsTests : BaseTestClass
{
    private static void AdvanceHostFrame(SystemManagers managers, ref double hostTime)
    {
        hostTime += 1.0 / 60.0;
        managers.Activity(hostTime);
    }

    private static RectangleRuntime CreateRectangle()
    {
        RectangleRuntime rectangle = new();
        rectangle.IsFilled = true;
        rectangle.Width = 50;
        rectangle.Height = 50;
        return rectangle;
    }

    /// <summary>
    /// Draws an equivalent rectangle through both entry points and asserts they report the same
    /// draw-call count. This is what pins the "never computed" bug: before the fix, the
    /// immediate-mode side of this comparison was always 0 regardless of what was drawn, while the
    /// layered side (<see cref="Renderer.Draw(SystemManagers)"/>, already pinned by
    /// <c>RendererDrawCallCountTests</c>) reported the real cost. The exact per-rectangle cost is
    /// intentionally not hardcoded here — it's an incidental detail of <see cref="RectangleRuntime"/>'s
    /// fill+stroke rendering, not part of the contract under test.
    /// </summary>
    [Fact]
    public void Begin_Draw_End_ReportsSameDrawCallCountAsLayeredPathForEquivalentContent()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        // Layered-path measurement: a rectangle added to the root, drawn through the existing,
        // already-pinned GumService.Draw() -> Renderer.Draw(SystemManagers) contract.
        RectangleRuntime layeredRectangle = CreateRectangle();
        layeredRectangle.AddToRoot();
        global::Gum.GumService.Default.Root.UpdateLayout();
        global::Gum.GumService.Default.Draw(); // warm-up: discard first-time costs
        global::Gum.GumService.Default.Draw();
        int layeredCount = renderer.RenderStateChangeStatistics.DrawCallCount;

        // Immediate-mode measurement: an equivalent, unparented rectangle drawn directly through
        // Renderer.Begin/Draw/End - the path GumBatch wraps, and the one under test here.
        RectangleRuntime immediateRectangle = CreateRectangle();

        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin();
        renderer.End(); // warm-up: discard first-time costs

        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin();
        renderer.Draw(immediateRectangle);
        renderer.End();
        int immediateModeCount = renderer.RenderStateChangeStatistics.DrawCallCount;

        immediateModeCount.ShouldBe(layeredCount);
    }

    [Fact]
    public void MultipleBeginEndCyclesInSameHostFrame_AccumulateDrawCallCount()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;
        RectangleRuntime rectangle = CreateRectangle();

        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin();
        renderer.Draw(rectangle);
        renderer.End();
        int afterFirstCycle = renderer.RenderStateChangeStatistics.DrawCallCount;
        afterFirstCycle.ShouldBeGreaterThan(0);

        // A second Begin/End cycle in the SAME host frame (no AdvanceHostFrame call) — the FRB2
        // shape of one cycle per camera plus a screen-level overlay pass, all within one frame.
        renderer.Begin();
        renderer.Draw(rectangle);
        renderer.End();
        int afterSecondCycle = renderer.RenderStateChangeStatistics.DrawCallCount;

        afterSecondCycle.ShouldBe(afterFirstCycle * 2);
    }

    [Fact]
    public void NextHostFrame_ResetsDrawCallCount()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;
        RectangleRuntime rectangle = CreateRectangle();

        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin();
        renderer.Draw(rectangle);
        renderer.End();
        int firstFrameCount = renderer.RenderStateChangeStatistics.DrawCallCount;
        firstFrameCount.ShouldBeGreaterThan(0);

        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin();
        renderer.Draw(rectangle);
        renderer.End();
        int secondFrameCount = renderer.RenderStateChangeStatistics.DrawCallCount;

        secondFrameCount.ShouldBe(firstFrameCount);
    }

    [Fact]
    public void LastFrameDrawStates_DoesNotGrowAcrossHostFrames_OnImmediateModePath()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;
        RectangleRuntime rectangle = CreateRectangle();

        // Warm-up: discard the first few cycles' one-time costs before measuring.
        for (int i = 0; i < 5; i++)
        {
            AdvanceHostFrame(managers, ref hostTime);
            renderer.Begin();
            renderer.Draw(rectangle);
            renderer.End();
        }

        int earlyCount = renderer.SpriteRenderer.LastFrameDrawStates.Count();

        const int measuredFrames = 200;
        for (int i = 0; i < measuredFrames; i++)
        {
            AdvanceHostFrame(managers, ref hostTime);
            renderer.Begin();
            renderer.Draw(rectangle);
            renderer.End();
        }

        int lateCount = renderer.SpriteRenderer.LastFrameDrawStates.Count();

        // Without a per-host-frame reset this list would grow by one entry per measured frame
        // instead of describing just the latest one.
        lateCount.ShouldBe(earlyCount);
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
            global::Gum.GumService.Default.Initialize(this, global::Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (global::Gum.GumService.Default.IsInitialized)
            {
                global::Gum.GumService.Default.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
