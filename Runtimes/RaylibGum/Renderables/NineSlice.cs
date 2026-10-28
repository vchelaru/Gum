using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using ToolsUtilitiesStandard.Helpers;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;
public class NineSlice : RenderableBase, IAnimatable, IAnimationChainRenderable, ITextureCoordinate, ICloneable
{
    /// <summary>
    /// Shared AnimationChain playback state. The constructor wires
    /// <see cref="AnimationChainLogic.ApplyFrame"/> to copy the active frame's
    /// texture and (UV-derived) source rectangle onto this NineSlice.
    /// </summary>
    public AnimationChainLogic AnimationLogic { get; private set; } = new AnimationChainLogic();

    // Convenience pass-throughs to AnimationLogic, mirroring the MonoGame NineSlice renderable
    // (RenderingLibrary/Graphics/NineSlice.cs) so shared code such as
    // CustomSetPropertyOnRenderable.AssignSourceFileOnNineSlice compiles identically on both.
    public AnimationChainList? AnimationChains
    {
        get => AnimationLogic.AnimationChains;
        set => AnimationLogic.AnimationChains = value;
    }

    public bool UpdateToCurrentAnimationFrame() => AnimationLogic.UpdateToCurrentAnimationFrame();

    public void RefreshCurrentChainToDesiredName() => AnimationLogic.RefreshCurrentChainToDesiredName();

    public NineSlice()
    {
        AnimationLogic.ApplyFrame = ApplyAnimationFrame;
        AnimationLogic.ApplyKeyframeColor = ApplyKeyframeColor;
    }

    /// <summary>
    /// Whether the nine-slice is drawn mirrored top to bottom inside its own bounds. Set from the
    /// current animation frame.
    /// </summary>
    public bool FlipVertical { get; set; }

    /// <summary>
    /// Whether the nine-slice is drawn reflected across its main diagonal (a transpose: the
    /// top-left and bottom-right corners stay put while the top-right and bottom-left swap).
    /// Applied after <c>FlipHorizontal</c> and <see cref="FlipVertical"/>, so a flipped nine-slice
    /// is flipped first and then transposed. The nine-slice keeps its own footprint, so
    /// a non-square one stretches the transposed image to fit. Set from the current animation
    /// frame.
    /// </summary>
    public bool FlipDiagonal { get; set; }

