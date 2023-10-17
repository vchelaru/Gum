using SkiaSharp;

namespace SkiaGum.Renderables
{
    class SolidRectangle : RenderableBase
    {
       
        public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
        {
            var radius = System.Math.Min(boundingRect.Width, boundingRect.Height) / 2.0f;
            using(var paint = GetPaint(boundingRect, absoluteRotation))
            {
                canvas.DrawRect(boundingRect, paint);
            }
        }
    }
}
