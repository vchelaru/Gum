using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseToggleButtonVisual = Gum.Forms.DefaultVisuals.V3.ToggleButtonVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled ToggleButton. Same leaf-large pill silhouette as
/// <see cref="ButtonVisual"/>. Off variants paint a darker canopy fill
/// (matches the disabled-ish base look so On reads as the active toggle),
/// On variants paint the leaf-bright accent body with dark text for
/// readable contrast.
/// </summary>
public class ToggleButtonVisual : BaseToggleButtonVisual
{
    private const float BorderThickness = 1f;
    private const float FocusRingThickness = 3f;
    private const float FocusRingInset = 3f;
    private const float ShadowBlur = 22f;

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

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "ForestGladeToggleFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyLarge(fill);
        fill.IsFilled = true;
        fill.Color = ForestGladePalette.ButtonRestFill;
        fill.HasDropshadow = true;
        fill.DropshadowColor = ForestGladePalette.GlowMedium;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = 0f;
        fill.DropshadowBlurX = ShadowBlur;
        fill.DropshadowBlurY = ShadowBlur;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "ForestGladeToggleBorder";
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyLarge(border);
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.Color = ForestGladeColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "ForestGladeToggleFocusRing";
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = FocusRingInset * 2f;
        ring.Height = FocusRingInset * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = 4f + FocusRingInset;
        ring.CustomRadiusTopLeft = 4f + FocusRingInset;
        ring.CustomRadiusTopRight = 18f + FocusRingInset;
        ring.CustomRadiusBottomRight = 4f + FocusRingInset;
        ring.CustomRadiusBottomLeft = 18f + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = ForestGladeColors.SunPale * 0.45f;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        Color offFill = ForestGladePalette.ButtonDisabledFill; // muted moss for "off"
        Color offBorder = ForestGladeColors.Border;
        Color onFill = ForestGladeColors.LeafBright;
        Color onBorder = ForestGladeColors.SunPale;
        Color disabledFill = new Color(28, 44, 36);
        Color disabledBorder = new Color(232, 255, 117, 26);

        // -------- Off --------
        States.EnabledOff.Apply = () => ApplyPalette(
            fill: offFill, border: offBorder,
            text: ForestGladeColors.Muted, showShadow: false, showFocusRing: false);

        States.HighlightedOff.Apply = () => ApplyPalette(
            fill: offFill, border: ForestGladeColors.BorderHover,
            text: ForestGladeColors.Text, showShadow: false, showFocusRing: false);

        States.PushedOff.Apply = () => ApplyPalette(
            fill: offFill, border: ForestGladeColors.BorderHover,
            text: ForestGladeColors.Text, showShadow: false, showFocusRing: false);

        States.FocusedOff.Apply = () => ApplyPalette(
            fill: offFill, border: ForestGladeColors.BorderHover,
            text: ForestGladeColors.Text, showShadow: false, showFocusRing: true);

        States.HighlightedFocusedOff.Apply = () => ApplyPalette(
            fill: offFill, border: ForestGladeColors.BorderHover,
            text: ForestGladeColors.Text, showShadow: false, showFocusRing: true);

        States.DisabledOff.Apply = () => ApplyPalette(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, showShadow: false, showFocusRing: false);

        States.DisabledFocusedOff.Apply = () => ApplyPalette(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, showShadow: false, showFocusRing: true);

        // -------- On --------
        // Active toggle: bright leaf-green fill, dark canopy text for contrast.
        Color onText = new Color(5, 63, 31);
        States.EnabledOn.Apply = () => ApplyPalette(
            fill: onFill, border: onBorder,
            text: onText, showShadow: true, showFocusRing: false);

        States.HighlightedOn.Apply = () => ApplyPalette(
            fill: onFill, border: onBorder,
            text: onText, showShadow: true, showFocusRing: false);

        States.PushedOn.Apply = () => ApplyPalette(
            fill: ForestGladePalette.ButtonPushedFill, border: onBorder,
            text: new Color(214, 245, 176), showShadow: false, showFocusRing: false);

        States.FocusedOn.Apply = () => ApplyPalette(
            fill: onFill, border: onBorder,
            text: onText, showShadow: true, showFocusRing: true);

        States.HighlightedFocusedOn.Apply = () => ApplyPalette(
            fill: onFill, border: onBorder,
            text: onText, showShadow: true, showFocusRing: true);

        States.DisabledOn.Apply = () => ApplyPalette(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, showShadow: false, showFocusRing: false);

        States.DisabledFocusedOn.Apply = () => ApplyPalette(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, showShadow: false, showFocusRing: true);
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
