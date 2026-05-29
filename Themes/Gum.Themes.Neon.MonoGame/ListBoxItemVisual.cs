using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled ListBoxItem visual. Rows tile flush inside the ListBox shell —
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

        _fill = CreateFill();
        AddChild(_fill);

        AddChild(TextInstance);
        TextInstance.X = 12f;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -24f;

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "NeonListItemFill";
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
        fill.CornerRadius = 0f;
        fill.IsFilled = true;
        fill.FillColor = Color.Transparent;
        fill.StrokeWidth = 0;
        return fill;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: NeonColors.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: NeonPalette.HoverRow, text: NeonColors.Text);

        States.Selected.Apply = () => ApplyPalette(
            fill: NeonPalette.SelectedRow, text: NeonColors.Accent);

        States.Focused.Apply = () => ApplyPalette(
            fill: NeonPalette.SelectedRow, text: NeonColors.Accent);

        States.Disabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: NeonColors.Disabled);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