    void ApplyAnimationFrame(Gum.Graphics.Animation.AnimationFrame frame)
    {
        Texture = frame.Texture;

        if (frame.Texture.HasValue)
        {
            Texture2D tex = frame.Texture.Value;
            int left = MathFunctions.RoundToInt(frame.LeftCoordinate * tex.Width);
            int width = MathFunctions.RoundToInt(frame.RightCoordinate * tex.Width) - left;
            int top = MathFunctions.RoundToInt(frame.TopCoordinate * tex.Height);
            int height = MathFunctions.RoundToInt(frame.BottomCoordinate * tex.Height) - top;
            SourceRectangle = new Raylib_cs.Rectangle(left, top, width, height);
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

    public NineSlice Clone()
    {
        var newInstance = (NineSlice)this.MemberwiseClone();
        ((IRenderableIpso)newInstance).SetParentDirect(null);
        newInstance._children = new();
        newInstance.AnimationLogic = AnimationLogic.Clone(newInstance.ApplyAnimationFrame, newInstance.ApplyKeyframeColor);
        return newInstance;
    }

    object ICloneable.Clone() => Clone();

    public Texture2D? Texture { get; set; }

    public Raylib_cs.Rectangle? SourceRectangle
    {
        get;
        set;
    }

    public float? TextureWidth => Texture?.Width;

    public float? TextureHeight => Texture?.Height;

    System.Drawing.Rectangle? ITextureCoordinate.SourceRectangle
    {
        get
        {
            if (SourceRectangle == null)
            {
                return null;
            }
            else
            {
                var rRect = SourceRectangle.Value;
                return new System.Drawing.Rectangle(
                    (int)rRect.X,
                    (int)rRect.Y,
                    (int)rRect.Width,
                    (int)rRect.Height
                    );
            }
        }
        set
        {
            if (value == null)
            {
                SourceRectangle = null;
            }
            else
            {
                SourceRectangle = new Rectangle(
                    value.Value.X,
                    value.Value.Y,
                    value.Value.Width,
                    value.Value.Height);
            }
        }
    }

    // This exists to satisfy the same syntax as MonoGame
    internal void SetSingleTexture(Texture2D? texture) => Texture = texture;

    bool ITextureCoordinate.Wrap 
    { 
        get => false;
        set {} 
    }

    public int Alpha
    {
        get
        {
            return Color.A;
        }
        set
        {
            if (value != Color.A)
            {
                Color = new Color(Color.R, Color.G, Color.B, (byte)value);
            }
        }
    }

    public int Red
    {
        get
        {
            return Color.R;
        }
        set
        {
            if (value != Color.R)
            {
                Color = new Color((byte)value, Color.G, Color.B, Color.A);
            }
        }
    }

    public int Green
    {
        get
        {
            return Color.G;
        }
        set
        {
            if (value != Color.G)
            {
                Color = new Color(Color.R, (byte)value, Color.B, Color.A);
            }
        }
    }

    public int Blue
    {
        get
        {
            return Color.B;
        }
        set
        {
            if (value != Color.B)
            {
                Color = new Color(Color.R, Color.G, (byte)value, Color.A);
            }
        }
    }

    public Color Color
    {
        get; set;
    } = Color.White;

    /// <summary>
    /// How <see cref="Color"/> combines with the texture. Add draws the nine-slice untinted and
    /// then adds Color in a second pass.
    /// </summary>
    public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;

    public global::Gum.RenderingLibrary.Blend? Blend { get; set; }

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

    public override void Render(ISystemManagers managers)
    {
        if (!Visible || Texture == null) return;

        var nonNullText = Texture.Value;

        var absoluteRotation = this.GetAbsoluteRotation();

        // Evaluated every render so an offset glide moves the nine-slice between frame changes.
        (float X, float Y) animationOffset = AnimationLogic.GetRenderOffset(absoluteRotation);

        // The default (non-tiling, BorderScale 1, no custom frame width) case is left on
        // raylib's built-in nine-patch draw — a single call that stretches the middle
        // bands. Tiling, BorderScale, CustomFrameTextureCoordinateWidth and flips all require
        // drawing the nine sections individually, which raylib's NPatch cannot express.
        bool isFlipped = FlipHorizontal || FlipVertical || FlipDiagonal;
        void DrawOnce()
        {
            if (!IsTilingMiddleSections && BorderScale == 1f && CustomFrameTextureCoordinateWidth == null && !isFlipped
                && CustomBorderScaleLeft == null && CustomBorderScaleTop == null
                && CustomBorderScaleRight == null && CustomBorderScaleBottom == null)
            {
                RenderNinePatch(nonNullText, absoluteRotation, animationOffset);
            }
            else
            {
                RenderSections(nonNullText, absoluteRotation, animationOffset);
            }
        }

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.BeginBlendMode(Blend.Value);
        }

        DrawOnce();

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.EndBlendMode();
        }

        if (ColorOperation == ColorOperation.Add)
        {
            var counter = global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter;
            counter.BeginShaderMode(global::RenderingLibrary.Graphics.Renderer.Self.AdditiveColorOverlayShader.Shader);
            counter.BeginBlendModeAddColorPreserveDestinationAlpha();

            DrawOnce();

            counter.EndBlendMode();
            counter.EndShaderMode();
        }
    }

