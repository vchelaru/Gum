using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled Button visual. Near-square corners (CSS <c>--r: 1px</c>),
/// surface-1 body with a 1 px cyan accent border, cyan text, and a Gaussian
/// glow rendered via the native Apos.Shapes drop shadow. State emphasis is
/// carried by glow alpha — hover and focus broaden the bloom.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    private const float CornerRadius = 1f;
    private const float BorderThickness = 1f;
    private const float FocusBorderThickness = 2f;

    /// <summary>
    /// Resting glow — CSS <c>box-shadow: 0 0 8px rgba(0,229,255,.2)</c>. Bumped
    /// in alpha/blur to compensate for the sRGB-composited vs linear-composited
    /// rendering mismatch documented in <c>.claude/skills/gum-theming/SKILL.md</c>.
    /// Hover/push glows kept compact so the bloom doesn't bleed across the body
    /// and visually wash out the text label.
    /// </summary>
    private const float RestGlowBlur = 16f;
    private const float HoverGlowBlur = 14f;
    private const float PushedGlowBlur = 12f;
    private const float FocusGlowBlur = 20f;

    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the V3 NineSlice background and underline focus indicator.
        // The text label stays but is re-parented last so the new shape layers
        // render behind it.
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        Width = 120;
        Height = 32;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = NeonColors.Accent;

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "NeonButtonFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.CornerRadius = CornerRadius;
        fill.IsFilled = true;
        fill.Color = NeonColors.Surface1;
        // Native Gaussian glow — replaces the old box-shadow stack. Toggled
        // per state via WireStates (alpha/blur scale with hover/focus/push).
        fill.HasDropshadow = true;
        fill.DropshadowColor = NeonPalette.GlowSubtle;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = 0f;
        fill.DropshadowBlurX = RestGlowBlur;
        fill.DropshadowBlurY = RestGlowBlur;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "NeonButtonBorder";
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.CornerRadius = CornerRadius;
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.Color = NeonColors.Accent;
        return border;
    }

    private void WireStates()
    {
        // Text stays cyan in every interactive state — using white on hover/push
        // washed against the glow bloom and the label became unreadable.
        States.Enabled.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent, text: NeonColors.Accent,
            glow: NeonPalette.GlowMedium, blur: RestGlowBlur, thickBorder: false);

        States.Highlighted.Apply = () => Apply(
            fill: NeonPalette.ButtonHoverFill, border: NeonColors.Accent, text: NeonColors.Accent,
            glow: NeonPalette.GlowMedium, blur: HoverGlowBlur, thickBorder: false);

        // Focused → thicker 2 px border + a wider glow halo. The thickness step
        // is the primary visual cue (glow alone wasn't legible enough).
        States.Focused.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent, text: NeonColors.Accent,
            glow: NeonPalette.GlowStrong, blur: FocusGlowBlur, thickBorder: true);

        States.HighlightedFocused.Apply = () => Apply(
            fill: NeonPalette.ButtonHoverFill, border: NeonColors.Accent, text: NeonColors.Accent,
            glow: NeonPalette.GlowStrong, blur: FocusGlowBlur, thickBorder: true);

        States.Pushed.Apply = () => Apply(
            fill: NeonPalette.ButtonPushedFill, border: NeonColors.Accent, text: NeonColors.Accent,
            glow: NeonPalette.GlowMedium, blur: PushedGlowBlur, thickBorder: false);

        States.Disabled.Apply = () => Apply(
            fill: NeonColors.Background, border: NeonColors.DisabledBorder, text: NeonColors.Muted,
            glow: NeonPalette.GlowSubtle, blur: 0f, thickBorder: false);

        States.DisabledFocused.Apply = () => Apply(
            fill: NeonColors.Background, border: NeonColors.DisabledBorder, text: NeonColors.Muted,
            glow: NeonPalette.GlowSubtle, blur: 0f, thickBorder: false);
    }

    private void Apply(Color fill, Color border, Color text, Color glow, float blur, bool thickBorder)
    {
        _fill.Color = fill;
        _border.Color = border;
        _border.StrokeWidth = thickBorder ? FocusBorderThickness : BorderThickness;
        _fill.DropshadowColor = glow;
        _fill.DropshadowBlurX = blur;
        _fill.DropshadowBlurY = blur;
        _fill.HasDropshadow = blur > 0f;
        TextInstance.Color = text;
    }
}
