using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Renderables
{
    public class RenderableCanvas : RenderableSkiaObject
    {
        public override void DrawToSurface(SKSurface surface)
        {
            // nothing (for now)
            //surface.Canvas.Clear(SKColors.Transparent);

            //var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            //using (var paint = CreatePaint())
            //{
            //    var adjustedRect = new SKRect(
            //        0 + Thickness / 2,
            //        0 + Thickness / 2,
            //        Width - Thickness / 2,
            //        Height - Thickness / 2);

            //    using (var path = new SKPath())
            //    {
            //        path.AddArc(adjustedRect, -startAngle, -sweepAngle);
            //        surface.Canvas.DrawPath(path, paint);
            //    }


            //}
        }
    }
}
