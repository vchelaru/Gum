using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled ListBoxItem. Square-cornered rows tile flush inside
/// the rounded ListBox shell — transparent at rest, sun-pale 8% tint on
/// hover, accent dim band when selected, sun-pale text when active. The
/// CSS spec also shows a 3 px leaf-bright inset stripe on the left of
/// selected rows; reproduced as a thin rect anchored to the row's left
/// edge.
/// </summary>
public class ListBoxItemVisual : BaseListBoxItemVisual
{
    private const float SelectionStripeWidth = 3f;

    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _selectionStripe;

    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        _selectionStripe = CreateSelectionStripe();
        AddChild(_selectionStripe);

        AddChild(TextInstance);
        TextInstance.X = 12f;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -24f;

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "ForestGladeListItemFill";
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
        fill.Color = Color.Transparent;
        return fill;
    }

    private static RoundedRectangleRuntime CreateSelectionStripe()
    {
        RoundedRectangleRuntime stripe = new RoundedRectangleRuntime();
        stripe.Name = "ForestGladeListItemStripe";
        stripe.X = 0f;
        stripe.XUnits = GeneralUnitType.PixelsFromSmall;
        stripe.YUnits = GeneralUnitType.PixelsFromMiddle;
        stripe.XOrigin = HorizontalAlignment.Left;
        stripe.YOrigin = VerticalAlignment.Center;
        stripe.Width = SelectionStripeWidth;
        stripe.Height = 0f;
        stripe.WidthUnits = DimensionUnitType.Absolute;
        stripe.HeightUnits = DimensionUnitType.RelativeToParent;
        stripe.CornerRadius = 0f;
        stripe.IsFilled = true;
        stripe.Color = ForestGladePalette.SelectionStripe;
        stripe.Visible = false;
        return stripe;
    }

    private void WireStates()
    {
        Color hoverFill = new Color(232, 255, 117, 20); // CSS .fg-lb-item.hov .08 alpha

        States.Enabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: ForestGladeColors.Text, stripe: false);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: hoverFill, text: ForestGladeColors.Text, stripe: false);

        States.Selected.Apply = () => ApplyPalette(
            fill: ForestGladePalette.SelectedRow, text: ForestGladeColors.SunPale, stripe: true);

        States.Focused.Apply = () => ApplyPalette(
            fill: ForestGladePalette.SelectedRow, text: ForestGladeColors.SunPale, stripe: true);

        States.Disabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: ForestGladeColors.Disabled, stripe: false);
    }

    private void ApplyPalette(Color fill, Color text, bool stripe)
    {
        _fill.Color = fill;
        TextInstance.Color = text;
        _selectionStripe.Visible = stripe;
    }
}
