using System;
using System.Collections.Generic;
using System.Text;
using Gum.Converters;
using Gum.Managers;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace SkiaPlugin.Renderables
{
    class RenderableArc : RenderableSkiaObject
    {
        float thickness = 10;
        public float Thickness
        {
            get => thickness;
            set
            {
                if(thickness != value)
                {
                    thickness = value;
                    needsUpdate = true;
                }
            }
        }

        float startAngle = 0;
        public float StartAngle
        {
            get => startAngle;
            set
            {
                if(startAngle != value)
                {
                    startAngle = value;
                    needsUpdate = true;
                }
            }

        }

        float sweepAngle = 90;
        public float SweepAngle
        {
            get => sweepAngle;
            set
            {
                if(sweepAngle != value)
                {
                    sweepAngle = value;
                    needsUpdate = true;
                }
            }
        }

        bool isEndRounded;
        public bool IsEndRounded
        {
            get => isEndRounded;
            set
            {
                if(isEndRounded != value)
                {
                    isEndRounded = value;
                    needsUpdate = true;
                }
            }
        }

        internal override void DrawToSurface(SKSurface surface)
        {
            surface.Canvas.Clear(SKColors.Transparent);

            var skColor = new SKColor(Color.R, Color.G, Color.B, Color.A);

            using (var paint = new SKPaint { Color = skColor, Style = SKPaintStyle.Stroke, StrokeWidth = Thickness, IsAntialias = true })
            {
                paint.StrokeCap = IsEndRounded ? SKStrokeCap.Round : SKStrokeCap.Butt;
                //var radius = Width / 2;
                //surface.Canvas.DrawCircle(new SKPoint(radius, radius), radius, paint);

                if(UseGradient)
                {
                    SetGradientOnPaint(paint);
                }


                var adjustedRect = new SKRect(
                    0 + Thickness / 2,
                    0 + Thickness / 2,
                    Width - Thickness / 2,
                    Height - Thickness / 2);

                using (var path = new SKPath())
                {
                    path.AddArc(adjustedRect, -startAngle, -sweepAngle);
                    surface.Canvas.DrawPath(path, paint);
                }


            }
        }
    }
}
