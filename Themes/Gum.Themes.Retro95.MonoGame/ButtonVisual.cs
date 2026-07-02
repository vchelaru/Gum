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
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled Button visual. Battleship-gray fill, 2 px raised bevel (white/light
/// outer, dark gray inner — flips to sunken when pressed). Focus indicator is the
/// canonical Win95 1 px dotted black rectangle, inset 4 px from the body edge.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    /// <summary>Inset from the body edge to the dotted focus rectangle. Matches the CSS
    /// <c>outline-offset:-5px</c> (negative = inward).</summary>
    private const float FocusIndicatorInset = 4f;

    private readonly Retro95Bevel _bevel;
    private readonly Retro95DottedFocusRect _focusRect;

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
        _focusRect = new Retro95DottedFocusRect(this, FocusIndicatorInset);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = Retro95Styling.ActiveStyle.Colors.Text;

        WireStates();
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.Surface,
            text: Retro95Styling.ActiveStyle.Colors.Text, focus: false);

        States.Highlighted.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.SurfaceHover,
            text: Retro95Styling.ActiveStyle.Colors.Text, focus: false);

        States.Focused.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.Surface,
            text: Retro95Styling.ActiveStyle.Colors.Text, focus: true);

        States.HighlightedFocused.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.SurfaceHover,
            text: Retro95Styling.ActiveStyle.Colors.Text, focus: true);

        States.Pushed.Apply = () => Apply(
            bevelMode: BevelMode.Sunken, fill: Retro95Styling.ActiveStyle.Colors.Surface,
            text: Retro95Styling.ActiveStyle.Colors.Text, focus: false);

        States.Disabled.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.Surface,
            text: Retro95Styling.ActiveStyle.Colors.DisabledText, focus: false);

        States.DisabledFocused.Apply = () => Apply(
            bevelMode: BevelMode.Raised, fill: Retro95Styling.ActiveStyle.Colors.Surface,
            text: Retro95Styling.ActiveStyle.Colors.DisabledText, focus: true);
    }

    private void Apply(BevelMode bevelMode, Color fill, Color text, bool focus)
    {
        _bevel.SetMode(bevelMode);
        _bevel.SetFill(fill);
        TextInstance.Color = text;
        if (focus) _focusRect.Show();
        else _focusRect.Hide();
    }
}
