using Gum.Content.AnimationChain;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using RenderingLibrary.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SkiaGum.Renderables;

public class NineSlice : RenderableShapeBase, IAnimatable, ITextureCoordinate
{
    /// <summary>
    /// Shared AnimationChain playback state. The constructor wires
    /// <see cref="AnimationChainLogic.ApplyFrame"/> to copy the active frame's
    /// texture and (UV-derived) source rectangle onto this NineSlice.
    /// </summary>
    public AnimationChainLogic AnimationLogic { get; private set; } = new AnimationChainLogic();

    /// <inheritdoc/>
    public override object Clone()
    {
        NineSlice clone = (NineSlice)base.Clone();
        clone.AnimationLogic = AnimationLogic.Clone(clone.ApplyAnimationFrame);
        // Each side disposes its own section images, so the clone builds its own.
        clone._sectionImages = null;
        // An owned image would be disposed by whichever side changes texture first, so the clone
        // builds its own. A caller-assigned image is shared, since neither side disposes it.
        if (_ownsImage && _texture != null)
        {
            clone._image = SKImage.FromBitmap(_texture);
        }
        return clone;
    }

    public NineSlice()
    {
        // RenderableShapeBase defaults Color to red, which would tint every
        // drawn nine-slice red once we route Color through a Modulate filter.
        // White is the no-tint identity for SKBlendMode.Modulate.
        Color = SKColors.White;

        AnimationLogic.ApplyFrame = ApplyAnimationFrame;
    }

    void ApplyAnimationFrame(Gum.Graphics.Animation.AnimationFrame frame)
    {
        Texture = frame.Texture;

        if (frame.Texture != null)
        {
            SKBitmap tex = frame.Texture;
            int left = MathFunctions.RoundToInt(frame.LeftCoordinate * tex.Width);
            int right = MathFunctions.RoundToInt(frame.RightCoordinate * tex.Width);
            int top = MathFunctions.RoundToInt(frame.TopCoordinate * tex.Height);
            int bottom = MathFunctions.RoundToInt(frame.BottomCoordinate * tex.Height);

            // NineSlice does not honour FlipHorizontal/FlipVertical per-slice at
            // render time the way Sprite does — slices each draw with their own
            // src rect math — so we always store the canonical (positive) rect.
            // If flip support is added later, switch to Sprite's swap pattern.
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

    /// <inheritdoc/>
    public bool AnimateSelf(double secondDifference)
    {
        if (!Visible)
        {
            return false;
        }
        return AnimationLogic.AnimateSelf(secondDifference);
    }

    /// <summary>
    /// The bitmap this nine-slice draws. Setting it builds a new <see cref="Image"/> that the
    /// nine-slice owns and disposes when it is replaced. The bitmap itself is never disposed here,
    /// since it usually comes from the shared content cache.
    /// </summary>
    public SKBitmap? Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            SetImage(value != null ? SKImage.FromBitmap(value) : null, ownsImage: true);
        }
    }
    private SKBitmap? _texture;

    /// <summary>
    /// The image this nine-slice draws. An image assigned here stays owned by the caller: the
    /// nine-slice never disposes it.
    /// </summary>
    public SKImage? Image
    {
        get => _image;
        set => SetImage(value, ownsImage: false);
    }

    private SKImage? _image;
    private bool _ownsImage;

    private void SetImage(SKImage? image, bool ownsImage)
    {
        if (ReferenceEquals(image, _image))
        {
            return;
        }
        ClearSectionImages();
        if (_ownsImage)
        {
            _image?.Dispose();
        }
        _image = image;
        _ownsImage = ownsImage && image != null;
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        SetImage(null, ownsImage: false);
        base.Dispose();
    }

    public Rectangle? SourceRectangle { get; set; }

    public float? TextureWidth => Texture?.Width;
    public float? TextureHeight => Texture?.Height;

    // RenderableShapeBase.Wrap is a non-virtual `public bool Wrap => false;`, so
    // the ITextureCoordinate.Wrap setter is exposed via explicit interface
    // implementation. Mirrors SkiaGum.Sprite, which has the same pattern.
    bool ITextureCoordinate.Wrap
    {
        get => false;
        set { }
    }

