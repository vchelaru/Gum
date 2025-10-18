using RenderingLibrary.Graphics;
using SkiaSharp;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using System;

namespace SkiaGum.Renderables;

public class Sprite : RenderableShapeBase, IAspectRatio, ITextureCoordinate
{
    public SKBitmap Texture 
    { 
        get => _texture;
        set
        {
            _texture = value;
            if(value != null)
            {
                Image = SKImage.FromBitmap(value);
            }
            else
            {
                Image = null;
            }
        }
    }

    public SKImage Image { get; set; }

    public float? TextureWidth => Texture?.Width;
    public float? TextureHeight => Texture?.Height;

    public Rectangle? SourceRectangle;
    private SKBitmap _texture;

    public Rectangle? EffectiveRectangle => SourceRectangle;

    Rectangle? ITextureCoordinate.SourceRectangle
    {
        get => SourceRectangle;
        set => SourceRectangle = value;
    }

    bool ITextureCoordinate.Wrap
    {
        get => false;
        set { } // don't support this yet...
    }

    public float AspectRatio => Texture != null ? (Texture.Width / (float)Texture.Height) : 1.0f;

    public Sprite()
    {

    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        ////////////Early Out/////////////
        if (Texture == null && Image == null)
        {
            return;
        }
        /////////End Early Out///////////////

        var paint = base.GetCachedPaint(boundingRect, absoluteRotation);

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
                    var imageWidth = System.Math.Abs(sourceRectangle.Left - sourceRectangle.Right);
                    var imageHeight = System.Math.Abs(sourceRectangle.Top - sourceRectangle.Bottom);
                    canvas.Scale(
                        isFlippedHorizontal ? -1 : 1, 
                        isFlippedVertical ? -1 : 1, 
                        isFlippedHorizontal ? 0 : 0, 
                        isFlippedVertical ? imageHeight/2f : 0);

                    var left = Math.Min(EffectiveRectangle.Value.Left, EffectiveRectangle.Value.Right);
                    var right = Math.Max(EffectiveRectangle.Value.Left, EffectiveRectangle.Value.Right);
                    var top = Math.Min(EffectiveRectangle.Value.Top, EffectiveRectangle.Value.Bottom);
                    var bottom = Math.Max(EffectiveRectangle.Value.Top, EffectiveRectangle.Value.Bottom);

                    sourceRectangle = new SKRect(
                        left, 
                        top, 
                        right, 
                        bottom);

                    if(Image != null)
                    {
                        canvas.DrawImage(Image, sourceRectangle, boundingRect, paint);
                    }
                    else
                    {
                        canvas.DrawBitmap(Texture, sourceRectangle, boundingRect, paint);
                    }

                }

            }
            else
            {
                if(Image != null)
                {
                    canvas.DrawImage(Image, sourceRectangle, boundingRect, paint);
                }
                else
                {
                    canvas.DrawBitmap(Texture, sourceRectangle, boundingRect, paint);
                }
            }

        }
        else
        {
            if (Image != null)
            {
                canvas.DrawImage(Image, boundingRect, paint);
            }
            else
            {
                canvas.DrawBitmap(Texture, boundingRect, paint);
            }
        }

        if (applyRotation)
        {
            canvas.Restore();
        }
    }


}
