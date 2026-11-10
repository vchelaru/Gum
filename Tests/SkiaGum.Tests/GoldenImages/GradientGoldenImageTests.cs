using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// Pins the pixels of linear and radial gradients. Two gradients with different stops are drawn in the
/// same frame, so a gradient that kept a reference to stop data shared with the next one would show the
/// second gradient's colors in the first.
/// </summary>
public class GradientGoldenImageTests
{
    public GradientGoldenImageTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void LinearAndRadialGradients_MatchGoldenBaseline()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(128, 64));
        GumService.Default.Initialize(surface.Canvas, 128, 64);

        RoundedRectangleRuntime linear = new()
        {
            X = 4,
            Y = 4,
            Width = 56,
            Height = 56,
            CornerRadius = 0,
            UseGradient = true,
            GradientType = GradientType.Linear,
            GradientX2 = 56,
            GradientY2 = 56,
            Red1 = 255, Green1 = 0, Blue1 = 0, Alpha1 = 255,
            Red2 = 0, Green2 = 0, Blue2 = 255, Alpha2 = 255,
        };
        GumService.Default.Root.Children.Add(linear);

        RoundedRectangleRuntime radial = new()
        {
            X = 68,
            Y = 4,
            Width = 56,
            Height = 56,
            CornerRadius = 0,
            UseGradient = true,
            GradientType = GradientType.Radial,
            GradientX1 = 28,
            GradientY1 = 28,
            GradientInnerRadius = 4,
            GradientOuterRadius = 28,
            Red1 = 0, Green1 = 255, Blue1 = 0, Alpha1 = 255,
            Red2 = 255, Green2 = 255, Blue2 = 0, Alpha2 = 128,
        };
        GumService.Default.Root.Children.Add(radial);

        GumService.Default.Draw();

        GoldenImageAssert.Matches(surface, "Gradient_LinearAndRadial", tolerance: 0);
    }
}
