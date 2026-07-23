using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using RenderingLibrary.Math;
using SkiaSharp;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using System;

namespace SkiaGum.Renderables;

public class Sprite : RenderableShapeBase, IAspectRatio, ITextureCoordinate, IAnimatable, ICloneable
{
    public object Clone()
    {
        return this.MemberwiseClone();
    }
    public SKBitmap? Texture 
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

    public SKImage? Image { get; set; }

    /// <summary>
    /// The render-target container whose baked offscreen texture this sprite displays, in place of a
    /// directly-assigned <see cref="Texture"/>/<see cref="Image"/> (#3988). When set, the sprite pulls
    /// the baked <see cref="SKImage"/> from the renderer at draw time; a null bake draws nothing.
    /// </summary>
    public IRenderableIpso? RenderTargetTextureSource { get; set; }

    public float? TextureWidth => RenderTargetTextureSource?.Width ?? Texture?.Width;
    public float? TextureHeight => RenderTargetTextureSource?.Height ?? Texture?.Height;

    public Rectangle? SourceRectangle;
    private SKBitmap? _texture;

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

    public float AspectRatio
    {
        get
        {
            if (RenderTargetTextureSource != null && RenderTargetTextureSource.Height != 0)
            {
                return RenderTargetTextureSource.Width / RenderTargetTextureSource.Height;
            }
            return Texture != null ? (Texture.Width / (float)Texture.Height) : 1.0f;
        }
    }

    public AnimationChainLogic AnimationLogic { get; } = new AnimationChainLogic();

    public Sprite()
    {
        AnimationLogic.ApplyFrame = ApplyAnimationFrame;
    }

    void ApplyAnimationFrame(Gum.Graphics.Animation.AnimationFrame frame)
    {
        Texture = frame.Texture;

        if (frame.Texture != null)
        {
            var tex = frame.Texture;
            var left = MathFunctions.RoundToInt(frame.LeftCoordinate * tex.Width);
            var right = MathFunctions.RoundToInt(frame.RightCoordinate * tex.Width);
            var top = MathFunctions.RoundToInt(frame.TopCoordinate * tex.Height);
            var bottom = MathFunctions.RoundToInt(frame.BottomCoordinate * tex.Height);

            // Skia encodes flips by swapping source rectangle edges (see DrawBound).
            if (frame.FlipHorizontal) (left, right) = (right, left);
            if (frame.FlipVertical) (top, bottom) = (bottom, top);

            SourceRectangle = new Rectangle(left, top, right - left, bottom - top);
        }
        else
        {
            SourceRectangle = null;
        }
    }

    public bool AnimateSelf(double secondDifference)
    {
        return AnimationLogic.AnimateSelf(secondDifference);
    }

    protected override SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
    {
        // Matches MonoGame's default sampler behaviour (no edge antialias on
        // sprites) and prevents seams when multiple Sprites abut at fractional
        // pixel boundaries. See NineSlice.GetPaint for the longer rationale.
        // Could be made opt-in via a property on Sprite if a consumer needs the
        // softer rotated-edge look back.
        SKPaint paint = base.GetPaint(boundingRect, absoluteRotation);
        paint.IsAntialias = false;
        return paint;
    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        // Render-target pull (#3988): display the baked texture of the referenced container instead of
        // an assigned Texture/Image. A null bake (degenerate size, or off-screen) draws nothing rather
        // than stale content, matching the raylib/MonoGame pull model.
        if (RenderTargetTextureSource != null)
        {
            SKImage? bakedImage = Renderer.Self.TryGetBakedRenderTargetFor(RenderTargetTextureSource);
            if (bakedImage == null)
            {
                return;
            }

            SKPaint renderTargetPaint = base.GetCachedPaint(boundingRect, absoluteRotation);
            canvas.DrawImage(bakedImage, boundingRect, renderTargetPaint);
            return;
        }

        ////////////Early Out/////////////
        if (Texture == null && Image == null)
        {
            return;
        }
        /////////End Early Out///////////////

        // RenderableShapeBase.Render has already saved the canvas, translated to
        // boundingRect.Left/Top, rotated by -absoluteRotation, and zeroed out the
        // boundingRect origin before calling DrawBound. Re-applying the rotation
        // here would double it (a 25 deg Sprite would render at 50 deg) — see the
        // matching fix in NineSlice.DrawBound.

        var paint = base.GetCachedPaint(boundingRect, absoluteRotation);

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
    }


}
