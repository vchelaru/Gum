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

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        using (var path = new SKPath())
        {
            path.AddArc(boundingRect, -StartAngle, -SweepAngle);
            canvas.DrawPath(path, paint);
        }
    }
}
