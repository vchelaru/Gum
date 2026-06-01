using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
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
            color: Color.Transparent,
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
            fill: Color.Transparent, text: BubblegumColors.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: BubblegumPalette.HoverRow, text: BubblegumColors.Text);

        States.Selected.Apply = () => ApplyPalette(
            fill: BubblegumPalette.SelectedRow, text: BubblegumPalette.SelectedRowText);

        States.Focused.Apply = () => ApplyPalette(
            fill: BubblegumPalette.SelectedRow, text: BubblegumPalette.SelectedRowText);

        States.Disabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: BubblegumColors.Disabled);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
