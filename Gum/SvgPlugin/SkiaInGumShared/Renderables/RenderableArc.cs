using SkiaSharp;

namespace SkiaGum.Renderables
{
    public class RenderableArc : RenderableSkiaObject
    {
        public float Thickness
        {
            get => StrokeWidth;
            set => StrokeWidth = value;
        }

        float startAngle = 0;
        public float StartAngle
        {
            get => startAngle;
            set
            {
                if(startAngle != value)
                {
                    startAngle = value;
                    needsUpdate = true;
                }
            }

        }

        float sweepAngle = 90;
        public float SweepAngle
        {
            get => sweepAngle;
            set
            {
                if(sweepAngle != value)
                {
                    sweepAngle = value;
                    needsUpdate = true;
                }
            }
        }

        bool isEndRounded;
        public bool IsEndRounded
        {
            get => isEndRounded;
            set
            {
                if(isEndRounded != value)
                {
                    isEndRounded = value;
                    needsUpdate = true;
                }
            }
        }

        protected override SKPaint CreatePaint()
        {
            var paint = base.CreatePaint();

            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeCap = IsEndRounded ? SKStrokeCap.Round : SKStrokeCap.Butt;

            return paint;
        }

        public override void DrawToSurface(SKSurface surface)
        {
            surface.Canvas.Clear(SKColors.Transparent);

            var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            using (var paint = CreatePaint())
            { 
                var adjustedRect = new SKRect(
                    XSizeSpillover + Thickness / 2,
                    YSizeSpillover + Thickness / 2,
                    XSizeSpillover + Width - Thickness / 2,
                    YSizeSpillover + Height - Thickness / 2);

                using (var path = new SKPath())
                {
                    path.AddArc(adjustedRect, -startAngle, -sweepAngle);
                    surface.Canvas.DrawPath(path, paint);
                }
            }
        }
    }
}
