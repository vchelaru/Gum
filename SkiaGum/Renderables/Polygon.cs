using Gum.Converters;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace SkiaGum.Renderables;

class Polygon : RenderableShapeBase
{
    public bool IsClosed { get; set; } = true;

    public List<SKPoint> Points
    {
        get; set;
    }

    public GeneralUnitType PointXUnits { get; set; } = GeneralUnitType.PixelsFromSmall;
    public GeneralUnitType PointYUnits { get; set; } = GeneralUnitType.PixelsFromSmall;

    public Polygon()
    {
        // set up some default triangle:

        Points = new List<SKPoint>();
        Points.Add(new SKPoint(0, -4));
        Points.Add(new SKPoint(16, 0));
        Points.Add(new SKPoint(0, 4));

    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        SKPath path = new SKPath();

        var thisPosition = new SKPoint(boundingRect.Left, boundingRect.Top);

        GetXY(boundingRect, Points[0], out float x, out float y);

        path.MoveTo(new SKPoint(x, y) + thisPosition);

        for (int i = 1; i < Points.Count; i++)
        {
            var point = Points[i];
            GetXY(boundingRect, point, out x, out y);

            path.LineTo(new SKPoint(x, y) + thisPosition);
        }

        if (IsClosed)
        {
            path.LineTo(Points[0] + thisPosition);
            path.Close();
        }

        canvas.DrawPath(path, paint);
    }

    private void GetXY(SKRect boundingRect, SKPoint point, out float x, out float y)
    {
        x = point.X;
        y = point.Y;
        switch (PointXUnits)
        {
            case GeneralUnitType.PixelsFromSmall:
                break;
            case GeneralUnitType.Percentage:
                x = x * boundingRect.Width / 100.0f;
                break;
            default:
                throw new NotImplementedException($"Need to implement {PointXUnits}");
        }

        switch (PointYUnits)
        {
            case GeneralUnitType.PixelsFromSmall:
                break;
            case GeneralUnitType.Percentage:
                y = y * boundingRect.Height / 100.0f;
                break;
            default:
                throw new NotImplementedException($"Need to implement {PointYUnits}");

        }

    }
}
