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
/// Surface1 body with a 1 px cyan accent border, cyan text, and a Gaussian
/// glow rendered via the native Apos.Shapes drop shadow. Hover and press
/// switch to opaque pre-blended tinted fills (so the glow halo doesn't show
/// through and wash the label). Focus is signalled by a distinct white 1 px
/// ring sitting 4 px outside the body — a separate shape from the body glow.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    private const float CornerRadius = 1f;
    private const float BorderThickness = 1f;

    private const float RestGlowBlur = 16f;
    private const float HoverGlowBlur = 10f;
    private const float PushedGlowBlur = 8f;

    /// <summary>White-ring focus indicator. Offset 4 px outside the body so
    /// the gap between body border and ring is unmistakable.</summary>
    private const float FocusRingInset = 4f;
    private const float FocusRingThickness = 1f;

    private readonly RoundedRectangleRuntime _focusRing;
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

        // Focus ring goes in first so it renders behind everything. Visible
        // only on focused states.
        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

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
        fill.HasDropshadow = true;
        fill.DropshadowColor = NeonPalette.GlowMedium;
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

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "NeonButtonFocusRing";
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = FocusRingInset * 2f;
        ring.Height = FocusRingInset * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = CornerRadius + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = NeonPalette.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        // Body fills are stored opaque (pre-blended) so the body fully blocks
        // the dropshadow halo behind it and the label stays readable.
        States.Enabled.Apply = () => Apply(
            fill: NeonColors.Surface1, text: NeonColors.Accent,
            glow: NeonPalette.GlowMedium, blur: RestGlowBlur, ring: false);

        States.Highlighted.Apply = () => Apply(
            fill: NeonPalette.ButtonHoverFill, text: NeonColors.Accent,
            glow: NeonPalette.GlowStrong, blur: HoverGlowBlur, ring: false);

        States.Focused.Apply = () => Apply(
            fill: NeonColors.Surface1, text: NeonColors.Accent,
            glow: NeonPalette.GlowMedium, blur: RestGlowBlur, ring: true);

        States.HighlightedFocused.Apply = () => Apply(
            fill: NeonPalette.ButtonHoverFill, text: NeonColors.Accent,
            glow: NeonPalette.GlowStrong, blur: HoverGlowBlur, ring: true);

        States.Pushed.Apply = () => Apply(
            fill: NeonPalette.ButtonPushedFill, text: NeonColors.Accent,
            glow: NeonPalette.GlowMedium, blur: PushedGlowBlur, ring: false);

        States.Disabled.Apply = () => Apply(
            fill: NeonColors.Background, text: NeonColors.Muted,
            glow: NeonPalette.GlowSubtle, blur: 0f, ring: false);

        States.DisabledFocused.Apply = () => Apply(
            fill: NeonColors.Background, text: NeonColors.Muted,
            glow: NeonPalette.GlowSubtle, blur: 0f, ring: true);
    }

    private void Apply(Color fill, Color text, Color glow, float blur, bool ring)
    {
        _fill.Color = fill;
        // Border stays a constant 1 px cyan — focus emphasis is carried by
        // the separate white ring, not by border thickness.
        _border.Color = NeonColors.Accent;
        _fill.DropshadowColor = glow;
        _fill.DropshadowBlurX = blur;
        _fill.DropshadowBlurY = blur;
        _fill.HasDropshadow = blur > 0f;
        TextInstance.Color = text;
        _focusRing.Visible = ring;
    }
}
