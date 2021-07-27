using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace SkiaPlugin.Renderables
{
    enum GradientType
    {
        Linear,
        Radial
    }

    class RenderableArc : RenderableSkiaObject
    {
        float thickness = 10;
        public float Thickness
        {
            get => thickness;
            set
            {
                if(thickness != value)
                {
                    thickness = value;
                    needsUpdate = true;
                }
            }
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

        bool useGradient;
        public bool UseGradient
        {
            get => useGradient;
            set
            {
                if(value != useGradient)
                {
                    useGradient = value;
                    needsUpdate = true;
                }
            }
        }

        float gradientX1;
        public float GradientX1
        {
            get => gradientX1;
            set
            {
                if(value != gradientX1)
                {
                    gradientX1 = value;
                    needsUpdate = true;
                }
            }
        }

        float gradientY1;
        public float GradientY1
        {
            get => gradientY1;
            set
            {
                if (value != gradientY1)
                {
                    gradientY1 = value;
                    needsUpdate = true;
                }
            }
        }

        float gradientX2;
        public float GradientX2
        {
            get => gradientX2;
            set
            {
                if (value != gradientX2)
                {
                    gradientX2 = value;
                    needsUpdate = true;
                }
            }
        }

        float gradientY2;
        public float GradientY2
        {
            get => gradientY2;
            set
            {
                if (value != gradientY2)
                {
                    gradientY2 = value;
                    needsUpdate = true;
                }
            }
        }

        int red1;
        public int Red1
        {
            get => red1;
            set
            {
                if(red1 != value)
                {
                    red1 = value;
                    needsUpdate = true;
                }
            }
        }

        int green1;
        public int Green1
        {
            get => green1;
            set
            {
                if (green1 != value)
                {
                    green1 = value;
                    needsUpdate = true;
                }
            }
        }

        int blue1;
        public int Blue1
        {
            get => blue1;
            set
            {
                if (blue1 != value)
                {
                    blue1 = value;
                    needsUpdate = true;
                }
            }
        }

        int red2;
        public int Red2
        {
            get => red2;
            set
            {
                if (red2 != value)
                {
                    red2 = value;
                    needsUpdate = true;
                }
            }
        }

        int green2;
        public int Green2
        {
            get => green2;
            set
            {
                if (green2 != value)
                {
                    green2 = value;
                    needsUpdate = true;
                }
            }
        }

        int blue2;
        public int Blue2
        {
            get => blue2;
            set
            {
                if (blue2 != value)
                {
                    blue2 = value;
                    needsUpdate = true;
                }
            }
        }

        GradientType gradientType;
        public GradientType GradientType
        {
            get => gradientType;
            set
            {
                if (gradientType != value)
                {
                    gradientType = value;
                    needsUpdate = true;
                }
            }
        }

        internal override void DrawToSurface(SKSurface surface)
        {
            surface.Canvas.Clear(SKColors.Transparent);

            var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            using (var paint = new SKPaint { Color = skColor, Style = SKPaintStyle.Stroke, StrokeWidth = Thickness, IsAntialias = true })
            {
                paint.StrokeCap = IsEndRounded ? SKStrokeCap.Round : SKStrokeCap.Butt;
                //var radius = Width / 2;
                //surface.Canvas.DrawCircle(new SKPoint(radius, radius), radius, paint);

                if(useGradient)
                {
                    var firstColor = new SKColor((byte)red1, (byte)green1, (byte)blue1);
                    var secondColor = new SKColor((byte)red2, (byte)green2, (byte)blue2);

                    if(gradientType == GradientType.Linear)
                    {

                        paint.Shader = SKShader.CreateLinearGradient(
                            new SKPoint(gradientX1, gradientY1), // left, top
                            new SKPoint(gradientX2, gradientY2), // right, bottom
                            new SKColor[] { firstColor, secondColor },
                            new float[] { 0, 1 },
                            SKShaderTileMode.Clamp);
                    }
                    else if(gradientType == GradientType.Radial)
                    {
                        var radiusSquared = (gradientX2 - gradientX1) * (gradientX2 - gradientX1) +
                            (gradientY2 - gradientY1) * (gradientY2 - gradientY1);
                        var radius = (float)Math.Sqrt(radiusSquared);
                        paint.Shader = SKShader.CreateRadialGradient(
                            new SKPoint(gradientX1, gradientY1), // center
                            radius,
                            new SKColor[] { firstColor, secondColor },
                            new float[] { 0, 1 },
                            SKShaderTileMode.Clamp);
                    }
                }


                var adjustedRect = new SKRect(
                    0 + Thickness / 2,
                    0 + Thickness / 2,
                    Width - Thickness / 2,
                    Height - Thickness / 2);

                using (var path = new SKPath())
                {
                    path.AddArc(adjustedRect, -startAngle, -sweepAngle);
                    surface.Canvas.DrawPath(path, paint);
                }


            }
        }
    }
}
