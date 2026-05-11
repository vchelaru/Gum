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

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;

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

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "BubblegumToggleFill";
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
        fill.Color = BubblegumColors.Surface1;
        fill.HasDropshadow = true;
        fill.DropshadowColor = ShadowColor;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = ShadowOffsetY;
        fill.DropshadowBlurX = ShadowBlur;
        fill.DropshadowBlurY = ShadowBlur;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "BubblegumToggleBorder";
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
        border.Color = BubblegumColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "BubblegumToggleFocusRing";
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
        ring.Color = BubblegumPalette.FocusRing;
        ring.Visible = false;
        return ring;
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
        _fill.Color = fill;
        _fill.HasDropshadow = showShadow;
        _border.Color = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
