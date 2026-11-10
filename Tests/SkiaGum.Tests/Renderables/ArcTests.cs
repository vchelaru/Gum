using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Renderable-level guards for the Skia <see cref="Arc"/> primitive. These cover behavior
/// the runtime layer (<c>ArcRuntime</c>) cannot enforce on its own — anything that depends
/// on how <c>Arc</c> constructs its paint or path.
/// </summary>
public class ArcTests
{
    // Skia's SKPath.AddArc autocloses an open path with a straight chord from the sweep
    // endpoint back to the start when SKPaint.Style = Fill. That "circular segment" shape is
    // a quirk of the Skia path API, not a feature anyone has asked for — the Apos.Shapes
    // backend doesn't expose any equivalent and the Gum tool surfaces no IsFilled variable on
    // arcs. Force Stroke style at the renderable so a stray `arc.IsFilled = true` (or a
    // FillColor setter that flips IsFilled as a side effect) cannot produce the segment shape.
    // True pie-wedge fills are reachable through the supported path: set Thickness = Width/2
    // on a square arc and the stroke band collapses to a wedge.
    [Fact]
    public void Arc_GetPaint_ShouldAlwaysUseStrokeStyle_EvenWhenIsFilledIsTrue()
    {
        TestableArc arc = new();
        arc.IsFilled = true;

        using SKPaint paint = arc.InvokeGetPaint(new SKRect(0, 0, 100, 100), absoluteRotation: 0);

        paint.Style.ShouldBe(
            SKPaintStyle.Stroke,
            "Arc must never render in Fill mode — Skia's SKPath.AddArc autocloses with a chord, " +
            "producing a circular-segment shape that's neither a useful arc nor a useful wedge.");
    }

    // Arc reuses its path between draws; these pin that each input the path depends on rebuilds it.
    // With the bounds below the arc is a ring of radius 40 around (50, 50): sweep 90 from angle 0
    // covers the top-right quarter, so (78, 22) is on it and (22, 22) is not.
    private static readonly SKRect RingBounds = new SKRect(10, 10, 90, 90);

    [Fact]
    public void DrawBound_SweepAngleChangedAfterFirstDraw_DrawsTheNewSweep()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 100));
        Arc arc = CreateArc(startAngle: 0, sweepAngle: 90);
        arc.DrawBound(RingBounds, surface.Canvas, absoluteRotation: 0);

        arc.SweepAngle = 180;
        surface.Canvas.Clear(SKColors.Transparent);
        arc.DrawBound(RingBounds, surface.Canvas, absoluteRotation: 0);

        PixelAt(surface, 22, 22).Alpha.ShouldBeGreaterThan((byte)0);
    }

    [Fact]
    public void DrawBound_StartAngleChangedAfterFirstDraw_DrawsFromTheNewStart()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 100));
        Arc arc = CreateArc(startAngle: 0, sweepAngle: 90);
        arc.DrawBound(RingBounds, surface.Canvas, absoluteRotation: 0);

        arc.StartAngle = 90;
        surface.Canvas.Clear(SKColors.Transparent);
        arc.DrawBound(RingBounds, surface.Canvas, absoluteRotation: 0);

        PixelAt(surface, 78, 22).Alpha.ShouldBe((byte)0);
        PixelAt(surface, 22, 22).Alpha.ShouldBeGreaterThan((byte)0);
    }

    [Fact]
    public void DrawBound_BoundsChangedAfterFirstDraw_DrawsInTheNewBounds()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 100));
        Arc arc = CreateArc(startAngle: 0, sweepAngle: 90);
        arc.DrawBound(new SKRect(10, 10, 50, 50), surface.Canvas, absoluteRotation: 0);

        surface.Canvas.Clear(SKColors.Transparent);
        arc.DrawBound(RingBounds, surface.Canvas, absoluteRotation: 0);

        PixelAt(surface, 78, 22).Alpha.ShouldBeGreaterThan((byte)0);
    }

    [Fact]
    public void Clone_ChangingTheCloneSweep_LeavesTheSourceDrawingItsOwnSweep()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 100));
        Arc source = CreateArc(startAngle: 0, sweepAngle: 90);
        source.DrawBound(RingBounds, surface.Canvas, absoluteRotation: 0);
        Arc clone = (Arc)source.Clone();

        clone.SweepAngle = 180;
        clone.DrawBound(RingBounds, surface.Canvas, absoluteRotation: 0);
        source.Dispose();
        surface.Canvas.Clear(SKColors.Transparent);
        clone.DrawBound(RingBounds, surface.Canvas, absoluteRotation: 0);

        PixelAt(surface, 22, 22).Alpha.ShouldBeGreaterThan((byte)0);
    }

    private static Arc CreateArc(float startAngle, float sweepAngle)
    {
        Arc arc = new Arc();
        arc.Color = SKColors.Red;
        arc.StrokeWidth = 10;
        arc.StartAngle = startAngle;
        arc.SweepAngle = sweepAngle;
        return arc;
    }

    private static SKColor PixelAt(SKSurface surface, int x, int y)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }

    private sealed class TestableArc : Arc
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);
    }
}
