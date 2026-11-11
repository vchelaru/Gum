using Gum.Content.AnimationChain;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using RenderingLibrary.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SkiaGum.Renderables;

public class NineSlice : RenderableShapeBase, IAnimatable, IAnimationChainRenderable, ITextureCoordinate
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
        clone.AnimationLogic = AnimationLogic.Clone(clone.ApplyAnimationFrame, clone.ApplyKeyframeColor);
        // Each side disposes its own section images, so the clone builds its own.
        clone._sectionImages = null;
        // An owned image would be disposed by whichever side changes texture first, so the clone
        // builds its own. A caller-assigned image is shared, since neither side disposes it.
        if (_ownsImage)
        {
            clone._image = Sprite.CreateOwnedCopy(_texture, _image);
            clone._ownsImage = clone._image != null;
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
        AnimationLogic.ApplyKeyframeColor = ApplyKeyframeColor;
    }

    /// <summary>
    /// Whether the nine-slice is drawn reflected across its main diagonal (a transpose: the
    /// top-left and bottom-right corners stay put while the top-right and bottom-left swap).
    /// Applied after the inherited <see cref="RenderableShapeBase.FlipHorizontal"/> and
    /// <see cref="RenderableShapeBase.FlipVertical"/>, so a flipped
    /// nine-slice is flipped first and then transposed. The nine-slice keeps its own footprint, so
    /// a non-square one stretches the transposed image to fit. Set from the current animation
    /// frame.
    /// </summary>
    public bool FlipDiagonal { get; set; }

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

            // Flips mirror the drawn nine-slice (see DrawBound), so the rectangle stays canonical.
            SourceRectangle = new Rectangle(left, top, right - left, bottom - top);
        }
        else
        {
            SourceRectangle = null;
        }

        FlipHorizontal = frame.FlipHorizontal;
        FlipVertical = frame.FlipVertical;
        FlipDiagonal = frame.FlipDiagonal;

        // A chain with color keyframes gets its color from ApplyKeyframeColor instead, which also
        // ignores a skipped frame's own channels.
        if (!AnimationLogic.IsColorDrivenByKeyframes)
        {
            ApplyColor(frame.Alpha, frame.Red, frame.Green, frame.Blue, frame.ColorOperation);
        }
    }

    void ApplyKeyframeColor(Gum.Graphics.Animation.KeyframeColor color) =>
        ApplyColor(color.Alpha, color.Red, color.Green, color.Blue, color.Operation);

    void ApplyColor(int? alpha, int? red, int? green, int? blue, AnimationFrameColorOperation? colorOperation)
    {
        if (alpha.HasValue)
        {
            Alpha = alpha.Value;
        }

        if (colorOperation == AnimationFrameColorOperation.Multiply)
        {
            Red = red ?? 255;
            Green = green ?? 255;
            Blue = blue ?? 255;
            ColorOperation = ColorOperation.Modulate;
        }
        else if (colorOperation == AnimationFrameColorOperation.Add)
        {
            // Black (0) is Add's identity, so an unset channel contributes nothing - unlike
            // Multiply's 255 identity above.
            Red = red ?? 0;
            Green = green ?? 0;
            Blue = blue ?? 0;
            ColorOperation = ColorOperation.Add;
        }
        else if (ColorOperation == ColorOperation.Add)
        {
            ColorOperation = ColorOperation.Modulate;
        }
    }

    bool IAnimationChainRenderable.IsChainAnimationEnabled => AnimationLogic.Animate;

    object? IAnimationChainRenderable.AnimationChainList => AnimationLogic.AnimationChains;

    bool IAnimationChainRenderable.IsNamedChainMissing => AnimationLogic.IsNamedChainMissing;

    void IAnimationChainRenderable.ClearAnimationChains() => AnimationLogic.AnimationChains = null;

    void IAnimationChainRenderable.SetChainAnimationEnabledCallback(Action? callback) => AnimationLogic.AnimateEnabled = callback;

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
    /// since it usually comes from the shared content cache. A bitmap that was already disposed
    /// (content unload) is treated as no texture, since Skia aborts the process when it builds an
    /// image from one.
    /// <para>
    /// The pixels are copied into an <see cref="SKImage"/> on purpose; the nine-slice never draws the
    /// SKBitmap directly. Measured 2026-11-11 (256x256 bitmap drawn 200 times per frame on a 1280x720 surface,
    /// NotifyPixelsChanged once per frame, 60 frames after warmup): on a GPU surface (NVIDIA RTX 5060 Laptop)
    /// DrawBitmap took 37.8 ms per frame against 0.46 ms for DrawImage (about 80x slower); on a CPU raster
    /// surface 6.7 ms against 4.3 ms (about 1.5x). Pixels written to the bitmap after assignment therefore do
    /// not show until the Texture is assigned again, which is the explicit refresh path for edits.
    /// </para>
    /// </summary>
    public SKBitmap? Texture
    {
        get => _texture;
        set
        {
            SKImage? image = Sprite.CreateImage(value);
            _texture = image != null ? value : null;
            SetImage(image, ownsImage: true);
        }
    }
    private SKBitmap? _texture;

    /// <summary>
    /// Copies the pixels of <see cref="Texture"/> into the image again, so pixels written to the bitmap since it was
    /// assigned show. Does nothing when the bitmap is null or disposed, when an <see cref="Image"/> was assigned over
    /// it, or when <paramref name="onlyBitmap"/> is given and is not the texture.
    /// </summary>
    internal void RefreshTexture(SKBitmap? onlyBitmap = null)
    {
        if (!_ownsImage || !Sprite.IsUsable(_texture) || (onlyBitmap != null && !ReferenceEquals(onlyBitmap, _texture)))
        {
            return;
        }
        SetImage(Sprite.CreateImage(_texture), ownsImage: true);
    }

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

    // Image is what DrawBound draws, so its size wins, as on Sprite.
    public float? TextureWidth => _image?.Width ?? _texture?.Width;
    public float? TextureHeight => _image?.Height ?? _texture?.Height;

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

    /// <summary>Left border scale override; null falls back to <see cref="BorderScale"/>. 0 draws no left border.</summary>
    public float? CustomBorderScaleLeft { get; set; }

    /// <summary>Top border scale override; null falls back to <see cref="BorderScale"/>. 0 draws no top border.</summary>
    public float? CustomBorderScaleTop { get; set; }

    /// <summary>Right border scale override; null falls back to <see cref="BorderScale"/>. 0 draws no right border.</summary>
    public float? CustomBorderScaleRight { get; set; }

    /// <summary>Bottom border scale override; null falls back to <see cref="BorderScale"/>. 0 draws no bottom border.</summary>
    public float? CustomBorderScaleBottom { get; set; }

    private float EffectiveBorderScaleLeft => CustomBorderScaleLeft ?? BorderScale;
    private float EffectiveBorderScaleTop => CustomBorderScaleTop ?? BorderScale;
    private float EffectiveBorderScaleRight => CustomBorderScaleRight ?? BorderScale;
    private float EffectiveBorderScaleBottom => CustomBorderScaleBottom ?? BorderScale;

    protected override SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
    {
        // Modulate the image's texels by Color so the unified runtime's Color
        // property tints the nine-slice.
        SKPaint paint = base.GetPaint(boundingRect, absoluteRotation);
        ApplyTextureTint(paint, SKBlendMode.Modulate);
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

    /// <summary>Test hook: draws every section with its own DrawImage, as before the lattice fast path.</summary>
    internal bool DisableLatticeFastPath { get; set; }

    // Up to nine sections, more when tiling; each DrawImage records itself.
    private protected override bool RecordsOwnDrawCalls => true;

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        // RenderableShapeBase.Render has already saved the canvas, translated to
        // boundingRect.Left/Top, rotated by -absoluteRotation, and zeroed out the
        // boundingRect origin. Do NOT re-apply rotation here.

        if (Image == null)
        {
            return;
        }

        // Evaluated every render so an offset glide moves the nine-slice between frame changes.
        // The offset is in the nine-slice's own axes, which the canvas is already rotated into.
        // Translating the canvas, rather than moving boundingRect, keeps the cached paint valid.
        (float X, float Y) offset = AnimationLogic.GetRenderOffset(0);
        bool hasOffset = offset.X != 0 || offset.Y != 0;
        bool canFlipDiagonal = FlipDiagonal && boundingRect.Width > 0 && boundingRect.Height > 0;
        if (!hasOffset && !canFlipDiagonal && !FlipHorizontal && !FlipVertical)
        {
            DrawBoundAtOrigin(boundingRect, canvas, absoluteRotation);
            return;
        }

        using (new SKAutoCanvasRestore(canvas, true))
        {
            if (hasOffset)
            {
                canvas.Translate(offset.X, offset.Y);
            }

            // Applied outermost, so the mirror below happens first and the transpose second.
            if (canFlipDiagonal)
            {
                canvas.Concat(Sprite.GetDiagonalFlipMatrix(boundingRect));
            }

            if (FlipHorizontal || FlipVertical)
            {
                // Mirror about the center of the rectangle being drawn into, so the flipped
                // nine-slice stays inside its own bounds.
                canvas.Scale(
                    FlipHorizontal ? -1 : 1,
                    FlipVertical ? -1 : 1,
                    boundingRect.MidX,
                    boundingRect.MidY);
            }

            DrawBoundAtOrigin(boundingRect, canvas, absoluteRotation);
        }
    }

    private void DrawBoundAtOrigin(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
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
            srcRight = Image!.Width;
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

        // Per-side destination thickness (clamped so opposing bands never overlap) and the texels
        // each band samples, shared with every other runtime.
        NineSliceBorders borders = NineSliceBorders.Compute(
            fullOutsideW, fullOutsideH,
            EffectiveBorderScaleLeft, EffectiveBorderScaleTop, EffectiveBorderScaleRight, EffectiveBorderScaleBottom,
            boundingRect.Width, boundingRect.Height);
        float destLeftW = borders.DestLeft;
        float destTopH = borders.DestTop;
        float destRightW = borders.DestRight;
        float destBottomH = borders.DestBottom;
        int srcL = borders.SourceLeft;
        int srcT = borders.SourceTop;
        int srcR = borders.SourceRight;
        int srcB = borders.SourceBottom;

        // Sampling follows Renderer.TextureFilter, like Sprite. Under Linear, a pixel at a section's
        // edge samples half a texel past it, and SkiaSharp's DrawImage(src, dest) does not clamp to
        // src, so it would blend in the neighboring section's texels. Each section is then drawn
        // from its own image (see GetSectionImage), whose edges clamp.
        SKSamplingOptions sampling = global::RenderingLibrary.Graphics.Renderer.TextureSampling;
        SectionDraw draw = new(canvas, paint, sampling, UseSectionImages: sampling.Filter != SKFilterMode.Nearest);

        if (TryDrawLattice(
                canvas, paint, sampling, boundingRect,
                srcLeft, srcTop, usedWidth, usedHeight, fullOutsideW, fullOutsideH, borders))
        {
            return;
        }

        float destLeft = boundingRect.Left;
        float destTop = boundingRect.Top;
        float destRight = boundingRect.Right;
        float destBottom = boundingRect.Bottom;
        float destInsideW = Math.Max(0f, boundingRect.Width - destLeftW - destRightW);
        float destInsideH = Math.Max(0f, boundingRect.Height - destTopH - destBottomH);

        DrawSection(
            srcLeft, srcTop, srcL, srcT,
            destLeft, destTop, destLeftW, destTopH,
            draw);

        DrawSection(
            srcRight - srcR, srcTop, srcR, srcT,
            destRight - destRightW, destTop, destRightW, destTopH,
            draw);

        DrawSection(
            srcLeft, srcBottom - srcB, srcL, srcB,
            destLeft, destBottom - destBottomH, destLeftW, destBottomH,
            draw);

        DrawSection(
            srcRight - srcR, srcBottom - srcB, srcR, srcB,
            destRight - destRightW, destBottom - destBottomH, destRightW, destBottomH,
            draw);

        DrawMiddleSection(
            srcLeft + srcL, srcTop, insideTextureW, srcT,
            destLeft + destLeftW, destTop, destInsideW, destTopH,
            tileHorizontally: IsTilingMiddleSections, tileVertically: false,
            EffectiveBorderScaleTop, EffectiveBorderScaleTop,
            draw);

        DrawMiddleSection(
            srcLeft + srcL, srcBottom - srcB, insideTextureW, srcB,
            destLeft + destLeftW, destBottom - destBottomH, destInsideW, destBottomH,
            tileHorizontally: IsTilingMiddleSections, tileVertically: false,
            EffectiveBorderScaleBottom, EffectiveBorderScaleBottom,
            draw);

        DrawMiddleSection(
            srcLeft, srcTop + srcT, srcL, insideTextureH,
            destLeft, destTop + destTopH, destLeftW, destInsideH,
            tileHorizontally: false, tileVertically: IsTilingMiddleSections,
            EffectiveBorderScaleLeft, EffectiveBorderScaleLeft,
            draw);

        DrawMiddleSection(
            srcRight - srcR, srcTop + srcT, srcR, insideTextureH,
            destRight - destRightW, destTop + destTopH, destRightW, destInsideH,
            tileHorizontally: false, tileVertically: IsTilingMiddleSections,
            EffectiveBorderScaleRight, EffectiveBorderScaleRight,
            draw);

        DrawMiddleSection(
            srcLeft + srcL, srcTop + srcT, insideTextureW, insideTextureH,
            destLeft + destLeftW, destTop + destTopH, destInsideW, destInsideH,
            tileHorizontally: IsTilingMiddleSections, tileVertically: IsTilingMiddleSections,
            BorderScale, BorderScale,
            draw);
    }

    // Divisions handed to DrawImageLattice, kept so a frame allocates nothing.
    private readonly int[] _latticeXDivs = new int[2];
    private readonly int[] _latticeYDivs = new int[2];

    // Draws all nine sections with one DrawImageLattice call. Measured 2026-11-11 (200 NineSlices, GPU
    // surface, Nearest): about 38 us per NineSlice for nine DrawImage calls against about 4 us for the
    // lattice, with identical pixels. Only the cases below are identical, so everything else keeps the
    // per-section draws:
    //  - Nearest only. The lattice ignores the sampling option, so Linear blends in the neighboring
    //    section's texels, which GetSectionImage exists to prevent.
    //  - One border scale on all four sides, no tiling. A lattice cell is as large as its
    //    texels, so a scale s draws the lattice into a rectangle 1/s the size under a canvas scale of s.
    //  - Axis aligned, with all eight section edges on whole device pixels (anything else rounds differently), and not touched by the canvas clip.
    //  - Borders that fit: when NineSliceBorders clamps a band the sections are no longer the texels' size.
    private bool TryDrawLattice(
        SKCanvas canvas, SKPaint paint, SKSamplingOptions sampling, SKRect boundingRect,
        int srcLeft, int srcTop, int usedWidth, int usedHeight, int fullOutsideW, int fullOutsideH,
        NineSliceBorders borders)
    {
        if (DisableLatticeFastPath || sampling.Filter != SKFilterMode.Nearest || IsTilingMiddleSections)
        {
            return false;
        }

        float scale = EffectiveBorderScaleLeft;
        if (scale <= 0f
            || EffectiveBorderScaleTop != scale
            || EffectiveBorderScaleRight != scale
            || EffectiveBorderScaleBottom != scale
            || BorderScale != scale)
        {
            return false;
        }

        if (srcLeft < 0 || srcTop < 0 || srcLeft + usedWidth > Image!.Width || srcTop + usedHeight > Image.Height)
        {
            return false;
        }

        if (fullOutsideW <= 0 || fullOutsideH <= 0
            || usedWidth - (fullOutsideW * 2) <= 0 || usedHeight - (fullOutsideH * 2) <= 0)
        {
            return false;
        }

        // The bands must be exactly the texels' size times the scale (no clamping), with room left in the middle.
        if (borders.SourceLeft != fullOutsideW || borders.SourceRight != fullOutsideW
            || borders.SourceTop != fullOutsideH || borders.SourceBottom != fullOutsideH
            || borders.DestLeft != fullOutsideW * scale || borders.DestRight != fullOutsideW * scale
            || borders.DestTop != fullOutsideH * scale || borders.DestBottom != fullOutsideH * scale
            || boundingRect.Width <= borders.DestLeft + borders.DestRight
            || boundingRect.Height <= borders.DestTop + borders.DestBottom)
        {
            return false;
        }

        // The lattice and the per-section draws only agree when every edge lands exactly on a device pixel; at a
        // fractional edge the two round differently, on CPU and GPU surfaces. Rotated or skewed canvases are not
        // axis aligned, so their edges cannot be checked.
        SKMatrix matrix = canvas.TotalMatrix;
        if (matrix.SkewX != 0 || matrix.SkewY != 0
            || !IsOnDevicePixel(matrix.ScaleX * boundingRect.Left + matrix.TransX)
            || !IsOnDevicePixel(matrix.ScaleX * (boundingRect.Left + borders.DestLeft) + matrix.TransX)
            || !IsOnDevicePixel(matrix.ScaleX * (boundingRect.Right - borders.DestRight) + matrix.TransX)
            || !IsOnDevicePixel(matrix.ScaleX * boundingRect.Right + matrix.TransX)
            || !IsOnDevicePixel(matrix.ScaleY * boundingRect.Top + matrix.TransY)
            || !IsOnDevicePixel(matrix.ScaleY * (boundingRect.Top + borders.DestTop) + matrix.TransY)
            || !IsOnDevicePixel(matrix.ScaleY * (boundingRect.Bottom - borders.DestBottom) + matrix.TransY)
            || !IsOnDevicePixel(matrix.ScaleY * boundingRect.Bottom + matrix.TransY))
        {
            return false;
        }

        // A clip with a fractional edge crops the lattice's triangles differently from the sections' rectangles
        // on a GPU surface. The canvas only reports the clip rounded outward to whole pixels, so the fast path
        // is taken only when the nine-slice sits at least a pixel inside that box, where no clip can touch it.
        SKRectI clip = canvas.DeviceClipBounds;
        float deviceLeft = matrix.ScaleX * boundingRect.Left + matrix.TransX;
        float deviceRight = matrix.ScaleX * boundingRect.Right + matrix.TransX;
        float deviceTop = matrix.ScaleY * boundingRect.Top + matrix.TransY;
        float deviceBottom = matrix.ScaleY * boundingRect.Bottom + matrix.TransY;
        if (Math.Min(deviceLeft, deviceRight) < clip.Left + 1
            || Math.Max(deviceLeft, deviceRight) > clip.Right - 1
            || Math.Min(deviceTop, deviceBottom) < clip.Top + 1
            || Math.Max(deviceTop, deviceBottom) > clip.Bottom - 1)
        {
            return false;
        }

        _latticeXDivs[0] = srcLeft + fullOutsideW;
        _latticeXDivs[1] = srcLeft + usedWidth - fullOutsideW;
        _latticeYDivs[0] = srcTop + fullOutsideH;
        _latticeYDivs[1] = srcTop + usedHeight - fullOutsideH;
        SKLattice lattice = new() { XDivs = _latticeXDivs, YDivs = _latticeYDivs };
        if (srcLeft != 0 || srcTop != 0 || usedWidth != Image.Width || usedHeight != Image.Height)
        {
            lattice.Bounds = SKRectI.Create(srcLeft, srcTop, usedWidth, usedHeight);
        }

        canvas.Save();
        canvas.Translate(boundingRect.Left, boundingRect.Top);
        canvas.Scale(scale);
        canvas.DrawImageLattice(Image, lattice, new SKRect(0, 0, boundingRect.Width / scale, boundingRect.Height / scale), paint);
        canvas.Restore();
        DrawStatistics?.RecordDrawCall();
        return true;
    }

    private static bool IsOnDevicePixel(float deviceCoordinate)
    {
        return Math.Abs(deviceCoordinate - MathF.Round(deviceCoordinate)) < 0.001f;
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
            tileHorizontally: false, tileVertically: false, tileScaleX: 1f, tileScaleY: 1f, draw);
    }

    private void DrawMiddleSection(
        int srcX, int srcY, int srcW, int srcH,
        float destX, float destY, float destW, float destH,
        bool tileHorizontally, bool tileVertically,
        float tileScaleX, float tileScaleY,
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
            DrawStatistics?.RecordDrawCall();
            return;
        }

        float tileDestW = tileHorizontally ? srcW * tileScaleX : destW;
        float tileDestH = tileVertically ? srcH * tileScaleY : destH;
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
                DrawStatistics?.RecordDrawCall();

                currentX += thisTileDestW;
            }
            currentY += thisTileDestH;
        }
    }
}