    private void RenderNinePatch(Texture2D texture, float absoluteRotation, (float X, float Y) animationOffset)
    {
        float x = this.GetAbsoluteLeft();
        float y = this.GetAbsoluteTop();

        var destinationRectangle = new Rectangle(
            x + animationOffset.X, y + animationOffset.Y, this.Width, this.Height);

        int leftSize, rightSize, topSize, bottomSize;

        if (SourceRectangle != null)
        {
            var rect = SourceRectangle.Value;
            leftSize = (int)(rect.Width / 3);
            rightSize = (int)(rect.Width / 3);
            topSize = (int)(rect.Height / 3);
            bottomSize = (int)(rect.Height / 3);
        }
        else
        {
            leftSize = (int)(texture.Width / 3);
            rightSize = (int)(texture.Width / 3);
            topSize = (int)(texture.Height / 3);
            bottomSize = (int)(texture.Height / 3);
        }

        var nPatchInfo = new NPatchInfo
        {
            Source = SourceRectangle ?? new Raylib_cs.Rectangle(0, 0, texture.Width, texture.Height),
            Left = leftSize,
            Top = topSize,
            Right = rightSize,
            Bottom = bottomSize,
            Layout = NPatchLayout.NinePatch
        };

        DrawTextureNPatch(
            texture,
            nPatchInfo,
            destinationRectangle,
            Vector2.Zero,
            -absoluteRotation,
            Color);
    }

    private void RenderSections(Texture2D texture, float absoluteRotation, (float X, float Y) animationOffset)
    {
        float x = this.GetAbsoluteLeft() + animationOffset.X;
        float y = this.GetAbsoluteTop() + animationOffset.Y;

        int srcLeft, srcTop, srcRight, srcBottom;
        if (SourceRectangle != null)
        {
            var rect = SourceRectangle.Value;
            srcLeft = (int)rect.X;
            srcTop = (int)rect.Y;
            srcRight = (int)(rect.X + rect.Width);
            srcBottom = (int)(rect.Y + rect.Height);
        }
        else
        {
            srcLeft = 0;
            srcTop = 0;
            srcRight = texture.Width;
            srcBottom = texture.Height;
        }

        var sections = ComputeDrawSections(
            srcLeft, srcTop, srcRight, srcBottom,
            this.Width, this.Height,
            BorderScale, EffectiveBorderScaleLeft, EffectiveBorderScaleTop, EffectiveBorderScaleRight, EffectiveBorderScaleBottom,
            CustomFrameTextureCoordinateWidth, IsTilingMiddleSections);

        // raylib's DrawTexturePro rotates each destination rect around its own top-left
        // (origin = Vector2.Zero). To keep the sections contiguous and rotate the whole
        // NineSlice as one piece, each section's local top-left is rotated around the
        // pivot (x, y) using the same matrix raylib applies internally.
        float rotationRadians = -absoluteRotation * (float)(Math.PI / 180.0);
        float cos = (float)Math.Cos(rotationRadians);
        float sin = (float)Math.Sin(rotationRadians);

        foreach (var section in sections)
        {
            var (localDest, source) = GetFlippedSection(
                section.Destination, section.Source, this.Width, this.Height,
                FlipHorizontal, FlipVertical, FlipDiagonal);

            float worldX = x + localDest.X * cos - localDest.Y * sin;
            float worldY = y + localDest.X * sin + localDest.Y * cos;

            var worldDest = new Rectangle(worldX, worldY, localDest.Width, localDest.Height);
            float drawRotation = -absoluteRotation;
            if (FlipDiagonal)
            {
                (worldDest, drawRotation, source) = Sprite.GetDiagonalFlipDraw(worldDest, drawRotation, source);
            }

            DrawTexturePro(
                texture,
                source,
                worldDest,
                Vector2.Zero,
                drawRotation,
                Color);
        }
    }

