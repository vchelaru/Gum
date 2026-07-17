using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
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

    private readonly RectangleRuntime _fill;

    public MenuItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        Background.Parent = null;
        ContainerInstance.Parent = null;

        _fill = BubblegumShapes.Fill(
            color: new Color(0, 0, 0, 0),
            name: "BubblegumMenuItemFill");
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
            fill: new Color(0, 0, 0, 0), text: BubblegumStyling.ActiveStyle.Colors.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.HoverOption, text: BubblegumStyling.ActiveStyle.Colors.Text);

        States.Selected.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.SelectedRow, text: BubblegumStyling.ActiveStyle.Colors.SelectedRowText);

        States.Focused.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.HoverOption, text: BubblegumStyling.ActiveStyle.Colors.Text);

        States.Disabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: BubblegumStyling.ActiveStyle.Colors.Disabled);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
        SubmenuIndicatorInstance.Color = text;
    }
}
