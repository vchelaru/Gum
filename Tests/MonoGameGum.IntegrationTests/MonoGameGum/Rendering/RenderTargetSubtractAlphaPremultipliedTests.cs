using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Forms;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #5678: in a premultiplied pipeline (<c>NormalBlendState == AlphaBlend</c>, as FRB's GumIdb
/// sets) a partially transparent <c>SubtractAlpha</c> mask must scale the destination by
/// (1 - srcAlpha) in both color and alpha. <c>SubtractAlphaPremultiplied</c> used to subtract the
/// mask's color from the destination color instead, so a partially transparent mask left the
/// color at (dest - maskColor) rather than dest * (1 - alpha), so it disagreed with the lowered alpha.
/// Premultiplied input is authored directly in the mask color (this harness has no FRB shader).
/// </summary>
public class RenderTargetSubtractAlphaPremultipliedTests : BaseTestClass
{
    private const int CaptureSize = 100;
    private static readonly XnaColor BackdropColor = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    private static readonly XnaColor FillColor = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);

    [Fact]
    public void PremultipliedHalfAlphaWhiteMask_InRenderTarget_HalvesFillColorAndAlpha()
    {
        Gum.BlendState previous = Renderer.NormalBlendState;
        try
        {
            Renderer.NormalBlendState = Gum.BlendState.AlphaBlend;

            // Premultiplied white at ~50% alpha (color 128, alpha 128). Correct bake: fill * 0.5 with alpha 0.5.
            XnaColor[] pixels = Render(new XnaColor((byte)128, (byte)128, (byte)128, (byte)128));

            XnaColor hole = pixels[(50 * CaptureSize) + 50];
            const int tolerance = 6;
            System.Math.Abs(hole.R - 100).ShouldBeLessThanOrEqualTo(tolerance);
            System.Math.Abs(hole.G - 15).ShouldBeLessThanOrEqualTo(tolerance);
            System.Math.Abs(hole.B - 15).ShouldBeLessThanOrEqualTo(tolerance);
            System.Math.Abs(hole.A - 128).ShouldBeLessThanOrEqualTo(tolerance);
        }
        finally
        {
            Renderer.NormalBlendState = previous;
        }
    }

    private static XnaColor[] Render(XnaColor premultipliedMaskColor)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime root = new();
        root.Width = CaptureSize;
        root.Height = CaptureSize;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = BackdropColor });

        ContainerRuntime renderTarget = new();
        renderTarget.Width = CaptureSize;
        renderTarget.Height = CaptureSize;
        renderTarget.IsRenderTarget = true;
        renderTarget.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = FillColor });

        ColoredRectangleRuntime mask = new() { X = 20, Y = 20, Width = 60, Height = 60, Color = premultipliedMaskColor };
        mask.BlendState = Gum.BlendState.SubtractAlphaPremultiplied.ToXNA();
        renderTarget.AddChild(mask);
#pragma warning restore CS0618
        root.AddChild(renderTarget);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureSize * CaptureSize];
        // The composite-back blit is not read: this harness has no FRB shader, so it can't model the
        // premultiplied blit. The baked target is what the blend under test writes.
        RenderTarget2D baked = renderer.TryGetBakedRenderTargetFor(renderTarget)!;
        baked.GetData(pixels);

        root.RemoveFromManagers();
        return pixels;
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
            };
        }

        protected override void Initialize()
        {
            base.Initialize();
            Gum.GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.Black);

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
