using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.Template.Variants;

/// <summary>
/// "Rich" variant of the Template Button - a pill with a flat hard-offset drop
/// shadow (the "stacked card" look). Compared to the flat
/// <see cref="Gum.Themes.Template.ButtonVisual"/> this changes ONLY the shapes:
/// the corner radius is bumped to half the default height so the fill renders as a
/// pill, and the fill is built with <see cref="TemplateShapes.FillWithDropshadow"/>
/// using an opaque shadow color + blur 0 for a sharp offset edge. The palette
/// tokens, the 7 states, and the state-callback structure are identical to the
/// flat source.
/// <para>
/// This class is part of the opt-in Variants gallery and is NOT registered by
/// default - see <see cref="TemplateTheme.RegisterVisuals"/>.
/// </para>
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    // Half the default 32px height -> pill. (Flat source used CornerRadius = 2.)
    private const float CornerRadius = 18f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    // Flat hard-offset "stacked card" shadow: opaque color + blur 0 + downward
    // offset gives a sharp edge sitting below the body (mirrors the Bubblegum
    // Button's HasDropshadow toggle, but flat rather than soft).
    private const float ShadowOffsetY = 4f;
    private const float ShadowBlur = 0f;

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

        _focusRing = TemplateShapes.FocusRing(TemplatePalette.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = TemplateShapes.FillWithDropshadow(
            color: TemplatePalette.Surface1,
            cornerRadius: CornerRadius,
            shadowColor: TemplatePalette.AccentPressed,
            offsetX: 0f,
            offsetY: ShadowOffsetY,
            blur: ShadowBlur);
        AddChild(_fill);

        _border = TemplateShapes.Border(TemplatePalette.Border, CornerRadius, BorderThickness);
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
            fill: TemplatePalette.Surface1, border: TemplatePalette.Border, text: TemplatePalette.Text, showFocusRing: false, showShadow: true);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: TemplatePalette.HoverFill, border: TemplatePalette.Accent, text: TemplatePalette.Text, showFocusRing: false, showShadow: true);

        States.Focused.Apply = () => ApplyPalette(
            fill: TemplatePalette.Surface1, border: TemplatePalette.Accent, text: TemplatePalette.Text, showFocusRing: true, showShadow: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            fill: TemplatePalette.HoverFill, border: TemplatePalette.Accent, text: TemplatePalette.Text, showFocusRing: true, showShadow: true);

        // Pressed/Disabled drop the shadow so the button reads as "down" / inert
        // (mirrors how the Bubblegum Button toggles _fill.HasDropshadow).
        States.Pushed.Apply = () => ApplyPalette(
            fill: TemplatePalette.PressedFill, border: TemplatePalette.Accent, text: TemplatePalette.Text, showFocusRing: false, showShadow: false);

        States.Disabled.Apply = () => ApplyPalette(
            fill: TemplatePalette.DisabledFill, border: TemplatePalette.DisabledBorder, text: TemplatePalette.DisabledText, showFocusRing: false, showShadow: false);

        States.DisabledFocused.Apply = () => ApplyPalette(
            fill: TemplatePalette.DisabledFill, border: TemplatePalette.DisabledBorder, text: TemplatePalette.DisabledText, showFocusRing: true, showShadow: false);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing, bool showShadow)
    {
        _fill.FillColor = fill;
        _fill.HasDropshadow = showShadow;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
