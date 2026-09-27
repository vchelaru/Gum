using SkiaSharp;
using System;

namespace SkiaGum.Renderables;

/// <summary>
/// Stroke-flavored rectangle renderable used as the second slot under the two-slot
/// fill+stroke composition model on <see cref="Gum.GueDeriving.RectangleRuntime"/>
/// (issue #2814). Mirrors how <see cref="Circle"/> serves as both slots on
/// <c>CircleRuntime</c>: <see cref="RenderableShapeBase.IsFilled"/> chooses Fill vs
/// Stroke paint style, so the same DrawBound code path works for either, but having a
/// dedicated stroke type keeps the naming parallel with the XNA-like
/// <c>LineRectangle</c> and lets the runtime hand back independent fill/stroke instances.
/// </summary>
public class LineRectangle : RenderableShapeBase
{
    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        SKPaint paint = GetCachedPaint(boundingRect, absoluteRotation);
        canvas.DrawRect(boundingRect, paint);
    }
}
