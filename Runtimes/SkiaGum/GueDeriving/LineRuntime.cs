using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GueDeriving;

/// <summary>
/// Runtime for a line shape that draws from the top-left to the bottom-right of its bounding rectangle.
/// Rotation, width, and height can be used to control the line's angle and length.
/// </summary>
public class LineRuntime : SkiaShapeRuntime
{
    protected override RenderableShapeBase ContainedRenderable => ContainedLine;

    Line mContainedLine;
    Line ContainedLine
    {
        get
        {
            if (mContainedLine == null)
            {
                mContainedLine = this.RenderableComponent as Line;
            }
            return mContainedLine;
        }
    }

    /// <summary>
    /// Whether the line endpoints are rounded. If false, endpoints are flat (butt cap).
    /// </summary>
    public bool IsRounded
    {
        get => ContainedLine.IsRounded;
        set => ContainedLine.IsRounded = value;
    }

    public LineRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new Line());
            Color = SKColors.White;
            Width = 100;
            Height = 0;
            StrokeWidth = 2;
            StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (LineRuntime)base.Clone();

        toReturn.mContainedLine = null;

        return toReturn;
    }
}
