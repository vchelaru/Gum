using Microsoft.Xna.Framework;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaPlugin.Renderables
{
    class RenderableRoundedRectangle : RenderableSkiaObject
    {
        public float CornerRadius { get; set; } = 5;

        public bool IsFilled { get; set; } = false;
        public float StrokeWidth { get; set; } = 1;


        internal override void DrawToSurface(SKSurface surface)
        {
            if(surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }
            if(surface.Canvas == null)
            {
                throw new ArgumentNullException(nameof(surface.Canvas));
            }
            surface.Canvas.Clear(SKColors.Transparent);


            using (var paint = CreatePaint())
            {
                var radius = Width / 2;


                var leftMargin = XSizeSpillover;
                var topMargin = YSizeSpillover;

                var drawWidth = Width;
                var drawHeight = Height;

                if(IsFilled == false)
                {
                    leftMargin += StrokeWidth / 2.0f;
                    topMargin += StrokeWidth / 2.0f;

                    drawWidth -= StrokeWidth;
                    drawHeight -= StrokeWidth;
                }
                surface.Canvas.DrawRoundRect(leftMargin,topMargin, drawWidth, drawHeight, CornerRadius, CornerRadius, paint);
            }
        }

        private SKPaint CreatePaint()
        {
            var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            var paint = new SKPaint 
            { 
                Color = skColor, 
                Style = IsFilled ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth,
                IsAntialias = true 
            };

            if (UseGradient)
            {
                SetGradientOnPaint(paint);
            }

            if (HasDropshadow)
            {
                var dropshadowSkColor = new SKColor(DropshadowColor.R, DropshadowColor.G, DropshadowColor.B, DropshadowColor.A);
                paint.ImageFilter = SKImageFilter.CreateDropShadow(
                            DropshadowOffsetX,
                            // See https://stackoverflow.com/questions/60456526/how-can-i-tell-the-amount-of-space-needed-for-a-skia-dropshadow
                            DropshadowOffsetY,
                            DropshadowBlurX/3.0f,
                            DropshadowBlurY/3.0f,
                            dropshadowSkColor,
                            SKDropShadowImageFilterShadowMode.DrawShadowAndForeground);
            }

            return paint;
        }
    }
}
