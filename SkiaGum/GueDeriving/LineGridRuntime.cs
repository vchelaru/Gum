using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving;


public class LineGridRuntime: SkiaShapeRuntime
{
    protected override RenderableBase ContainedRenderable => mContainedLineGrid;

    public ushort CellWidth
    {
        get => mContainedLineGrid.CellWidth;
        set => mContainedLineGrid.CellWidth = value; 
    }
    public ushort CellHeight
    {
        get => mContainedLineGrid.CellHeight; 
        set => mContainedLineGrid.CellHeight = value; 
    }

    public SKColor Color
    {
        get => ContainedLineGrid.Color;
        set => ContainedLineGrid.Color = value;
    }

    public void LineGridCell(double pX, double pY, out int colX, out int colY)
    {
        mContainedLineGrid.LineGridCell(pX, pY, out colX, out colY);
    }
    public bool GetCellPosition(int colX, int colY, out float left, out float top, out float right, out float bottom) 
    {
        return mContainedLineGrid.GetCellPosition(colX, colY, out left, out top, out right, out bottom);
    }

    private LineGrid mContainedLineGrid;
    LineGrid ContainedLineGrid
    {
        get
        {
            if(mContainedLineGrid == null)
                mContainedLineGrid = this.RenderableComponent as LineGrid;
            return mContainedLineGrid;
        }
        set { mContainedLineGrid = value; }
    }

    public LineGridRuntime()
    {
        SetContainedObject(new LineGrid());
        ContainedLineGrid = this.RenderableComponent as LineGrid;

        // Make defaults 100 to match Glue
        Width = 100;
        Height = 100;
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (LineGridRuntime)base.Clone();

        toReturn.mContainedLineGrid = null;

        return toReturn;
    }

    public override void PreRender()
    {
        base.PreRender();
    }
}
