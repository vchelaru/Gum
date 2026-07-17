using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
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

        _focusRing = HazardShapes.FocusRing(HazardStyling.ActiveStyle.Colors.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Surface1, CornerRadius);
        AddChild(_fill);

        _border = HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness);
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
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Border, text: HazardStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.HoverFill, border: HazardStyling.ActiveStyle.Colors.BorderHover, text: HazardStyling.ActiveStyle.Colors.TextBright, showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Accent, text: HazardStyling.ActiveStyle.Colors.Text, showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.HoverFill, border: HazardStyling.ActiveStyle.Colors.Accent, text: HazardStyling.ActiveStyle.Colors.TextBright, showFocusRing: true);

        // Pressed flashes the full hazard-yellow accent with black ink text - the
        // design's signature button-press feedback (.sv-btn.pre).
        States.Pushed.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.Accent, border: HazardStyling.ActiveStyle.Colors.Accent, text: HazardStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder, text: HazardStyling.ActiveStyle.Colors.DisabledText, showFocusRing: false);

        States.DisabledFocused.Apply = () => ApplyPalette(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder, text: HazardStyling.ActiveStyle.Colors.DisabledText, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
