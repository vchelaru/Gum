using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseMenuVisual = Gum.Forms.DefaultVisuals.V3.MenuVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled Menu visual. Surface1 fill with a 2 px pink hairline at the
/// bottom to separate the menu bar from the page body. No CSS source for menus
/// in Bubblegum — keeps the palette consistent with the other Bubblegum chrome.
/// </summary>
public class MenuVisual : BaseMenuVisual
{
    private const float SeparatorHeight = 2f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _bottomSeparator;

    public MenuVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        InnerPanelInstance.Parent = null;

        _fill = BubblegumShapes.Fill(
            color: BubblegumStyling.ActiveStyle.Colors.Surface1,
            name: "BubblegumMenuFill");
        AddChild(_fill);

        _bottomSeparator = CreateBottomSeparator();
        AddChild(_bottomSeparator);

        AddChild(InnerPanelInstance);
    }

    private static RectangleRuntime CreateBottomSeparator()
    {
        RectangleRuntime separator = new RectangleRuntime();
        separator.Name = "BubblegumMenuBottomSeparator";
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
        separator.IsFilled = true;
        separator.FillColor = BubblegumStyling.ActiveStyle.Colors.Border;
        separator.StrokeWidth = 0;
        return separator;
    }
}
