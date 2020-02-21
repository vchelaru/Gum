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
        public Color Color = Color.White;

        public int Alpha
        {
            get => Color.A;
            set
            {
                Color.A = (byte)value;
                needsUpdate = true;
            }
        }

        public int Red
        {
            get => Color.R;
            set
            {
                Color.R = (byte)value;
                needsUpdate = true;
            }
        }

        public int Green
        {
            get => Color.G;
            set
            {
                Color.G = (byte)value;
                needsUpdate = true;
            }
        }

        public int Blue
        {
            get => Color.B;
            set
            {
                Color.B = (byte)value;
                needsUpdate = true;
            }
        }

        internal override void DrawToSurface(SKSurface surface)
        {
            surface.Canvas.Clear(SKColors.Transparent);

            var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            using (var whitePaint = new SKPaint { Color = skColor, Style = SKPaintStyle.Fill, IsAntialias = true })
            {
                var radius = Width / 2;
                surface.Canvas.DrawCircle(new SKPoint(radius, radius), radius, whitePaint);
            }
        }
    }
}
