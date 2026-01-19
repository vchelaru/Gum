using SkiaSharp;
using System;

namespace SkiaGum.Renderables; 

public class LineGrid: RenderableShapeBase 
{
    public ushort CellWidth { get; set; }
    public ushort CellHeight { get; set; }

    public LineGrid()
    {
        CellWidth = 10;
        CellHeight = 10;
        Color = SKColor.Parse("#77FFFFFF");
    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        float CellHeight = this.CellHeight;
        int cellcount = (int)(boundingRect.Height / CellHeight);
        for(int i = 1; i < cellcount; i++)
        {
            float thisY = i * CellHeight + boundingRect.Top;
            canvas.DrawLine(boundingRect.Left, thisY, boundingRect.Right, thisY, paint);
        }

        float CellWidth = this.CellWidth;
        cellcount = (int)(boundingRect.Width / CellWidth);
        for(int i = 1; i < cellcount; i++)
        {
            float thisX = i * CellWidth + boundingRect.Left;
            canvas.DrawLine(thisX, boundingRect.Top, thisX, boundingRect.Bottom, paint);
        }
    }

    public void LineGridCell(double pX, double pY, out int colX, out int colY)
    {
        colX = (int)Math.Ceiling(pX / CellWidth);
        colY = (int)Math.Ceiling(pY / CellHeight);
    }

    public bool GetCellPosition(int colX, int colY, out float left, out float top, out float right, out float bottom)
    {
        left = -1;
        top = -1;
        right = -1;
        bottom = -1;
        var numcellsX = Width / CellWidth;
        var numcellsY = Height / CellHeight;
        if((colX < 0) || (colX > numcellsX) || (colY < 0) || (colY > numcellsY)) return false;

        left = (colX - 1) * CellWidth;
        right = CellWidth;
        top = (colY - 1) * CellHeight;
        bottom = CellHeight;
        return true;
    }
}
