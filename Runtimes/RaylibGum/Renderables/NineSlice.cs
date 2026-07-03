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
public class NineSlice : RenderableBase, IAnimatable, ITextureCoordinate, ICloneable
{
    /// <summary>
    /// Shared AnimationChain playback state. The constructor wires
    /// <see cref="AnimationChainLogic.ApplyFrame"/> to copy the active frame's
    /// texture and (UV-derived) source rectangle onto this NineSlice.
    /// </summary>
    public AnimationChainLogic AnimationLogic { get; } = new AnimationChainLogic();

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
    }

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

        // Raylib NineSlice does not currently honour FlipHorizontal/Vertical
        // (no FlipHorizontal field on this renderable, and DrawTextureNPatch
        // does not support negative src rects). If/when flip support is added,
        // copy frame.FlipHorizontal / frame.FlipVertical here.
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

    public NineSlice Clone()
    {
        var newInstance = (NineSlice)this.MemberwiseClone();
        ((IRenderableIpso)newInstance).SetParentDirect(null);
        newInstance._children = new();
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

    public override void Render(ISystemManagers managers)
    {
        if (!Visible || Texture == null) return;

        var nonNullText = Texture.Value;

        var absoluteRotation = this.GetAbsoluteRotation();

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.BeginBlendMode(Blend.Value.ToRaylibBlendMode());
        }

        // The default (non-tiling, BorderScale 1, no custom frame width) case is left on
        // raylib's built-in nine-patch draw — a single call that stretches the middle
        // bands. Tiling, BorderScale, and CustomFrameTextureCoordinateWidth all require
        // drawing the nine sections individually, which raylib's NPatch cannot express.
        if (!IsTilingMiddleSections && BorderScale == 1f && CustomFrameTextureCoordinateWidth == null)
        {
            RenderNinePatch(nonNullText, absoluteRotation);
        }
        else
        {
            RenderSections(nonNullText, absoluteRotation);
        }

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.EndBlendMode();
        }
    }

    private void RenderNinePatch(Texture2D texture, float absoluteRotation)
    {
        int x = (int)this.GetAbsoluteLeft();
        int y = (int)this.GetAbsoluteTop();

        var destinationRectangle = new Rectangle(
            x, y, this.Width, this.Height);

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

    private void RenderSections(Texture2D texture, float absoluteRotation)
    {
        float x = this.GetAbsoluteLeft();
        float y = this.GetAbsoluteTop();

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
            BorderScale, CustomFrameTextureCoordinateWidth, IsTilingMiddleSections);

        // raylib's DrawTexturePro rotates each destination rect around its own top-left
        // (origin = Vector2.Zero). To keep the sections contiguous and rotate the whole
        // NineSlice as one piece, each section's local top-left is rotated around the
        // pivot (x, y) using the same matrix raylib applies internally.
        float rotationRadians = -absoluteRotation * (float)(Math.PI / 180.0);
        float cos = (float)Math.Cos(rotationRadians);
        float sin = (float)Math.Sin(rotationRadians);

        foreach (var section in sections)
        {
            var localDest = section.Destination;
            float worldX = x + localDest.X * cos - localDest.Y * sin;
            float worldY = y + localDest.X * sin + localDest.Y * cos;

            var worldDest = new Rectangle(worldX, worldY, localDest.Width, localDest.Height);

            DrawTexturePro(
                texture,
                section.Source,
                worldDest,
                Vector2.Zero,
                -absoluteRotation,
                Color);
        }
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

        // Destination corner thickness, scaled by BorderScale and clamped so two opposing
        // corners never overlap the available width/height.
        float destCornerW = Math.Min(fullOutsideW * borderScale, destWidth / 2f);
        float destCornerH = Math.Min(fullOutsideH * borderScale, destHeight / 2f);

        // Texture-pixel corner thickness must shrink in lockstep when destCorner has been
        // clamped, otherwise the corner draw would sample more pixels than fit on screen.
        int srcCornerW = borderScale > 0 ? (int)Math.Round(destCornerW / borderScale) : fullOutsideW;
        int srcCornerH = borderScale > 0 ? (int)Math.Round(destCornerH / borderScale) : fullOutsideH;
        if (srcCornerW > fullOutsideW)
        {
            srcCornerW = fullOutsideW;
        }
        if (srcCornerH > fullOutsideH)
        {
            srcCornerH = fullOutsideH;
        }

        float destInsideW = destWidth - destCornerW * 2;
        float destInsideH = destHeight - destCornerH * 2;
        if (destInsideW < 0)
        {
            destInsideW = 0;
        }
        if (destInsideH < 0)
        {
            destInsideH = 0;
        }

        // Corners.
        AddSection(sections,
            srcLeft, srcTop, srcCornerW, srcCornerH,
            0, 0, destCornerW, destCornerH);
        AddSection(sections,
            srcRight - srcCornerW, srcTop, srcCornerW, srcCornerH,
            destWidth - destCornerW, 0, destCornerW, destCornerH);
        AddSection(sections,
            srcLeft, srcBottom - srcCornerH, srcCornerW, srcCornerH,
            0, destHeight - destCornerH, destCornerW, destCornerH);
        AddSection(sections,
            srcRight - srcCornerW, srcBottom - srcCornerH, srcCornerW, srcCornerH,
            destWidth - destCornerW, destHeight - destCornerH, destCornerW, destCornerH);

        // Top / bottom edges.
        AddMiddleSections(sections,
            srcLeft + srcCornerW, srcTop, insideTextureW, srcCornerH,
            destCornerW, 0, destInsideW, destCornerH,
            tileHorizontally: isTilingMiddleSections, tileVertically: false, borderScale);
        AddMiddleSections(sections,
            srcLeft + srcCornerW, srcBottom - srcCornerH, insideTextureW, srcCornerH,
            destCornerW, destHeight - destCornerH, destInsideW, destCornerH,
            tileHorizontally: isTilingMiddleSections, tileVertically: false, borderScale);

        // Left / right edges.
        AddMiddleSections(sections,
            srcLeft, srcTop + srcCornerH, srcCornerW, insideTextureH,
            0, destCornerH, destCornerW, destInsideH,
            tileHorizontally: false, tileVertically: isTilingMiddleSections, borderScale);
        AddMiddleSections(sections,
            srcRight - srcCornerW, srcTop + srcCornerH, srcCornerW, insideTextureH,
            destWidth - destCornerW, destCornerH, destCornerW, destInsideH,
            tileHorizontally: false, tileVertically: isTilingMiddleSections, borderScale);

        // Center.
        AddMiddleSections(sections,
            srcLeft + srcCornerW, srcTop + srcCornerH, insideTextureW, insideTextureH,
            destCornerW, destCornerH, destInsideW, destInsideH,
            tileHorizontally: isTilingMiddleSections, tileVertically: isTilingMiddleSections, borderScale);

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
        bool tileHorizontally, bool tileVertically, float borderScale)
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

        float tileDestW = tileHorizontally ? srcW * borderScale : destW;
        float tileDestH = tileVertically ? srcH * borderScale : destH;
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
