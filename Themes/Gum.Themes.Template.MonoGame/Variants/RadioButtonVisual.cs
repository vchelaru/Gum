using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseRadioButtonVisual = Gum.Forms.DefaultVisuals.V3.RadioButtonVisual;

namespace Gum.Themes.Template.Variants;

/// <summary>
/// "Rich" variant of the Template RadioButton. Compared to the flat
/// <see cref="Gum.Themes.Template.RadioButtonVisual"/> this changes ONLY the
/// focus-ring shape: a soft translucent-accent glow (a wider stroke in a
/// translucent accent color) shown on focused states, instead of the flat
/// variant's crisp 1px opaque ring. The palette tokens, the outer/inner circles,
/// and all states are identical to the flat source.
/// <para>
/// The ring is built inline (left-anchored to the 16px box) rather than via
/// <see cref="TemplateShapes.CircleFocusRing"/>, because that helper centers and
/// sizes its ring to the full-width parent control - which would not align with
/// the left-anchored radio circle. It mirrors the CircleFocusRing technique (a
/// hidden, oversized, stroked-only circle toggled by <c>Visible</c>) applied to
/// the box's fixed geometry.
/// </para>
/// <para>
/// This class is part of the opt-in Variants gallery and is NOT registered by
/// default - see <see cref="TemplateTheme.RegisterVisuals"/>.
/// </para>
/// </summary>
public class RadioButtonVisual : BaseRadioButtonVisual
{
    private const float OuterSize = 16f;
    private const float InnerSize = 8f;
    private const float BorderThickness = 1f;
    // Soft glow: a wider, translucent stroke offset further outside the circle.
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;
    private const float BoxToLabelGap = 8f;

    private readonly CircleRuntime _focusRing;
    private readonly CircleRuntime _outerFill;
    private readonly CircleRuntime _outerBorder;
    private readonly CircleRuntime _innerDot;

    public RadioButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Drop the base NineSlice ring (its inner sprite dot is parented to it
        // and goes with it) and the underline focus indicator.
        RadioBackground.Parent = null;
        FocusedIndicator.Parent = null;

        // Spec is 16-px circle + 8-px gap before the label; base assumed 24+4.
        TextInstance.X = OuterSize + BoxToLabelGap;
        TextInstance.Width = -(OuterSize + BoxToLabelGap);

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _outerFill = CreateOuterFill();
        AddChild(_outerFill);

        _outerBorder = CreateOuterBorder();
        AddChild(_outerBorder);

        _innerDot = CreateInnerDot();
        AddChild(_innerDot);

        WireStates();
    }

    private static CircleRuntime CreateOuterFill()
    {
        CircleRuntime circle = new CircleRuntime();
        circle.Name = "RadioOuterFill";
        circle.X = 0;
        circle.Y = 0;
        circle.XUnits = GeneralUnitType.PixelsFromSmall;
        circle.YUnits = GeneralUnitType.PixelsFromMiddle;
        circle.XOrigin = HorizontalAlignment.Left;
        circle.YOrigin = VerticalAlignment.Center;
        circle.Width = OuterSize;
        circle.Height = OuterSize;
        circle.WidthUnits = DimensionUnitType.Absolute;
        circle.HeightUnits = DimensionUnitType.Absolute;
        circle.IsFilled = true;
        circle.FillColor = TemplateStyling.ActiveStyle.Colors.Surface1;
        circle.StrokeWidth = 0;
        return circle;
    }

    private static CircleRuntime CreateOuterBorder()
    {
        CircleRuntime circle = new CircleRuntime();
        circle.Name = "RadioOuterBorder";
        circle.X = 0;
        circle.Y = 0;
        circle.XUnits = GeneralUnitType.PixelsFromSmall;
        circle.YUnits = GeneralUnitType.PixelsFromMiddle;
        circle.XOrigin = HorizontalAlignment.Left;
        circle.YOrigin = VerticalAlignment.Center;
        circle.Width = OuterSize;
        circle.Height = OuterSize;
        circle.WidthUnits = DimensionUnitType.Absolute;
        circle.HeightUnits = DimensionUnitType.Absolute;
        circle.IsFilled = false;
        circle.StrokeWidth = BorderThickness;
        circle.StrokeWidthUnits = DimensionUnitType.Absolute;
        circle.StrokeColor = TemplateStyling.ActiveStyle.Colors.Border;
        return circle;
    }

    private static CircleRuntime CreateFocusRing()
    {
        // Soft glow: a wider (3px) translucent-accent stroke sitting ~2px outside
        // the circle, vs the flat variant's crisp 1px opaque ring. Left-anchored
        // and offset by -inset to stay centered on the left-anchored outer circle.
        CircleRuntime ring = new CircleRuntime();
        ring.Name = "RadioFocusRing";
        ring.X = -FocusRingInset;
        ring.Y = 0;
        ring.XUnits = GeneralUnitType.PixelsFromSmall;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Left;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = OuterSize + (FocusRingInset * 2f);
        ring.Height = OuterSize + (FocusRingInset * 2f);
        ring.WidthUnits = DimensionUnitType.Absolute;
        ring.HeightUnits = DimensionUnitType.Absolute;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = TemplateStyling.ActiveStyle.Colors.AccentGlow;
        ring.Visible = false;
        return ring;
    }

