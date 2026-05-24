using System;
using SkiaSharp;

namespace SkiaGum.Renderables;

public class Circle : RenderableShapeBase, ICloneable
{
    /// <summary>
    /// Issue #2790 — required by <see cref="Gum.Wireframe.GraphicalUiElement.Clone"/> so
    /// shape runtimes can be deep-copied. MemberwiseClone copies every paint/dimension
    /// field; the children collection, parent pointer, and cached paint are reset so the
    /// clone is structurally independent of the source.
    /// </summary>
    public object Clone()
    {
        Circle clone = (Circle)MemberwiseClone();
        clone.mChildren = new();
        clone.mParent = null;
        clone.ClearCachedPaint();
        return clone;
    }

    /// <summary>
    /// Derived radius — computed from the current bounding box on get (matching what
    /// <see cref="DrawBound"/> uses), and on set assigns <see cref="RenderableShapeBase.Width"/>
    /// and <see cref="RenderableShapeBase.Height"/> to <c>value * 2</c>. Provided for API
    /// parity with the XNA-like <c>LineCircle</c> renderable so the shared
    /// <c>CircleRuntime.Radius</c> read/write compiles uniformly across backends; the
    /// setter actually drives the visual through Width/Height (the same dimensions
    /// <see cref="DrawBound"/> reads).
    /// </summary>
    public float Radius
    {
        get => System.Math.Min(Width, Height) / 2.0f;
        set
        {
            Width = value * 2;
            Height = value * 2;
        }
    }

    /// <summary>
    /// Issue #2834 — pushed by <see cref="Gum.GueDeriving.CircleRuntime.PreRender"/> on the
    /// fill slot when a visible stroke is paired with the fill. Subtracted from the rendered
    /// radius so the fill's outer AA halo lands inside the stroke's opaque band, eliminating
    /// the pink halo the two-slot model would otherwise produce on the inside of the stroke.
    /// Applied at render time only; Width/Height stay layout-owned (the fill IS the runtime's
    /// contained sizing object, so mutating its Width would feed back into layout). Ignored
    /// when <see cref="RenderableShapeBase.IsFilled"/> is false — only the fill instance
    /// honors the inset.
    /// </summary>
    public float FillRadiusInset { get; set; }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        var radius = System.Math.Min(boundingRect.Width, boundingRect.Height) / 2.0f;
        if (IsFilled)
        {
            radius = System.Math.Max(0f, radius - FillRadiusInset);
        }
        canvas.DrawCircle(boundingRect.MidX, boundingRect.MidY, radius, paint);
    }
}
