using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Issue #7295: a dropshadow with blur N extends N pixels in every runtime. "Extends" is measured
/// on a white shape and white shadow over black, outward from the shape edge along the middle row,
/// as the farthest pixel with value >= 1 (about 0.4% alpha; a Gaussian tail never reaches zero).
/// </summary>
public class ShadowReachTests : BaseTestClass
{
    private const int CanvasSize = 400;
    private const int ShapeSize = 100;
    private const int ShapeMin = (CanvasSize - ShapeSize) / 2;

    // Brightness is alpha here: white on black. The threshold is pixel value >= 1 (about 0.4%),
    // the smallest nonzero value, standing in for 0.5%; value >= 8 is about 3%.
    private static (int Reach, int ReachAt3Percent) MeasureReach(bool circle, float blur)
    {
        ShapeHolder shape = circle ? CreateCircle(blur) : CreateRectangle(blur);
        shape.Apply();

        RenderTexture2D host = LoadRenderTexture(CanvasSize, CanvasSize);
        try
        {
            BeginDrawing();
            BeginTextureMode(host);
            ClearBackground(Color.Black);
            GumService.Default.Draw();
            EndTextureMode();
            EndDrawing();

            Image image = LoadImageFromTexture(host.Texture);
            try
            {
                int row = CanvasSize / 2;
                int edge = ShapeMin + ShapeSize;
                int reach = 0;
                int reach3 = 0;
                for (int x = edge; x < CanvasSize; x++)
                {
                    Color pixel = GetImageColor(image, x, CanvasSize - 1 - row);
                    if (pixel.R >= 1)
                    {
                        reach = x - edge + 1;
                    }
                    if (pixel.R >= 8)
                    {
                        reach3 = x - edge + 1;
                    }
                }
                return (reach, reach3);
            }
            finally
            {
                UnloadImage(image);
            }
        }
        finally
        {
            UnloadRenderTexture(host);
            GumService.Default.Root.Children.Clear();
        }
    }

    private sealed class ShapeHolder
    {
        private readonly Gum.Wireframe.GraphicalUiElement _element;
        public ShapeHolder(Gum.Wireframe.GraphicalUiElement element) { _element = element; }
        public void Apply()
        {
            GumService.Default.Root.Children.Add(_element);
            GumService.Default.Root.UpdateLayout();
        }
    }

    private static ShapeHolder CreateCircle(float blur)
    {
        CircleRuntime circle = new();
        circle.X = ShapeMin;
        circle.Y = ShapeMin;
        circle.Width = ShapeSize;
        circle.Height = ShapeSize;
        circle.IsFilled = true;
        circle.FillColor = Color.White;
        circle.HasDropshadow = true;
        circle.DropshadowColor = Color.White;
        circle.DropshadowOffsetX = 0;
        circle.DropshadowOffsetY = 0;
        circle.DropshadowBlur = blur;
        return new ShapeHolder(circle);
    }

    private static ShapeHolder CreateRectangle(float blur)
    {
        RectangleRuntime rectangle = new();
        rectangle.X = ShapeMin;
        rectangle.Y = ShapeMin;
        rectangle.Width = ShapeSize;
        rectangle.Height = ShapeSize;
        rectangle.IsFilled = true;
        rectangle.FillColor = Color.White;
        rectangle.HasDropshadow = true;
        rectangle.DropshadowColor = Color.White;
        rectangle.DropshadowOffsetX = 0;
        rectangle.DropshadowOffsetY = 0;
        rectangle.DropshadowBlur = blur;
        return new ShapeHolder(rectangle);
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
    [InlineData(0.1f, 1)]
    [InlineData(8f, 10)]
    [InlineData(16f, 20)]
    [InlineData(32f, 39)]
    [InlineData(50f, 60)]
    [InlineData(56f, ShadowBlurRenderer.MaxRadius)]
    [InlineData(500f, ShadowBlurRenderer.MaxRadius)]
    public void GetKernelRadius_IsThreeSigmaCappedAtMaxRadius(float blur, int expectedRadius)
    {
        ShadowBlurRenderer.GetKernelRadius(blur).ShouldBe(expectedRadius);
    }

    [Fact]
    public void MaxRadius_CoversBlurFiftySixPastHalfPercentReach()
    {
        // The shadow reaches about 1.1 * blur, so the kernel and the render texture padding must be
        // at least that wide at blur 56 or the cap clips the shadow.
        ShadowBlurRenderer.MaxRadius.ShouldBeGreaterThanOrEqualTo((int)MathF.Ceiling(56f * 1.1f));
    }
}