    // Moves one section to where it lands once the whole nine-slice is flipped. Horizontal and
    // vertical flips mirror the destination within the nine-slice's footprint and mark the source
    // with a negative dimension, which raylib reads as a flip. A diagonal flip then transposes the
    // destination; the caller turns the source to match (Sprite.GetDiagonalFlipDraw).
    internal static (Rectangle Destination, Rectangle Source) GetFlippedSection(
        Rectangle destination, Rectangle source, float width, float height,
        bool flipHorizontal, bool flipVertical, bool flipDiagonal)
    {
        if (flipHorizontal)
        {
            destination.X = width - destination.X - destination.Width;
            source.Width = -source.Width;
        }

        if (flipVertical)
        {
            destination.Y = height - destination.Y - destination.Height;
            source.Height = -source.Height;
        }

        if (flipDiagonal && width > 0 && height > 0)
        {
            float widthOverHeight = width / height;
            float heightOverWidth = height / width;
            destination = new Rectangle(
                destination.Y * widthOverHeight,
                destination.X * heightOverWidth,
                destination.Height * widthOverHeight,
                destination.Width * heightOverWidth);
        }

        return (destination, source);
    }

    /// <summary>
    /// Computes the nine-slice draw sections (source + destination rectangles) for the
    /// given source region and destination size. Destination rectangles are in the
    /// NineSlice's local, unrotated space with the origin at the top-left; the caller
    /// applies rotation. When <paramref name="isTilingMiddleSections"/> is true the
    /// edge/center bands are emitted as repeated tiles rather than a single stretched quad.
    /// </summary>
    internal static List<NineSliceDrawSection> ComputeDrawSections(
        int srcLeft, int srcTop, int srcRight, int srcBottom,
        float destWidth, float destHeight,
        float borderScale, float? customFrameTextureCoordinateWidth,
        bool isTilingMiddleSections)
    {
        return ComputeDrawSections(
            srcLeft, srcTop, srcRight, srcBottom, destWidth, destHeight,
            borderScale, borderScale, borderScale, borderScale, borderScale,
            customFrameTextureCoordinateWidth, isTilingMiddleSections);
    }

    /// <summary>
    /// Same as the uniform overload, with each side's effective border scale (the
    /// <c>CustomBorderScale*</c> override, or <paramref name="borderScale"/> when unset).
    /// <paramref name="borderScale"/> itself sizes the center's tiles.
    /// </summary>
    internal static List<NineSliceDrawSection> ComputeDrawSections(
        int srcLeft, int srcTop, int srcRight, int srcBottom,
        float destWidth, float destHeight,
        float borderScale,
        float scaleLeft, float scaleTop, float scaleRight, float scaleBottom,
        float? customFrameTextureCoordinateWidth,
        bool isTilingMiddleSections)
    {
        var sections = new List<NineSliceDrawSection>();

        int usedWidth = srcRight - srcLeft;
        int usedHeight = srcBottom - srcTop;
        if (usedWidth <= 0 || usedHeight <= 0)
        {
            return sections;
        }

        int fullOutsideW;
        int fullOutsideH;
        if (customFrameTextureCoordinateWidth.HasValue)
        {
            fullOutsideW = (int)Math.Round(customFrameTextureCoordinateWidth.Value);
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
            fullOutsideW, fullOutsideH, scaleLeft, scaleTop, scaleRight, scaleBottom, destWidth, destHeight);

        float destLeft = borders.DestLeft;
        float destTop = borders.DestTop;
        float destRight = borders.DestRight;
        float destBottom = borders.DestBottom;
        int srcL = borders.SourceLeft;
        int srcT = borders.SourceTop;
        int srcR = borders.SourceRight;
        int srcB = borders.SourceBottom;

        float destInsideW = Math.Max(0f, destWidth - destLeft - destRight);
        float destInsideH = Math.Max(0f, destHeight - destTop - destBottom);

        // Corners.
        AddSection(sections,
            srcLeft, srcTop, srcL, srcT,
            0, 0, destLeft, destTop);
        AddSection(sections,
            srcRight - srcR, srcTop, srcR, srcT,
            destWidth - destRight, 0, destRight, destTop);
        AddSection(sections,
            srcLeft, srcBottom - srcB, srcL, srcB,
            0, destHeight - destBottom, destLeft, destBottom);
        AddSection(sections,
            srcRight - srcR, srcBottom - srcB, srcR, srcB,
            destWidth - destRight, destHeight - destBottom, destRight, destBottom);

        // Top / bottom edges tile at their own side's scale.
        AddMiddleSections(sections,
            srcLeft + srcL, srcTop, insideTextureW, srcT,
            destLeft, 0, destInsideW, destTop,
            tileHorizontally: isTilingMiddleSections, tileVertically: false, scaleTop, scaleTop);
        AddMiddleSections(sections,
            srcLeft + srcL, srcBottom - srcB, insideTextureW, srcB,
            destLeft, destHeight - destBottom, destInsideW, destBottom,
            tileHorizontally: isTilingMiddleSections, tileVertically: false, scaleBottom, scaleBottom);

        // Left / right edges.
        AddMiddleSections(sections,
            srcLeft, srcTop + srcT, srcL, insideTextureH,
            0, destTop, destLeft, destInsideH,
            tileHorizontally: false, tileVertically: isTilingMiddleSections, scaleLeft, scaleLeft);
        AddMiddleSections(sections,
            srcRight - srcR, srcTop + srcT, srcR, insideTextureH,
            destWidth - destRight, destTop, destRight, destInsideH,
            tileHorizontally: false, tileVertically: isTilingMiddleSections, scaleRight, scaleRight);

        // Center.
        AddMiddleSections(sections,
            srcLeft + srcL, srcTop + srcT, insideTextureW, insideTextureH,
            destLeft, destTop, destInsideW, destInsideH,
            tileHorizontally: isTilingMiddleSections, tileVertically: isTilingMiddleSections, borderScale, borderScale);

        return sections;
    }