    /// <summary>
    /// Edge thickness in texture pixels. When null, the renderable uses 1/3 of the
    /// effective texture region for each corner/edge band (the standard nine-slice split).
    /// </summary>
    public float? CustomFrameTextureCoordinateWidth { get; set; }

    /// <summary>
    /// When true, the Top, Bottom, Left, Right, and Center sections are repeated
    /// (tiled) at their natural source size scaled by <see cref="BorderScale"/>,
    /// rather than stretched to fill the available space.
    /// </summary>
    public bool IsTilingMiddleSections { get; set; }

    /// <summary>
    /// Multiplier applied to the destination thickness of every nine-slice band,
    /// allowing the border to be drawn larger or smaller than its source pixel size.
    /// </summary>
    public float BorderScale { get; set; } = 1f;

    protected override SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
    {
        // Modulate the image's texels by Color so the unified runtime's Color
        // property tints the nine-slice. The base paint has Color set too, but
        // that only matters for non-image draws (DrawRect, etc.); for DrawImage
        // we need a ColorFilter.
        SKPaint paint = base.GetPaint(boundingRect, absoluteRotation);
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
            paint.ColorFilter = SKColorFilter.CreateBlendMode(Color, SKBlendMode.Modulate);
        }
        // Antialias on DrawImage anti-aliases the destination rect's edges. When
        // two sections of the nine-slice abut at a fractional pixel boundary
        // (because layout placed the whole NineSlice at a non-integer position),
        // the two AA edges leave partial-coverage gaps along the seam and the
        // background bleeds through as a darker line. Disabling AA gives hard
        // edges that meet cleanly; the cost is slightly jaggier outlines when
        // the NineSlice is rotated, which is consistent with the pixel-art use
        // case nine-slice borders are typically authored for.
        paint.IsAntialias = false;
        return paint;
    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        // RenderableShapeBase.Render has already saved the canvas, translated to
        // boundingRect.Left/Top, rotated by -absoluteRotation, and zeroed out the
        // boundingRect origin. Do NOT re-apply rotation here.

        if (Image == null)
        {
            return;
        }

        SKPaint paint = base.GetCachedPaint(boundingRect, absoluteRotation);

        int srcLeft;
        int srcTop;
        int srcRight;
        int srcBottom;
        if (SourceRectangle.HasValue)
        {
            srcLeft = SourceRectangle.Value.Left;
            srcTop = SourceRectangle.Value.Top;
            srcRight = SourceRectangle.Value.Right;
            srcBottom = SourceRectangle.Value.Bottom;
        }
        else
        {
            srcLeft = 0;
            srcTop = 0;
            srcRight = Image.Width;
            srcBottom = Image.Height;
        }

        int usedWidth = srcRight - srcLeft;
        int usedHeight = srcBottom - srcTop;
        if (usedWidth <= 0 || usedHeight <= 0)
        {
            return;
        }

        int fullOutsideW;
        int fullOutsideH;
        if (CustomFrameTextureCoordinateWidth.HasValue)
        {
            fullOutsideW = (int)Math.Round(CustomFrameTextureCoordinateWidth.Value);
            fullOutsideH = fullOutsideW;
        }
        else
        {
            fullOutsideW = (usedWidth + 1) / 3;
            fullOutsideH = (usedHeight + 1) / 3;
        }

        int insideTextureW = usedWidth - (fullOutsideW * 2);
        int insideTextureH = usedHeight - (fullOutsideH * 2);

        // Destination corner thickness, scaled by BorderScale and clamped so two
        // opposing corners never overlap the available width/height.
        float destCornerW = Math.Min(fullOutsideW * BorderScale, boundingRect.Width / 2f);
        float destCornerH = Math.Min(fullOutsideH * BorderScale, boundingRect.Height / 2f);

        // Texture-pixel corner thickness must shrink in lockstep when destCorner has been
        // clamped, otherwise the corner draw would sample more pixels than fit on screen.
        int srcCornerW = BorderScale > 0 ? (int)Math.Round(destCornerW / BorderScale) : fullOutsideW;
        int srcCornerH = BorderScale > 0 ? (int)Math.Round(destCornerH / BorderScale) : fullOutsideH;
        if (srcCornerW > fullOutsideW)
        {
            srcCornerW = fullOutsideW;
        }
        if (srcCornerH > fullOutsideH)
        {
            srcCornerH = fullOutsideH;
        }

