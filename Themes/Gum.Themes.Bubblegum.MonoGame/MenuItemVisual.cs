using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseMenuItemVisual = Gum.Forms.DefaultVisuals.V3.MenuItemVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled MenuItem visual. Transparent at rest, HoverOption tint on
/// hover, AccentLight band when the submenu is open (Selected). Submenu popup
/// uses the Bubblegum <see cref="ScrollViewerVisual"/> via the
/// <c>ScrollViewerVisualTemplate</c> hook.
/// </summary>
public class MenuItemVisual : BaseMenuItemVisual
{
    private const float HorizontalPadding = 12f;
    private const float VerticalPadding = 6f;

    private readonly ColoredRectangleRuntime _fill;

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
                menuItem.ScrollViewerVisualTemplate = BubblegumSubmenuScrollTemplate;
            }
        }
    }

    private VisualTemplate BubblegumSubmenuScrollTemplate => new VisualTemplate(() =>
    {
        ScrollViewerVisual visual = new ScrollViewerVisual(fullInstantiation: true, tryCreateFormsObject: false);
        visual.HasEvents = true;
        visual.MakeSizedToChildren();
        return visual;
    });

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: BubblegumColors.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: BubblegumPalette.HoverOption, text: BubblegumColors.Text);

        States.Selected.Apply = () => ApplyPalette(
            fill: BubblegumPalette.SelectedRow, text: BubblegumPalette.SelectedRowText);

        States.Focused.Apply = () => ApplyPalette(
            fill: BubblegumPalette.HoverOption, text: BubblegumColors.Text);

        States.Disabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: BubblegumColors.Disabled);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.Color = fill;
        TextInstance.Color = text;
        SubmenuIndicatorInstance.Color = text;
    }

    private static ColoredRectangleRuntime CreateFill()
    {
        ColoredRectangleRuntime fill = new ColoredRectangleRuntime();
        fill.Name = "BubblegumMenuItemFill";
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
        fill.Color = Color.Transparent;
        return fill;
    }
}
