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

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled ToggleButton visual. Off variants paint a white pill with a
/// 2.5 px peach border and teal text; On variants paint a sky-blue pill with the
/// hard offset shadow (matching <see cref="ButtonVisual"/>) and white text, so an
/// active toggle reads as a lit-up Meadow button.
/// </summary>
public class ToggleButtonVisual : BaseToggleButtonVisual
{
    private const float CornerRadius = 16f;
    private const float BorderThickness = 2.5f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    private const float ShadowOffsetY = 4f;
    private const float ShadowBlur = 0f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ToggleButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
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
        fill.Name = "MeadowToggleFill";
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
        fill.FillColor = MeadowStyling.ActiveStyle.Colors.White;
        fill.StrokeWidth = 0;
        fill.HasDropshadow = false;
        fill.DropshadowColor = MeadowStyling.ActiveStyle.Colors.BlueDark;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = ShadowOffsetY;
        fill.DropshadowBlur = ShadowBlur;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "MeadowToggleBorder";
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
        border.StrokeColor = MeadowStyling.ActiveStyle.Colors.PeachDark;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "MeadowToggleFocusRing";
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
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = MeadowStyling.ActiveStyle.Colors.SageDark;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        // Off: white pill, peach border, teal text, no shadow (flat outline).
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.PeachDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, showShadow: false, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, showShadow: false, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.Teal,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, showShadow: false, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, showShadow: false, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, showShadow: false, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, showShadow: false, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, showShadow: false, showFocusRing: true);

        // On: blue pill with the hard offset shadow, white text.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Blue, border: MeadowStyling.ActiveStyle.Colors.Blue,
            text: MeadowStyling.ActiveStyle.Colors.White, showShadow: true, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.BlueHover, border: MeadowStyling.ActiveStyle.Colors.BlueHover,
            text: MeadowStyling.ActiveStyle.Colors.White, showShadow: true, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.BlueDark, border: MeadowStyling.ActiveStyle.Colors.BlueDark,
            text: MeadowStyling.ActiveStyle.Colors.White, showShadow: false, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Blue, border: MeadowStyling.ActiveStyle.Colors.Blue,
            text: MeadowStyling.ActiveStyle.Colors.White, showShadow: true, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.BlueHover, border: MeadowStyling.ActiveStyle.Colors.BlueHover,
            text: MeadowStyling.ActiveStyle.Colors.White, showShadow: true, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Disabled, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.Cream2, showShadow: false, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Disabled, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.Cream2, showShadow: false, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showShadow, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _fill.HasDropshadow = showShadow;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
