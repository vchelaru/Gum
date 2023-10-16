using SkiaSharp;

namespace SkiaGum.Renderables
{
    class RoundedRectangle : RenderableBase
    {
        public float CornerRadius { get; set; }

        public RoundedRectangle()
        {
            CornerRadius = 5;
            Color = SKColors.White;
        }

        public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
        {
            using (var paint = GetPaint(boundingRect, absoluteRotation))
            {
                canvas.DrawRoundRect(boundingRect, CornerRadius, CornerRadius, paint);
            }
        }
    }
}
