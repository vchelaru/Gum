using SkiaSharp;

namespace SkiaGum.Renderables;

public class Arc : RenderableShapeBase
{
    #region Fields/Properties

    public float StartAngle
    {
        get;
        set;
    } = 0;

    public float SweepAngle
    {
        get;
        set;
    } = 90;

    public float Thickness
    {
        get => base.StrokeWidth;
        set => base.StrokeWidth = value;
    }

    /// <summary>
    /// Arc renders only in stroke mode. The setter is intentionally a no-op so a stray
    /// <c>IsFilled = true</c> (or a <c>FillColor</c> setter on <c>ArcRuntime</c> that flips
    /// <c>IsFilled</c> as a side effect) cannot reach the chord-fill path. Skia's
    /// <c>SKPath.AddArc</c> autocloses an open path with a straight chord from sweep-end back
    /// to start when filled, producing a circular-segment shape with no real use case. True
    /// pie-wedge fills are reachable via <c>Thickness = Width/2</c> on a square arc — the
    /// stroke band collapses to a wedge. See <c>ArcRuntime.Thickness</c> for details.
    /// Apos.Shapes' Arc primitive ignores <c>IsFilled</c> for the same reason; this keeps the
    /// two backends behaviorally aligned.
    /// </summary>
    public override bool IsFilled
    {
        get => false;
        set { }
    }

    bool _isEndRounded;
    public bool IsEndRounded
    {
        get => _isEndRounded;
        set
        {
            _isEndRounded = value;
            ClearCachedPaint();
        }
    }

    #endregion

    public Arc() : base()
    {
        IsFilled = false;
    }

    protected override SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
    {
        // base.GetPaint sets Style from IsFilled — Arc's IsFilled override pins it to false,
        // so paint always comes back stroked. See the IsFilled override above for rationale.
        var paint = base.GetPaint(boundingRect, absoluteRotation);
        paint.StrokeCap = IsEndRounded ? SKStrokeCap.Round : SKStrokeCap.Butt;
        return paint;
    }

    protected override bool BlendsWholeBounds => true;

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        using SKPath path = CreatePath(boundingRect);
        canvas.DrawPath(path, paint);
    }

    private SKPath CreatePath(SKRect boundingRect)
    {
        using var builder = new SKPathBuilder();
        if (System.Math.Abs(SweepAngle) >= 360)
        {
            // A full sweep is a ring. AddArc with a full sweep is empty or seamed at most start
            // angles (#7394), and arcs split at an arbitrary angle stroke with visible edge error,
            // so draw the same oval a circle uses, turned so it begins at the start angle (the
            // dash phase follows it).
            builder.AddOval(
                new SKRect(-1, -1, 1, 1),
                SweepAngle > 0 ? SKPathDirection.CounterClockwise : SKPathDirection.Clockwise);
            SKMatrix matrix = SKMatrix.Concat(
                SKMatrix.CreateTranslation(boundingRect.MidX, boundingRect.MidY),
                SKMatrix.Concat(
                    SKMatrix.CreateScale(boundingRect.Width / 2, boundingRect.Height / 2),
                    SKMatrix.CreateRotationDegrees(-StartAngle)));
            SKPath ring = builder.Detach();
            ring.Transform(matrix);
            return ring;
        }

        builder.AddArc(boundingRect, -StartAngle, -SweepAngle);
        return builder.Detach();
    }
}
