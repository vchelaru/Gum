using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// End-to-end proof that the golden-image harness (<see cref="PixelComparer"/> +
/// <see cref="GoldenImageAssert"/>) can catch a real SkiaGum rendering regression, not just
/// diff in-memory bitmaps. Renders an actual <see cref="RectangleRuntime"/> through
/// <see cref="GumService"/>'s headless CPU raster surface, the same path production code uses.
/// </summary>
public class RectangleGoldenImageTests
{
    public RectangleGoldenImageTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void RedFilledRectangle_MatchesGoldenBaseline()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        RectangleRuntime rectangle = new()
        {
            X = 8,
            Y = 8,
            Width = 48,
            Height = 48,
            IsFilled = true,
            FillColor = SKColors.Red,
        };
        GumService.Default.Root.Children.Add(rectangle);

        GumService.Default.Draw();

        GoldenImageAssert.Matches(surface, "Rectangle_Red");
    }
}
