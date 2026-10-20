using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A 360 degree <see cref="ArcRuntime"/> and a stroked <see cref="CircleRuntime"/> with the same box and
/// stroke width are the same shape, so they must draw the same pixels (#6254).
/// </summary>
public class ArcMatchesCircleTests
{
    private const int CanvasSize = 100;
    private const float BoxSize = 60;
    private const float StrokeWidth = 8;

    [Theory]
    [InlineData(20f)]
    [InlineData(20.5f)]
    [InlineData(20.3f)]
    public void FullSweepArc_DrawsSamePixelsAsStrokedCircle(float position)
    {
        using SKBitmap circle = Render(position, ShapeKind.Circle);
        using SKBitmap arc = Render(position, ShapeKind.FullSweepArc);

        MaxChannelDifference(circle, arc).ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public void FullSweepArc_AtIntegerPosition_ChangesNoPixelsOutsideItsBox()
    {
        using SKBitmap arc = Render(20f, ShapeKind.FullSweepArc);

        CountChangedPixelsOutsideBox(arc, 20f).ShouldBe(0);
    }

    internal enum ShapeKind { Circle, FullSweepArc }

    internal static SKColor Backdrop => new SKColor(70, 70, 90);
    internal static SKColor ShapeColor => new SKColor(255, 0, 0);

    internal static SKBitmap Render(float position, ShapeKind kind)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(CanvasSize, CanvasSize));
        Gum.GumService.Default.Initialize(surface.Canvas, CanvasSize, CanvasSize);
        ContainerRuntime root = new() { Width = CanvasSize, Height = CanvasSize };
        root.Children.Add(new RectangleRuntime
        {
            Width = CanvasSize, Height = CanvasSize, IsFilled = true, StrokeWidth = 0, FillColor = Backdrop,
        });

        GraphicalUiElement shape;
        switch (kind)
        {
            case ShapeKind.Circle:
                shape = new CircleRuntime
                {
                    IsFilled = false, StrokeWidth = StrokeWidth, StrokeColor = ShapeColor,
                };
                break;
            default:
                shape = new ArcRuntime
                {
                    StartAngle = 0, SweepAngle = 360, Thickness = StrokeWidth, StrokeColor = ShapeColor,
                };
                break;
        }
        shape.X = position;
        shape.Y = position;
        shape.Width = BoxSize;
        shape.Height = BoxSize;
        root.Children.Add(shape);
        Gum.GumService.Default.Root.Children.Add(root);

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    internal static int MaxChannelDifference(SKBitmap a, SKBitmap b)
    {
        int max = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                SKColor ca = a.GetPixel(x, y);
                SKColor cb = b.GetPixel(x, y);
                max = System.Math.Max(max, System.Math.Abs(ca.Red - cb.Red));
                max = System.Math.Max(max, System.Math.Abs(ca.Green - cb.Green));
                max = System.Math.Max(max, System.Math.Abs(ca.Blue - cb.Blue));
                max = System.Math.Max(max, System.Math.Abs(ca.Alpha - cb.Alpha));
            }
        }
        return max;
    }

    internal static int CountChangedPixelsOutsideBox(SKBitmap bitmap, float position)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                bool inside = x >= position && x < position + BoxSize && y >= position && y < position + BoxSize;
                if (!inside && bitmap.GetPixel(x, y) != Backdrop)
                {
                    count++;
                }
            }
        }
        return count;
    }
}
