using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled ListBoxItem visual. Rows tile flush inside the ListBox shell —
/// transparent at rest, light-pink HoverRow tint on hover, AccentLight band when
/// selected (matches <c>.bb-lb-item.sel</c>) with AccentDark text for contrast.
/// </summary>
public class ListBoxItemVisual : BaseListBoxItemVisual
{
    private readonly RectangleRuntime _fill;

    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        _fill = BubblegumShapes.Fill(
            color: new Color(0, 0, 0, 0),
            cornerRadius: 0f,
            name: "BubblegumListItemFill");
        AddChild(_fill);

        AddChild(TextInstance);
        TextInstance.X = 12f;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -24f;

        WireStates();
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: BubblegumStyling.ActiveStyle.Colors.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.HoverRow, text: BubblegumStyling.ActiveStyle.Colors.Text);

        States.Selected.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.SelectedRow, text: BubblegumStyling.ActiveStyle.Colors.SelectedRowText);

        States.Focused.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.SelectedRow, text: BubblegumStyling.ActiveStyle.Colors.SelectedRowText);

        States.Disabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: BubblegumStyling.ActiveStyle.Colors.Disabled);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
