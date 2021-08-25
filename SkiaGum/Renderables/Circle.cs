using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Renderables
{
    class Circle : RenderableBase
    {
        public override void DrawBound(SKRect boundingRect, SKCanvas canvas)
        {
            using (var paint = GetPaint(boundingRect))
            {
                var radius = System.Math.Min(boundingRect.Width, boundingRect.Height) / 2.0f;
                canvas.DrawCircle(boundingRect.MidX, boundingRect.MidY, radius, paint);
            }
        }
    }
}
