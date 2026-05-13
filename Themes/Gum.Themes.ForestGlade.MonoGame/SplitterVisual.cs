using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseSplitterVisual = Gum.Forms.DefaultVisuals.V3.SplitterVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade Splitter visual. CSS spec is a hanging vine with three leaf
/// nodes — non-trivial to reproduce in shape primitives. We render the vine
/// cord (a thin bark-colored rect down the middle) and leave the decorative
/// leaf clusters out for v1; consumers can subclass and add leaf
/// <c>ColoredCircleRuntime</c>s if needed.
/// </summary>
public class SplitterVisual : BaseSplitterVisual
{
    private readonly ColoredRectangleRuntime _fill;

    public SplitterVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);
    }

    private static ColoredRectangleRuntime CreateFill()
    {
        ColoredRectangleRuntime fill = new ColoredRectangleRuntime();
        fill.Name = "ForestGladeSplitterFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.Color = ForestGladePalette.VineCord;
        return fill;
    }
}
