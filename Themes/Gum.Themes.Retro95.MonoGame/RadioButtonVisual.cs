using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseRadioButtonVisual = Gum.Forms.DefaultVisuals.V3.RadioButtonVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled RadioButton visual. A 13 px white circle with a 1 px dark gray
/// outer ring (approximating the Win95 bevel-circle look without per-quadrant bevel
/// primitives) and a small black inner dot when selected. Matches the spirit of
/// <c>.rc-rdot</c>; exact 2-px-bevel-circle fidelity would require a baked NineSlice
/// texture or per-corner Apos arcs.
/// </summary>
public class RadioButtonVisual : BaseRadioButtonVisual
{
    private const float OuterSize = 13f;
    private const float InnerSize = 5f;
    private const float BorderThickness = 1f;
    private const float BoxToLabelGap = 6f;

    private readonly ColoredCircleRuntime _outerFill;
    private readonly ColoredCircleRuntime _outerBorder;
    private readonly ColoredCircleRuntime _innerDot;

    public RadioButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        RadioBackground.Parent = null;
        FocusedIndicator.Parent = null;

        // Match the Win95 tight stack cadence (same rationale as CheckBoxVisual).
        Height = 16;
        HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        TextInstance.X = OuterSize + BoxToLabelGap;
        TextInstance.Width = -(OuterSize + BoxToLabelGap);

        _outerFill = CreateCircle("Retro95RadioFill", OuterSize, filled: true, color: Retro95Colors.WhiteFill);
        AddChild(_outerFill);

        _outerBorder = CreateCircle("Retro95RadioBorder", OuterSize, filled: false, color: Retro95Colors.ShadowOuter);
        _outerBorder.StrokeWidth = BorderThickness;
        _outerBorder.StrokeWidthUnits = DimensionUnitType.Absolute;
        AddChild(_outerBorder);

        _innerDot = CreateInnerDot();
        AddChild(_innerDot);
        _innerDot.Visible = false;

        WireStates();
    }

    private static ColoredCircleRuntime CreateCircle(string name, float size, bool filled, Color color)
    {
        ColoredCircleRuntime c = new ColoredCircleRuntime();
        c.Name = name;
        c.X = 0;
        c.Y = 0;
        c.XUnits = GeneralUnitType.PixelsFromSmall;
        c.YUnits = GeneralUnitType.PixelsFromMiddle;
        c.XOrigin = HorizontalAlignment.Left;
        c.YOrigin = VerticalAlignment.Center;
        c.Width = size;
        c.Height = size;
        c.WidthUnits = DimensionUnitType.Absolute;
        c.HeightUnits = DimensionUnitType.Absolute;
        c.IsFilled = filled;
        c.Color = color;
        return c;
    }

    private static ColoredCircleRuntime CreateInnerDot()
    {
        const float inset = (OuterSize - InnerSize) / 2f;
        ColoredCircleRuntime dot = new ColoredCircleRuntime();
        dot.Name = "Retro95RadioDot";
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
        dot.Color = Retro95Colors.Text;
        return dot;
    }

    private void WireStates()
    {
        // -------- Off --------
        States.EnabledOff.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: false);
        States.HighlightedOff.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: false);
        States.FocusedOff.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: false);
        States.HighlightedFocusedOff.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: false);
        States.PushedOff.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: false);
        States.DisabledOff.Apply = () => Apply(white: false, text: Retro95Colors.DisabledText, dotVisible: false);
        States.DisabledFocusedOff.Apply = () => Apply(white: false, text: Retro95Colors.DisabledText, dotVisible: false);

        // -------- On --------
        States.EnabledOn.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: true);
        States.HighlightedOn.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: true);
        States.FocusedOn.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: true);
        States.HighlightedFocusedOn.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: true);
        States.PushedOn.Apply = () => Apply(white: true, text: Retro95Colors.Text, dotVisible: true);
        States.DisabledOn.Apply = () => Apply(white: false, text: Retro95Colors.DisabledText, dotVisible: false);
        States.DisabledFocusedOn.Apply = () => Apply(white: false, text: Retro95Colors.DisabledText, dotVisible: false);
    }

    private void Apply(bool white, Color text, bool dotVisible)
    {
        _outerFill.Color = white ? Retro95Colors.WhiteFill : Retro95Colors.Surface;
        TextInstance.Color = text;
        _innerDot.Visible = dotVisible;
    }
}
