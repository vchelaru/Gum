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

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled ToggleButton visual. Pill-shape mirroring
/// <see cref="ButtonVisual"/> — Off variants paint white with a 2 px pink border
/// and pink text; On variants paint accent-filled with white text so the toggle
/// reads as active.
/// </summary>
public class ToggleButtonVisual : BaseToggleButtonVisual
{
    private const float CornerRadius = 16f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    /// <summary>
    /// Pink-tinted Gaussian halo, matching <see cref="ButtonVisual"/> so the
    /// two pill controls read as one family. Toggled per state via
    /// <c>_fill.HasDropshadow</c> (off when pressed / disabled).
    /// </summary>
    private const float ShadowOffsetY = 3f;
    private const float ShadowBlur = 12f;
    private static readonly Color ShadowColor = new Color(255, 107, 157, 160);

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

        _focusRing = BubblegumShapes.FocusRing(
            color: BubblegumStyling.ActiveStyle.Colors.FocusRing,
            cornerRadius: CornerRadius,
            inset: FocusRingInset,
            thickness: FocusRingThickness,
            name: "BubblegumToggleFocusRing");
        AddChild(_focusRing);

        _fill = BubblegumShapes.FillWithDropshadow(
            color: BubblegumStyling.ActiveStyle.Colors.Surface1,
            cornerRadius: CornerRadius,
            shadowColor: ShadowColor,
            offsetX: 0f,
            offsetY: ShadowOffsetY,
            blur: ShadowBlur,
            name: "BubblegumToggleFill");
        AddChild(_fill);

        _border = BubblegumShapes.Border(
            color: BubblegumStyling.ActiveStyle.Colors.Border,
            cornerRadius: CornerRadius,
            thickness: BorderThickness,
            name: "BubblegumToggleBorder");
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private void WireStates()
    {
        // Off variants: white pill with pink border, pink text. Shadow on
        // except for pressed/disabled, matching the Button state pattern.
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Border,
            text: BubblegumStyling.ActiveStyle.Colors.Text, showShadow: true, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, showShadow: true, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.AccentDark,
            text: BubblegumStyling.ActiveStyle.Colors.Text, showShadow: false, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, showShadow: true, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, showShadow: true, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.DisabledFill, border: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: BubblegumStyling.ActiveStyle.Colors.Disabled, showShadow: false, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.DisabledFill, border: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: BubblegumStyling.ActiveStyle.Colors.Disabled, showShadow: false, showFocusRing: true);

        // On variants: accent-filled body, white text.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Accent, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.White, showShadow: true, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.AccentHover, border: BubblegumStyling.ActiveStyle.Colors.AccentHover,
            text: BubblegumStyling.ActiveStyle.Colors.White, showShadow: true, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.AccentDark, border: BubblegumStyling.ActiveStyle.Colors.AccentDark,
            text: BubblegumStyling.ActiveStyle.Colors.White, showShadow: false, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Accent, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.White, showShadow: true, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.AccentHover, border: BubblegumStyling.ActiveStyle.Colors.AccentHover,
            text: BubblegumStyling.ActiveStyle.Colors.White, showShadow: true, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Disabled, border: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: BubblegumStyling.ActiveStyle.Colors.White, showShadow: false, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Disabled, border: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: BubblegumStyling.ActiveStyle.Colors.White, showShadow: false, showFocusRing: true);
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
