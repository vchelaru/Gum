using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Rotated text turns about the Text's top-left, and its alignment offset turns with it, as on MonoGame
/// (<c>RenderingLibrary.Graphics.Text</c> rotates the alignment offset by the absolute rotation) and Raylib (issue #6568).
/// </summary>
public class TextRotatedAlignmentTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(30f)]
    [InlineData(90f)]
    public void Render_AlignmentOffset_RotatesWithText(float rotation)
    {
        const float width = 80;
        const float height = 60;
        (HorizontalAlignment h, float hFraction)[] horizontals =
            { (HorizontalAlignment.Left, 0), (HorizontalAlignment.Center, 0.5f), (HorizontalAlignment.Right, 1) };
        (VerticalAlignment v, float vFraction)[] verticals =
            { (VerticalAlignment.Top, 0), (VerticalAlignment.Center, 0.5f), (VerticalAlignment.Bottom, 1) };

        (float X, float Y) reference = RenderAndGetInkCentroid(rotation, width, height,
            HorizontalAlignment.Left, VerticalAlignment.Top, out float textWidth, out float textHeight);

        float radians = rotation * MathF.PI / 180f;
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        foreach ((HorizontalAlignment h, float hFraction) in horizontals)
        {
            foreach ((VerticalAlignment v, float vFraction) in verticals)
            {
                (float X, float Y) actual = RenderAndGetInkCentroid(rotation, width, height, h, v, out _, out _);

                float localX = (width - textWidth) * hFraction;
                float localY = (height - textHeight) * vFraction;
                // Gum rotation is counterclockwise on a y-down screen.
                float expectedX = reference.X + localX * cos + localY * sin;
                float expectedY = reference.Y - localX * sin + localY * cos;

                string label = $"Rotation {rotation}, {h}/{v}";
                actual.X.ShouldBe(expectedX, 1f, label);
                actual.Y.ShouldBe(expectedY, 1f, label);
            }
        }
    }

    private static (float X, float Y) RenderAndGetInkCentroid(float rotation, float width, float height,
        HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment,
        out float textWidth, out float textHeight)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(240, 240));
        surface.Canvas.Clear(SKColors.Transparent);
        SystemManagers managers = new SystemManagers();
        managers.Canvas = surface.Canvas;

        Text sut = new Text();
        sut.OverrideTextRenderingPositionMode = TextRenderingPositionMode.FreeFloating;
        sut.RawText = "Hi";
        sut.FontSize = 16;
        sut.X = 100;
        sut.Y = 120;
        sut.Width = width;
        sut.Height = height;
        sut.Rotation = rotation;
        sut.HorizontalAlignment = horizontalAlignment;
        sut.VerticalAlignment = verticalAlignment;

        sut.Render(managers);
        textWidth = sut.WrappedTextWidth;
        textHeight = sut.WrappedTextHeight;

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        double sumX = 0, sumY = 0, sumAlpha = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                byte alpha = bitmap.GetPixel(x, y).Alpha;
                sumX += (x + 0.5) * alpha;
                sumY += (y + 0.5) * alpha;
                sumAlpha += alpha;
            }
        }

        sumAlpha.ShouldBeGreaterThan(0, "the text drew no pixels");
        return ((float)(sumX / sumAlpha), (float)(sumY / sumAlpha));
    }
}
