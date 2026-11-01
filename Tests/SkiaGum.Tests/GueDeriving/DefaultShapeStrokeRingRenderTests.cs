using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// #7346 - a fresh Circle/Rectangle carries a 1 px white stroke on every runtime (the documented
/// default). Pins it by pixel: red fill over black, the left-edge pixel is the white ring.
/// </summary>
public class DefaultShapeStrokeRingRenderTests
{
    public DefaultShapeStrokeRingRenderTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static SKColor EdgePixel(GraphicalUiElement shape, bool gradient = false)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        surface.Canvas.Clear(SKColors.Black);

        shape.X = 0;
        shape.Y = 0;
        shape.Width = 64;
        shape.Height = 64;
        shape.SetProperty("IsFilled", true);
        if (gradient)
        {
            shape.SetProperty("UseGradient", true);
            shape.SetProperty("GradientType", GradientType.Radial);
            shape.SetProperty("Red1", 255);
            shape.SetProperty("Green1", 0);
            shape.SetProperty("Blue1", 0);
            shape.SetProperty("Alpha1", 255);
            shape.SetProperty("Red2", 255);
            shape.SetProperty("Green2", 0);
            shape.SetProperty("Blue2", 0);
            shape.SetProperty("Alpha2", 255);
            shape.SetProperty("GradientX1", 32f);
            shape.SetProperty("GradientY1", 32f);
            shape.SetProperty("GradientInnerRadius", 0f);
            shape.SetProperty("GradientOuterRadius", 32f);
        }
        else
        {
            shape.SetProperty("FillRed", 255);
            shape.SetProperty("FillGreen", 0);
            shape.SetProperty("FillBlue", 0);
        }
        shape.AddToManagers(SystemManagers.Default, SystemManagers.Default.Renderer.MainLayer);
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(0, 32);
    }

    [Fact]
    public void DefaultCircle_StrokeWidthIsOne()
    {
        new CircleRuntime().StrokeWidth.ShouldBe(1f);
    }

    [Fact]
    public void DefaultCircle_DrawsWhiteRingAtEdge()
    {
        EdgePixel(new CircleRuntime()).Green.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void DefaultCircle_Gradient_DrawsWhiteRingAtEdge()
    {
        EdgePixel(new CircleRuntime(), gradient: true).Green.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void CircleWithStrokeWidthZero_DrawsNoRing()
    {
        CircleRuntime circle = new();
        circle.StrokeWidth = 0;

        EdgePixel(circle).Green.ShouldBeLessThan((byte)50);
    }

    [Fact]
    public void DefaultRectangle_DrawsWhiteRingAtEdge()
    {
        EdgePixel(new RectangleRuntime()).Green.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void DefaultRectangle_Gradient_DrawsWhiteRingAtEdge()
    {
        EdgePixel(new RectangleRuntime(), gradient: true).Green.ShouldBeGreaterThan((byte)200);
    }
}
