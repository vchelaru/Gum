using RenderingLibrary;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkiaGum.Renderables
{
    class Polygon : RenderableBase
    {
        public List<SKPoint> Points
        {
            get; set;
        }

        public Polygon()
        {
            // set up some default triangle:

            Points = new List<SKPoint>();
            Points.Add(new SKPoint(0, -4));
            Points.Add(new SKPoint(16, 0));
            Points.Add(new SKPoint(0,  4));

        }

        public override void DrawBound(SKRect boundingRect, SKCanvas canvas)
        {

            SKMatrix scaleMatrix = SKMatrix.MakeScale(1, 1);
            //// Gum uses counter clockwise rotation, Skia uses clockwise, so invert:
            SKMatrix rotationMatrix = SKMatrix.MakeRotationDegrees(-Rotation);
            SKMatrix translateMatrix = SKMatrix.MakeTranslation(this.GetAbsoluteX(), this.GetAbsoluteY());
            SKMatrix result = SKMatrix.MakeIdentity();

            SKMatrix.Concat(
                ref result, rotationMatrix, scaleMatrix);
            SKMatrix.Concat(
                ref result, translateMatrix, result);
            canvas.Save();
            canvas.SetMatrix(result);


            SKPath path = new SKPath();

            path.MoveTo(Points[0]);

            for(int i = 0; i < Points.Count; i++)
            {
                path.LineTo(Points[i]);
            }
            path.LineTo(Points[0]);

            path.Close();
            using (var paintToUse = GetPaint(boundingRect))
            {
                canvas.DrawPath(path, paintToUse);
            }

            canvas.Restore();
        }
    }
}
