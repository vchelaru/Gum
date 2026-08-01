using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Covers the render-target bake walk after it was migrated onto the shared
/// <see cref="IRenderableOrderer"/> subtree entry point (#4154). The bake previously ran its own
/// recursive walk with no off-screen cull, so a clipping container inside a render target drew
/// every scrolled-off descendant. Going through the shared builder gives the bake the same
/// visibility / cull / clip semantics as the main pass — matching raylib, which already culled
/// inside bakes.
/// </summary>
public class RenderTargetBakeClipWalkTests : BaseTestClass
{
    [Fact]
    public void ClippedChild_ScrolledOutsideClip_InsideRenderTargetBake_IsCulled()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime renderTargetRoot = new();
        renderTargetRoot.X = 0;
        renderTargetRoot.Y = 0;
        renderTargetRoot.Width = 200;
        renderTargetRoot.Height = 200;
        renderTargetRoot.IsRenderTarget = true;

        // Clips to the top half of the render target: absolute Y [0, 100].
        ContainerRuntime clipParent = new();
        clipParent.Width = 200;
        clipParent.Height = 100;
        clipParent.ClipsChildren = true;

        RenderCountingRuntime inside = new();
        inside.Width = 180;
        inside.Height = 80;
        inside.Y = 0;
        clipParent.AddChild(inside);

        // Absolute Y [150, 190] — past the clip bottom plus its cull margin, but still inside the
        // 200-tall render target, so only the cull (not the render-target bounds) can skip it.
        RenderCountingRuntime outside = new();
        outside.Width = 180;
        outside.Height = 40;
        outside.Y = 150;
        clipParent.AddChild(outside);

        renderTargetRoot.AddChild(clipParent);
        renderTargetRoot.AddToManagers(managers, null);
        renderTargetRoot.UpdateLayout();

        try
        {
            CameraScissorExtensions.CullOffscreenWhenClipped = false;
            renderer.Draw(managers);
            inside.RenderCallCount.ShouldBeGreaterThan(0);
            outside.RenderCallCount.ShouldBeGreaterThan(0);

            inside.ResetRenderCallCount();
            outside.ResetRenderCallCount();

            CameraScissorExtensions.CullOffscreenWhenClipped = true;
            renderer.Draw(managers);
            inside.RenderCallCount.ShouldBeGreaterThan(0);
            outside.RenderCallCount.ShouldBe(0);
        }
        finally
        {
            CameraScissorExtensions.CullOffscreenWhenClipped = true;
            renderTargetRoot.RemoveFromManagers();
        }
    }

    [Fact]
    public void ClippedContent_InsideRenderTargetBake_ClipsToItsContainer()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        const int renderTargetSize = 200;

        ContainerRuntime renderTargetRoot = new();
        renderTargetRoot.X = 0;
        renderTargetRoot.Y = 0;
        renderTargetRoot.Width = renderTargetSize;
        renderTargetRoot.Height = renderTargetSize;
        renderTargetRoot.IsRenderTarget = true;

        // Clips to the top half of the render target: absolute Y [0, 100].
        ContainerRuntime clipParent = new();
        clipParent.Width = renderTargetSize;
        clipParent.Height = 100;
        clipParent.ClipsChildren = true;

        // Taller than the clip, so its bottom half must be scissored away inside the bake.
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill without the shape dependency.
        ColoredRectangleRuntime tallChild = new();
#pragma warning restore CS0618
        tallChild.Width = 180;
        tallChild.Height = 180;
        tallChild.Y = 0;
        tallChild.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);
        clipParent.AddChild(tallChild);

        renderTargetRoot.AddChild(clipParent);
        renderTargetRoot.AddToManagers(managers, null);
        renderTargetRoot.UpdateLayout();

        try
        {
            renderer.Draw(managers);
            gd.SetRenderTarget(null);

            RenderTarget2D baked = renderer.TryGetBakedRenderTargetFor(renderTargetRoot)!;
            baked.Width.ShouldBe(renderTargetSize);
            baked.Height.ShouldBe(renderTargetSize);

            Color[] pixels = new Color[baked.Width * baked.Height];
            baked.GetData(pixels);

            Color insideClip = pixels[(40 * baked.Width) + 90];
            Color belowClip = pixels[(150 * baked.Width) + 90];

            insideClip.G.ShouldBeGreaterThan((byte)200);
            belowClip.A.ShouldBeLessThan((byte)50);
        }
        finally
        {
            renderTargetRoot.RemoveFromManagers();
        }
    }

    /// <summary>
    /// Renderable that records how many times the walk asked it to draw. Draws nothing itself —
    /// the count is the observable, which is what separates "culled" from "drawn but scissored".
    /// </summary>
    private sealed class RenderCountingRenderable : InvisibleRenderable
    {
        public int RenderCallCount { get; private set; }

        public void ResetRenderCallCount() => RenderCallCount = 0;

        public override void Render(ISystemManagers managers) => RenderCallCount++;
    }

    /// <summary>
    /// Runtime wrapper for <see cref="RenderCountingRenderable"/> so it can be positioned and
    /// nested through normal Gum layout, following the standard Runtime-wraps-Renderable pattern.
    /// </summary>
    private sealed class RenderCountingRuntime : GraphicalUiElement
    {
        private readonly RenderCountingRenderable _renderable;

        public RenderCountingRuntime() : base(new RenderCountingRenderable(), whatContainsThis: null)
        {
            _renderable = (RenderCountingRenderable)RenderableComponent;
        }

        public int RenderCallCount => _renderable.RenderCallCount;

        public void ResetRenderCallCount() => _renderable.ResetRenderCallCount();
    }

    /// <summary>
    /// Minimal Game host that initializes a fresh <see cref="GumService"/> per test so
    /// <see cref="Renderer.Draw(SystemManagers)"/> can be invoked against a live device.
    /// </summary>
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
            Gum.GumService.Default.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
