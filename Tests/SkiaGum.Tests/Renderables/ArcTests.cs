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

    private sealed class TestableArc : Arc
    {
        public SKPaint InvokeGetPaint(SKRect boundingRect, float absoluteRotation)
            => GetPaint(boundingRect, absoluteRotation);
    }
}
