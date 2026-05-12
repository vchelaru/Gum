using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled Button visual. Leaf-large per-corner radii silhouette
/// (sharp top-left/bottom-right, rounded top-right/bottom-left), saturated
/// canopy-green fill, sun-pale tinted border, leaf-bright drop-shadow glow.
/// Hover brightens the fill, press darkens it, focus paints a separate
/// sun-pale ring 3 px outside the body. Disabled drops to moss-green muted.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    // Stroke widths picked up from the CSS spec (1 px standard border,
    // 3 px focus ring). Drop-shadow blurs are exaggerated against the CSS
    // literal because Apos.Shapes composites in sRGB rather than linear
    // light — same alpha math reads markedly fainter in-engine. See the
    // gum-theming skill for the long-form rationale.
    private const float BorderThickness = 1f;
    private const float FocusRingThickness = 3f;
    private const float FocusRingInset = 3f;

    private const float RestGlowBlur = 18f;
    private const float HoverGlowBlur = 28f;
    private const float PushedGlowBlur = 0f;

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the V3 NineSlice background and underline focus indicator.
        // The text label stays but is re-parented last so the new shape
        // layers render behind it.
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
        // doesn't alpha-blend over the ring pixels and dim them. The ring
        // pixels are entirely outside the body bounds.
        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = ForestGladeColors.Text;

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "ForestGladeButtonFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyLarge(fill);
        fill.IsFilled = true;
        fill.Color = ForestGladePalette.ButtonRestFill;
        fill.HasDropshadow = true;
        fill.DropshadowColor = ForestGladePalette.GlowMedium;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = 0f;
        fill.DropshadowBlurX = RestGlowBlur;
        fill.DropshadowBlurY = RestGlowBlur;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "ForestGladeButtonBorder";
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyLarge(border);
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.Color = ForestGladeColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "ForestGladeButtonFocusRing";
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        // RelativeToParent + the extra inset gives a ring that sits FocusRingInset
        // pixels outside the body on every edge. The per-corner radii are bumped
        // by the same inset so the ring's outer corners stay parallel to the
        // body's outer corners (otherwise the gap would taper at the corners).
        ring.Width = FocusRingInset * 2f;
        ring.Height = FocusRingInset * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = 4f + FocusRingInset;
        ring.CustomRadiusTopLeft = 4f + FocusRingInset;
        ring.CustomRadiusTopRight = 18f + FocusRingInset;
        ring.CustomRadiusBottomRight = 4f + FocusRingInset;
        ring.CustomRadiusBottomLeft = 18f + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = ForestGladeColors.SunPale * 0.45f;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(
            fill: ForestGladePalette.ButtonRestFill, text: ForestGladeColors.Text,
            border: ForestGladeColors.Border,
            glow: ForestGladePalette.GlowMedium, blur: RestGlowBlur, ring: false);

        States.Highlighted.Apply = () => Apply(
            fill: ForestGladePalette.ButtonHoverFill, text: ForestGladeColors.Text,
            border: ForestGladeColors.BorderHover,
            glow: ForestGladePalette.GlowStrong, blur: HoverGlowBlur, ring: false);

        States.Focused.Apply = () => Apply(
            fill: ForestGladePalette.ButtonRestFill, text: ForestGladeColors.Text,
            border: ForestGladeColors.BorderHover,
            glow: ForestGladePalette.GlowStrong, blur: HoverGlowBlur, ring: true);

        States.HighlightedFocused.Apply = () => Apply(
            fill: ForestGladePalette.ButtonHoverFill, text: ForestGladeColors.Text,
            border: ForestGladeColors.BorderHover,
            glow: ForestGladePalette.GlowStrong, blur: HoverGlowBlur, ring: true);

        States.Pushed.Apply = () => Apply(
            fill: ForestGladePalette.ButtonPushedFill,
            // CSS press state shifts text toward sun-pale (#d6f5b0) — readable
            // contrast against the darker pushed fill.
            text: new Color(214, 245, 176),
            border: ForestGladeColors.Border,
            glow: ForestGladePalette.GlowMedium, blur: PushedGlowBlur, ring: false);

        States.Disabled.Apply = () => Apply(
            fill: ForestGladePalette.ButtonDisabledFill,
            text: ForestGladeColors.Disabled,
            // CSS disabled border alpha drops to .10 — half of the rest border.
            border: new Color(232, 255, 117, 26),
            glow: ForestGladePalette.GlowSubtle, blur: 0f, ring: false);

        States.DisabledFocused.Apply = () => Apply(
            fill: ForestGladePalette.ButtonDisabledFill,
            text: ForestGladeColors.Disabled,
            border: new Color(232, 255, 117, 26),
            glow: ForestGladePalette.GlowSubtle, blur: 0f, ring: true);
    }

    private void Apply(Color fill, Color text, Color border, Color glow, float blur, bool ring)
    {
        _fill.Color = fill;
        _border.Color = border;
        _fill.DropshadowColor = glow;
        _fill.DropshadowBlurX = blur;
        _fill.DropshadowBlurY = blur;
        _fill.HasDropshadow = blur > 0f;
        TextInstance.Color = text;
        _focusRing.Visible = ring;
    }
}
