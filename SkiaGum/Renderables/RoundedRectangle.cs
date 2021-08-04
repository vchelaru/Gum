using RenderingLibrary;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Renderables
{
    class RoundedRectangle : RenderableBase
    {
        public SKColor Color
        {
            get; 
            set;
        }

        public int Alpha
        {
            get => Color.Alpha;
            set => this.Color = new SKColor(this.Color.Red, this.Color.Green, this.Color.Blue, (byte)value);
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

        public SKColor DropshadowColor
        {
            get; set;
        }

        public int DropshadowAlpha
        {
            get => DropshadowColor.Alpha;
            set
            {
                this.DropshadowColor = new SKColor(this.DropshadowColor.Red, this.DropshadowColor.Green, this.DropshadowColor.Blue, (byte)value);
            }
        }

        public int DropshadowBlue
        {
            get => DropshadowColor.Blue;
            set
            {
                this.DropshadowColor = new SKColor(this.DropshadowColor.Red, this.DropshadowColor.Green, (byte)value, this.DropshadowColor.Alpha);
            }
        }

        public int DropshadowGreen
        {
            get => DropshadowColor.Green;
            set
            {
                this.DropshadowColor = new SKColor(this.DropshadowColor.Red, (byte)value, this.DropshadowColor.Blue, this.DropshadowColor.Alpha);
            }
        }

        public int DropshadowRed
        {
            get => DropshadowColor.Red;
            set
            {
                this.DropshadowColor = new SKColor((byte)value, this.DropshadowColor.Green, this.DropshadowColor.Blue, this.DropshadowColor.Alpha);
            }
        }

        public bool HasDropshadow { get; set; }

        public float DropshadowOffsetX { get; set; }
        public float DropshadowOffsetY { get; set; }

        public float DropshadowBlurX { get; set; }
        public float DropshadowBlurY { get; set; }

        public float CornerRadius { get; set; }

        float XSizeSpillover => HasDropshadow ? DropshadowBlurX + Math.Abs(DropshadowOffsetX) : 0;
        float YSizeSpillover => HasDropshadow ? DropshadowBlurY + Math.Abs(DropshadowOffsetY) : 0;

        public bool IsFilled { get; set; } = true;
        public float StrokeWidth { get; set; } = 2;

        public RoundedRectangle()
        {
            CornerRadius = 5;
            Color = SKColors.White;
        }


        public override void DrawBound(SKRect boundingRect, SKCanvas canvas)
        {
            using (var paint = CreatePaint(boundingRect))
            {
                var rotation = this.GetAbsoluteRotation();

                var applyRotation = rotation != 0;

                if(applyRotation)
                {
                    var oldX = boundingRect.Left;
                    var oldY = boundingRect.Top;

                    canvas.Save();

                    boundingRect.Left = 0;
                    boundingRect.Right -= oldX;
                    boundingRect.Top = 0;
                    boundingRect.Bottom -= oldY;

                    canvas.Translate(oldX, oldY);
                    canvas.RotateDegrees(-rotation);
                }

                // If this is stroke-only, then the stroke is centered around the bounds 
                // we pass in. Therefore, we need to move the bounds "in" by half of the 
                // stroke width
                if(IsFilled == false)
                {
                    boundingRect.Left += StrokeWidth / 2.0f;
                    boundingRect.Top += StrokeWidth / 2.0f;
                    boundingRect.Right -= StrokeWidth / 2.0f;
                    boundingRect.Bottom -= StrokeWidth / 2.0f;
                }
                canvas.DrawRoundRect(boundingRect, CornerRadius, CornerRadius, paint);

                if(applyRotation)
                {
                    canvas.Restore();
                }

            }
        }

        private SKPaint CreatePaint(SKRect boundingRect)
        {
            var paint = new SKPaint 
            { 
                Color = Color,
                Style = IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth,
                IsAntialias = true 
            };

            if (HasDropshadow)
            {
                paint.ImageFilter = SKImageFilter.CreateDropShadow(
                            DropshadowOffsetX,
                            // See https://stackoverflow.com/questions/60456526/how-can-i-tell-the-amount-of-space-needed-for-a-skia-dropshadow
                            DropshadowOffsetY,
                            DropshadowBlurX / 3.0f,
                            DropshadowBlurY / 3.0f,
                            DropshadowColor,
                            SKDropShadowImageFilterShadowMode.DrawShadowAndForeground);
            }


            if (UseGradient)
            {
                ApplyGradientToPaint(boundingRect, paint);
            }

            return paint;
        }
    }
}
