using SkiaSharp;

namespace SkiaGum.Renderables;

public class Circle : RenderableShapeBase
{
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

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        var radius = System.Math.Min(boundingRect.Width, boundingRect.Height) / 2.0f;
        canvas.DrawCircle(boundingRect.MidX, boundingRect.MidY, radius, paint);
    }
}
