using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled Button visual. Replaces the V3 ButtonVisual's NineSlice
/// background and underline focus indicator with a stack of Apos.Shapes rounded
/// rectangles built by <see cref="HazardShapes"/>: a filled body, a stroked
/// border, and a focus ring that sits just outside the control and is only visible
/// while focused. The text label is re-parented last so it renders on top.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    private const float CornerRadius = 0f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the base visuals we're replacing. The text label stays but is
        // re-parented last so the new shape layers render behind it.
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        Width = 96;
        Height = 32;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _focusRing = HazardShapes.FocusRing(HazardPalette.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = HazardShapes.Fill(HazardPalette.Surface1, CornerRadius);
        AddChild(_fill);

        _border = HazardShapes.Border(HazardPalette.Border, CornerRadius, BorderThickness);
        AddChild(_border);

        // Re-attach the base TextInstance on top of the new shape stack.
        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private void WireStates()
    {
        // Replace (=, not +=) the base callbacks: the base's SetValuesForState targets
        // the now-detached NineSlice background and underline indicator, neither of
        // which is in this visual's render tree any more.
        States.Enabled.Apply = () => ApplyPalette(
            fill: HazardPalette.Surface1, border: HazardPalette.Border, text: HazardPalette.Text, showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: HazardPalette.HoverFill, border: HazardPalette.BorderHover, text: HazardPalette.TextBright, showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            fill: HazardPalette.Surface1, border: HazardPalette.Accent, text: HazardPalette.Text, showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            fill: HazardPalette.HoverFill, border: HazardPalette.Accent, text: HazardPalette.TextBright, showFocusRing: true);

        // Pressed flashes the full hazard-yellow accent with black ink text - the
        // design's signature button-press feedback (.sv-btn.pre).
        States.Pushed.Apply = () => ApplyPalette(
            fill: HazardPalette.Accent, border: HazardPalette.Accent, text: HazardPalette.PressedText, showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            fill: HazardPalette.DisabledFill, border: HazardPalette.DisabledBorder, text: HazardPalette.DisabledText, showFocusRing: false);

        States.DisabledFocused.Apply = () => ApplyPalette(
            fill: HazardPalette.DisabledFill, border: HazardPalette.DisabledBorder, text: HazardPalette.DisabledText, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
