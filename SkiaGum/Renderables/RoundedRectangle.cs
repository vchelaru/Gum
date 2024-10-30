using RenderingLibrary;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Renderables
{
    class RoundedRectangle : RenderableBase, IClipPath
    {
        public float CornerRadius { get; set; }

        public RoundedRectangle()
        {
            CornerRadius = 5;
            Color = SKColors.White;
        }

        public SKPath GetClipPath()
        {
            SKPath path = new SKPath();

            var absoluteX = this.GetAbsoluteX();
            var absoluteY = this.GetAbsoluteY();
            var boundingRect = new SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

            path.AddRoundRect(boundingRect, CornerRadius, CornerRadius);

            return path;
        }

        public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
        {
            using (var paint = GetPaint(boundingRect, absoluteRotation))
            {
                canvas.DrawRoundRect(boundingRect, CornerRadius, CornerRadius, paint);
            }
        }
    }
}
