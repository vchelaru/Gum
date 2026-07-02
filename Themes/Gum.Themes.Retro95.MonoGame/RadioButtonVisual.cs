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

    private readonly CircleRuntime _outerFill;
    private readonly CircleRuntime _outerBorder;
    private readonly CircleRuntime _innerDot;
    private readonly Retro95DottedFocusRect _focusRect;

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

        _outerFill = CreateCircle("Retro95RadioFill", OuterSize, filled: true, color: Retro95Styling.ActiveStyle.Colors.WhiteFill);
        AddChild(_outerFill);

        _outerBorder = CreateCircle("Retro95RadioBorder", OuterSize, filled: false, color: Retro95Styling.ActiveStyle.Colors.ShadowOuter);
        _outerBorder.StrokeWidth = BorderThickness;
        _outerBorder.StrokeWidthUnits = DimensionUnitType.Absolute;
        AddChild(_outerBorder);

        _innerDot = CreateInnerDot();
        AddChild(_innerDot);
        _innerDot.Visible = false;

        // Dotted focus ring around the label (same Win95 idiom as CheckBox).
        _focusRect = new Retro95DottedFocusRect(this, inset: 0f);
        _focusRect.Container.X = OuterSize + BoxToLabelGap - 2f;
        _focusRect.Container.XUnits = GeneralUnitType.PixelsFromSmall;
        _focusRect.Container.XOrigin = HorizontalAlignment.Left;
        _focusRect.Container.Width = -(OuterSize + BoxToLabelGap - 4f);
        _focusRect.Container.WidthUnits = DimensionUnitType.RelativeToParent;
        _focusRect.Container.Height = 0f;
        _focusRect.Container.HeightUnits = DimensionUnitType.RelativeToParent;

        WireStates();
    }

    private static CircleRuntime CreateCircle(string name, float size, bool filled, Color color)
    {
        CircleRuntime c = new CircleRuntime();
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
        if (filled)
        {
            c.FillColor = color;
            c.StrokeWidth = 0;
        }
        else
        {
            c.StrokeColor = color;
        }
        return c;
    }

    private static CircleRuntime CreateInnerDot()
    {
        const float inset = (OuterSize - InnerSize) / 2f;
        CircleRuntime dot = new CircleRuntime();
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
        dot.FillColor = Retro95Styling.ActiveStyle.Colors.Text;
        dot.StrokeWidth = 0;
        return dot;
    }

    private void WireStates()
    {
        // -------- Off --------
        States.EnabledOff.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: false, focus: false);
        States.HighlightedOff.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: false, focus: false);
        States.FocusedOff.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: false, focus: true);
        States.HighlightedFocusedOff.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: false, focus: true);
        States.PushedOff.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: false, focus: false);
        States.DisabledOff.Apply = () => Apply(white: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, dotVisible: false, focus: false);
        States.DisabledFocusedOff.Apply = () => Apply(white: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, dotVisible: false, focus: true);

        // -------- On --------
        States.EnabledOn.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: true, focus: false);
        States.HighlightedOn.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: true, focus: false);
        States.FocusedOn.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: true, focus: true);
        States.HighlightedFocusedOn.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: true, focus: true);
        States.PushedOn.Apply = () => Apply(white: true, text: Retro95Styling.ActiveStyle.Colors.Text, dotVisible: true, focus: false);
        States.DisabledOn.Apply = () => Apply(white: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, dotVisible: false, focus: false);
        States.DisabledFocusedOn.Apply = () => Apply(white: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, dotVisible: false, focus: true);
    }

    private void Apply(bool white, Color text, bool dotVisible, bool focus)
    {
        _outerFill.FillColor = white ? Retro95Styling.ActiveStyle.Colors.WhiteFill : Retro95Styling.ActiveStyle.Colors.Surface;
        TextInstance.Color = text;
        _innerDot.Visible = dotVisible;
        if (focus) _focusRect.Show(); else _focusRect.Hide();
    }
}
