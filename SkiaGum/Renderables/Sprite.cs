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

        public void Render(SKCanvas canvas)
        {
            if (AbsoluteVisible)
            {
                SKMatrix scaleMatrix = SKMatrix.MakeScale(1, 1);
                //// Gum uses counter clockwise rotation, Skia uses clockwise, so invert:
                SKMatrix rotationMatrix = SKMatrix.MakeRotationDegrees(-Rotation);
                SKMatrix translateMatrix = SKMatrix.MakeTranslation(this.GetAbsoluteX(), this.GetAbsoluteY());
                SKMatrix result = SKMatrix.MakeIdentity();

                SKMatrix.Concat(
                    ref result, rotationMatrix, scaleMatrix);
                SKMatrix.Concat(
                    ref result, translateMatrix, result);
                canvas.Save();
                canvas.SetMatrix(result);

                var destination = new SKRect(0, 0, Width, Height);

                canvas.DrawBitmap(Texture, destination);
                canvas.Restore();
            }
        }
    }
}
