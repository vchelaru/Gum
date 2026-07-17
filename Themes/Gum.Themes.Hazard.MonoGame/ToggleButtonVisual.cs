using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
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

        _focusRing = HazardShapes.FocusRing(HazardStyling.ActiveStyle.Colors.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Surface1, CornerRadius);
        AddChild(_fill);

        _border = HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness);
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private void WireStates()
    {
        // Off variants: same palette as the standard Hazard Button.
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Border,
            text: HazardStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.HoverFill, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.PressedFill, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.HoverFill, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, showFocusRing: true);

        // On variants: hazard-yellow accent-filled body so the active state is
        // unmistakable. Text flips to PressedText (black ink) for legibility
        // against the saturated accent fill.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.Accent, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.AccentHover, border: HazardStyling.ActiveStyle.Colors.AccentHover,
            text: HazardStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.AccentPressed, border: HazardStyling.ActiveStyle.Colors.AccentPressed,
            text: HazardStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.Accent, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.PressedText, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.AccentHover, border: HazardStyling.ActiveStyle.Colors.AccentHover,
            text: HazardStyling.ActiveStyle.Colors.PressedText, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
