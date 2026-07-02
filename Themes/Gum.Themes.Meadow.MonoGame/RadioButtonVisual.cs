using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseRadioButtonVisual = Gum.Forms.DefaultVisuals.V3.RadioButtonVisual;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled RadioButton visual. A 22 px white circle with a 2.5 px peach
/// border and a 10 px sage inner dot when selected; focus paints a soft sage halo.
/// </summary>
public class RadioButtonVisual : BaseRadioButtonVisual
{
    private const float OuterSize = 22f;
    private const float InnerSize = 10f;
    private const float BorderThickness = 2.5f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;
    private const float BoxToLabelGap = 10f;

    private readonly CircleRuntime _focusRing;
    private readonly CircleRuntime _outerFill;
    private readonly CircleRuntime _outerBorder;
    private readonly CircleRuntime _innerDot;

    public RadioButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        RadioBackground.Parent = null;
        FocusedIndicator.Parent = null;

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
        CircleRuntime c = new CircleRuntime();
        c.Name = "MeadowRadioOuterFill";
        c.X = 0;
        c.Y = 0;
        c.XUnits = GeneralUnitType.PixelsFromSmall;
        c.YUnits = GeneralUnitType.PixelsFromMiddle;
        c.XOrigin = HorizontalAlignment.Left;
        c.YOrigin = VerticalAlignment.Center;
        c.Width = OuterSize;
        c.Height = OuterSize;
        c.WidthUnits = DimensionUnitType.Absolute;
        c.HeightUnits = DimensionUnitType.Absolute;
        c.IsFilled = true;
        c.FillColor = MeadowStyling.ActiveStyle.Colors.White;
        c.StrokeWidth = 0;
        return c;
    }

    private static CircleRuntime CreateOuterBorder()
    {
        CircleRuntime c = new CircleRuntime();
        c.Name = "MeadowRadioOuterBorder";
        c.X = 0;
        c.Y = 0;
        c.XUnits = GeneralUnitType.PixelsFromSmall;
        c.YUnits = GeneralUnitType.PixelsFromMiddle;
        c.XOrigin = HorizontalAlignment.Left;
        c.YOrigin = VerticalAlignment.Center;
        c.Width = OuterSize;
        c.Height = OuterSize;
        c.WidthUnits = DimensionUnitType.Absolute;
        c.HeightUnits = DimensionUnitType.Absolute;
        c.IsFilled = false;
        c.StrokeWidth = BorderThickness;
        c.StrokeWidthUnits = DimensionUnitType.Absolute;
        c.StrokeColor = MeadowStyling.ActiveStyle.Colors.PeachDark;
        return c;
    }

    private static CircleRuntime CreateFocusRing()
    {
        CircleRuntime c = new CircleRuntime();
        c.Name = "MeadowRadioFocusRing";
        c.X = -FocusRingInset;
        c.Y = 0;
        c.XUnits = GeneralUnitType.PixelsFromSmall;
        c.YUnits = GeneralUnitType.PixelsFromMiddle;
        c.XOrigin = HorizontalAlignment.Left;
        c.YOrigin = VerticalAlignment.Center;
        c.Width = OuterSize + (FocusRingInset * 2f);
        c.Height = OuterSize + (FocusRingInset * 2f);
        c.WidthUnits = DimensionUnitType.Absolute;
        c.HeightUnits = DimensionUnitType.Absolute;
        c.IsFilled = false;
        c.StrokeWidth = FocusRingThickness;
        c.StrokeWidthUnits = DimensionUnitType.Absolute;
        c.StrokeColor = MeadowStyling.ActiveStyle.Colors.SageFocusRing;
        c.Visible = false;
        return c;
    }

    private static CircleRuntime CreateInnerDot()
    {
        const float inset = (OuterSize - InnerSize) / 2f;
        CircleRuntime dot = new CircleRuntime();
        dot.Name = "MeadowRadioInnerDot";
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
        dot.FillColor = MeadowStyling.ActiveStyle.Colors.SageDark;
        dot.StrokeWidth = 0;
        dot.Visible = false;
        return dot;
    }

    private void WireStates()
    {
        // -------- Off --------
        States.EnabledOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.PeachDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: false, ring: false);

        States.HighlightedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: false, ring: false);

        States.FocusedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: false, ring: true);

        States.HighlightedFocusedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: false, ring: true);

        States.PushedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.Teal,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: false, ring: false);

        States.DisabledOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, innerVisible: false, ring: false);

        States.DisabledFocusedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, innerVisible: false, ring: true);

        // -------- On -------- sage inner dot.
        States.EnabledOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: true, innerColor: MeadowStyling.ActiveStyle.Colors.SageDark,
            ring: false);

        States.HighlightedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: true, innerColor: MeadowStyling.ActiveStyle.Colors.SageDark,
            ring: false);

        States.FocusedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: true, innerColor: MeadowStyling.ActiveStyle.Colors.SageDark,
            ring: true);

        States.HighlightedFocusedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: true, innerColor: MeadowStyling.ActiveStyle.Colors.SageDark,
            ring: true);

        // Pressed-selected deepens the dot to teal (CSS .pp-rad.pre.sel).
        States.PushedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.Teal,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, innerVisible: true, innerColor: MeadowStyling.ActiveStyle.Colors.Teal,
            ring: false);

        States.DisabledOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, innerVisible: false, ring: false);

        States.DisabledFocusedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, innerVisible: false, ring: true);
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
