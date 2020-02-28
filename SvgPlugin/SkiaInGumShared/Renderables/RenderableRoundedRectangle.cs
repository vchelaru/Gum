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

        public bool HasDropshadow { get; set; }

        public float DropshadowOffsetX { get; set; }
        public float DropshadowOffsetY { get; set; }

        public float DropshadowBlurX { get; set; }
        public float DropshadowBlurY { get; set; }


        protected override float XSizeSpillover => HasDropshadow ? DropshadowBlurX + Math.Abs(DropshadowOffsetX) : 0;
        protected override float YSizeSpillover => HasDropshadow ? DropshadowBlurY + Math.Abs(DropshadowOffsetY) : 0;

        internal override void DrawToSurface(SKSurface surface)
        {
            surface.Canvas.Clear(SKColors.Transparent);


            using (var paint = CreatePaint())
            {
                var radius = Width / 2;

                var leftMargin = XSizeSpillover;
                var topMargin = YSizeSpillover;
                surface.Canvas.DrawRoundRect(leftMargin,topMargin, Width, Height, CornerRadius, CornerRadius, paint);
            }
        }

        private SKPaint CreatePaint()
        {
            var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            var paint = 
                new SKPaint { Color = skColor, Style = SKPaintStyle.Fill, IsAntialias = true };

            if(HasDropshadow)
            {

                paint.ImageFilter = SKImageFilter.CreateDropShadow(
                            DropshadowOffsetX,
                            // See https://stackoverflow.com/questions/60456526/how-can-i-tell-the-amount-of-space-needed-for-a-skia-dropshadow
                            DropshadowOffsetY,
                            DropshadowBlurX/3.0f,
                            DropshadowBlurY/3.0f,
                            SKColors.Black,
                            SKDropShadowImageFilterShadowMode.DrawShadowAndForeground);
            }

            return paint;
        }
    }
}
