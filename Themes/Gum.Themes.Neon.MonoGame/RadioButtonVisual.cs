using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseRadioButtonVisual = Gum.Forms.DefaultVisuals.V3.RadioButtonVisual;

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled RadioButton visual. 20 px outer circle with a 2 px pink
/// border, 8 px inner accent dot when selected, 3 px translucent focus ring.
/// </summary>
public class RadioButtonVisual : BaseRadioButtonVisual
{
    private const float OuterSize = 20f;
    private const float InnerSize = 8f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;
    private const float BoxToLabelGap = 8f;

    private readonly ColoredCircleRuntime _focusRing;
    private readonly ColoredCircleRuntime _outerFill;
    private readonly ColoredCircleRuntime _outerBorder;
    private readonly ColoredCircleRuntime _innerDot;

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

    private static ColoredCircleRuntime CreateOuterFill()
    {
        ColoredCircleRuntime c = new ColoredCircleRuntime();
        c.Name = "NeonRadioOuterFill";
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
        c.Color = NeonColors.Surface1;
        return c;
    }

    private static ColoredCircleRuntime CreateOuterBorder()
    {
        ColoredCircleRuntime c = new ColoredCircleRuntime();
        c.Name = "NeonRadioOuterBorder";
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
        c.Color = NeonColors.Border;
        return c;
    }

    private static ColoredCircleRuntime CreateFocusRing()
    {
        ColoredCircleRuntime c = new ColoredCircleRuntime();
        c.Name = "NeonRadioFocusRing";
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
        c.Color = NeonPalette.GlowSubtle;
        c.Visible = false;
        return c;
    }

    private static ColoredCircleRuntime CreateInnerDot()
    {
        const float inset = (OuterSize - InnerSize) / 2f;
        ColoredCircleRuntime dot = new ColoredCircleRuntime();
        dot.Name = "NeonRadioInnerDot";
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
        dot.Color = NeonColors.Accent;
        dot.Visible = false;
        return dot;
    }

    private void WireStates()
    {
        // -------- Off --------
        States.EnabledOff.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Border,
            text: NeonColors.Text, innerVisible: false, ring: false);

        States.HighlightedOff.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, innerVisible: false, ring: false);

        States.FocusedOff.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, innerVisible: false, ring: true);

        States.HighlightedFocusedOff.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, innerVisible: false, ring: true);

        States.PushedOff.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, innerVisible: false, ring: false);

        States.DisabledOff.Apply = () => Apply(
            fill: NeonColors.Disabled, border: NeonColors.Disabled,
            text: NeonColors.Disabled, innerVisible: false, ring: false);

        States.DisabledFocusedOff.Apply = () => Apply(
            fill: NeonColors.Disabled, border: NeonColors.Disabled,
            text: NeonColors.Disabled, innerVisible: false, ring: true);

        // -------- On --------
        States.EnabledOn.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, innerVisible: true, innerColor: NeonColors.Accent,
            ring: false);

        States.HighlightedOn.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, innerVisible: true, innerColor: NeonColors.Accent,
            ring: false);

        States.FocusedOn.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, innerVisible: true, innerColor: NeonColors.Accent,
            ring: true);

        States.HighlightedFocusedOn.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, innerVisible: true, innerColor: NeonColors.Accent,
            ring: true);

        States.PushedOn.Apply = () => Apply(
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, innerVisible: true, innerColor: NeonColors.Accent,
            ring: false);

        States.DisabledOn.Apply = () => Apply(
            fill: NeonColors.Disabled, border: NeonColors.Disabled,
            text: NeonColors.Disabled, innerVisible: false, ring: false);

        States.DisabledFocusedOn.Apply = () => Apply(
            fill: NeonColors.Disabled, border: NeonColors.Disabled,
            text: NeonColors.Disabled, innerVisible: false, ring: true);
    }

    private void Apply(Color fill, Color border, Color text, bool innerVisible, bool ring,
        Color? innerColor = null)
    {
        _outerFill.Color = fill;
        _outerBorder.Color = border;
        TextInstance.Color = text;
        _innerDot.Visible = innerVisible;
        if (innerColor.HasValue)
        {
            _innerDot.Color = innerColor.Value;
        }
        _focusRing.Visible = ring;
    }
}
