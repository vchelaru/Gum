using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseMenuVisual = Gum.Forms.DefaultVisuals.V3.MenuVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled Menu visual. Surface1 fill matching the rest of the Dark Pro
/// chrome, plus a 1 px Border-colored hairline at the bottom to visually separate
/// the top menu bar from the page body below. V3.Menu has no state callbacks
/// (the MenuCategory state set is empty), so this is static chrome.
/// </summary>
public class MenuVisual : BaseMenuVisual
{
    private const float SeparatorHeight = 1f;

    private readonly ColoredRectangleRuntime _fill;
    private readonly ColoredRectangleRuntime _bottomSeparator;

    public MenuVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // V3 order is [Background, InnerPanelInstance]. Replace Background with
        // the Dark Pro chrome, and reattach InnerPanelInstance last so menu
        // items render on top of the fill.
        Background.Parent = null;
        InnerPanelInstance.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        _bottomSeparator = CreateBottomSeparator();
        AddChild(_bottomSeparator);

        AddChild(InnerPanelInstance);
    }

    private static ColoredRectangleRuntime CreateFill()
    {
        ColoredRectangleRuntime fill = new ColoredRectangleRuntime();
        fill.Name = "DarkProMenuFill";
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
        fill.Color = DarkProColors.Surface1;
        return fill;
    }

    private static ColoredRectangleRuntime CreateBottomSeparator()
    {
        ColoredRectangleRuntime separator = new ColoredRectangleRuntime();
        separator.Name = "DarkProMenuBottomSeparator";
        separator.X = 0;
        separator.Y = 0;
        separator.XUnits = GeneralUnitType.PixelsFromMiddle;
        separator.YUnits = GeneralUnitType.PixelsFromLarge;
        separator.XOrigin = HorizontalAlignment.Center;
        separator.YOrigin = VerticalAlignment.Bottom;
        separator.Width = 0;
        separator.Height = SeparatorHeight;
        separator.WidthUnits = DimensionUnitType.RelativeToParent;
        separator.HeightUnits = DimensionUnitType.Absolute;
        separator.Color = DarkProColors.Border;
        return separator;
    }
}
