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

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

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

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
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
        fill.FillColor = DarkProStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
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
        border.StrokeColor = DarkProStyling.ActiveStyle.Colors.Border;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
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
        ring.StrokeColor = DarkProStyling.ActiveStyle.Colors.Accent;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        // Off variants: same palette as the standard Dark Pro Button.
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface1, border: DarkProStyling.ActiveStyle.Colors.Border,
            text: DarkProStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface2, border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.PressedFill, border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.Text, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface1, border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.Text, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface2, border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.Text, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.DisabledFill, border: DarkProStyling.ActiveStyle.Colors.DisabledBorder,
            text: DarkProStyling.ActiveStyle.Colors.DisabledText, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.DisabledFill, border: DarkProStyling.ActiveStyle.Colors.DisabledBorder,
            text: DarkProStyling.ActiveStyle.Colors.DisabledText, showFocusRing: true);

        // On variants: accent-filled body so the active state is unmistakable.
        // Text flips to PressedText (a light-blue from the source mockup) for
        // legibility against the saturated accent fill.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Accent, border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.HoverAccent, border: DarkProStyling.ActiveStyle.Colors.HoverAccent,
            text: DarkProStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.AccentPressed, border: DarkProStyling.ActiveStyle.Colors.AccentPressed,
            text: DarkProStyling.ActiveStyle.Colors.PressedText, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Accent, border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.PressedText, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.HoverAccent, border: DarkProStyling.ActiveStyle.Colors.HoverAccent,
            text: DarkProStyling.ActiveStyle.Colors.PressedText, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.DisabledFill, border: DarkProStyling.ActiveStyle.Colors.DisabledBorder,
            text: DarkProStyling.ActiveStyle.Colors.DisabledText, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.DisabledFill, border: DarkProStyling.ActiveStyle.Colors.DisabledBorder,
            text: DarkProStyling.ActiveStyle.Colors.DisabledText, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
