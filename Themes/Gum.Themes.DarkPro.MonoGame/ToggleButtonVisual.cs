using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseToggleButtonVisual = Gum.Forms.DefaultVisuals.V3.ToggleButtonVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled ToggleButton visual. Same three-layer shape stack as
/// <see cref="ButtonVisual"/> (focus ring + fill + 1 px border). The "On" /
/// pressed-stay variants paint with the Accent palette so the toggle reads as
/// active; the "Off" variants match the standard Button look.
/// </summary>
public class ToggleButtonVisual : BaseToggleButtonVisual
{
    private const float CornerRadius = 2f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;

    public ToggleButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach V3's NineSlice background + underline focus indicator. Text
        // label stays but is re-parented last so the new shape layers render
        // behind it.
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        Width = 96;
        Height = 32;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "DarkProToggleFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.CornerRadius = CornerRadius;
        fill.IsFilled = true;
        fill.Color = DarkProColors.Surface1;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "DarkProToggleBorder";
        border.X = 0;
        border.Y = 0;
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.CornerRadius = CornerRadius;
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.Color = DarkProColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "DarkProToggleFocusRing";
        ring.X = 0;
        ring.Y = 0;
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = FocusRingInset * 2f;
        ring.Height = FocusRingInset * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = CornerRadius + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = BorderThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = DarkProColors.Accent;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        // Off variants: same palette as the standard Dark Pro Button.
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: DarkProColors.Surface1, border: DarkProColors.Border,
            text: DarkProColors.Text, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: DarkProColors.Surface2, border: DarkProColors.Accent,
            text: DarkProColors.Text, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: DarkProColors.PressedFill, border: DarkProColors.Accent,
            text: DarkProColors.Text, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: DarkProColors.Surface1, border: DarkProColors.Accent,
            text: DarkProColors.Text, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: DarkProColors.Surface2, border: DarkProColors.Accent,
            text: DarkProColors.Text, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: DarkProColors.DisabledFill, border: DarkProColors.DisabledBorder,
            text: DarkProColors.DisabledText, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: DarkProColors.DisabledFill, border: DarkProColors.DisabledBorder,
            text: DarkProColors.DisabledText, showFocusRing: true);

        // On variants: accent-filled body so the active state is unmistakable.
        // Text flips to PressedText (a light-blue from the source mockup) for
        // legibility against the saturated accent fill.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: DarkProColors.Accent, border: DarkProColors.Accent,
            text: DarkProColors.PressedText, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: DarkProColors.HoverAccent, border: DarkProColors.HoverAccent,
            text: DarkProColors.PressedText, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: DarkProColors.AccentPressed, border: DarkProColors.AccentPressed,
            text: DarkProColors.PressedText, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: DarkProColors.Accent, border: DarkProColors.Accent,
            text: DarkProColors.PressedText, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: DarkProColors.HoverAccent, border: DarkProColors.HoverAccent,
            text: DarkProColors.PressedText, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: DarkProColors.DisabledFill, border: DarkProColors.DisabledBorder,
            text: DarkProColors.DisabledText, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: DarkProColors.DisabledFill, border: DarkProColors.DisabledBorder,
            text: DarkProColors.DisabledText, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing)
    {
        _fill.Color = fill;
        _border.Color = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
