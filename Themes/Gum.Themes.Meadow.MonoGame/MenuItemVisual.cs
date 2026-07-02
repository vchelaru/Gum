using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseMenuItemVisual = Gum.Forms.DefaultVisuals.V3.MenuItemVisual;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled MenuItem visual. Transparent at rest, peach-light tint on hover,
/// a sage band when the submenu is open (Selected). Label uses the Quicksand body
/// face; the submenu popup uses the Meadow <see cref="ScrollViewerVisual"/>.
/// </summary>
public class MenuItemVisual : BaseMenuItemVisual
{
    private const float HorizontalPadding = 12f;
    private const float VerticalPadding = 6f;

    private readonly RectangleRuntime _fill;

    public MenuItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        Background.Parent = null;
        ContainerInstance.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        AddChild(ContainerInstance);

        ContainerInstance.X = HorizontalPadding;
        ContainerInstance.Y = VerticalPadding;
        Width = HorizontalPadding * 2f;
        WidthUnits = DimensionUnitType.RelativeToChildren;
        Height = VerticalPadding * 2f;
        HeightUnits = DimensionUnitType.RelativeToChildren;

        TextInstance.Font = MeadowStyling.ActiveStyle.Text.BodyFontFamily;

        WireStates();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new MenuItem(this);
        }
    }

    /// <inheritdoc/>
    public override object FormsControlAsObject
    {
        get => base.FormsControlAsObject;
        set
        {
            base.FormsControlAsObject = value;
            if (value is MenuItem menuItem)
            {
                menuItem.ScrollViewerVisualTemplate = MeadowSubmenuScrollTemplate;
            }
        }
    }

    private VisualTemplate MeadowSubmenuScrollTemplate => new VisualTemplate(() =>
    {
        ScrollViewerVisual visual = new ScrollViewerVisual(fullInstantiation: true, tryCreateFormsObject: false);
        visual.HasEvents = true;
        visual.MakeSizedToChildren();
        return visual;
    });

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: MeadowStyling.ActiveStyle.Colors.TealDark);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.HoverOption, text: MeadowStyling.ActiveStyle.Colors.TealDark);

        States.Selected.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.SelectedRow, text: MeadowStyling.ActiveStyle.Colors.SelectedRowText);

        States.Focused.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.HoverOption, text: MeadowStyling.ActiveStyle.Colors.TealDark);

        States.Disabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: MeadowStyling.ActiveStyle.Colors.DisabledInk);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
        SubmenuIndicatorInstance.Color = text;
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowMenuItemFill";
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
        fill.CornerRadius = 8f;
        fill.IsFilled = true;
        fill.FillColor = new Color(0, 0, 0, 0);
        fill.StrokeWidth = 0;
        return fill;
    }
}
