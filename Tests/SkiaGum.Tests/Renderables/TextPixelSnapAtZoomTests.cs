using Gum.GueDeriving;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #7101: text snapped its origin to whole Gum units, then the camera zoom scaled the result, so at zoom 2 a
/// container at X 6.5 (device pixel 13, where a nine-slice starts) drew its text at device pixel 14. Snapping has
/// to happen in device pixels, the same pixel grid every other renderable lands on.
/// </summary>
public class TextPixelSnapAtZoomTests : BaseTestClass
{
    const int Size = 160;

    [Theory]
    [InlineData(2f, 6.5f)]
    [InlineData(2f, 6.25f)]
    [InlineData(2f, 6.75f)]
    [InlineData(4f, 6.25f)]
    [InlineData(3f, 6.5f)]
    [InlineData(1.5f, 6.5f)]
    [InlineData(1f, 6f)]
    public void Render_TextOriginSnapsInDevicePixels(float zoom, float x)
    {
        using SKBitmap atWholeUnit = RenderText(zoom, 6f, 10f);
        using SKBitmap atFraction = RenderText(zoom, x, 10f);

        int expectedShift = DeviceRound(x * zoom) - DeviceRound(6f * zoom);
        (int wholeLeft, _, _, _) = InkBounds(atWholeUnit);
        (int fractionLeft, _, _, _) = InkBounds(atFraction);

        (fractionLeft - wholeLeft).ShouldBe(expectedShift, $"zoom {zoom}, X {x}");
    }

    [Theory]
    [InlineData(2f, 6.5f)]
    [InlineData(2f, 6.25f)]
    [InlineData(3f, 6.5f)]
    public void Render_TextOriginSnapsInDevicePixelsVertically(float zoom, float y)
    {
        using SKBitmap atWholeUnit = RenderText(zoom, 10f, 6f);
        using SKBitmap atFraction = RenderText(zoom, 10f, y);

        int expectedShift = DeviceRound(y * zoom) - DeviceRound(6f * zoom);
        (_, int wholeTop, _, _) = InkBounds(atWholeUnit);
        (_, int fractionTop, _, _) = InkBounds(atFraction);

        (fractionTop - wholeTop).ShouldBe(expectedShift, $"zoom {zoom}, Y {y}");
    }

    static int DeviceRound(float value) => (int)MathF.Round(value, MidpointRounding.AwayFromZero);

    static SKBitmap RenderText(float zoom, float x, float y)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(Size, Size));
        surface.Canvas.Clear(SKColors.Transparent);
        Gum.GumService.Default.Initialize(surface.Canvas, Size, Size);
        Gum.GumService.Default.SystemManagers.Renderer.Camera.Zoom = zoom;
        TextRuntime text = new() { Text = "HI", X = x, Y = y };
        Gum.GumService.Default.Root.Children.Add(text);

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    static (int left, int top, int right, int bottom) InkBounds(SKBitmap bitmap)
    {
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (int row = 0; row < bitmap.Height; row++)
        {
            for (int column = 0; column < bitmap.Width; column++)
            {
                // Faint anti-aliasing at the glyph edge is part of the ink; only a fully transparent pixel is background.
                if (bitmap.GetPixel(column, row).Alpha > 64)
                {
                    left = Math.Min(left, column);
                    top = Math.Min(top, row);
                    right = Math.Max(right, column + 1);
                    bottom = Math.Max(bottom, row + 1);
                }
            }
        }
        left.ShouldNotBe(int.MaxValue, "nothing was drawn");
        return (left, top, right, bottom);
    }
}
