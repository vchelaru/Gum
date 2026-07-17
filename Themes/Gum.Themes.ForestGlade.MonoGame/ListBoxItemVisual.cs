using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
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

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _selectionStripe;

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

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
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
        fill.FillColor = new Color(0, 0, 0, 0);
        fill.StrokeWidth = 0;
        // Selected row CSS: linear-gradient(90deg, rgba(71,246,65,.22), rgba(71,246,65,.05))
        // — leaf-bright fading from left to right. Gradient is enabled here
        // and the stops are toggled per state by WireStates (transparent stops
        // for hover/rest).
        fill.UseGradient = true;
        fill.GradientType = GradientType.Linear;
        fill.GradientX1Units = GeneralUnitType.PixelsFromSmall;
        fill.GradientY1Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientX1 = 0f;
        fill.GradientY1 = 0f;
        fill.GradientX2Units = GeneralUnitType.PixelsFromLarge;
        fill.GradientY2Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientX2 = 0f;
        fill.GradientY2 = 0f;
        return fill;
    }

    private static RectangleRuntime CreateSelectionStripe()
    {
        RectangleRuntime stripe = new RectangleRuntime();
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
        stripe.FillColor = ForestGladeStyling.ActiveStyle.Colors.SelectionStripe;
        stripe.StrokeWidth = 0;
        stripe.Visible = false;
        return stripe;
    }

    private void WireStates()
    {
        Color hoverFill = new Color(232, 255, 117, 20); // CSS .fg-lb-item.hov .08 alpha
        Color selLeft = new Color(71, 246, 65, 56);     // CSS .22
        Color selRight = new Color(71, 246, 65, 13);    // CSS .05

        States.Enabled.Apply = () => ApplyPalette(
            fillLeft: new Color(0, 0, 0, 0), fillRight: new Color(0, 0, 0, 0),
            text: ForestGladeStyling.ActiveStyle.Colors.Text, stripe: false);

        States.Highlighted.Apply = () => ApplyPalette(
            fillLeft: hoverFill, fillRight: hoverFill,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, stripe: false);

        States.Selected.Apply = () => ApplyPalette(
            fillLeft: selLeft, fillRight: selRight,
            text: ForestGladeStyling.ActiveStyle.Colors.SunPale, stripe: true);

        States.Focused.Apply = () => ApplyPalette(
            fillLeft: selLeft, fillRight: selRight,
            text: ForestGladeStyling.ActiveStyle.Colors.SunPale, stripe: true);

        States.Disabled.Apply = () => ApplyPalette(
            fillLeft: new Color(0, 0, 0, 0), fillRight: new Color(0, 0, 0, 0),
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, stripe: false);
    }

    private void ApplyPalette(Color fillLeft, Color fillRight, Color text, bool stripe)
    {
        _fill.FillColor = fillLeft;
        _fill.Color2 = fillRight;
        TextInstance.Color = text;
        _selectionStripe.Visible = stripe;
    }
}
