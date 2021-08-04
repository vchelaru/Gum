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

        public bool UseGradient { get; set; }
        public int Red1 { get; set; }
        public int Green1 { get; set; }
        public int Blue1 { get; set; }

        public int Red2 { get; set; }
        public int Green2 { get; set; }
        public int Blue2 { get; set; }

        public GradientType GradientType { get; set; }

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
                var firstColor = new SKColor((byte)Red1, (byte)Green1, (byte)Blue1);
                var secondColor = new SKColor((byte)Red2, (byte)Green2, (byte)Blue2);

                var effectiveGradientX1 = GradientX1;
                switch (this.GradientX1Units)
                {
                    case GeneralUnitType.PixelsFromMiddle:
                        effectiveGradientX1 += Width / 2.0f;
                        break;
                    case GeneralUnitType.PixelsFromLarge:
                        effectiveGradientX1 += Width;
                        break;
                    case GeneralUnitType.Percentage:
                        effectiveGradientX1 = Width * 100 / GradientX1;
                        break;
                }

                var effectiveGradientX2 = GradientX1;
                //switch (this.GradientX2Units)
                //{
                //    case PositionUnitType.PixelsFromCenterX:
                //        effectiveGradientX2 += Width / 2.0f;
                //        break;
                //    case PositionUnitType.PixelsFromRight:
                //        effectiveGradientX2 += Width;
                //        break;
                //    case PositionUnitType.PercentageWidth:
                //        effectiveGradientX2 = Width * 100 / GradientX2;
                //        break;
                //}

                var effectiveGradientY1 = GradientY1;
                switch (this.GradientY1Units)
                {
                    case GeneralUnitType.PixelsFromMiddle:
                        effectiveGradientY1 += Height / 2.0f;
                        break;
                    case GeneralUnitType.PixelsFromLarge:
                        effectiveGradientY1 += Height;
                        break;
                    case GeneralUnitType.Percentage:
                        effectiveGradientY1 = Height * 100 / GradientY1;
                        break;
                }

                var effectiveGradientY2 = GradientY2;
                //switch (this.GradientY2Units)
                //{
                //    case PositionUnitType.PixelsFromCenterY:
                //        effectiveGradientY2 += Height / 2.0f;
                //        break;
                //    case PositionUnitType.PixelsFromBottom:
                //        effectiveGradientY2 += Height;
                //        break;
                //    case PositionUnitType.PercentageHeight:
                //        effectiveGradientY2 = Height * 100 / GradientY2;
                //        break;
                //}

                effectiveGradientX1 += boundingRect.Left;
                effectiveGradientY1 += boundingRect.Top;
                effectiveGradientX2 += boundingRect.Left;
                effectiveGradientY2 += boundingRect.Top;

                if (GradientType == GradientType.Linear)
                {

                    paint.Shader = SKShader.CreateLinearGradient(
                        new SKPoint(effectiveGradientX1, effectiveGradientY1), // left, top
                        new SKPoint(effectiveGradientX2, effectiveGradientY2), // right, bottom
                        new SKColor[] { firstColor, secondColor },
                        new float[] { 0, 1 },
                        SKShaderTileMode.Clamp);
                }
                else if (GradientType == GradientType.Radial)
                {
                    var effectiveOuterRadius = GradientOuterRadius;

                    switch (GradientOuterRadiusUnits)
                    {
                        case Gum.DataTypes.DimensionUnitType.Percentage:
                            effectiveOuterRadius = Width * GradientOuterRadius / 100;
                            break;
                        case Gum.DataTypes.DimensionUnitType.RelativeToContainer:
                            effectiveOuterRadius = Width / 2 + GradientOuterRadius;
                            break;
                    }

                    if (effectiveOuterRadius <= 0)
                    {
                        effectiveOuterRadius = 100;
                    }

                    var effectiveInnerRadius = GradientInnerRadius;

                    switch (GradientInnerRadiusUnits)
                    {
                        case Gum.DataTypes.DimensionUnitType.Percentage:
                            effectiveInnerRadius = Width * GradientInnerRadius / 100;
                            break;
                        case Gum.DataTypes.DimensionUnitType.RelativeToContainer:
                            effectiveInnerRadius = Width / 2 + GradientInnerRadius;
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
