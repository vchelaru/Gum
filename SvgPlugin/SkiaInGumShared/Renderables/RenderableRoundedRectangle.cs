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
    }
}
