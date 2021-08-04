using System;
using System.Collections.Generic;
using System.Text;
using Gum.Converters;
using Gum.Managers;
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



        float gradientInnerRadius;
        public float GradientInnerRadius
        {
            get => gradientInnerRadius;
            set
            {
                if(value != gradientInnerRadius)
                {
                    gradientInnerRadius = value;
                    needsUpdate = true;
                }
            }
        }

        float gradientOuterRadius;
        public float GradientOuterRadius
        {
            get => gradientOuterRadius;
            set
            {
                if (value != gradientOuterRadius)
                {
                    gradientOuterRadius = value;
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

                    var effectiveGradientX1 = gradientX1;
                    switch(this.GradientX1Units)
                    {
                        case PositionUnitType.PixelsFromCenterX:
                            effectiveGradientX1 += Width / 2.0f;
                            break;
                        case PositionUnitType.PixelsFromRight:
                            effectiveGradientX1 += Width;
                            break;
                        case PositionUnitType.PercentageWidth:
                            effectiveGradientX1 = Width * 100 / gradientX1;
                            break;
                    }

                    var effectiveGradientX2 = gradientX1;
                    switch (this.GradientX2Units)
                    {
                        case PositionUnitType.PixelsFromCenterX:
                            effectiveGradientX2 += Width / 2.0f;
                            break;
                        case PositionUnitType.PixelsFromRight:
                            effectiveGradientX2 += Width;
                            break;
                        case PositionUnitType.PercentageWidth:
                            effectiveGradientX2 = Width * 100 / gradientX2;
                            break;
                    }

                    var effectiveGradientY1 = gradientY1;
                    switch(this.GradientY1Units)
                    {
                        case PositionUnitType.PixelsFromCenterY:
                            effectiveGradientY1 += Height / 2.0f;
                            break;
                        case PositionUnitType.PixelsFromBottom:
                            effectiveGradientY1 += Height;
                            break;
                        case PositionUnitType.PercentageHeight:
                            effectiveGradientY1 = Height * 100 / gradientY1;
                            break;
                    }

                    var effectiveGradientY2 = gradientY2;
                    switch (this.GradientY2Units)
                    {
                        case PositionUnitType.PixelsFromCenterY:
                            effectiveGradientY2 += Height / 2.0f;
                            break;
                        case PositionUnitType.PixelsFromBottom:
                            effectiveGradientY2 += Height;
                            break;
                        case PositionUnitType.PercentageHeight:
                            effectiveGradientY2 = Height * 100 / gradientY2;
                            break;
                    }


                    if (gradientType == GradientType.Linear)
                    {

                        paint.Shader = SKShader.CreateLinearGradient(
                            new SKPoint(effectiveGradientX1, effectiveGradientY1), // left, top
                            new SKPoint(effectiveGradientX2, effectiveGradientY2), // right, bottom
                            new SKColor[] { firstColor, secondColor },
                            new float[] { 0, 1 },
                            SKShaderTileMode.Clamp);
                    }
                    else if(gradientType == GradientType.Radial)
                    {
                        var effectiveOuterRadius = gradientOuterRadius;

                        switch(gradientOuterRadiusUnits)
                        {
                            case Gum.DataTypes.DimensionUnitType.Percentage:
                                effectiveOuterRadius = Width * gradientOuterRadius / 100;
                                break;
                            case Gum.DataTypes.DimensionUnitType.RelativeToContainer:
                                effectiveOuterRadius = Width/2 + gradientOuterRadius;
                                break;
                        }

                        if (effectiveOuterRadius <= 0)
                        {
                            effectiveOuterRadius = 100;
                        }

                        var effectiveInnerRadius = gradientInnerRadius;

                        switch(gradientInnerRadiusUnits)
                        {
                            case Gum.DataTypes.DimensionUnitType.Percentage:
                                effectiveInnerRadius = Width * gradientInnerRadius / 100;
                                break;
                            case Gum.DataTypes.DimensionUnitType.RelativeToContainer:
                                effectiveInnerRadius = Width/2 + gradientInnerRadius;
                                break;
                        }

                        var innerToOuterRatio = effectiveInnerRadius / effectiveOuterRadius;

                        paint.Shader = SKShader.CreateRadialGradient(
                            new SKPoint(effectiveGradientX1, effectiveGradientY1), // center
                            effectiveOuterRadius,
                            new SKColor[] { firstColor, secondColor },
                            new float[] { innerToOuterRatio, 1 },
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
