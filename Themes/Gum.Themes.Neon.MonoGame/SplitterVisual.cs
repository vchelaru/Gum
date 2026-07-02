using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseSplitterVisual = Gum.Forms.DefaultVisuals.V3.SplitterVisual;

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled Splitter visual. AccentLight fill — matches
/// <c>.bb-split-div</c> (a soft pink divider between two panes). V3.Splitter has
/// no state category, so this is static chrome.
/// </summary>
public class SplitterVisual : BaseSplitterVisual
{
    private readonly RectangleRuntime _fill;

    public SplitterVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "NeonSplitterFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.IsFilled = true;
        fill.FillColor = NeonStyling.ActiveStyle.Colors.AccentDim;
        fill.StrokeWidth = 0;
        return fill;
    }
}
