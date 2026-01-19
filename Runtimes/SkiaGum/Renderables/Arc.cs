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
