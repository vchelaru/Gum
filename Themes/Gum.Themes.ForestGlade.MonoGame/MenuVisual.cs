using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseMenuVisual = Gum.Forms.DefaultVisuals.V3.MenuVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled Menu visual. Deep canopy fill with a sun-pale
/// hairline at the bottom to separate the menu bar from the page body. No
/// CSS source for menus in the Forest Glade mockup — chrome stays
/// consistent with the rest of the theme.
/// </summary>
public class MenuVisual : BaseMenuVisual
{
    private const float SeparatorHeight = 1f;

    private readonly ColoredRectangleRuntime _fill;
    private readonly ColoredRectangleRuntime _bottomSeparator;

    public MenuVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
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
        fill.Name = "ForestGladeMenuFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.Color = ForestGladeColors.CanopyDeep;
        return fill;
    }

    private static ColoredRectangleRuntime CreateBottomSeparator()
    {
        ColoredRectangleRuntime separator = new ColoredRectangleRuntime();
        separator.Name = "ForestGladeMenuBottomSeparator";
        separator.XUnits = GeneralUnitType.PixelsFromMiddle;
        separator.YUnits = GeneralUnitType.PixelsFromLarge;
        separator.XOrigin = HorizontalAlignment.Center;
        separator.YOrigin = VerticalAlignment.Bottom;
        separator.Width = 0;
        separator.Height = SeparatorHeight;
        separator.WidthUnits = DimensionUnitType.RelativeToParent;
        separator.HeightUnits = DimensionUnitType.Absolute;
        separator.Color = new Color(232, 255, 117, 51); // CSS .fg-hdr border .20
        return separator;
    }
}
