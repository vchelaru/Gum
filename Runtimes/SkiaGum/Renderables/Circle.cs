using SkiaSharp;

namespace SkiaGum.Renderables;

public class Circle : RenderableShapeBase
{
    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        var radius = System.Math.Min(boundingRect.Width, boundingRect.Height) / 2.0f;
        canvas.DrawCircle(boundingRect.MidX, boundingRect.MidY, radius, paint);
    }
}