        // Sampling follows Renderer.TextureFilter, like Sprite. Under Linear, a pixel at a section's
        // edge samples half a texel past it, and SkiaSharp's DrawImage(src, dest) does not clamp to
        // src, so it would blend in the neighboring section's texels. Each section is then drawn
        // from its own image (see GetSectionImage), whose edges clamp.
        SKSamplingOptions sampling = global::RenderingLibrary.Graphics.Renderer.TextureSampling;
        SectionDraw draw = new(canvas, paint, sampling, UseSectionImages: sampling.Filter != SKFilterMode.Nearest);

        float destLeft = boundingRect.Left;
        float destTop = boundingRect.Top;
        float destRight = boundingRect.Right;
        float destBottom = boundingRect.Bottom;
        float destInsideW = boundingRect.Width - destCornerW * 2;
        float destInsideH = boundingRect.Height - destCornerH * 2;
        if (destInsideW < 0)
        {
            destInsideW = 0;
        }
        if (destInsideH < 0)
        {
            destInsideH = 0;
        }

        DrawSection(
            srcLeft, srcTop, srcCornerW, srcCornerH,
            destLeft, destTop, destCornerW, destCornerH,
            draw);

        DrawSection(
            srcRight - srcCornerW, srcTop, srcCornerW, srcCornerH,
            destRight - destCornerW, destTop, destCornerW, destCornerH,
            draw);

        DrawSection(
            srcLeft, srcBottom - srcCornerH, srcCornerW, srcCornerH,
            destLeft, destBottom - destCornerH, destCornerW, destCornerH,
            draw);

        DrawSection(
            srcRight - srcCornerW, srcBottom - srcCornerH, srcCornerW, srcCornerH,
            destRight - destCornerW, destBottom - destCornerH, destCornerW, destCornerH,
            draw);

        DrawMiddleSection(
            srcLeft + srcCornerW, srcTop, insideTextureW, srcCornerH,
            destLeft + destCornerW, destTop, destInsideW, destCornerH,
            tileHorizontally: IsTilingMiddleSections, tileVertically: false,
            draw);

        DrawMiddleSection(
            srcLeft + srcCornerW, srcBottom - srcCornerH, insideTextureW, srcCornerH,
            destLeft + destCornerW, destBottom - destCornerH, destInsideW, destCornerH,
            tileHorizontally: IsTilingMiddleSections, tileVertically: false,
            draw);

        DrawMiddleSection(
            srcLeft, srcTop + srcCornerH, srcCornerW, insideTextureH,
            destLeft, destTop + destCornerH, destCornerW, destInsideH,
            tileHorizontally: false, tileVertically: IsTilingMiddleSections,
            draw);

        DrawMiddleSection(
            srcRight - srcCornerW, srcTop + srcCornerH, srcCornerW, insideTextureH,
            destRight - destCornerW, destTop + destCornerH, destCornerW, destInsideH,
            tileHorizontally: false, tileVertically: IsTilingMiddleSections,
            draw);

