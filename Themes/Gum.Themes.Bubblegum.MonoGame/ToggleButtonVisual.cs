using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
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
            color: BubblegumPalette.FocusRing,
            cornerRadius: CornerRadius,
            inset: FocusRingInset,
            thickness: FocusRingThickness,
            name: "BubblegumToggleFocusRing");
        AddChild(_focusRing);

        _fill = BubblegumShapes.FillWithDropshadow(
            color: BubblegumColors.Surface1,
            cornerRadius: CornerRadius,
            shadowColor: ShadowColor,
            offsetX: 0f,
            offsetY: ShadowOffsetY,
            blur: ShadowBlur,
            name: "BubblegumToggleFill");
        AddChild(_fill);

        _border = BubblegumShapes.Border(
            color: BubblegumColors.Border,
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
            fill: BubblegumColors.Surface1, border: BubblegumColors.Border,
            text: BubblegumColors.Text, showShadow: true, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, showShadow: true, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: BubblegumColors.Surface1, border: BubblegumColors.AccentDark,
            text: BubblegumColors.Text, showShadow: false, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, showShadow: true, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, showShadow: true, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled,
            text: BubblegumColors.Disabled, showShadow: false, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled,
            text: BubblegumColors.Disabled, showShadow: false, showFocusRing: true);

        // On variants: accent-filled body, white text.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: BubblegumColors.Accent, border: BubblegumColors.Accent,
            text: Color.White, showShadow: true, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: BubblegumColors.AccentHover, border: BubblegumColors.AccentHover,
            text: Color.White, showShadow: true, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: BubblegumColors.AccentDark, border: BubblegumColors.AccentDark,
            text: Color.White, showShadow: false, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: BubblegumColors.Accent, border: BubblegumColors.Accent,
            text: Color.White, showShadow: true, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: BubblegumColors.AccentHover, border: BubblegumColors.AccentHover,
            text: Color.White, showShadow: true, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: BubblegumColors.Disabled, border: BubblegumColors.Disabled,
            text: Color.White, showShadow: false, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: BubblegumColors.Disabled, border: BubblegumColors.Disabled,
            text: Color.White, showShadow: false, showFocusRing: true);
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
