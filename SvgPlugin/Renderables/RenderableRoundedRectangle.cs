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
            surface.Canvas.Clear(SKColors.Transparent);

            var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            using (var paint = new SKPaint { Color = skColor, Style = SKPaintStyle.Fill, IsAntialias = true })
            {
                var radius = Width / 2;
                surface.Canvas.DrawRoundRect(0,0,Width, Height, CornerRadius, CornerRadius, paint);
            }
        }
    }
}
