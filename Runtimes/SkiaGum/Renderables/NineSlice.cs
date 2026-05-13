using SkiaSharp;
using System;
using System.Drawing;

namespace SkiaGum.Renderables;

public class NineSlice : RenderableShapeBase, ICloneable
{
    public SKBitmap? Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            Image = value != null ? SKImage.FromBitmap(value) : null;
        }
    }
    private SKBitmap? _texture;

    public SKImage? Image { get; set; }

    public Rectangle? SourceRectangle { get; set; }

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

    public object Clone() => this.MemberwiseClone();

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        if (Image == null)
        {
            return;
        }

        SKPaint paint = base.GetCachedPaint(boundingRect, absoluteRotation);

        bool applyRotation = absoluteRotation != 0;
        if (applyRotation)
        {
            float oldX = boundingRect.Left;
            float oldY = boundingRect.Top;

            canvas.Save();

            boundingRect.Left = 0;
            boundingRect.Right -= oldX;
            boundingRect.Top = 0;
            boundingRect.Bottom -= oldY;

            canvas.Translate(oldX, oldY);
            canvas.RotateDegrees(-absoluteRotation);
        }

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
            if (applyRotation)
            {
                canvas.Restore();
            }
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
            canvas, paint);

        DrawSection(
            srcRight - srcCornerW, srcTop, srcCornerW, srcCornerH,
            destRight - destCornerW, destTop, destCornerW, destCornerH,
            canvas, paint);

        DrawSection(
            srcLeft, srcBottom - srcCornerH, srcCornerW, srcCornerH,
            destLeft, destBottom - destCornerH, destCornerW, destCornerH,
            canvas, paint);

        DrawSection(
            srcRight - srcCornerW, srcBottom - srcCornerH, srcCornerW, srcCornerH,
            destRight - destCornerW, destBottom - destCornerH, destCornerW, destCornerH,
            canvas, paint);

        DrawMiddleSection(
            srcLeft + srcCornerW, srcTop, insideTextureW, srcCornerH,
            destLeft + destCornerW, destTop, destInsideW, destCornerH,
            tileHorizontally: IsTilingMiddleSections, tileVertically: false,
            canvas, paint);

        DrawMiddleSection(
            srcLeft + srcCornerW, srcBottom - srcCornerH, insideTextureW, srcCornerH,
            destLeft + destCornerW, destBottom - destCornerH, destInsideW, destCornerH,
            tileHorizontally: IsTilingMiddleSections, tileVertically: false,
            canvas, paint);

        DrawMiddleSection(
            srcLeft, srcTop + srcCornerH, srcCornerW, insideTextureH,
            destLeft, destTop + destCornerH, destCornerW, destInsideH,
            tileHorizontally: false, tileVertically: IsTilingMiddleSections,
            canvas, paint);

        DrawMiddleSection(
            srcRight - srcCornerW, srcTop + srcCornerH, srcCornerW, insideTextureH,
            destRight - destCornerW, destTop + destCornerH, destCornerW, destInsideH,
            tileHorizontally: false, tileVertically: IsTilingMiddleSections,
            canvas, paint);

        DrawMiddleSection(
            srcLeft + srcCornerW, srcTop + srcCornerH, insideTextureW, insideTextureH,
            destLeft + destCornerW, destTop + destCornerH, destInsideW, destInsideH,
            tileHorizontally: IsTilingMiddleSections, tileVertically: IsTilingMiddleSections,
            canvas, paint);

        if (applyRotation)
        {
            canvas.Restore();
        }
    }

    private void DrawSection(
        int srcX, int srcY, int srcW, int srcH,
        float destX, float destY, float destW, float destH,
        SKCanvas canvas, SKPaint paint)
    {
        if (destW <= 0 || destH <= 0 || srcW <= 0 || srcH <= 0)
        {
            return;
        }
        SKRect src = new SKRect(srcX, srcY, srcX + srcW, srcY + srcH);
        SKRect dest = new SKRect(destX, destY, destX + destW, destY + destH);
        canvas.DrawImage(Image, src, dest, paint);
    }

    private void DrawMiddleSection(
        int srcX, int srcY, int srcW, int srcH,
        float destX, float destY, float destW, float destH,
        bool tileHorizontally, bool tileVertically,
        SKCanvas canvas, SKPaint paint)
    {
        if (destW <= 0 || destH <= 0 || srcW <= 0 || srcH <= 0)
        {
            return;
        }

        if (!tileHorizontally && !tileVertically)
        {
            DrawSection(srcX, srcY, srcW, srcH, destX, destY, destW, destH, canvas, paint);
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
                canvas.DrawImage(Image, src, dest, paint);

                currentX += thisTileDestW;
            }
            currentY += thisTileDestH;
        }
    }
}
