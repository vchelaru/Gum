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

    // CSS has two stacked drop shadows: a dark offset-down "depth" shadow
    // for rest state plus a subtle green glow. Apos.Shapes only supports
    // one shadow per shape, so we pick the dominant one per state — dark
    // depth shadow when resting (matches CSS .fg-btn), green glow when
    // hovered/focused (matches CSS .fg-btn.hov / .foc).
    private const float RestShadowOffsetY = 4f;
    private const float RestShadowBlur = 14f;
    private const float HoverGlowBlur = 24f;
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

        // Vertical 2-stop linear gradient — Apos.Shapes' RoundedRectangleRuntime
        // supports 2 stops natively. CSS spec is 3 stops (top/mid/bottom); we
        // use the top and bottom CSS values and let the renderer interpolate.
        //
        // Gradient endpoints use PixelsFromSmall/PixelsFromLarge (with value 0)
        // rather than Percentage. PixelsFromSmall Y=0 resolves to absoluteTop;
        // PixelsFromLarge Y=0 resolves to absoluteTop+Height. Both correctly
        // include the renderable's world position. The Percentage unit branch
        // in RenderableShapeBase.GetGradient currently has a bug — it
        // overwrites the world position with a local coord, so a Percentage-
        // based gradient renders far from the actual button and the body
        // samples only the END color (flat fill). Tracked separately.
        fill.UseGradient = true;
        fill.GradientType = GradientType.Linear;
        fill.GradientX1Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientY1Units = GeneralUnitType.PixelsFromSmall;
        fill.GradientX1 = 0f;
        fill.GradientY1 = 0f;
        fill.GradientX2Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientY2Units = GeneralUnitType.PixelsFromLarge;
        fill.GradientX2 = 0f;
        fill.GradientY2 = 0f;
        // Initial gradient colors get set on first state apply; seed with the
        // rest pair so the button renders correctly before WireStates runs.
        fill.Alpha1 = 255;
        fill.Alpha2 = 255;
        fill.Red1 = ForestGladePalette.ButtonRestFillTop.R;
        fill.Green1 = ForestGladePalette.ButtonRestFillTop.G;
        fill.Blue1 = ForestGladePalette.ButtonRestFillTop.B;
        fill.Red2 = ForestGladePalette.ButtonRestFillBottom.R;
        fill.Green2 = ForestGladePalette.ButtonRestFillBottom.G;
        fill.Blue2 = ForestGladePalette.ButtonRestFillBottom.B;

        // Initial shadow: dark offset-down "lit from above" depth (matches
        // CSS .fg-btn rest state). Hover/focus states swap this for a
        // green glow via WireStates.
        fill.HasDropshadow = true;
        fill.DropshadowColor = ForestGladePalette.DarkShadow;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = RestShadowOffsetY;
        fill.DropshadowBlurX = RestShadowBlur;
        fill.DropshadowBlurY = RestShadowBlur;
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
            fillTop: ForestGladePalette.ButtonRestFillTop,
            fillBottom: ForestGladePalette.ButtonRestFillBottom,
            text: ForestGladeColors.Text,
            border: ForestGladeColors.Border,
            shadow: ForestGladePalette.DarkShadow,
            shadowOffsetY: RestShadowOffsetY, shadowBlur: RestShadowBlur,
            ring: false);

        States.Highlighted.Apply = () => Apply(
            fillTop: ForestGladePalette.ButtonHoverFillTop,
            fillBottom: ForestGladePalette.ButtonHoverFillBottom,
            text: ForestGladeColors.Text,
            border: ForestGladeColors.BorderHover,
            shadow: ForestGladePalette.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: HoverGlowBlur,
            ring: false);

        States.Focused.Apply = () => Apply(
            fillTop: ForestGladePalette.ButtonRestFillTop,
            fillBottom: ForestGladePalette.ButtonRestFillBottom,
            text: ForestGladeColors.Text,
            border: ForestGladeColors.BorderHover,
            shadow: ForestGladePalette.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: HoverGlowBlur,
            ring: true);

        States.HighlightedFocused.Apply = () => Apply(
            fillTop: ForestGladePalette.ButtonHoverFillTop,
            fillBottom: ForestGladePalette.ButtonHoverFillBottom,
            text: ForestGladeColors.Text,
            border: ForestGladeColors.BorderHover,
            shadow: ForestGladePalette.GlowStrong,
            shadowOffsetY: 0f, shadowBlur: HoverGlowBlur,
            ring: true);

        States.Pushed.Apply = () => Apply(
            fillTop: ForestGladePalette.ButtonPushedFillTop,
            fillBottom: ForestGladePalette.ButtonPushedFillBottom,
            // CSS press state shifts text toward sun-pale (#d6f5b0) — readable
            // contrast against the darker pushed fill.
            text: new Color(214, 245, 176),
            border: ForestGladeColors.Border,
            shadow: ForestGladePalette.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: PushedGlowBlur,
            ring: false);

        States.Disabled.Apply = () => Apply(
            fillTop: ForestGladePalette.ButtonDisabledFillTop,
            fillBottom: ForestGladePalette.ButtonDisabledFillBottom,
            text: ForestGladeColors.Disabled,
            // CSS disabled border alpha drops to .10 — half of the rest border.
            border: new Color(232, 255, 117, 26),
            shadow: ForestGladePalette.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: 0f,
            ring: false);

        States.DisabledFocused.Apply = () => Apply(
            fillTop: ForestGladePalette.ButtonDisabledFillTop,
            fillBottom: ForestGladePalette.ButtonDisabledFillBottom,
            text: ForestGladeColors.Disabled,
            border: new Color(232, 255, 117, 26),
            shadow: ForestGladePalette.DarkShadow,
            shadowOffsetY: 0f, shadowBlur: 0f,
            ring: true);
    }

    private void Apply(Color fillTop, Color fillBottom, Color text, Color border,
        Color shadow, float shadowOffsetY, float shadowBlur, bool ring)
    {
        _fill.Red1 = fillTop.R;
        _fill.Green1 = fillTop.G;
        _fill.Blue1 = fillTop.B;
        _fill.Alpha1 = fillTop.A;
        _fill.Red2 = fillBottom.R;
        _fill.Green2 = fillBottom.G;
        _fill.Blue2 = fillBottom.B;
        _fill.Alpha2 = fillBottom.A;
        _border.Color = border;
        _fill.DropshadowColor = shadow;
        _fill.DropshadowOffsetY = shadowOffsetY;
        _fill.DropshadowBlurX = shadowBlur;
        _fill.DropshadowBlurY = shadowBlur;
        _fill.HasDropshadow = shadowBlur > 0f;
        TextInstance.Color = text;
        _focusRing.Visible = ring;
    }
}
