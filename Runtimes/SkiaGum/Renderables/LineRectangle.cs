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
/// <remarks>
/// Implements <see cref="ICloneable"/> so <see cref="Gum.Wireframe.GraphicalUiElement.Clone"/>
/// can deep-copy a <c>RectangleRuntime</c> without throwing &#8212; same recipe as
/// <see cref="Circle"/> (issue #2790): MemberwiseClone, then reset children, parent,
/// and the cached paint so the clone is structurally independent of the source.
/// </remarks>
public class LineRectangle : RenderableShapeBase, ICloneable
{
    /// <summary>
    /// Issue #2814 &#8212; required by <see cref="Gum.Wireframe.GraphicalUiElement.Clone"/>
    /// so <c>RectangleRuntime.Clone</c> can produce an independent stroke slot. Mirrors
    /// the <see cref="Circle.Clone"/> pattern added in issue #2790.
    /// </summary>
    public object Clone()
    {
        LineRectangle clone = (LineRectangle)MemberwiseClone();
        clone.mChildren = new();
        clone.mParent = null;
        clone.ClearCachedPaint();
        return clone;
    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        SKPaint paint = GetCachedPaint(boundingRect, absoluteRotation);
        canvas.DrawRect(boundingRect, paint);
    }
}
