using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pins the premultiplied-alpha composite fix for <see cref="ContainerRuntime.IsRenderTarget"/>
/// containers (issue #1696). <see cref="Renderer.RenderToRenderTarget"/> always bakes a render
/// target's children over a transparent clear, which yields premultiplied contents regardless of
/// <see cref="Renderer.NormalBlendState"/>. The composite-back blit in
/// <see cref="Renderer.DrawRenderTargetToScreen"/> must therefore (1) blend with
/// <c>BlendState.AlphaBlend</c> instead of the container's default (non-premultiplied) blend, and
/// (2) tint with a premultiplied alpha color, or the result darkens (wrong blend) and/or
/// re-lightens under group alpha (wrong tint) relative to the same content drawn directly.
/// </summary>
public class RenderTargetAlphaCompositeTests : BaseTestClass
{
    // Mirrors RaylibGum.Tests' Draw_SemiTransparentElement_InsideRenderTargetMatchesDirectDraw.
    // Container.Alpha stays at its default (255) here, so the tint is (255,255,255,255) either
    // way — this isolates the blend-state half of the fix from the tint half.
    [Fact]
    public void SemiTransparentElement_InsideRenderTarget_MatchesDirectDraw()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        Color frameColor = new Color((byte)70, (byte)70, (byte)90, (byte)255);
        Color semiTransparentWhite = new Color((byte)255, (byte)255, (byte)255, (byte)128);

        ContainerRuntime directRoot = new();
        directRoot.X = 0;
        directRoot.Y = 0;
        directRoot.Width = 100;
        directRoot.Height = 100;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill without the shape dependency.
        directRoot.AddChild(new ColoredRectangleRuntime { Width = 100, Height = 100, Color = frameColor });
        directRoot.AddChild(new ColoredRectangleRuntime { Width = 100, Height = 100, Color = semiTransparentWhite });
#pragma warning restore CS0618
        directRoot.AddToManagers(managers, null);
        directRoot.UpdateLayout();

        Color direct = RenderToCaptureAndSample(gd, renderer, managers, 100, 100, 50, 50);

        directRoot.RemoveFromManagers();

        ContainerRuntime rtRoot = new();
        rtRoot.X = 0;
        rtRoot.Y = 0;
        rtRoot.Width = 100;
        rtRoot.Height = 100;
#pragma warning disable CS0618
        rtRoot.AddChild(new ColoredRectangleRuntime { Width = 100, Height = 100, Color = frameColor });
#pragma warning restore CS0618

        ContainerRuntime inner = new();
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;
#pragma warning disable CS0618
        inner.AddChild(new ColoredRectangleRuntime { Width = 100, Height = 100, Color = semiTransparentWhite });
#pragma warning restore CS0618
        rtRoot.AddChild(inner);

        rtRoot.AddToManagers(managers, null);
        rtRoot.UpdateLayout();

        Color viaRenderTarget = RenderToCaptureAndSample(gd, renderer, managers, 100, 100, 50, 50);

        rtRoot.RemoveFromManagers();

        // Only RGB is compared: the outer capture texture's own alpha channel accumulates
        // differently for the two scenes (the direct scene draws two overlapping semi-transparent
        // layers into the capture; the RT scene draws one opaque layer plus one fully-covering
        // composited blit), which is an artifact of the readback capture technique, not a
        // difference a user would ever see on screen (there is no further compositing after the
        // real backbuffer).
        const int tolerance = 4;
        System.Math.Abs(viaRenderTarget.R - direct.R).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(viaRenderTarget.G - direct.G).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(viaRenderTarget.B - direct.B).ShouldBeLessThanOrEqualTo(tolerance);
    }

    // Worked example from the issue: a 50%-alpha white child inside a render-target container
    // whose group Alpha is also reduced to 50%. Isolates the tint half of the fix — reverting
    // just the blend fix or just the tint fix each land far outside tolerance of the correct
    // ~116, so this single assertion requires both halves of the fix together.
    [Fact]
    public void ReducedGroupAlpha_OnRenderTarget_CompositesToPremultipliedExpectedValue()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        Color frameColor = new Color((byte)70, (byte)70, (byte)90, (byte)255);
        Color semiTransparentWhite = new Color((byte)255, (byte)255, (byte)255, (byte)128);

        ContainerRuntime root = new();
        root.X = 0;
        root.Y = 0;
        root.Width = 100;
        root.Height = 100;
#pragma warning disable CS0618
        root.AddChild(new ColoredRectangleRuntime { Width = 100, Height = 100, Color = frameColor });
#pragma warning restore CS0618

        ContainerRuntime inner = new();
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;
        inner.Alpha = 128;
#pragma warning disable CS0618
        inner.AddChild(new ColoredRectangleRuntime { Width = 100, Height = 100, Color = semiTransparentWhite });
#pragma warning restore CS0618
        root.AddChild(inner);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        Color composited = RenderToCaptureAndSample(gd, renderer, managers, 100, 100, 50, 50);

        root.RemoveFromManagers();

        const int expected = 117;
        const int tolerance = 6;
        System.Math.Abs(composited.R - expected).ShouldBeLessThanOrEqualTo(tolerance);
    }

    private static Color RenderToCaptureAndSample(GraphicsDevice gd, Renderer renderer, SystemManagers managers, int w, int h, int sx, int sy)
    {
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(Color.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        Color[] data = new Color[w * h];
        capture.GetData(data);

        return data[(sy * w) + sx];
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
