using RenderingLibrary.Graphics;
using SkiaSharp;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using System;

namespace SkiaGum.Renderables
{
    public class Sprite : RenderableBase, IAspectRatio
    {
        public SKBitmap Texture { get; set; }

        public Rectangle? SourceRectangle;

        public Rectangle? EffectiveRectangle
        {
            get
            {
                Rectangle? sourceRectangle = SourceRectangle;
                return sourceRectangle;
            }
        }

        public float AspectRatio => Texture != null ? (Texture.Width / (float)Texture.Height) : 1.0f;

        public Sprite()
        {

        }

        public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
        {
            ////////////Early Out/////////////
            if (Texture == null)
            {
                return;
            }
            /////////End Early Out///////////////

            var applyRotation = absoluteRotation != 0;

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
                canvas.RotateDegrees(-absoluteRotation);
            }

            if(EffectiveRectangle != null)
            {

                var sourceRectangle = new SKRect(EffectiveRectangle.Value.Left, EffectiveRectangle.Value.Top, EffectiveRectangle.Value.Right, EffectiveRectangle.Value.Bottom);

                var isFlippedHorizontal =
                    sourceRectangle.Left > sourceRectangle.Right;

                var isFlippedVertical = 
                    sourceRectangle.Top > sourceRectangle.Bottom;

                if (isFlippedHorizontal || isFlippedVertical)
                {
                    using (new SKAutoCanvasRestore(canvas, true))
                    {
                        var imageWidth = sourceRectangle.Left - sourceRectangle.Right;
                        var imageHeight = sourceRectangle.Top - sourceRectangle.Bottom;
                        canvas.Scale(
                            isFlippedHorizontal ? -1 : 1, 
                            isFlippedVertical ? -1 : 1, 
                            isFlippedHorizontal ? imageWidth : 0, 
                            isFlippedVertical ? imageHeight : 0);

                        var left = Math.Min(EffectiveRectangle.Value.Left, EffectiveRectangle.Value.Right);
                        var right = Math.Max(EffectiveRectangle.Value.Left, EffectiveRectangle.Value.Right);
                        var top = Math.Min(EffectiveRectangle.Value.Top, EffectiveRectangle.Value.Bottom);
                        var bottom = Math.Max(EffectiveRectangle.Value.Top, EffectiveRectangle.Value.Bottom);

                        sourceRectangle = new SKRect(
                            left, 
                            top, 
                            right, 
                            bottom);
                        canvas.DrawBitmap(Texture, sourceRectangle, boundingRect);
                    }

                }
                else
                {

                    canvas.DrawBitmap(Texture, sourceRectangle, boundingRect);
                }

            }
            else
            {
                canvas.DrawBitmap(Texture, boundingRect);
            }

            if (applyRotation)
            {
                canvas.Restore();
            }
        }


    }
}
