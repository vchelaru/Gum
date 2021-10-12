using RenderingLibrary;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Renderables
{
    class Circle : RenderableBase
    {
        public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
        {
            using (var paint = GetPaint(boundingRect, absoluteRotation))
            {
                var rotation = this.GetAbsoluteRotation();
                var applyRotation = rotation != 0;

                if (applyRotation)
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

                var radius = System.Math.Min(boundingRect.Width, boundingRect.Height) / 2.0f;
                canvas.DrawCircle(boundingRect.MidX, boundingRect.MidY, radius, paint);

                if (applyRotation)
                {
                    canvas.Restore();
                }
            }
        }
    }
}
