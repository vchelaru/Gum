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

        public bool UseGradient { get; set; }
        public int Red1 { get; set; }
        public int Green1 { get; set; }
        public int Blue1 { get; set; }

        public int Red2 { get; set; }
        public int Green2 { get; set; }
        public int Blue2 { get; set; }

        public GradientType GradientType { get; set; }

        public float GradientX1 { get; set; }
        public float GradientY1 { get; set; }

        public float GradientX2 { get; set; }
        public float GradientY2 { get; set; }

        public float GradientOuterRadius { get; set; }
        public float GradientInnerRadius { get; set; }

        public bool IsEndRounded { get; set; }

        SKPaint Paint
        {
            get
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
                    var firstColor = new SKColor((byte)Red1, (byte)Green1, (byte)Blue1);
                    var secondColor = new SKColor((byte)Red2, (byte)Green2, (byte)Blue2);

                    if (GradientType == GradientType.Linear)
                    {

                        paint.Shader = SKShader.CreateLinearGradient(
                            new SKPoint(GradientX1, GradientY1), // left, top
                            new SKPoint(GradientX2, GradientY2), // right, bottom
                            new SKColor[] { firstColor, secondColor },
                            new float[] { 0, 1 },
                            SKShaderTileMode.Clamp);
                    }
                    else if (GradientType == GradientType.Radial)
                    {
                        var outerRadius = GradientOuterRadius;
                        if (GradientOuterRadius <= 0)
                        {
                            outerRadius = 100;
                        }
                        var innerToOuterRatio = GradientInnerRadius / outerRadius;

                        paint.Shader = SKShader.CreateRadialGradient(
                            new SKPoint(GradientX1, GradientY1), // center
                            outerRadius,
                            new SKColor[] { firstColor, secondColor },
                            new float[] { innerToOuterRatio, 1 },
                            SKShaderTileMode.Clamp);
                    }
                }

                return paint;


            }
        }
        public override void DrawBound(SKRect boundingRect, SKCanvas canvas)
        {
            using (var paint = Paint)
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
