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

        _focusRing = TemplateShapes.FocusRing(TemplateStyling.ActiveStyle.Colors.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = TemplateShapes.Fill(TemplateStyling.ActiveStyle.Colors.Surface1, CornerRadius);
        AddChild(_fill);

        _border = TemplateShapes.Border(TemplateStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness);
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private void WireStates()
    {
        // Off variants: same palette as the standard Template Button.
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.Surface1, border: TemplateStyling.ActiveStyle.Colors.Border,
            text: TemplateStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.HoverFill, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.PressedFill, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.Surface1, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.HoverFill, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.DisabledFill, border: TemplateStyling.ActiveStyle.Colors.DisabledBorder,
            text: TemplateStyling.ActiveStyle.Colors.DisabledText, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.DisabledFill, border: TemplateStyling.ActiveStyle.Colors.DisabledBorder,
            text: TemplateStyling.ActiveStyle.Colors.DisabledText, showFocusRing: true);

        // On variants: accent-filled body so the active state is unmistakable.
        // Text flips to PressedText (a light-blue from the source palette) for
        // legibility against the saturated accent fill.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.Accent, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.AccentHover, border: TemplateStyling.ActiveStyle.Colors.AccentHover,
            text: TemplateStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.AccentPressed, border: TemplateStyling.ActiveStyle.Colors.AccentPressed,
            text: TemplateStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.Accent, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.PressedText, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.AccentHover, border: TemplateStyling.ActiveStyle.Colors.AccentHover,
            text: TemplateStyling.ActiveStyle.Colors.PressedText, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.DisabledFill, border: TemplateStyling.ActiveStyle.Colors.DisabledBorder,
            text: TemplateStyling.ActiveStyle.Colors.DisabledText, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.DisabledFill, border: TemplateStyling.ActiveStyle.Colors.DisabledBorder,
            text: TemplateStyling.ActiveStyle.Colors.DisabledText, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
