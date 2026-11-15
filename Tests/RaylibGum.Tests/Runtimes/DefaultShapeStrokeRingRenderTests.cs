using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7346 - a fresh Circle/Rectangle carries a 1 px white stroke on every runtime (the documented
/// default). Pins it by pixel: red fill over black, the left-edge pixel is the white ring.
/// </summary>
public class DefaultShapeStrokeRingRenderTests : BaseTestClass
{
    const int Size = 64;

    private static Color EdgePixel(GraphicalUiElement shape, bool gradient = false)
    {
        ContainerRuntime container = new();
        container.Width = Size;
        container.Height = Size;
        container.IsRenderTarget = true;

        RectangleRuntime background = new();
        background.Width = Size;
        background.Height = Size;
        background.IsFilled = true;
        background.FillColor = new Color((byte)0, (byte)0, (byte)0, (byte)255);
        background.StrokeWidth = 0;
        container.Children.Add(background);

        shape.Width = Size;
        shape.Height = Size;
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
            shape.SetProperty("GradientX1", Size / 2f);
            shape.SetProperty("GradientY1", Size / 2f);
            shape.SetProperty("GradientInnerRadius", 0f);
            shape.SetProperty("GradientOuterRadius", Size / 2f);
        }
        else
        {
            shape.SetProperty("FillRed", 255);
            shape.SetProperty("FillGreen", 0);
            shape.SetProperty("FillBlue", 0);
        }
        container.Children.Add(shape);

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        Image image = LoadImageFromTexture(Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value.Texture);
        Color pixel = GetImageColor(image, 0, Size / 2);
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Clear();
        return pixel;
    }

    [Fact]
    public void DefaultCircle_StrokeWidthIsOne()
    {
        new CircleRuntime().StrokeWidth.ShouldBe(1f);
    }

    [Fact]
    public void DefaultCircle_DrawsWhiteRingAtEdge()
    {
        EdgePixel(new CircleRuntime()).G.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void DefaultCircle_Gradient_DrawsWhiteRingAtEdge()
    {
        EdgePixel(new CircleRuntime(), gradient: true).G.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void CircleWithStrokeWidthZero_DrawsNoRing()
    {
        CircleRuntime circle = new();
        circle.StrokeWidth = 0;

        EdgePixel(circle).G.ShouldBeLessThan((byte)50);
    }

    [Fact]
    public void DefaultRectangle_DrawsWhiteRingAtEdge()
    {
        EdgePixel(new RectangleRuntime()).G.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public void DefaultRectangle_Gradient_DrawsWhiteRingAtEdge()
    {
        EdgePixel(new RectangleRuntime(), gradient: true).G.ShouldBeGreaterThan((byte)200);
    }
}