    private static void AddSection(List<NineSliceDrawSection> sections,
        int srcX, int srcY, int srcW, int srcH,
        float destX, float destY, float destW, float destH)
    {
        if (destW <= 0 || destH <= 0 || srcW <= 0 || srcH <= 0)
        {
            return;
        }
        sections.Add(new NineSliceDrawSection(
            new Raylib_cs.Rectangle(srcX, srcY, srcW, srcH),
            new Raylib_cs.Rectangle(destX, destY, destW, destH)));
    }

    private static void AddMiddleSections(List<NineSliceDrawSection> sections,
        int srcX, int srcY, int srcW, int srcH,
        float destX, float destY, float destW, float destH,
        bool tileHorizontally, bool tileVertically, float tileScaleX, float tileScaleY)
    {
        if (destW <= 0 || destH <= 0 || srcW <= 0 || srcH <= 0)
        {
            return;
        }

        if (!tileHorizontally && !tileVertically)
        {
            AddSection(sections, srcX, srcY, srcW, srcH, destX, destY, destW, destH);
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

                AddSection(sections, srcX, srcY, thisSrcW, thisSrcH,
                    destX + currentX, destY + currentY, thisTileDestW, thisTileDestH);

                currentX += thisTileDestW;
            }
            currentY += thisTileDestH;
        }
    }
}

/// <summary>
/// One source-to-destination quad of a nine-slice draw. <see cref="Destination"/> is in
/// the NineSlice's local, unrotated space (origin at the top-left); rotation is applied by
/// the renderer. Produced by <see cref="NineSlice.ComputeDrawSections"/>.
/// </summary>
internal readonly struct NineSliceDrawSection
{
    public NineSliceDrawSection(Raylib_cs.Rectangle source, Raylib_cs.Rectangle destination)
    {
        Source = source;
        Destination = destination;
    }

    /// <summary>Source rectangle in texture pixels.</summary>
    public Raylib_cs.Rectangle Source { get; }

    /// <summary>Destination rectangle in local (unrotated) space, origin at the top-left.</summary>
    public Raylib_cs.Rectangle Destination { get; }
}
