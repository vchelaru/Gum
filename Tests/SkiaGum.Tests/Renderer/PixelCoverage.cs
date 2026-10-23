using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Compares rendered scenes by shape: how many pixels match a color and where their center is.
/// Used where antialiasing or resampling makes pixel-for-pixel comparison too strict.
/// </summary>
internal static class PixelCoverage
{
    internal record struct Coverage(int Count, float CenterX, float CenterY);

    internal static bool IsRed(SKColor c) => c.Red > 150 && c.Green < 80 && c.Blue < 80;

    internal static bool IsBlue(SKColor c) => c.Blue > 150 && c.Green < 80 && c.Red < 80;

    internal static RectangleRuntime Rect(float x, float y, float width, float height, SKColor color) =>
        new() { X = x, Y = y, Width = width, Height = height, IsFilled = true, FillColor = color };

    internal static void AssertSameCoverage(SKBitmap expected, SKBitmap actual, System.Func<SKColor, bool> predicate,
        int minimumCount, float centerTolerance, int countTolerancePercent)
    {
        Coverage e = Measure(expected, predicate);
        Coverage a = Measure(actual, predicate);
        string label = $"expected={e} actual={a}";
        e.Count.ShouldBeGreaterThan(minimumCount, label);
        a.Count.ShouldBeInRange(
            e.Count * (100 - countTolerancePercent) / 100, e.Count * (100 + countTolerancePercent) / 100, label);
        a.CenterX.ShouldBe(e.CenterX, centerTolerance, label);
        a.CenterY.ShouldBe(e.CenterY, centerTolerance, label);
    }

    internal static Coverage Measure(SKBitmap bitmap, System.Func<SKColor, bool> predicate)
    {
        int count = 0;
        long sumX = 0;
        long sumY = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (predicate(bitmap.GetPixel(x, y)))
                {
                    count++;
                    sumX += x;
                    sumY += y;
                }
            }
        }
        return count == 0 ? new Coverage(0, -1, -1) : new Coverage(count, sumX / (float)count, sumY / (float)count);
    }

    internal static SKBitmap Render(int size, System.Func<GraphicalUiElement> build)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(size, size));
        GumService.Default.Initialize(surface.Canvas, size, size);
        GraphicalUiElement root = build();
        GumService.Default.Root.Children.Add(root);

        surface.Canvas.Clear(SKColors.Black);
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        SKBitmap bitmap = SKBitmap.FromImage(image);
        GumService.Default.Root.Children.Remove(root);
        return bitmap;
    }
}
