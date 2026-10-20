using SkiaSharp;
using System;

namespace SkiaGum.Renderables;

/// <summary>
/// A line shape that draws from the top-left to the bottom-right of its bounding rectangle.
/// </summary>
/// <remarks>
/// A line is not inset like Rectangle and Circle. The stroke is centered on the geometric line
/// (<see cref="IsOffsetAppliedForStroke"/> is false), so a 1px line along a box edge covers half a
/// pixel on each side and bleeds outside the box, and round caps extend half the stroke width past
/// the endpoints. Rectangle and Circle are inset because their box is the visible boundary; a 1px
/// line inset the same way would vanish. This is intentional and matches the MonoGame line.
/// </remarks>
public class Line : RenderableShapeBase
{
    bool _isRounded;

    /// <summary>
    /// Whether the line endpoints are rounded. If false, endpoints are flat (butt cap).
    /// </summary>
    public bool IsRounded
    {
        get => _isRounded;
        set
        {
            _isRounded = value;
            ClearCachedPaint();
        }
    }

    /// <inheritdoc/>
    public override float XSizeSpillover => base.XSizeSpillover + StrokeWidth / 2.0f;

    /// <inheritdoc/>
    public override float YSizeSpillover => base.YSizeSpillover + StrokeWidth / 2.0f;

    public Line()
    {
        IsFilled = false;
        IsOffsetAppliedForStroke = false;
        CanRenderAt0Dimension = true;
    }

    protected override SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
    {
        var paint = base.GetPaint(boundingRect, absoluteRotation);
        paint.StrokeCap = IsRounded ? SKStrokeCap.Round : SKStrokeCap.Butt;
        return paint;
    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        canvas.DrawLine(boundingRect.Left, boundingRect.Top, boundingRect.Right, boundingRect.Bottom, paint);
    }
}
