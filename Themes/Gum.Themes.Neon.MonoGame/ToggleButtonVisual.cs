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

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled ToggleButton visual. Pill-shape mirroring
/// <see cref="ButtonVisual"/> — Off variants paint white with a 2 px pink border
/// and pink text; On variants paint accent-filled with white text so the toggle
/// reads as active.
/// </summary>
public class ToggleButtonVisual : BaseToggleButtonVisual
{
    private const float CornerRadius = 1f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 4f;
    private const float FocusRingThickness = 1f;

    /// <summary>
    /// Cyan Gaussian halo, matching <see cref="ButtonVisual"/> so the two
    /// pill controls read as one family. Toggled per state via
    /// <c>_fill.HasDropshadow</c> (off when disabled).
    /// </summary>
    private const float ShadowOffsetY = 0f;
    private const float ShadowBlur = 36f;

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

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        // Focus ring is added AFTER the fill so the body's dropshadow halo
        // (which paints during the fill's draw call) doesn't alpha-blend over
        // it and dim the white stroke. The ring lives entirely outside the
        // body's pixel bounds, so painting it on top doesn't obscure content.
        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "NeonToggleFill";
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
        fill.FillColor = NeonStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        fill.HasDropshadow = true;
        fill.DropshadowColor = NeonStyling.ActiveStyle.Colors.GlowStrong;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = ShadowOffsetY;
        fill.DropshadowBlur = ShadowBlur;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "NeonToggleBorder";
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
        border.StrokeColor = NeonStyling.ActiveStyle.Colors.Border;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "NeonToggleFocusRing";
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
        ring.StrokeColor = NeonStyling.ActiveStyle.Colors.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        // Off variants: white pill with pink border, pink text. Shadow on
        // except for pressed/disabled, matching the Button state pattern.
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Border,
            text: NeonStyling.ActiveStyle.Colors.Text, showShadow: true, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Accent,
            text: NeonStyling.ActiveStyle.Colors.Text, showShadow: true, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Accent,
            text: NeonStyling.ActiveStyle.Colors.Text, showShadow: false, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Accent,
            text: NeonStyling.ActiveStyle.Colors.Text, showShadow: true, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Accent,
            text: NeonStyling.ActiveStyle.Colors.Text, showShadow: true, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Disabled, border: NeonStyling.ActiveStyle.Colors.Disabled,
            text: NeonStyling.ActiveStyle.Colors.Disabled, showShadow: false, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Disabled, border: NeonStyling.ActiveStyle.Colors.Disabled,
            text: NeonStyling.ActiveStyle.Colors.Disabled, showShadow: false, showFocusRing: true);

        // On variants: solid accent body, dark text against the bright cyan.
        // Hover/focus do NOT modulate the fill (the prior translucent-on-hover
        // approach let the dropshadow halo bleed through, producing a magenta
        // tinge against the leftover pink shadow). State emphasis comes from
        // the focus ring + the (now cyan, not pink) glow.
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Accent, border: NeonStyling.ActiveStyle.Colors.Accent,
            text: NeonStyling.ActiveStyle.Colors.Background, showShadow: true, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Accent, border: NeonStyling.ActiveStyle.Colors.Accent,
            text: NeonStyling.ActiveStyle.Colors.Background, showShadow: true, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Accent, border: NeonStyling.ActiveStyle.Colors.Accent,
            text: NeonStyling.ActiveStyle.Colors.Background, showShadow: false, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Accent, border: NeonStyling.ActiveStyle.Colors.Accent,
            text: NeonStyling.ActiveStyle.Colors.Background, showShadow: true, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Accent, border: NeonStyling.ActiveStyle.Colors.Accent,
            text: NeonStyling.ActiveStyle.Colors.Background, showShadow: true, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Disabled, border: NeonStyling.ActiveStyle.Colors.Disabled,
            text: new Color(255, 255, 255), showShadow: false, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: NeonStyling.ActiveStyle.Colors.Disabled, border: NeonStyling.ActiveStyle.Colors.Disabled,
            text: new Color(255, 255, 255), showShadow: false, showFocusRing: true);
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