        DrawMiddleSection(
            srcLeft + srcCornerW, srcTop + srcCornerH, insideTextureW, insideTextureH,
            destLeft + destCornerW, destTop + destCornerH, destInsideW, destInsideH,
            tileHorizontally: IsTilingMiddleSections, tileVertically: IsTilingMiddleSections,
            draw);
    }

    // What every section of one DrawBound call draws with.
    private readonly record struct SectionDraw(
        SKCanvas Canvas, SKPaint Paint, SKSamplingOptions Sampling, bool UseSectionImages);

    // Section images by source rect, kept across frames so a GPU canvas uploads each one once.
    // Cleared whenever Image changes.
    private Dictionary<SKRectI, SKImage>? _sectionImages;

    // A cap on distinct section rects (an animated SourceRectangle or resized corners add more)
    // before the cache is dropped and rebuilt.
    private const int MaxCachedSectionImages = 64;

    // Returns the image holding just this section, or null when the caller should sample Image
    // directly, as Nearest does: the section is all of Image, or not inside it.
    private SKImage? GetSectionImage(SKRectI section)
    {
        _sectionImages ??= new Dictionary<SKRectI, SKImage>();
        if (_sectionImages.TryGetValue(section, out SKImage? cached))
        {
            return cached;
        }

        SKRectI imageBounds = new SKRectI(0, 0, Image!.Width, Image.Height);
        // A whole-image section already clamps at its edges. Subset would also hand back Image
        // itself here, which the cache must never dispose.
        if (section == imageBounds || !imageBounds.Contains(section))
        {
            return null;
        }

        if (_sectionImages.Count >= MaxCachedSectionImages)
        {
            ClearSectionImages();
            _sectionImages = new Dictionary<SKRectI, SKImage>();
        }

        SKImage? sectionImage = Image.Subset(section);
        if (sectionImage != null)
        {
            _sectionImages[section] = sectionImage;
        }
        return sectionImage;
    }

    private void ClearSectionImages()
    {
        if (_sectionImages == null)
        {
            return;
        }
        foreach (SKImage sectionImage in _sectionImages.Values)
        {
            sectionImage.Dispose();
        }
        _sectionImages = null;
    }

    private void DrawSection(
        int srcX, int srcY, int srcW, int srcH,
        float destX, float destY, float destW, float destH,
        SectionDraw draw)
    {
        DrawMiddleSection(srcX, srcY, srcW, srcH, destX, destY, destW, destH,
            tileHorizontally: false, tileVertically: false, draw);
    }

    private void DrawMiddleSection(
        int srcX, int srcY, int srcW, int srcH,
        float destX, float destY, float destW, float destH,
        bool tileHorizontally, bool tileVertically,
        SectionDraw draw)
    {
        if (destW <= 0 || destH <= 0 || srcW <= 0 || srcH <= 0)
        {
            return;
        }

        // Source coordinates below are relative to sourceImage: the section's own image starts at
        // the section's corner.
        SKImage sourceImage = Image!;
        SKImage? sectionImage = draw.UseSectionImages
            ? GetSectionImage(SKRectI.Create(srcX, srcY, srcW, srcH))
            : null;
        if (sectionImage != null)
        {
            sourceImage = sectionImage;
            srcX = 0;
            srcY = 0;
        }

        if (!tileHorizontally && !tileVertically)
        {
            SKRect src = new SKRect(srcX, srcY, srcX + srcW, srcY + srcH);
            SKRect dest = new SKRect(destX, destY, destX + destW, destY + destH);
            draw.Canvas.DrawImage(sourceImage, src, dest, draw.Sampling, draw.Paint);
            return;
        }

        float tileDestW = tileHorizontally ? srcW * BorderScale : destW;
        float tileDestH = tileVertically ? srcH * BorderScale : destH;
        if (tileDestW <= 0 || tileDestH <= 0)
        {
            return;
        }

        float currentY = 0;
        while (currentY < destH)
        {
            float remainingH = destH - currentY;
            float thisTileDestH = Math.Min(tileDestH, remainingH);
            int thisSrcH = tileVertically
                ? (int)Math.Round(srcH * (thisTileDestH / tileDestH))
                : srcH;
            if (thisSrcH <= 0)
            {
                break;
            }

            float currentX = 0;
            while (currentX < destW)
            {
                float remainingW = destW - currentX;
                float thisTileDestW = Math.Min(tileDestW, remainingW);
                int thisSrcW = tileHorizontally
                    ? (int)Math.Round(srcW * (thisTileDestW / tileDestW))
                    : srcW;
                if (thisSrcW <= 0)
                {
                    break;
                }

                SKRect src = new SKRect(srcX, srcY, srcX + thisSrcW, srcY + thisSrcH);
                SKRect dest = new SKRect(
                    destX + currentX,
                    destY + currentY,
                    destX + currentX + thisTileDestW,
                    destY + currentY + thisTileDestH);
                draw.Canvas.DrawImage(sourceImage, src, dest, draw.Sampling, draw.Paint);

                currentX += thisTileDestW;
            }
            currentY += thisTileDestH;
        }
    }
}
