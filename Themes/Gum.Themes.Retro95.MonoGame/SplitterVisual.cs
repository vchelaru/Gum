using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseSplitterVisual = Gum.Forms.DefaultVisuals.V3.SplitterVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled Splitter visual. Plain gray fill matching the surface, with the
/// hairline divider on the right edge per <c>.rc-split-div</c>.
/// </summary>
public class SplitterVisual : BaseSplitterVisual
{
    private readonly RectangleRuntime _fill;

    public SplitterVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;

        _fill = new RectangleRuntime();
        _fill.Name = "Retro95SplitterFill";
        _fill.X = 0; _fill.Y = 0;
        _fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        _fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        _fill.XOrigin = HorizontalAlignment.Center;
        _fill.YOrigin = VerticalAlignment.Center;
        _fill.Width = 0; _fill.Height = 0;
        _fill.WidthUnits = DimensionUnitType.RelativeToParent;
        _fill.HeightUnits = DimensionUnitType.RelativeToParent;
        _fill.IsFilled = true;
        _fill.FillColor = Retro95Styling.ActiveStyle.Colors.Surface;
        _fill.StrokeWidth = 0;
        AddChild(_fill);
    }
}
