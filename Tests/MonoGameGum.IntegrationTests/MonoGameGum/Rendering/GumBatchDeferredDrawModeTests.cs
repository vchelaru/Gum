using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pins <see cref="Renderer.GumBatchDrawMode.Deferred"/> (issue #4573): multiple <see cref="Renderer.Draw(IRenderableIpso)"/>
/// calls within one <see cref="Renderer.Begin"/>/<see cref="Renderer.End"/> cycle accumulate and run
/// through the active <see cref="Renderer.SiblingOrdering"/> once at <c>End</c>, instead of each call
/// submitting immediately and unbatched. <see cref="Renderer.GumBatchDrawMode.Immediate"/> (the
/// default, no mode argument) is pinned unchanged here - this feature is purely additive.
/// </summary>
public class GumBatchDeferredDrawModeTests : BaseTestClass
{
    private static void AdvanceHostFrame(SystemManagers managers, ref double hostTime)
    {
        hostTime += 1.0 / 60.0;
        managers.Activity(hostTime);
    }

    private static SpriteRuntime CreateSprite(Texture2D texture, float y)
    {
        SpriteRuntime sprite = new();
        sprite.Texture = texture;
        sprite.Y = y;
        return sprite;
    }

    [Fact]
    public void Immediate_AlternatingTextureSprites_StillAddsOneDrawCallPerSprite()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Texture2D textureA = new(game.GraphicsDevice, 1, 1);
        textureA.SetData(new[] { Color.Red });
        Texture2D textureB = new(game.GraphicsDevice, 1, 1);
        textureB.SetData(new[] { Color.Blue });

        IRenderableOrderer originalOrdering = Renderer.SiblingOrdering;
        try
        {
            Renderer.SiblingOrdering = BatchKeyGroupedOrderer.Instance;

            const int spriteCount = 10;
            SpriteRuntime[] sprites = new SpriteRuntime[spriteCount];
            for (int i = 0; i < spriteCount; i++)
            {
                sprites[i] = CreateSprite(i % 2 == 0 ? textureA : textureB, i * 10);
            }

            AdvanceHostFrame(managers, ref hostTime);
            renderer.Begin(); // Immediate is the default - no mode argument.
            foreach (SpriteRuntime sprite in sprites)
            {
                renderer.Draw(sprite);
            }
            renderer.End();

            renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(spriteCount);
        }
        finally
        {
            Renderer.SiblingOrdering = originalOrdering;
            textureA.Dispose();
            textureB.Dispose();
        }
    }

    [Fact]
    public void Deferred_AlternatingTextureSprites_CollapsesToOneDrawCallPerTexture()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Texture2D textureA = new(game.GraphicsDevice, 1, 1);
        textureA.SetData(new[] { Color.Red });
        Texture2D textureB = new(game.GraphicsDevice, 1, 1);
        textureB.SetData(new[] { Color.Blue });

        IRenderableOrderer originalOrdering = Renderer.SiblingOrdering;
        try
        {
            Renderer.SiblingOrdering = BatchKeyGroupedOrderer.Instance;

            const int spriteCount = 10;
            SpriteRuntime[] sprites = new SpriteRuntime[spriteCount];
            for (int i = 0; i < spriteCount; i++)
            {
                sprites[i] = CreateSprite(i % 2 == 0 ? textureA : textureB, i * 10);
            }

            AdvanceHostFrame(managers, ref hostTime);
            renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
            foreach (SpriteRuntime sprite in sprites)
            {
                renderer.Draw(sprite);
            }
            renderer.End();

            // Same alternating-texture scene as the Immediate test above, but now batched across
            // the separate Draw() calls since they share one End()-time BuildDrawList/Submit pass:
            // one draw call per distinct texture instead of one per sprite.
            renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(2);
        }
        finally
        {
            Renderer.SiblingOrdering = originalOrdering;
            textureA.Dispose();
            textureB.Dispose();
        }
    }

    /// <summary>
    /// Ground-truth check prompted by an FRB2 report of RenderStateChangeStatistics.DrawCallCount
    /// (13) exceeding the real GraphicsDevice.Metrics.DrawCount total for the whole frame (8) - a
    /// subset can never legitimately exceed the whole. This independently measures the true
    /// Metrics delta across two Begin/End cycles in one host frame (mirroring FRB2's per-camera +
    /// overlay shape) and asserts Gum's own tally matches it exactly, to prove or disprove that
    /// Renderer's own delta-tracking is the source of the discrepancy.
    /// </summary>
    [Fact]
    public void MultipleCyclesInOneHostFrame_DrawCallCountMatchesTrueGpuTotalForTheFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Texture2D texture = new(game.GraphicsDevice, 1, 1);
        texture.SetData(new[] { Color.Red });

        RectangleRuntime cardA = new();
        cardA.IsFilled = true;
        cardA.Width = 10;
        cardA.Height = 10;
        RectangleRuntime cardB = new();
        cardB.IsFilled = true;
        cardB.Width = 10;
        cardB.Height = 10;
        cardB.X = 50;
        SpriteRuntime overlayText = CreateSprite(texture, 0);

        try
        {
            // Warm-up cycle: discard first-time costs (e.g. default texture load) before
            // measuring, matching the pattern used elsewhere in this file.
            AdvanceHostFrame(managers, ref hostTime);
            renderer.Begin();
            renderer.End();

            AdvanceHostFrame(managers, ref hostTime);
            long trueDrawCountBefore = game.GraphicsDevice.Metrics.DrawCount;

            // Cycle 1: "main camera" pass drawing two cards, Deferred (FRB2's actual mode).
            renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
            renderer.Draw(cardA);
            renderer.Draw(cardB);
            renderer.End();

            // Cycle 2: "overlay" pass - a second Begin/End cycle in the SAME host frame, matching
            // FRB2's per-camera-plus-overlay shape.
            renderer.Begin();
            renderer.Draw(overlayText);
            renderer.End();

            long trueDrawCountAfter = game.GraphicsDevice.Metrics.DrawCount;
            int trueDrawCallCountThisFrame = (int)(trueDrawCountAfter - trueDrawCountBefore);

            renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(trueDrawCallCountThisFrame);
        }
        finally
        {
            texture.Dispose();
        }
    }

    /// <summary>
    /// Pins the Renderer-side wiring for issue #4575's follow-up: Renderer resets
    /// BatchKeyGroupedOrderer's break tally at the same points it resets
    /// RenderStateChangeStatistics - once per host frame on this (Deferred) path - so it
    /// accumulates across multiple Begin/End cycles in one frame instead of the second cycle
    /// wiping out the first's tally, and still resets cleanly on the next frame.
    /// </summary>
    [Fact]
    public void Deferred_MultipleCyclesInOneHostFrame_AccumulateOrdererTally_ResetsNextFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Texture2D textureA = new(game.GraphicsDevice, 1, 1);
        textureA.SetData(new[] { Color.Red });
        Texture2D textureB = new(game.GraphicsDevice, 1, 1);
        textureB.SetData(new[] { Color.Blue });

        IRenderableOrderer originalOrdering = Renderer.SiblingOrdering;
        try
        {
            Renderer.SiblingOrdering = BatchKeyGroupedOrderer.Instance;

            void RunOneCycleOfAlternatingSprites()
            {
                renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
                for (int i = 0; i < 4; i++)
                {
                    renderer.Draw(CreateSprite(i % 2 == 0 ? textureA : textureB, i * 10));
                }
                renderer.End();
            }

            AdvanceHostFrame(managers, ref hostTime);
            RunOneCycleOfAlternatingSprites();
            ((BatchKeyGroupedOrderer)Renderer.SiblingOrdering).NoCandidateInWindowBreakCount.ShouldBe(1);

            RunOneCycleOfAlternatingSprites(); // same host frame - no AdvanceHostFrame call
            ((BatchKeyGroupedOrderer)Renderer.SiblingOrdering).NoCandidateInWindowBreakCount.ShouldBe(2);

            AdvanceHostFrame(managers, ref hostTime);
            RunOneCycleOfAlternatingSprites();
            ((BatchKeyGroupedOrderer)Renderer.SiblingOrdering).NoCandidateInWindowBreakCount.ShouldBe(1);
        }
        finally
        {
            Renderer.SiblingOrdering = originalOrdering;
            textureA.Dispose();
            textureB.Dispose();
        }
    }

    /// <summary>
    /// Records <see cref="SpriteRenderer.ForcedMatrix"/> as it stands at the moment this renderable
    /// is actually submitted. Every re-<c>BeginSpriteBatch</c> (a clip enter/exit, a batch flush)
    /// composes ForcedMatrix into the transform it hands the SpriteBatch, so whatever this captures
    /// is what the GPU would be drawing with.
    /// </summary>
    private class ForcedMatrixProbe : ContainerRuntime
    {
        public Matrix? ObservedForcedMatrix { get; private set; }
        public bool WasRendered { get; private set; }

        public override void Render(ISystemManagers managers)
        {
            ObservedForcedMatrix = ((SystemManagers)managers).Renderer.SpriteRenderer.ForcedMatrix;
            WasRendered = true;
            base.Render(managers);
        }
    }

    [Fact]
    public void Deferred_WithForcedMatrixAndAClip_StillHasForcedMatrixWhenTheClippedSubtreeSubmits()
    {
        // Renderer.End() clears spriteRenderer.ForcedMatrix, then runs the Deferred submit. In
        // Immediate mode every Draw() had already submitted while the matrix was still set, so
        // nothing noticed. In Deferred mode the whole submit - including the re-BeginSpriteBatch
        // that entering a clip forces - happens after the clear, so the clipped subtree renders at
        // the camera transform instead of the caller's.
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Matrix forced = Matrix.CreateScale(3f);

        ContainerRuntime clip = new();
        clip.ClipsChildren = true;
        clip.Width = 100;
        clip.Height = 100;

        ForcedMatrixProbe probe = new();
        probe.Width = 10;
        probe.Height = 10;
        clip.Children.Add(probe);

        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin(forced, Renderer.GumBatchDrawMode.Immediate);
        renderer.Draw(clip);
        renderer.End();

        probe.WasRendered.ShouldBeTrue();
        probe.ObservedForcedMatrix.ShouldBe(forced, "Immediate mode baseline");

        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin(forced, Renderer.GumBatchDrawMode.Deferred);
        renderer.Draw(clip);
        renderer.End();

        // The two modes must agree - Deferred is meant to change batching, not transforms.
        probe.ObservedForcedMatrix.ShouldBe(forced);

        // And the matrix must not leak into the next cycle, which passes none.
        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin(null, Renderer.GumBatchDrawMode.Deferred);
        renderer.Draw(clip);
        renderer.End();

        probe.ObservedForcedMatrix.ShouldBeNull();
    }

    /// <summary>
    /// Records the order renderables actually reach <c>Render</c> in.
    /// </summary>
    private class OrderProbe : ContainerRuntime
    {
        private readonly List<string> _log;
        private readonly string _label;

        public OrderProbe(List<string> log, string label, float y, float z)
        {
            _log = log;
            _label = label;
            Y = y;
            Z = z;
            Width = 10;
            Height = 10;
        }

        public override void Render(ISystemManagers managers)
        {
            _log.Add(_label);
            base.Render(managers);
        }
    }

    /// <summary>
    /// Issue #4583 proposed passing <c>_layers[0].SecondarySortOnY</c> to the deferred flush's
    /// <c>SortByZ</c> so Deferred would stop diverging from Immediate on equal-Z renderables. The
    /// premise has it backwards: the GumBatch path never adds its renderables to layer 0 (that
    /// layer is only a render-state / clip-bounds source), so Immediate does no sorting at all - it
    /// submits in call order - and Deferred's stable Z-sort leaves equal-Z entries in call order
    /// too. The two modes already agree; honoring SecondarySortOnY in the deferred flush is what
    /// would introduce a divergence. This pins the agreement.
    /// </summary>
    [Fact]
    public void SecondarySortOnYOnLayerZero_DoesNotReorderEqualZDraws_InEitherMode()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Layer layerZero = renderer.Layers[0];
        bool originalSecondarySortOnY = layerZero.SecondarySortOnY;
        try
        {
            layerZero.SecondarySortOnY = true;

            // Drawn in an order that disagrees with Y, all at the same Z, so a Y-based secondary
            // sort would visibly reorder them.
            AdvanceHostFrame(managers, ref hostTime);
            List<string> immediateOrder = new();
            renderer.Begin();
            renderer.Draw(new OrderProbe(immediateOrder, "A", y: 30, z: 0));
            renderer.Draw(new OrderProbe(immediateOrder, "B", y: 10, z: 0));
            renderer.Draw(new OrderProbe(immediateOrder, "C", y: 20, z: 0));
            renderer.End();

            immediateOrder.ShouldBe(new[] { "A", "B", "C" });

            AdvanceHostFrame(managers, ref hostTime);
            List<string> deferredOrder = new();
            renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
            renderer.Draw(new OrderProbe(deferredOrder, "A", y: 30, z: 0));
            renderer.Draw(new OrderProbe(deferredOrder, "B", y: 10, z: 0));
            renderer.Draw(new OrderProbe(deferredOrder, "C", y: 20, z: 0));
            renderer.End();

            deferredOrder.ShouldBe(immediateOrder);
        }
        finally
        {
            layerZero.SecondarySortOnY = originalSecondarySortOnY;
        }
    }

    /// <summary>
    /// Renders normally but records how many times it was submitted.
    /// </summary>
    private class RenderCountProbe : ContainerRuntime
    {
        public int RenderCount { get; private set; }

        public RenderCountProbe()
        {
            Width = 10;
            Height = 10;
        }

        public override void Render(ISystemManagers managers)
        {
            RenderCount++;
            base.Render(managers);
        }
    }

    /// <summary>
    /// Throws out of <c>Render</c>, so the deferred flush's Submit throws mid-cycle.
    /// </summary>
    private class ThrowingRenderProbe : RenderCountProbe
    {
        public override void Render(ISystemManagers managers)
        {
            base.Render(managers);
            throw new InvalidOperationException("Simulated renderable failure.");
        }
    }

    /// <summary>
    /// Issue #4584: the deferred flush cleared its accumulated roots only after Submit returned, so
    /// a renderable that threw left the whole failed cycle queued. The next Begin/End then drew the
    /// previous cycle's roots on top of its own - a stale-draw corruption in whatever frame follows
    /// an exception a game catches and continues from.
    /// </summary>
    [Fact]
    public void Deferred_WhenSubmitThrows_DoesNotResubmitTheFailedCycleOnTheNextCycle()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        ThrowingRenderProbe thrower = new();
        RenderCountProbe nextCycleProbe = new();

        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
        renderer.Draw(thrower);
        Should.Throw<InvalidOperationException>(() => renderer.End());

        thrower.RenderCount.ShouldBe(1);

        AdvanceHostFrame(managers, ref hostTime);
        renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
        renderer.Draw(nextCycleProbe);
        Should.NotThrow(() => renderer.End());

        thrower.RenderCount.ShouldBe(1, "the failed cycle's roots must not survive into the next cycle");
        nextCycleProbe.RenderCount.ShouldBe(1);
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
