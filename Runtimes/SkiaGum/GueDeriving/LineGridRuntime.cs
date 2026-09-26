using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;

#if FRB
namespace SkiaGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif


public class LineGridRuntime: SkiaShapeRuntime
{
    protected override RenderableShapeBase ContainedRenderable => ContainedLineGrid;

    public ushort CellWidth
    {
        get => ContainedLineGrid.CellWidth;
        set => ContainedLineGrid.CellWidth = value; 
    }
    public ushort CellHeight
    {
        get => ContainedLineGrid.CellHeight; 
        set => ContainedLineGrid.CellHeight = value; 
    }

    /// <summary>
    /// The grid line color. Redeclared here so LineGrid keeps a non-obsolete Color (the base shape Color is obsolete).
    /// </summary>
    public new SKColor Color
    {
        get => ContainedLineGrid.Color;
        set => ContainedLineGrid.Color = value;
    }

    public void LineGridCell(double pX, double pY, out int colX, out int colY)
    {
        ContainedLineGrid.LineGridCell(pX, pY, out colX, out colY);
    }
    public bool GetCellPosition(int colX, int colY, out float left, out float top, out float right, out float bottom) 
    {
        return ContainedLineGrid.GetCellPosition(colX, colY, out left, out top, out right, out bottom);
    }

    private LineGrid? mContainedLineGrid;
    LineGrid ContainedLineGrid
    {
        get
        {
            mContainedLineGrid ??= this.RenderableComponent as LineGrid
                ?? throw new System.InvalidOperationException(
                    $"{nameof(LineGridRuntime)} has no LineGrid renderable.");
            return mContainedLineGrid;
        }
        set { mContainedLineGrid = value; }
    }

    public LineGridRuntime()
    {
        LineGrid lineGrid = new LineGrid();
        SetContainedObject(lineGrid);
        ContainedLineGrid = lineGrid;

        StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;

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
