using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using BaseToggleButtonVisual = Gum.Forms.DefaultVisuals.V3.ToggleButtonVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled ToggleButton visual. Same three-layer shape stack as
/// <see cref="ButtonVisual"/> (focus ring + fill + 1 px border, built via
/// <see cref="HazardShapes"/>). The "On" / pressed-stay variants paint with the
/// Accent palette so the toggle reads as active; the "Off" variants match the
/// standard Button look.
/// </summary>
public class ToggleButtonVisual : BaseToggleButtonVisual
{
    private const float CornerRadius = 0f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ToggleButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach V3's NineSlice background + underline focus indicator. Text
        // label stays but is re-parented last so the new shape layers render
        // behind it.
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

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private void WireStates()
    {
        // Off variants: same palette as the standard Hazard Button.
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: HazardPalette.Surface1, border: HazardPalette.Border,
            text: HazardPalette.Text, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: HazardPalette.HoverFill, border: HazardPalette.Accent,
            text: HazardPalette.Text, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: HazardPalette.PressedFill, border: HazardPalette.Accent,
            text: HazardPalette.Text, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: HazardPalette.Surface1, border: HazardPalette.Accent,
            text: HazardPalette.Text, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: HazardPalette.HoverFill, border: HazardPalette.Accent,
            text: HazardPalette.Text, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: HazardPalette.DisabledFill, border: HazardPalette.DisabledBorder,
            text: HazardPalette.DisabledText, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: HazardPalette.DisabledFill, border: HazardPalette.DisabledBorder,
            text: HazardPalette.DisabledText, showFocusRing: true);

        // On variants: hazard-yellow accent-filled body so the active state is
        // unmistakable. Text flips to PressedText (black ink) for legibility
        // against the saturated accent fill.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: HazardPalette.Accent, border: HazardPalette.Accent,
            text: HazardPalette.PressedText, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: HazardPalette.AccentHover, border: HazardPalette.AccentHover,
            text: HazardPalette.PressedText, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: HazardPalette.AccentPressed, border: HazardPalette.AccentPressed,
            text: HazardPalette.PressedText, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: HazardPalette.Accent, border: HazardPalette.Accent,
            text: HazardPalette.PressedText, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: HazardPalette.AccentHover, border: HazardPalette.AccentHover,
            text: HazardPalette.PressedText, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: HazardPalette.DisabledFill, border: HazardPalette.DisabledBorder,
            text: HazardPalette.DisabledText, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: HazardPalette.DisabledFill, border: HazardPalette.DisabledBorder,
            text: HazardPalette.DisabledText, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
