using SkiaSharp;

namespace SkiaGum.Renderables
{
    class Circle : RenderableBase
    {
        public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
        {
            using (var paint = GetPaint(boundingRect, absoluteRotation))
            {
                var radius = System.Math.Min(boundingRect.Width, boundingRect.Height) / 2.0f;
                canvas.DrawCircle(boundingRect.MidX, boundingRect.MidY, radius, paint);
            }
        }
    }
}
