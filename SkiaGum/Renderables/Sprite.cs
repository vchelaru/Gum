using RenderingLibrary.Graphics;
using SkiaSharp;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;

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
                var skRect = new SKRect(EffectiveRectangle.Value.Left, EffectiveRectangle.Value.Top, EffectiveRectangle.Value.Right, EffectiveRectangle.Value.Bottom);
                canvas.DrawBitmap(Texture, skRect, boundingRect);
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
