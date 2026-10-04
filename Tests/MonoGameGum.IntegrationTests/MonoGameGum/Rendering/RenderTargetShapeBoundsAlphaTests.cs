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
/// Issue #5689: an alpha-only blend (MinAlpha, ReplaceAlpha) on a shape applies to the shape's whole
/// bounding rectangle, so a Circle or rounded Rectangle works as a mask. Outside the body but inside
/// the bounds the shape's alpha is 0, so MinAlpha erases and ReplaceAlpha sets alpha to 0, and the
/// backdrop shows through the render-target container. SubtractAlpha (alpha 0 subtracts nothing),
/// Normal and Additive keep touching only the body. The mask shape is at (20,20) size 60x60, so
/// pixel (22,22) is inside the bounds but outside a circle or a 30px-radius rounded rectangle.
/// </summary>
public class RenderTargetShapeBoundsAlphaTests : BaseTestClass
{
    private const int CaptureSize = 100;
    private const int Tolerance = 8;
    private static readonly XnaColor BackdropColor = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    private static readonly XnaColor FillColor = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);

    public enum MaskShape { Circle, RoundedRectangle }

    [Theory]
    [InlineData(MaskShape.Circle, Gum.RenderingLibrary.Blend.MinAlpha)]
    [InlineData(MaskShape.Circle, Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(MaskShape.RoundedRectangle, Gum.RenderingLibrary.Blend.MinAlpha)]
    [InlineData(MaskShape.RoundedRectangle, Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    public void OpaqueAlphaMask_ErasesBoundsOutsideBody_KeepsBodyAndOutsideBounds(
        MaskShape shape, Gum.RenderingLibrary.Blend blend)
    {
        XnaColor[] pixels = Render(shape, blend, maskAlpha: 255);

        AssertNear(pixels[(22 * CaptureSize) + 22], BackdropColor);
        AssertNear(pixels[(50 * CaptureSize) + 50], FillColor);
        AssertNear(pixels[(5 * CaptureSize) + 5], FillColor);
    }

    [Theory]
    [InlineData(MaskShape.Circle, Gum.RenderingLibrary.Blend.MinAlpha)]
    [InlineData(MaskShape.Circle, Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    public void HalfAlphaMask_ClampsBodyToHalf_AndErasesBoundsOutsideBody(
        MaskShape shape, Gum.RenderingLibrary.Blend blend)
    {
        XnaColor[] pixels = Render(shape, blend, maskAlpha: 128);
        float a = 128 / 255f;

        XnaColor body = pixels[(50 * CaptureSize) + 50];
        System.Math.Abs(body.R - (FillColor.R * a + BackdropColor.R * (1 - a))).ShouldBeLessThanOrEqualTo(Tolerance);
        System.Math.Abs(body.G - (FillColor.G * a + BackdropColor.G * (1 - a))).ShouldBeLessThanOrEqualTo(Tolerance);

        AssertNear(pixels[(22 * CaptureSize) + 22], BackdropColor);
    }

    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    public void CircleMask_AntiAliasedEdge_IsPartial(Gum.RenderingLibrary.Blend blend)
    {
        XnaColor[] pixels = Render(MaskShape.Circle, blend, maskAlpha: 255);

        // The circle's anti-aliased fringe must blend fill and backdrop on at least one pixel
        // instead of every pixel snapping to one of them.
        bool foundPartial = false;
        for (int y = 20; y < 80 && !foundPartial; y++)
        {
            for (int x = 20; x < 80; x++)
            {
                XnaColor pixel = pixels[(y * CaptureSize) + x];
                if (pixel.R > BackdropColor.R + 10 && pixel.R < FillColor.R - 10)
                {
                    foundPartial = true;
                    break;
                }
            }
        }
        foundPartial.ShouldBeTrue();
    }

    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.SubtractAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.Normal)]
    [InlineData(Gum.RenderingLibrary.Blend.Additive)]
    public void OtherBlends_LeaveBoundsOutsideBodyUntouched(Gum.RenderingLibrary.Blend blend)
    {
        XnaColor[] pixels = Render(MaskShape.Circle, blend, maskAlpha: 255);

        // Corner of the bounds, outside the circle body: still the fill, not erased.
        AssertNear(pixels[(22 * CaptureSize) + 22], FillColor);
    }

    [Fact]
    public void AlphaMaskCircle_AfterNormalShape_InOneBake_StillErasesBoundsOutsideBody()
    {
        // Normal shape first, then the mask: exercises ShapeRenderer.EnsureBlend re-opening the batch.
        XnaColor[] pixels = Render(MaskShape.Circle, Gum.RenderingLibrary.Blend.MinAlpha, maskAlpha: 255,
            normalShapeFirst: true);

        AssertNear(pixels[(22 * CaptureSize) + 22], BackdropColor);
        AssertNear(pixels[(50 * CaptureSize) + 50], FillColor);
    }

    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    public void NormalShapeAfterMask_DrawsOverTheErasedCorner(Gum.RenderingLibrary.Blend blend)
    {
        // The mask leaves the stencil state behind unless it is restored; a later Normal shape
        // inside the erased corner must still draw.
        XnaColor[] pixels = Render(MaskShape.Circle, blend, maskAlpha: 255, normalShapeAfter: true);

        AssertNear(pixels[(28 * CaptureSize) + 28], new XnaColor((byte)0, (byte)0, (byte)255, (byte)255));
        AssertNear(pixels[(22 * CaptureSize) + 22], BackdropColor);
    }

    [Fact]
    public void MinAlphaMask_KeepsPartiallyTransparentContentInBody_ButReplaceDoesNot()
    {
        // Min keeps the smaller of destination and mask alpha: content that is already 50% stays 50%
        // under an opaque mask. This is what separates it from ReplaceAlpha, which writes the mask alpha.
        float a = 128 / 255f;
        XnaColor expectedHalf = new XnaColor(
            (byte)(FillColor.R * a + BackdropColor.R * (1 - a)),
            (byte)(FillColor.G * a + BackdropColor.G * (1 - a)),
            (byte)(FillColor.B * a + BackdropColor.B * (1 - a)),
            (byte)255);

        XnaColor[] min = Render(MaskShape.Circle, Gum.RenderingLibrary.Blend.MinAlpha, maskAlpha: 255, fillAlpha: 128);
        AssertNear(min[(50 * CaptureSize) + 50], expectedHalf);
        AssertNear(min[(22 * CaptureSize) + 22], BackdropColor);

        XnaColor[] replace = Render(MaskShape.Circle, Gum.RenderingLibrary.Blend.ReplaceAlpha, maskAlpha: 255, fillAlpha: 128);
        System.Math.Abs(replace[(50 * CaptureSize) + 50].R - expectedHalf.R).ShouldBeGreaterThan(Tolerance);
    }

    private static void AssertNear(XnaColor actual, XnaColor expected)
    {
        System.Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(Tolerance);
        System.Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(Tolerance);
        System.Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(Tolerance);
    }

    private static XnaColor[] Render(MaskShape shape, Gum.RenderingLibrary.Blend blend, int maskAlpha,
        bool normalShapeFirst = false, bool normalShapeAfter = false, int fillAlpha = 255)
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
        renderTarget.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = new XnaColor(FillColor.R, FillColor.G, FillColor.B, (byte)fillAlpha) });
#pragma warning restore CS0618

        if (normalShapeFirst)
        {
            CircleRuntime first = new();
            first.X = 0;
            first.Y = 0;
            first.Width = 10;
            first.Height = 10;
            first.IsFilled = true;
            first.StrokeWidth = 0;
            first.FillColor = FillColor;
            renderTarget.AddChild(first);
        }

        XnaColor maskColor = new XnaColor((byte)255, (byte)255, (byte)255, (byte)maskAlpha);
        if (shape == MaskShape.Circle)
        {
            CircleRuntime circle = new();
            circle.X = 20;
            circle.Y = 20;
            circle.Width = 60;
            circle.Height = 60;
            circle.IsFilled = true;
            circle.StrokeWidth = 0;
            circle.FillColor = maskColor;
            circle.Blend = blend;
            renderTarget.AddChild(circle);
        }
        else
        {
            RectangleRuntime rect = new();
            rect.X = 20;
            rect.Y = 20;
            rect.Width = 60;
            rect.Height = 60;
            rect.CornerRadius = 30;
            rect.IsFilled = true;
            rect.StrokeWidth = 0;
            rect.FillColor = maskColor;
            rect.Blend = blend;
            renderTarget.AddChild(rect);
        }
        if (normalShapeAfter)
        {
            CircleRuntime after = new();
            after.X = 24;
            after.Y = 24;
            after.Width = 8;
            after.Height = 8;
            after.IsFilled = true;
            after.StrokeWidth = 0;
            after.FillColor = new XnaColor((byte)0, (byte)0, (byte)255, (byte)255);
            renderTarget.AddChild(after);
        }
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
