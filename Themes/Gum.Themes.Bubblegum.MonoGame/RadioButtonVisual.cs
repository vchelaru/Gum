using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseRadioButtonVisual = Gum.Forms.DefaultVisuals.V3.RadioButtonVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled RadioButton visual. 20 px outer circle with a 2 px pink
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
        c.Name = "BubblegumRadioOuterFill";
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
        c.FillColor = BubblegumStyling.ActiveStyle.Colors.Surface1;
        c.StrokeWidth = 0;
        return c;
    }

    private static CircleRuntime CreateOuterBorder()
    {
        CircleRuntime c = new CircleRuntime();
        c.Name = "BubblegumRadioOuterBorder";
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
        c.StrokeColor = BubblegumStyling.ActiveStyle.Colors.Border;
        return c;
    }

    private static CircleRuntime CreateFocusRing()
    {
        CircleRuntime c = new CircleRuntime();
        c.Name = "BubblegumRadioFocusRing";
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
        c.StrokeColor = BubblegumStyling.ActiveStyle.Colors.FocusRing;
        c.Visible = false;
        return c;
    }

    private static CircleRuntime CreateInnerDot()
    {
        const float inset = (OuterSize - InnerSize) / 2f;
        CircleRuntime dot = new CircleRuntime();
        dot.Name = "BubblegumRadioInnerDot";
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
        dot.FillColor = BubblegumStyling.ActiveStyle.Colors.Accent;
        dot.StrokeWidth = 0;
        dot.Visible = false;
        return dot;
    }

    private void WireStates()
    {
        // -------- Off --------
        States.EnabledOff.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Border,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: false);

        States.HighlightedOff.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: false);

        States.FocusedOff.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: true);

        States.HighlightedFocusedOff.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: true);

        States.PushedOff.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.AccentDark,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: false);

        States.DisabledOff.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.DisabledFill, border: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: BubblegumStyling.ActiveStyle.Colors.Disabled, innerVisible: false, ring: false);

        States.DisabledFocusedOff.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.DisabledFill, border: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: BubblegumStyling.ActiveStyle.Colors.Disabled, innerVisible: false, ring: true);

        // -------- On --------
        States.EnabledOn.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: BubblegumStyling.ActiveStyle.Colors.Accent,
            ring: false);

        States.HighlightedOn.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: BubblegumStyling.ActiveStyle.Colors.Accent,
            ring: false);

        States.FocusedOn.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: BubblegumStyling.ActiveStyle.Colors.Accent,
            ring: true);

        States.HighlightedFocusedOn.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: BubblegumStyling.ActiveStyle.Colors.Accent,
            ring: true);

        States.PushedOn.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.AccentDark,
            text: BubblegumStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: BubblegumStyling.ActiveStyle.Colors.AccentDark,
            ring: false);

        States.DisabledOn.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.DisabledFill, border: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: BubblegumStyling.ActiveStyle.Colors.Disabled, innerVisible: false, ring: false);

        States.DisabledFocusedOn.Apply = () => Apply(
            fill: BubblegumStyling.ActiveStyle.Colors.DisabledFill, border: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: BubblegumStyling.ActiveStyle.Colors.Disabled, innerVisible: false, ring: true);
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
