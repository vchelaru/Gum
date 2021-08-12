using Microsoft.Xna.Framework;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaPlugin.Renderables
{
    class RenderableCircle : RenderableSkiaObject
    {

        internal override void DrawToSurface(SKSurface surface)
        {
            surface.Canvas.Clear(SKColors.Transparent);

            var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            using (var paint = new SKPaint { Color = skColor, Style = SKPaintStyle.Fill, IsAntialias = true })
            {
                if (UseGradient)
                {
                    SetGradientOnPaint(paint);
                }

                var radius = Width / 2;
                surface.Canvas.DrawCircle(new SKPoint(radius, radius), radius, paint);
            }
        }
    }
}
