using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Runtimes;

/// <summary>
/// #7344 follow-up - translucent gradient stops must blend like Skia: straight (unpremultiplied)
/// stop interpolation, each pixel painted once (no alpha accumulation), and a rectangle's radial
/// gradient clipped to the rectangle rather than a circle. Pixels are read over an opaque white
/// backdrop, where Skia's result is <c>rgb * a + 255 * (1 - a)</c>.
/// </summary>
public class ShapeGradientTranslucentRenderTests : BaseTestClass
{
    const int Size = 64;

    private static Color[,] Render(GraphicalUiElementFactory factory)
    {
        ContainerRuntime container = new();
        container.Width = Size;
        container.Height = Size;
        container.IsRenderTarget = true;

        RectangleRuntime background = new();
        background.Width = Size;
        background.Height = Size;
        background.IsFilled = true;
        background.FillColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);
        container.Children.Add(background);
        container.Children.Add(factory(Size));

        Gum.GumService.Default.Root.Children.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        Image image = LoadImageFromTexture(Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value.Texture);
        Color[,] pixels = new Color[Size, Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                pixels[x, y] = GetImageColor(image, x, y);
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Clear();
        return pixels;
    }

    delegate GraphicalUiElement GraphicalUiElementFactory(int size);

    private static GraphicalUiElement CreateRadial(GraphicalUiElement shape, int size)
    {
        shape.Width = size;
        shape.Height = size;
        shape.SetProperty("IsFilled", true);
        shape.SetProperty("UseGradient", true);
        shape.SetProperty("GradientType", GradientType.Radial);
        shape.SetProperty("Red1", 255);
        shape.SetProperty("Green1", 0);
        shape.SetProperty("Blue1", 0);
        shape.SetProperty("Alpha1", 255);
        shape.SetProperty("Red2", 0);
        shape.SetProperty("Green2", 0);
        shape.SetProperty("Blue2", 255);
        shape.SetProperty("Alpha2", 80);
        shape.SetProperty("GradientX1", (float)size / 2);
        shape.SetProperty("GradientY1", (float)size / 2);
        shape.SetProperty("GradientInnerRadius", 0f);
        shape.SetProperty("GradientOuterRadius", (float)size / 2);
        return shape;
    }

    /// <summary>Skia's result for the red (opaque) to blue (alpha 80) radial at distance r, over white.</summary>
    private static (float r, float g, float b) Expected(float distance)
    {
        float t = System.Math.Clamp(distance / (Size / 2f), 0f, 1f);
        float a = (255f + (80f - 255f) * t) / 255f;
        float red = 255f * (1f - t);
        float blue = 255f * t;
        return (red * a + 255f * (1f - a), 0f * a + 255f * (1f - a), blue * a + 255f * (1f - a));
    }

    private static void ShouldMatchExpected(Color actual, float distance)
    {
        (float r, float g, float b) = Expected(distance);
        ((float)actual.R).ShouldBe(r, tolerance: 14f, $"R at distance {distance}");
        ((float)actual.G).ShouldBe(g, tolerance: 14f, $"G at distance {distance}");
        ((float)actual.B).ShouldBe(b, tolerance: 14f, $"B at distance {distance}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    public void Circle_RadialWithTranslucentEndStop_MatchesStraightInterpolationOverWhite(int distance)
    {
        Color[,] pixels = Render(size => CreateRadial(new CircleRuntime(), size));

        ShouldMatchExpected(pixels[Size / 2 + distance, Size / 2], distance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    public void Rectangle_RadialWithTranslucentEndStop_MatchesStraightInterpolationOverWhite(int distance)
    {
        Color[,] pixels = Render(size => CreateRadial(new RectangleRuntime(), size));

        ShouldMatchExpected(pixels[Size / 2 + distance, Size / 2], distance);
    }

    [Fact]
    public void Rectangle_RadialWithTranslucentEndStop_FillsCornersAsEndStopNotBackdrop()
    {
        Color[,] pixels = Render(size => CreateRadial(new RectangleRuntime(), size));

        // The corner is beyond the outer radius, so it is the end stop (blue at alpha 80) over
        // white (~175,175,255), not the backdrop (a circle-shaped fill leaves it near white/gray).
        ShouldMatchExpected(pixels[3, 3], distance: Size);
        ShouldMatchExpected(pixels[Size - 4, Size - 4], distance: Size);
    }

    [Fact]
    public void Rectangle_RadialWithTranslucentEndStop_HasNoHardRingAtOuterRadius()
    {
        Color[,] pixels = Render(size => CreateRadial(new RectangleRuntime(), size));

        // Walking along the diagonal past the outer radius must not jump: stacked translucent
        // circles produced a hard ring where the outermost circle ended.
        int center = Size / 2;
        for (int step = 20; step < 28; step++)
        {
            Color a = pixels[center + step, center + step];
            Color b = pixels[center + step + 1, center + step + 1];
            System.Math.Abs(a.R - b.R).ShouldBeLessThan(10);
            System.Math.Abs(a.B - b.B).ShouldBeLessThan(10);
        }
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void ChangingGradientUnitsAfterCreation_ChangesNextDraw(string shapeName)
    {
        GraphicalUiElement shape = shapeName == "Circle" ? new CircleRuntime() : new RectangleRuntime();
        shape.Width = Size;
        shape.Height = Size;
        shape.SetProperty("IsFilled", true);
        shape.SetProperty("UseGradient", true);
        shape.SetProperty("GradientType", GradientType.Linear);
        shape.SetProperty("Red1", 255);
        shape.SetProperty("Green1", 0);
        shape.SetProperty("Blue1", 0);
        shape.SetProperty("Red2", 0);
        shape.SetProperty("Green2", 0);
        shape.SetProperty("Blue2", 255);
        shape.SetProperty("GradientX1", 0f);
        shape.SetProperty("GradientY1", 0f);
        shape.SetProperty("GradientX2", 200f);
        shape.SetProperty("GradientY2", 0f);

        Color[,] before = Render(_ => shape);

        // 200 px axis: x = 40 is t = 0.2. As 200% of the 64 px width the axis is 128 px, so t = 0.31.
        shape.SetProperty("GradientX2Units", PositionUnitType.PercentageWidth);
        Color[,] after = Render(_ => shape);

        after[40, Size / 2].B.ShouldBeGreaterThan((byte)(before[40, Size / 2].B + 12));
    }
}
