using Shouldly;
using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Issue #7295: a dropshadow with blur N extends N pixels in every runtime. "Extends" is measured
/// on a white shape and white shadow over black, outward from the shape edge along the middle row,
/// as the farthest pixel whose alpha is at least 0.5% (a Gaussian tail never reaches zero).
/// </summary>
public class ShadowReachTests
{
    private const int CanvasSize = 400;
    private const int ShapeSize = 100;
    private const int ShapeMin = (CanvasSize - ShapeSize) / 2;

    public ShadowReachTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static (int Reach, int ReachAt3Percent) MeasureReach(bool circle, float blur)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(CanvasSize, CanvasSize));
        surface.Canvas.Clear(SKColors.Black);
        GumService.Default.Initialize(surface.Canvas, CanvasSize, CanvasSize);

        SkiaShapeRuntime shape = circle ? new CircleRuntime() : new RectangleRuntime();
        shape.X = ShapeMin;
        shape.Y = ShapeMin;
        shape.Width = ShapeSize;
        shape.Height = ShapeSize;
        shape.IsFilled = true;
        shape.FillColor = SKColors.White;
        shape.HasDropshadow = true;
        shape.DropshadowColor = SKColors.White;
        shape.DropshadowOffsetX = 0;
        shape.DropshadowOffsetY = 0;
        shape.DropshadowBlurX = blur;
        shape.DropshadowBlurY = blur;
        GumService.Default.Root.Children.Add(shape);

        GumService.Default.Draw();

        using SKBitmap bitmap = SKBitmap.FromImage(surface.Snapshot());
        int row = CanvasSize / 2;
        int edge = ShapeMin + ShapeSize;
        int reach = 0;
        int reach3 = 0;
        for (int x = edge; x < CanvasSize; x++)
        {
            byte brightness = bitmap.GetPixel(x, row).Red;
            if (brightness >= 1)
            {
                reach = x - edge + 1;
            }
            if (brightness >= 8)
            {
                reach3 = x - edge + 1;
            }
        }
        return (reach, reach3);
    }

    [Theory]
    [InlineData(true, 8)]
    [InlineData(true, 16)]
    [InlineData(true, 32)]
    [InlineData(true, 56)]
    [InlineData(false, 8)]
    [InlineData(false, 16)]
    [InlineData(false, 32)]
    [InlineData(false, 56)]
    public void Draw_FilledShapeWithBlur_ReachesBlurPixelsAtHalfPercentAlpha(bool circle, float blur)
    {
        (int reach, int reach3) = MeasureReach(circle, blur);

        reach.ShouldBeInRange(
            (int)MathF.Floor(blur * 0.85f),
            (int)MathF.Ceiling(blur * 1.15f),
            $"{(circle ? "circle" : "rect")} blur {blur}: reach {reach} (0.5%), {reach3} (3%)");
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(10f, 4f)]
    [InlineData(56f, 22.4f)]
    public void GetSigma_IsZeroPointFourTimesBlur(float blur, float expectedSigma)
    {
        DropshadowBlur.GetSigma(blur).ShouldBe(expectedSigma, 0.0001f);
    }
}
