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

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade RadioButton — 18 px berry-shaped outer circle, 9 px inner
/// dot with a sun-pale fill when selected, accent halo focus ring. The CSS
/// uses a radial gradient on the inner dot (sun-pale → leaf-bright); since
/// Apos.Shapes circle runtimes are solid-filled, we pick sun-pale as the
/// dominant tone.
/// </summary>
public class RadioButtonVisual : BaseRadioButtonVisual
{
    private const float OuterSize = 18f;
    private const float InnerSize = 9f;
    private const float BorderThickness = 1.5f;
    private const float FocusRingInset = 3f;
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
        c.Name = "ForestGladeRadioOuterFill";
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
        c.FillColor = new Color(3, 28, 32);
        c.StrokeWidth = 0;
        return c;
    }

    private static CircleRuntime CreateOuterBorder()
    {
        CircleRuntime c = new CircleRuntime();
        c.Name = "ForestGladeRadioOuterBorder";
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
        c.StrokeColor = new Color(232, 255, 117, 76);
        return c;
    }

    private static CircleRuntime CreateFocusRing()
    {
        CircleRuntime c = new CircleRuntime();
        c.Name = "ForestGladeRadioFocusRing";
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
        c.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.AccentHalo;
        c.Visible = false;
        return c;
    }

    private static CircleRuntime CreateInnerDot()
    {
        const float inset = (OuterSize - InnerSize) / 2f;
        CircleRuntime dot = new CircleRuntime();
        dot.Name = "ForestGladeRadioInnerDot";
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
        dot.FillColor = ForestGladeStyling.ActiveStyle.Colors.SunPale;
        dot.StrokeWidth = 0;
        dot.Visible = false;
        return dot;
    }

    private void WireStates()
    {
        Color restBorder = new Color(232, 255, 117, 76);
        Color hoverBorder = new Color(232, 255, 117, 140);
        Color selectedBorder = ForestGladeStyling.ActiveStyle.Colors.SunPale;
        Color restFill = new Color(3, 28, 32);
        Color disabledBorder = new Color(232, 255, 117, 26);
        Color disabledFill = new Color(8, 16, 18);
        Color innerSelected = ForestGladeStyling.ActiveStyle.Colors.SunPale;
        Color innerPushed = new Color(0, 126, 41); // CSS pushed gradient mid

        // -------- Off --------
        States.EnabledOff.Apply = () => Apply(
            fill: restFill, border: restBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: false);

        States.HighlightedOff.Apply = () => Apply(
            fill: restFill, border: hoverBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: false);

        States.FocusedOff.Apply = () => Apply(
            fill: restFill, border: ForestGladeStyling.ActiveStyle.Colors.LeafBright,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: true);

        States.HighlightedFocusedOff.Apply = () => Apply(
            fill: restFill, border: ForestGladeStyling.ActiveStyle.Colors.LeafBright,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: true);

        States.PushedOff.Apply = () => Apply(
            fill: restFill, border: ForestGladeStyling.ActiveStyle.Colors.LeafBright,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: false, ring: false);

        States.DisabledOff.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, innerVisible: false, ring: false);

        States.DisabledFocusedOff.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, innerVisible: false, ring: true);

        // -------- On --------
        States.EnabledOn.Apply = () => Apply(
            fill: restFill, border: selectedBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: innerSelected,
            ring: false);

        States.HighlightedOn.Apply = () => Apply(
            fill: restFill, border: selectedBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: innerSelected,
            ring: false);

        States.FocusedOn.Apply = () => Apply(
            fill: restFill, border: selectedBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: innerSelected,
            ring: true);

        States.HighlightedFocusedOn.Apply = () => Apply(
            fill: restFill, border: selectedBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: innerSelected,
            ring: true);

        States.PushedOn.Apply = () => Apply(
            fill: restFill, border: selectedBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, innerVisible: true, innerColor: innerPushed,
            ring: false);

        States.DisabledOn.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, innerVisible: false, ring: false);

        States.DisabledFocusedOn.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, innerVisible: false, ring: true);
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
