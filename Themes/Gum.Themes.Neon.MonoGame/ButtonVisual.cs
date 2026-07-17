using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
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

    // Blur is roughly 2.5× the CSS spec, matching the alpha bump in
    // NeonPalette — same sRGB-vs-linear compositing reasoning. The opaque
    // hover/push fills block the bloom from showing through the body, so
    // wide blurs no longer wash out the text label.
    private const float RestGlowBlur = 32f;
    private const float HoverGlowBlur = 40f;
    private const float PushedGlowBlur = 44f;

    /// <summary>White-ring focus indicator. Offset 4 px outside the body so
    /// the gap between body border and ring is unmistakable.</summary>
    private const float FocusRingInset = 4f;
    private const float FocusRingThickness = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

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

        // Focus ring renders AFTER the fill so the body's dropshadow halo
        // doesn't alpha-blend over the white stroke and dim it. The ring
        // pixels are entirely outside the body bounds, so painting on top
        // doesn't obscure inner content.
        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = NeonStyling.ActiveStyle.Colors.Accent;

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
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
        fill.FillColor = NeonStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        fill.HasDropshadow = true;
        fill.DropshadowColor = NeonStyling.ActiveStyle.Colors.GlowMedium;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = 0f;
        fill.DropshadowBlur = RestGlowBlur;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
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
        border.StrokeColor = NeonStyling.ActiveStyle.Colors.Accent;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
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
        ring.StrokeColor = NeonStyling.ActiveStyle.Colors.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        // Body fills are stored opaque (pre-blended) so the body fully blocks
        // the dropshadow halo behind it and the label stays readable.
        States.Enabled.Apply = () => Apply(
            fill: NeonStyling.ActiveStyle.Colors.Surface1, text: NeonStyling.ActiveStyle.Colors.Accent,
            glow: NeonStyling.ActiveStyle.Colors.GlowMedium, blur: RestGlowBlur, ring: false);

        States.Highlighted.Apply = () => Apply(
            fill: NeonStyling.ActiveStyle.Colors.ButtonHoverFill, text: NeonStyling.ActiveStyle.Colors.Accent,
            glow: NeonStyling.ActiveStyle.Colors.GlowStrong, blur: HoverGlowBlur, ring: false);

        States.Focused.Apply = () => Apply(
            fill: NeonStyling.ActiveStyle.Colors.Surface1, text: NeonStyling.ActiveStyle.Colors.Accent,
            glow: NeonStyling.ActiveStyle.Colors.GlowStrong, blur: HoverGlowBlur, ring: true);

        States.HighlightedFocused.Apply = () => Apply(
            fill: NeonStyling.ActiveStyle.Colors.ButtonHoverFill, text: NeonStyling.ActiveStyle.Colors.Accent,
            glow: NeonStyling.ActiveStyle.Colors.GlowStrong, blur: HoverGlowBlur, ring: true);

        States.Pushed.Apply = () => Apply(
            fill: NeonStyling.ActiveStyle.Colors.ButtonPushedFill, text: NeonStyling.ActiveStyle.Colors.Accent,
            glow: NeonStyling.ActiveStyle.Colors.GlowStrong, blur: PushedGlowBlur, ring: false);

        States.Disabled.Apply = () => Apply(
            fill: NeonStyling.ActiveStyle.Colors.Background, text: NeonStyling.ActiveStyle.Colors.Muted,
            glow: NeonStyling.ActiveStyle.Colors.GlowSubtle, blur: 0f, ring: false);

        States.DisabledFocused.Apply = () => Apply(
            fill: NeonStyling.ActiveStyle.Colors.Background, text: NeonStyling.ActiveStyle.Colors.Muted,
            glow: NeonStyling.ActiveStyle.Colors.GlowSubtle, blur: 0f, ring: true);
    }

    private void Apply(Color fill, Color text, Color glow, float blur, bool ring)
    {
        _fill.FillColor = fill;
        // Border stays a constant 1 px cyan — focus emphasis is carried by
        // the separate white ring, not by border thickness.
        _border.StrokeColor = NeonStyling.ActiveStyle.Colors.Accent;
        _fill.DropshadowColor = glow;
        _fill.DropshadowBlur = blur;
        _fill.HasDropshadow = blur > 0f;
        TextInstance.Color = text;
        _focusRing.Visible = ring;
    }
}
