using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseMenuVisual = Gum.Forms.DefaultVisuals.V3.MenuVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled Menu visual. Plain gray fill with a 1 px hairline divider at the
/// bottom (matches <c>.rc-menubar</c>).
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
        fill.Name = "Retro95MenuFill";
        fill.X = 0; fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0; fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.Color = Retro95Colors.Surface;
        return fill;
    }

    private static ColoredRectangleRuntime CreateBottomSeparator()
    {
        ColoredRectangleRuntime separator = new ColoredRectangleRuntime();
        separator.Name = "Retro95MenuDivider";
        separator.X = 0; separator.Y = 0;
        separator.XUnits = GeneralUnitType.PixelsFromMiddle;
        separator.YUnits = GeneralUnitType.PixelsFromLarge;
        separator.XOrigin = HorizontalAlignment.Center;
        separator.YOrigin = VerticalAlignment.Bottom;
        separator.Width = 0;
        separator.Height = SeparatorHeight;
        separator.WidthUnits = DimensionUnitType.RelativeToParent;
        separator.HeightUnits = DimensionUnitType.Absolute;
        separator.Color = Retro95Colors.HairlineDivider;
        return separator;
    }
}
