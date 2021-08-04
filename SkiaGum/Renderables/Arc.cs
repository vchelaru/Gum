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
        public SKColor Color
        {
            get; set;
        } = SKColors.Red;

        public int Alpha
        {
            get => Color.Alpha;
            set
            {
                this.Color = new SKColor(this.Color.Red, this.Color.Green, this.Color.Blue, (byte)value);
            }
        }

        public int Blue
        {
            get => Color.Blue;
            set
            {
                this.Color = new SKColor(this.Color.Red, this.Color.Green, (byte)value, this.Color.Alpha);
            }
        }

        public int Green
        {
            get => Color.Green;
            set
            {
                this.Color = new SKColor(this.Color.Red, (byte)value, this.Color.Blue, this.Color.Alpha);
            }
        }

        public int Red
        {
            get => Color.Red;
            set
            {
                this.Color = new SKColor((byte)value, this.Color.Green, this.Color.Blue, this.Color.Alpha);
            }
        }

        public float Thickness
        {
            get;
            set;
        } = 10;

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


        public bool IsEndRounded { get; set; }

        SKPaint GetPaint(SKRect boundingRect)
        {
            var paint = new SKPaint
            {
                Color = this.Color,
                IsAntialias = true,
                StrokeWidth = Thickness,
                Style = SKPaintStyle.Stroke
            };

            paint.StrokeCap = IsEndRounded ? SKStrokeCap.Round : SKStrokeCap.Butt;


            if (UseGradient)
            {
                ApplyGradientToPaint(boundingRect, paint);
            }

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
