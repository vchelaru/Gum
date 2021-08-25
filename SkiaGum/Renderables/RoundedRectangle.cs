using RenderingLibrary;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Renderables
{
    class RoundedRectangle : RenderableBase
    {
        public float CornerRadius { get; set; }

        float XSizeSpillover => HasDropshadow ? DropshadowBlurX + Math.Abs(DropshadowOffsetX) : 0;
        float YSizeSpillover => HasDropshadow ? DropshadowBlurY + Math.Abs(DropshadowOffsetY) : 0;

        public RoundedRectangle()
        {
            CornerRadius = 5;
            Color = SKColors.White;
        }


        public override void DrawBound(SKRect boundingRect, SKCanvas canvas)
        {
            using (var paint = GetPaint(boundingRect))
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

    }
}