    private static CircleRuntime CreateInnerDot()
    {
        // 8-px filled circle centered on the 16-px outer circle. Outer is at
        // parent.Left (Left origin), so the inner is offset by (Outer-Inner)/2
        // = 4 from the left.
        const float inset = (OuterSize - InnerSize) / 2f;
        CircleRuntime dot = new CircleRuntime();
        dot.Name = "RadioInnerDot";
        dot.X = inset;
        dot.Y = 0;
        dot.XUnits = GeneralUnitType.PixelsFromSmall;
        dot.YUnits = GeneralUnitType.PixelsFromMiddle;
        dot.XOrigin = HorizontalAlignment.Left;
        dot.YOrigin = VerticalAlignment.Center;
        dot.Width = InnerSize;
        dot.Height = InnerSize;
        dot.WidthUnits = DimensionUnitType.Absolute;
        dot.HeightUnits = DimensionUnitType.Absolute;
        dot.IsFilled = true;
        dot.FillColor = TemplateStyling.ActiveStyle.Colors.Accent;
        dot.StrokeWidth = 0;
        dot.Visible = false;
        return dot;
    }

    private void WireStates()
    {
        // -------- Unselected (Off) --------
        States.EnabledOff.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.Surface1, border: TemplateStyling.ActiveStyle.Colors.Border,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: false);

        // Border tracks interaction state only — the inner dot alone signals
        // value. Hover/Highlighted gets BorderHover (matching TextBox's softer
        // hover→focus progression), focus drives Accent + ring, and the same
        // pattern is mirrored on On below.
        States.HighlightedOff.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.Surface2, border: TemplateStyling.ActiveStyle.Colors.BorderHover,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: false);

        States.FocusedOff.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.Surface1, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: true);

        States.HighlightedFocusedOff.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.Surface2, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: true);

        States.PushedOff.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.PressedFill, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: false);

        States.DisabledOff.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.DisabledFill, border: TemplateStyling.ActiveStyle.Colors.DisabledBorder,
            text: TemplateStyling.ActiveStyle.Colors.DisabledText, innerVisible: false, ring: false);

        States.DisabledFocusedOff.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.DisabledFill, border: TemplateStyling.ActiveStyle.Colors.DisabledBorder,
            text: TemplateStyling.ActiveStyle.Colors.DisabledText, innerVisible: false, ring: true);

        // -------- Selected (On) --------
        // Border mirrors the Off variants exactly — the value is communicated solely
        // by the accent dot inside. Off and On look identical in chrome; the dot is
        // the only difference.
        States.EnabledOn.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.Surface1, border: TemplateStyling.ActiveStyle.Colors.Border,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: TemplateStyling.ActiveStyle.Colors.Accent,
            ring: false);

        States.HighlightedOn.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.Surface2, border: TemplateStyling.ActiveStyle.Colors.BorderHover,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: TemplateStyling.ActiveStyle.Colors.Accent,
            ring: false);

        States.FocusedOn.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.Surface1, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: TemplateStyling.ActiveStyle.Colors.Accent,
            ring: true);

        States.HighlightedFocusedOn.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.Surface2, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: TemplateStyling.ActiveStyle.Colors.Accent,
            ring: true);

        States.PushedOn.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.PressedFill, border: TemplateStyling.ActiveStyle.Colors.Accent,
            text: TemplateStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: TemplateStyling.ActiveStyle.Colors.Accent,
            ring: false);

        States.DisabledOn.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.DisabledFill, border: TemplateStyling.ActiveStyle.Colors.DisabledBorder,
            text: TemplateStyling.ActiveStyle.Colors.DisabledText, innerVisible: true,
            innerColor: TemplateStyling.ActiveStyle.Colors.DisabledText, ring: false);

        States.DisabledFocusedOn.Apply = () => Apply(
            fill: TemplateStyling.ActiveStyle.Colors.DisabledFill, border: TemplateStyling.ActiveStyle.Colors.DisabledBorder,
            text: TemplateStyling.ActiveStyle.Colors.DisabledText, innerVisible: true,
            innerColor: TemplateStyling.ActiveStyle.Colors.DisabledText, ring: true);
    }

    private void Apply(Color fill, Color border, Color text, bool innerVisible, bool ring,
        Color? innerColor = null)
    {
        _outerFill.FillColor = fill;
        _outerBorder.StrokeColor = border;
        TextInstance.Color = text;
        _innerDot.Visible = innerVisible;
        if (innerColor.HasValue)
        {
            _innerDot.FillColor = innerColor.Value;
        }
        _focusRing.Visible = ring;
    }
}
