using Gum.Converters;
using Gum.Managers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Renderables
{
    public class Arc : RenderableBase
    {
        public float StartAngle
        {
            get;
            set;
        } = 0;

        public float SweepAngle
        {
            get;
            set;
        } = 90;

        public float Thickness
        {
            get => base.StrokeWidth;
            set => base.StrokeWidth = value;
        }

        public bool IsEndRounded { get; set; }


        protected override SKPaint GetPaint(SKRect boundingRect)
        {
            var paint = base.GetPaint(boundingRect);
            paint.StrokeCap = IsEndRounded ? SKStrokeCap.Round : SKStrokeCap.Butt;
            return paint;


        }

        public override void DrawBound(SKRect boundingRect, SKCanvas canvas)
        {
            using (var paint = GetPaint(boundingRect))
            {
                var adjustedRect = new SKRect(
                    boundingRect.Left + Thickness / 2,
                    boundingRect.Top + Thickness / 2,
                    boundingRect.Right - Thickness / 2,
                    boundingRect.Bottom - Thickness / 2);

                using (var path = new SKPath())
                {
                    path.AddArc(adjustedRect, -StartAngle, -SweepAngle);
                    canvas.DrawPath(path, paint);
                }
            }
        }
    }
}
