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

    // #6256: Skia's arc is an ellipse that fills a box that is not square, with a uniform pen. The MonoGame arc
    // matches the outer extent and has the exact stroke on the short axis only (ArcFillsBoxTests).
    [Theory]
    [InlineData(60f, 40f)]
    [InlineData(40f, 60f)]
    public void FullSweepArc_InNonSquareBox_FillsTheBoxWithAUniformStroke(float width, float height)
    {
        using SKBitmap arc = Render(20f, ShapeKind.FullSweepArc, width, height);

        // Thickness along the horizontal and vertical lines through the box's center.
        int row = 20 + ((int)height / 2);
        int column = 20 + ((int)width / 2);
        double horizontal = 0;
        double vertical = 0;
        for (int i = 0; i < 20; i++)
        {
            horizontal += Coverage(arc.GetPixel(18 + i, row));
            vertical += Coverage(arc.GetPixel(column, 18 + i));
        }
        horizontal.ShouldBe(StrokeWidth, 0.3);
        vertical.ShouldBe(StrokeWidth, 0.3);

        Coverage(arc.GetPixel(20, row)).ShouldBeGreaterThan(0.8);
        Coverage(arc.GetPixel(20 + (int)width - 1, row)).ShouldBeGreaterThan(0.8);
        Coverage(arc.GetPixel(column, 20)).ShouldBeGreaterThan(0.8);
        Coverage(arc.GetPixel(column, 20 + (int)height - 1)).ShouldBeGreaterThan(0.8);
        Coverage(arc.GetPixel(19, row)).ShouldBeLessThan(0.1);
        Coverage(arc.GetPixel(column, 19)).ShouldBeLessThan(0.1);
        Coverage(arc.GetPixel(20 + (int)width, row)).ShouldBeLessThan(0.1);
        Coverage(arc.GetPixel(column, 20 + (int)height)).ShouldBeLessThan(0.1);
    }

    private static double Coverage(SKColor color) => (color.Red - Backdrop.Red) / (double)(ShapeColor.Red - Backdrop.Red);

    internal enum ShapeKind { Circle, FullSweepArc }

    internal static SKColor Backdrop => new SKColor(70, 70, 90);
    internal static SKColor ShapeColor => new SKColor(255, 0, 0);

    internal static SKBitmap Render(float position, ShapeKind kind, float width = BoxSize, float height = BoxSize)
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
        shape.Width = width;
        shape.Height = height;
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
