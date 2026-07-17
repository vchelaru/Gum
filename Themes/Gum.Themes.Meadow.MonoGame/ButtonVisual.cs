using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled Button visual. A chunky sky-blue pill (corner radius ≈ height/2)
/// with a <b>hard</b> offset drop shadow — the signature cottagecore "0 4px 0"
/// solid bottom edge (blur 0, opaque blue-dark), not a soft Gaussian halo. The
/// focus ring is a 3 px sage stroke just outside the body.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    /// <summary>Corner radius for pill rendering — half the default 36 px height.</summary>
    private const float CornerRadius = 18f;

    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    /// <summary>
    /// Hard offset shadow, matching CSS <c>box-shadow: 0 4px 0 var(--blued)</c>.
    /// Unlike the soft Gaussian shadows elsewhere in Gum themes, this one is
    /// <b>opaque</b> with <c>blur = 0</c>, producing the flat "stacked card"
    /// bottom edge that defines Meadow's cozy chunky buttons.
    /// </summary>
    private const float ShadowOffsetY = 4f;
    private const float ShadowBlur = 0f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        Width = 120;
        Height = 36;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = MeadowStyling.ActiveStyle.Colors.White;

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowFill";
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
        fill.FillColor = MeadowStyling.ActiveStyle.Colors.Blue;
        fill.StrokeWidth = 0;
        // Hard offset shadow — flat, opaque, no blur. Toggled per state.
        fill.HasDropshadow = true;
        fill.DropshadowColor = MeadowStyling.ActiveStyle.Colors.BlueDark;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = ShadowOffsetY;
        fill.DropshadowBlur = ShadowBlur;
        return fill;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "MeadowFocusRing";
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
        States.Enabled.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Blue, text: MeadowStyling.ActiveStyle.Colors.White,
            showShadow: true, showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.BlueHover, text: MeadowStyling.ActiveStyle.Colors.White,
            showShadow: true, showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Blue, text: MeadowStyling.ActiveStyle.Colors.White,
            showShadow: true, showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.BlueHover, text: MeadowStyling.ActiveStyle.Colors.White,
            showShadow: true, showFocusRing: true);

        // Pressed sinks into the page: darker fill, shadow off (CSS drops the
        // 4 px edge and translates the button down on press).
        States.Pushed.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.BlueDark, text: MeadowStyling.ActiveStyle.Colors.White,
            showShadow: false, showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Disabled, text: MeadowStyling.ActiveStyle.Colors.Cream2,
            showShadow: false, showFocusRing: false);

        States.DisabledFocused.Apply = () => ApplyPalette(
            fill: MeadowStyling.ActiveStyle.Colors.Disabled, text: MeadowStyling.ActiveStyle.Colors.Cream2,
            showShadow: false, showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color text, bool showShadow, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _fill.HasDropshadow = showShadow;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
