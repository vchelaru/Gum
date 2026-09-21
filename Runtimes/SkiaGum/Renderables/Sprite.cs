using Gum.Content.AnimationChain;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using RenderingLibrary.Math;
using SkiaSharp;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using System;

namespace SkiaGum.Renderables;

public class Sprite : RenderableShapeBase, IAspectRatio, ITextureCoordinate, IAnimatable, ICloneable, IRenderTargetTextureReferencer
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

    /// <summary>
    /// How the sprite's tint <see cref="RenderableShapeBase.Color"/> combines with its texture,
    /// matching MonoGame's/raylib's <see cref="ColorOperation"/> (#4821). <see cref="ColorOperation.Modulate"/>
    /// (the default) multiplies texture RGBA by the tint; <see cref="ColorOperation.ColorTextureAlpha"/>
    /// uses the texture only as an alpha mask and fills with the tint color, via <see cref="SKBlendMode.SrcIn"/>
    /// in <see cref="GetPaint"/>.
    /// </summary>
    public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;

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
        // RenderableShapeBase defaults Color to red, which would tint every drawn sprite red once
        // GetPaint routes Color through a Modulate filter (mirrors SkiaGum.Renderables.NineSlice's
        // constructor). White is the no-tint identity for SKBlendMode.Modulate.
        Color = SKColors.White;

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

        if (frame.Alpha.HasValue)
        {
            Alpha = frame.Alpha.Value;
        }

        if (frame.ColorOperation == AnimationFrameColorOperation.Multiply)
        {
            Red = frame.Red ?? 255;
            Green = frame.Green ?? 255;
            Blue = frame.Blue ?? 255;
            ColorOperation = ColorOperation.Modulate;
        }
        else if (frame.ColorOperation == AnimationFrameColorOperation.Add)
        {
            // Black (0) is Add's identity, so an unset channel contributes nothing - unlike
            // Multiply's 255 identity above.
            Red = frame.Red ?? 0;
            Green = frame.Green ?? 0;
            Blue = frame.Blue ?? 0;
            ColorOperation = ColorOperation.Add;
        }
        else if (ColorOperation == ColorOperation.Add)
        {
            ColorOperation = ColorOperation.Modulate;
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

        // Modulate/ColorTextureAlpha (#4821, matching MonoGame/raylib's SpriteRuntime.ColorOperation):
        // Modulate multiplies the image's texels by Color; ColorTextureAlpha instead uses the image
        // only as an alpha mask and fills with Color (SrcIn: result = tint * dst.Alpha). Mirrors
        // NineSlice.GetPaint — the base paint's Color is set too, but that only matters for
        // non-image draws; DrawImage needs a ColorFilter. White (the default Color, see the
        // constructor) is the Modulate identity, so this is a no-op for sprites that never touch
        // Red/Green/Blue.
        // Add draws the texture untinted and adds Color as a single-pass color-matrix offset,
        // rather than a second draw pass like RenderingLibrary.Graphics.Renderer.
        // DrawAdditiveColorOverlay. The offset is added in unpremultiplied space and gets scaled by
        // the pixel's own alpha when Skia premultiplies for compositing, so it's naturally masked by
        // the drawn image's silhouette.
        if (ColorOperation == ColorOperation.Add)
        {
            float[] addMatrix =
            {
                1, 0, 0, 0, Color.Red / 255f,
                0, 1, 0, 0, Color.Green / 255f,
                0, 0, 1, 0, Color.Blue / 255f,
                0, 0, 0, 1, 0,
            };
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(addMatrix);
        }
        else
        {
            paint.ColorFilter = SKColorFilter.CreateBlendMode(Color,
                ColorOperation == ColorOperation.ColorTextureAlpha ? SKBlendMode.SrcIn : SKBlendMode.Modulate);
        }

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
