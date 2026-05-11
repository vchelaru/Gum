using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled Button visual. Battleship-gray fill, 2 px raised bevel (white/light
/// outer, dark gray inner — flips to sunken when pressed). Focus indicator is a 1 px
/// dotted-feel inset (approximated with a solid 1 px dark rectangle, since no dotted
/// stroke exists in the runtime). Matches <c>.rc-btn</c> from the CSS.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    /// <summary>Inset from the body edge to the dotted focus rectangle. Matches the CSS
    /// <c>outline-offset:-5px</c> (negative = inward).</summary>
    private const float FocusIndicatorInset = 4f;
    private const float FocusIndicatorThickness = 1f;

    private readonly Retro95Bevel _bevel;
    private readonly ColoredRectangleRuntime _focusIndicatorTop;
    private readonly ColoredRectangleRuntime _focusIndicatorBottom;
    private readonly ColoredRectangleRuntime _focusIndicatorLeft;
    private readonly ColoredRectangleRuntime _focusIndicatorRight;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        Width = 96;
        Height = 24;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Raised);

        _focusIndicatorTop = CreateFocusStrip(top: true, vertical: false);
        _focusIndicatorBottom = CreateFocusStrip(top: false, vertical: false);
        _focusIndicatorLeft = CreateFocusStrip(top: true, vertical: true);
        _focusIndicatorRight = CreateFocusStrip(top: false, vertical: true);
        AddChild(_focusIndicatorTop);
        AddChild(_focusIndicatorBottom);
        AddChild(_focusIndicatorLeft);
        AddChild(_focusIndicatorRight);
        SetFocusIndicatorVisible(false);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = Retro95Colors.Text;

        WireStates();
    }

    private ColoredRectangleRuntime CreateFocusStrip(bool top, bool vertical)
    {
        ColoredRectangleRuntime r = new ColoredRectangleRuntime();
        r.Name = "Retro95FocusStrip";
        r.Color = Retro95Colors.Text;
        if (!vertical)
        {
            // top / bottom horizontal strip
            r.X = 0;
            r.Y = top ? FocusIndicatorInset : -FocusIndicatorInset;
            r.XUnits = GeneralUnitType.PixelsFromMiddle;
            r.YUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            r.XOrigin = HorizontalAlignment.Center;
            r.YOrigin = top ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            r.Width = -(FocusIndicatorInset * 2f);
            r.Height = FocusIndicatorThickness;
            r.WidthUnits = DimensionUnitType.RelativeToParent;
            r.HeightUnits = DimensionUnitType.Absolute;
        }
        else
        {
            // left / right vertical strip
            r.X = top ? FocusIndicatorInset : -FocusIndicatorInset;
            r.Y = 0;
            r.XUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            r.YUnits = GeneralUnitType.PixelsFromMiddle;
            r.XOrigin = top ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            r.YOrigin = VerticalAlignment.Center;
            r.Width = FocusIndicatorThickness;
            r.Height = -(FocusIndicatorInset * 2f);
            r.WidthUnits = DimensionUnitType.Absolute;
            r.HeightUnits = DimensionUnitType.RelativeToParent;
        }
        return r;
    }

    private void SetFocusIndicatorVisible(bool visible)
    {
        _focusIndicatorTop.Visible = visible;
        _focusIndicatorBottom.Visible = visible;
        _focusIndicatorLeft.Visible = visible;
        _focusIndicatorRight.Visible = visible;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.Surface,
            text: Retro95Colors.Text, focus: false);

        States.Highlighted.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.SurfaceHover,
            text: Retro95Colors.Text, focus: false);

        States.Focused.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.Surface,
            text: Retro95Colors.Text, focus: true);

        States.HighlightedFocused.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.SurfaceHover,
            text: Retro95Colors.Text, focus: true);

        States.Pushed.Apply = () => Apply(
            bevelMode: BevelMode.Sunken, fill: Retro95Colors.Surface,
            text: Retro95Colors.Text, focus: false);

        States.Disabled.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.Surface,
            text: Retro95Colors.DisabledText, focus: false);

        States.DisabledFocused.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Colors.Surface,
            text: Retro95Colors.DisabledText, focus: true);
    }

    private void Apply(BevelMode bevelMode, Color fill, Color text, bool focus)
    {
        _bevel.SetMode(bevelMode);
        _bevel.SetFill(fill);
        TextInstance.Color = text;
        SetFocusIndicatorVisible(focus);
    }
}
