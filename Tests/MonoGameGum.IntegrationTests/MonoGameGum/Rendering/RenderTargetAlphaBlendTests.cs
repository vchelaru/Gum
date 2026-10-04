using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Forms;
using Gum.GueDeriving;
using MonoGameAndGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issues #5673 / #5682: a child with <c>Blend = ReplaceAlpha</c> or <c>MinAlpha</c> inside a
/// render-target container must leave the baked color premultiplied against the alpha it writes,
/// so the composite-back blit shows (fill * a + backdrop * (1 - a)) rather than leftover color
/// bleeding through. Same class of fault as #5671. Covers SpriteBatch (a rectangle) and Apos.Shapes
/// (a circle). The mask is 50% alpha: MinAlpha over an opaque fill yields 50%, ReplaceAlpha writes
/// 50%, so both expect an even mix of fill and backdrop inside the mask and plain fill outside.
/// </summary>
public class RenderTargetAlphaBlendTests : BaseTestClass
{
    private const int CaptureSize = 100;
    private static readonly XnaColor BackdropColor = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    private static readonly XnaColor FillColor = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);
    private const int MaskAlpha = 128;

    [Fact]
    public void ReplaceAlphaRectangle_InRenderTarget_MixesFillAndBackdrop() =>
        AssertMasked(Render(useCircle: false, Gum.RenderingLibrary.Blend.ReplaceAlpha));

    [Fact]
    public void ReplaceAlphaCircle_InRenderTarget_MixesFillAndBackdrop() =>
        AssertMasked(Render(useCircle: true, Gum.RenderingLibrary.Blend.ReplaceAlpha));

    [Fact]
    public void MinAlphaRectangle_InRenderTarget_MixesFillAndBackdrop() =>
        AssertMasked(Render(useCircle: false, Gum.RenderingLibrary.Blend.MinAlpha));

    [Fact]
    public void MinAlphaCircle_InRenderTarget_MixesFillAndBackdrop() =>
        AssertMasked(Render(useCircle: true, Gum.RenderingLibrary.Blend.MinAlpha));

    // A normal shape followed by a masked shape in one bake exercises ShapeRenderer.EnsureBlend,
    // which re-opens the batch mid-run and must also apply the bake swap.
    [Fact]
    public void ReplaceAlphaCircle_AfterNormalCircle_InRenderTarget_MixesFillAndBackdrop() =>
        AssertMixedBatch(Render(useCircle: true, Gum.RenderingLibrary.Blend.ReplaceAlpha, normalCircleFill: true));

    [Fact]
    public void MinAlphaCircle_AfterNormalCircle_InRenderTarget_MixesFillAndBackdrop() =>
        AssertMixedBatch(Render(useCircle: true, Gum.RenderingLibrary.Blend.MinAlpha, normalCircleFill: true));

    private static void AssertMixedBatch(XnaColor[] pixels)
    {
        const int tolerance = 8;
        float a = MaskAlpha / 255f;

        XnaColor masked = pixels[(50 * CaptureSize) + 50];
        System.Math.Abs(masked.R - (FillColor.R * a + BackdropColor.R * (1 - a))).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(masked.G - (FillColor.G * a + BackdropColor.G * (1 - a))).ShouldBeLessThanOrEqualTo(tolerance);

        // Inside the normal circle but outside the mask: plain fill, so the normal shape was
        // drawn with its own blend.
        XnaColor fillOnly = pixels[(25 * CaptureSize) + 50];
        System.Math.Abs(fillOnly.R - FillColor.R).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(fillOnly.G - FillColor.G).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(fillOnly.B - FillColor.B).ShouldBeLessThanOrEqualTo(tolerance);
    }

    private static void AssertMasked(XnaColor[] pixels)
    {
        const int tolerance = 8;
        float a = MaskAlpha / 255f;

        XnaColor inside = pixels[(50 * CaptureSize) + 50];
        System.Math.Abs(inside.R - (FillColor.R * a + BackdropColor.R * (1 - a))).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(inside.G - (FillColor.G * a + BackdropColor.G * (1 - a))).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(inside.B - (FillColor.B * a + BackdropColor.B * (1 - a))).ShouldBeLessThanOrEqualTo(tolerance);

        XnaColor outside = pixels[(5 * CaptureSize) + 5];
        System.Math.Abs(outside.R - FillColor.R).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(outside.G - FillColor.G).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(outside.B - FillColor.B).ShouldBeLessThanOrEqualTo(tolerance);
    }

    private static XnaColor[] Render(bool useCircle, Gum.RenderingLibrary.Blend blend, bool normalCircleFill = false)
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
        if (normalCircleFill)
        {
            CircleRuntime fill = new();
            fill.Width = CaptureSize;
            fill.Height = CaptureSize;
            fill.IsFilled = true;
            fill.StrokeWidth = 0;
            fill.FillColor = FillColor;
            renderTarget.AddChild(fill);
        }
        else
        {
            renderTarget.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = FillColor });
        }

        if (useCircle)
        {
            CircleRuntime circle = new();
            circle.X = normalCircleFill ? 35 : 20;
            circle.Y = normalCircleFill ? 35 : 20;
            circle.Width = normalCircleFill ? 30 : 60;
            circle.Height = normalCircleFill ? 30 : 60;
            circle.IsFilled = true;
            circle.StrokeWidth = 0;
            circle.FillColor = new XnaColor((byte)255, (byte)255, (byte)255, (byte)MaskAlpha);
            circle.Blend = blend;
            renderTarget.AddChild(circle);
        }
        else
        {
            ColoredRectangleRuntime mask = new() { X = 20, Y = 20, Width = 60, Height = 60, Color = XnaColor.White };
            mask.Alpha = MaskAlpha;
            mask.Blend = blend;
            renderTarget.AddChild(mask);
        }
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
        capture.GetData(pixels);

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
                // Apos.Shapes uses an SM4 effect the default Reach profile can't load - #4403.
                GraphicsProfile = GraphicsProfile.HiDef,
            };
        }

        protected override void Initialize()
        {
            base.Initialize();
            Gum.GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
            ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
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
