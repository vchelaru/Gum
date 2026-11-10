using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using Vector2 = System.Numerics.Vector2;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// <see cref="Polygon"/> reuses its path between draws. These pin that each input the path is built
/// from, including edits made to <see cref="Polygon.Points"/> in place, still reaches the canvas.
/// </summary>
public class PolygonPathCacheTests
{
    // A 80x80 square. (15, 85) is inside it near the bottom-left corner.
    private static readonly SKRect Bounds = new SKRect(0, 0, 100, 100);

    private static Polygon CreateSquare()
    {
        Polygon polygon = new Polygon();
        polygon.Color = SKColors.Red;
        polygon.SetPoints(new[]
        {
            new Vector2(10, 10),
            new Vector2(90, 10),
            new Vector2(90, 90),
            new Vector2(10, 90),
        });
        return polygon;
    }

    [Fact]
    public void DrawBound_PointMovedInPlaceAfterFirstDraw_DrawsTheNewShape()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(150, 100));
        Polygon polygon = CreateSquare();
        polygon.DrawBound(Bounds, surface.Canvas, absoluteRotation: 0);
        PixelAt(surface, 15, 85).Alpha.ShouldBeGreaterThan((byte)0);

        polygon.SetPointAt(new Vector2(50, 90), index: 3);
        surface.Canvas.Clear(SKColors.Transparent);
        polygon.DrawBound(Bounds, surface.Canvas, absoluteRotation: 0);

        PixelAt(surface, 15, 85).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void DrawBound_PointAddedAfterFirstDraw_DrawsTheNewShape()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(150, 100));
        Polygon polygon = CreateSquare();
        polygon.DrawBound(Bounds, surface.Canvas, absoluteRotation: 0);

        polygon.InsertPointAt(new Vector2(10, 50), index: 4);
        polygon.SetPointAt(new Vector2(50, 90), index: 3);
        surface.Canvas.Clear(SKColors.Transparent);
        polygon.DrawBound(Bounds, surface.Canvas, absoluteRotation: 0);

        polygon.PointCount.ShouldBe(5);
        PixelAt(surface, 15, 85).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void DrawBound_BoundsMovedAfterFirstDraw_DrawsAtTheNewPosition()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(150, 100));
        Polygon polygon = CreateSquare();
        polygon.DrawBound(Bounds, surface.Canvas, absoluteRotation: 0);

        surface.Canvas.Clear(SKColors.Transparent);
        polygon.DrawBound(new SKRect(40, 0, 140, 100), surface.Canvas, absoluteRotation: 0);

        PixelAt(surface, 15, 85).Alpha.ShouldBe((byte)0);
        PixelAt(surface, 60, 50).Alpha.ShouldBeGreaterThan((byte)0);
    }

    [Fact]
    public void Clone_MovingAPointOnTheClone_LeavesTheSourceDrawingItsOwnShape()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(150, 100));
        Polygon source = CreateSquare();
        source.DrawBound(Bounds, surface.Canvas, absoluteRotation: 0);
        Polygon clone = (Polygon)source.Clone();

        clone.SetPointAt(new Vector2(50, 90), index: 3);
        clone.DrawBound(Bounds, surface.Canvas, absoluteRotation: 0);
        clone.Dispose();
        surface.Canvas.Clear(SKColors.Transparent);
        source.DrawBound(Bounds, surface.Canvas, absoluteRotation: 0);

        PixelAt(surface, 15, 85).Alpha.ShouldBeGreaterThan((byte)0);
    }

    private static SKColor PixelAt(SKSurface surface, int x, int y)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }
}
