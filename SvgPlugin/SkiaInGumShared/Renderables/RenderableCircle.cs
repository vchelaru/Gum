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

            using (var paint = CreatePaint())
            {
                var radius = Width / 2;
                surface.Canvas.DrawCircle(new SKPoint(radius, radius), 
                    // subtract 1 on the radius to allow antialiasing to work
                    radius-1, paint);
            }
        }
    }
}
