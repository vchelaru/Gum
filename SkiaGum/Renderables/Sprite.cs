using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

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


            canvas.DrawBitmap(Texture, boundingRect);

            if (applyRotation)
            {
                canvas.Restore();
            }
        }


    }
}
