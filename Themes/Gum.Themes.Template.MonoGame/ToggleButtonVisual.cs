using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using BaseToggleButtonVisual = Gum.Forms.DefaultVisuals.V3.ToggleButtonVisual;

namespace Gum.Themes.Template;

/// <summary>
/// Template-styled ToggleButton visual. Same three-layer shape stack as
/// <see cref="ButtonVisual"/> (focus ring + fill + 1 px border, built via
/// <see cref="TemplateShapes"/>). The "On" / pressed-stay variants paint with the
/// Accent palette so the toggle reads as active; the "Off" variants match the
/// standard Button look.
/// </summary>
public class ToggleButtonVisual : BaseToggleButtonVisual
{
    private const float CornerRadius = 2f;
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

        _focusRing = TemplateShapes.FocusRing(TemplatePalette.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = TemplateShapes.Fill(TemplatePalette.Surface1, CornerRadius);
        AddChild(_fill);

        _border = TemplateShapes.Border(TemplatePalette.Border, CornerRadius, BorderThickness);
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private void WireStates()
    {
        // Off variants: same palette as the standard Template Button.
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: TemplatePalette.Surface1, border: TemplatePalette.Border,
            text: TemplatePalette.Text, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: TemplatePalette.HoverFill, border: TemplatePalette.Accent,
            text: TemplatePalette.Text, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: TemplatePalette.PressedFill, border: TemplatePalette.Accent,
            text: TemplatePalette.Text, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: TemplatePalette.Surface1, border: TemplatePalette.Accent,
            text: TemplatePalette.Text, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: TemplatePalette.HoverFill, border: TemplatePalette.Accent,
            text: TemplatePalette.Text, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: TemplatePalette.DisabledFill, border: TemplatePalette.DisabledBorder,
            text: TemplatePalette.DisabledText, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: TemplatePalette.DisabledFill, border: TemplatePalette.DisabledBorder,
            text: TemplatePalette.DisabledText, showFocusRing: true);

        // On variants: accent-filled body so the active state is unmistakable.
        // Text flips to PressedText (a light-blue from the source palette) for
        // legibility against the saturated accent fill.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: TemplatePalette.Accent, border: TemplatePalette.Accent,
            text: TemplatePalette.PressedText, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: TemplatePalette.AccentHover, border: TemplatePalette.AccentHover,
            text: TemplatePalette.PressedText, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: TemplatePalette.AccentPressed, border: TemplatePalette.AccentPressed,
            text: TemplatePalette.PressedText, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: TemplatePalette.Accent, border: TemplatePalette.Accent,
            text: TemplatePalette.PressedText, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: TemplatePalette.AccentHover, border: TemplatePalette.AccentHover,
            text: TemplatePalette.PressedText, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: TemplatePalette.DisabledFill, border: TemplatePalette.DisabledBorder,
            text: TemplatePalette.DisabledText, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: TemplatePalette.DisabledFill, border: TemplatePalette.DisabledBorder,
            text: TemplatePalette.DisabledText, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
