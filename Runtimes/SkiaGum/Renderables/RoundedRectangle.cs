using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SkiaGum.Renderables;

public class RoundedRectangle : RenderableShapeBase, IClipPath, ICloneable
{
    public float CornerRadius { get; set; }

    public float? CustomRadiusTopLeft { get; set; } = null;
    public float? CustomRadiusTopRight { get; set; } = null;
    public float? CustomRadiusBottomRight { get; set; } = null;
    public float? CustomRadiusBottomLeft { get; set; } = null;

    public RoundedRectangle()
    {
        CornerRadius = 5;
        Color = SKColors.White;
    }

    public SKPath GetClipPath()
    {
        SKPath path = new SKPath();

        var absoluteX = this.GetAbsoluteX();
        var absoluteY = this.GetAbsoluteY();
        var boundingRect = new SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

        path.AddRoundRect(boundingRect, CornerRadius, CornerRadius);

        return path;
    }

    object ICloneable.Clone() => Clone();

    public RoundedRectangle Clone()
    {
        var newInstance = (RoundedRectangle)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.mChildren = new ();
        newInstance.ClearCachedPaint();

        return newInstance;
    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);

        if (CustomRadiusBottomLeft == null && CustomRadiusBottomRight == null && CustomRadiusTopLeft == null && CustomRadiusTopRight == null)
        {
            canvas.DrawRoundRect(boundingRect, CornerRadius, CornerRadius, paint);
        }
        else
        {
            using SKPath path = BuildCustomCornerPath(boundingRect);
            canvas.DrawPath(path, paint);
        }
    }

    /// <summary>
    /// Builds the per-corner-radius outline path used when at least one
    /// <c>CustomRadius*</c> override is set. Extracted from <see cref="DrawBound"/> so the
    /// geometry is testable without an <see cref="SKCanvas"/> (path bounds can be asserted
    /// directly via <see cref="SKPath.Bounds"/>).
    /// </summary>
    /// <remarks>
    /// Issue #4030 follow-up — unlike <see cref="SKCanvas.DrawRoundRect(SKRect, float, float, SKPaint)"/>
    /// (which clamps its radii to fit the rect internally), the manual <see cref="SKPath.ArcTo"/>
    /// construction here does not. A per-corner radius larger than half the bounding rect's width
    /// or height produces an arc whose circle is bigger than the corner it's cut into, so the path
    /// bulges past the rect's edge -- most visible on the stroke slot, whose bounding rect is
    /// shrunk by half the stroke width (<see cref="RenderableShapeBase.IsOffsetAppliedForStroke"/>)
    /// while the corner radius pushed onto it stays the fill's un-shrunk value. Each corner radius
    /// is clamped to half the smaller of the rect's width/height so no arc can ever extend past the
    /// rect on any side.
    /// </remarks>
    internal SKPath BuildCustomCornerPath(SKRect boundingRect)
    {
        float maxRadius = System.Math.Max(0f, System.Math.Min(boundingRect.Width, boundingRect.Height) / 2f);

        float topLeft = System.Math.Min(CustomRadiusTopLeft ?? CornerRadius, maxRadius);
        float topRight = System.Math.Min(CustomRadiusTopRight ?? CornerRadius, maxRadius);
        float bottomLeft = System.Math.Min(CustomRadiusBottomLeft ?? CornerRadius, maxRadius);
        float bottomRight = System.Math.Min(CustomRadiusBottomRight ?? CornerRadius, maxRadius);

        SKPath path = new SKPath();
        path.MoveTo(boundingRect.Left + topLeft, boundingRect.Top);
        path.LineTo(boundingRect.Right - topRight, boundingRect.Top);
        path.ArcTo(SKRect.Create(boundingRect.Right - topRight * 2, boundingRect.Top, topRight * 2, topRight * 2), 270, 90, false);

        path.LineTo(boundingRect.Right, boundingRect.Bottom - bottomRight);
        path.ArcTo(SKRect.Create(boundingRect.Right - bottomRight * 2, boundingRect.Bottom - bottomRight * 2, bottomRight * 2, bottomRight * 2), 0, 90, false);

        path.LineTo(boundingRect.Left + bottomLeft, boundingRect.Bottom);
        path.ArcTo(SKRect.Create(boundingRect.Left, boundingRect.Bottom - bottomLeft * 2, bottomLeft * 2, bottomLeft * 2), 90, 90, false);

        path.LineTo(boundingRect.Left, boundingRect.Top + topLeft);
        path.ArcTo(SKRect.Create(boundingRect.Left, boundingRect.Top, topLeft * 2, topLeft * 2), 180, 90, false);

        path.Close();
        return path;
    }
}
