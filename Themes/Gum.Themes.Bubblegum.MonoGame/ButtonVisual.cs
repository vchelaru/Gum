using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled Button visual. Pill-shape (corner radius ≈ height/2), no
/// outline border at rest, accent-pink filled body, and a soft three-layer
/// drop shadow approximating the CSS box-shadow from the source mockup. The
/// focus ring is a translucent 3 px stroke painted just outside the body.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    /// <summary>
    /// Corner radius for pill rendering. Set to half the button height so the
    /// rounded rect collapses to a perfect pill. Bubblegum buttons are 36 px
    /// tall by default (matches the source mockup's `padding:8px 22px` after
    /// font ascent). If a consumer resizes the button, the pill stays at 18 px
    /// radius — slightly more "rounded rect" than "pill," but still on-brand.
    /// Promote to a height-tracking computed value if dynamic sizing matters.
    /// </summary>
    private const float CornerRadius = 18f;

    /// <summary>Focus ring inset and stroke (matches CSS `0 0 0 3px`).</summary>
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    /// <summary>
    /// Drop-shadow approximation. The CSS `box-shadow: 0 3px 10px rgba(...,.4)`
    /// is a soft Gaussian; Apos.Shapes can't render Gaussian blurs directly,
    /// so we stack three progressively-larger, progressively-fainter rounded
    /// rects beneath the body. Three layers is the sweet spot — two reads
    /// flat, four reads excessive at this size.
    /// </summary>
    private const float ShadowOffsetY = 3f;
    private const float ShadowSpread1 = 0f;
    private const float ShadowSpread2 = 4f;
    private const float ShadowSpread3 = 8f;
    private static readonly Color ShadowColor1 = new Color(255, 107, 157, 70);
    private static readonly Color ShadowColor2 = new Color(255, 107, 157, 40);
    private static readonly Color ShadowColor3 = new Color(255, 107, 157, 20);

    private readonly RoundedRectangleRuntime _shadow1;
    private readonly RoundedRectangleRuntime _shadow2;
    private readonly RoundedRectangleRuntime _shadow3;
    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the V3 NineSlice background and underline focus indicator.
        // The text label stays but is re-parented last so the new shape layers
        // render behind it.
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        Width = 120;
        Height = 36;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        // Stack bottom-up: outermost (faintest) shadow first so it renders
        // behind the smaller, brighter shadows above it.
        _shadow3 = CreateShadow(ShadowSpread3, ShadowColor3);
        AddChild(_shadow3);
        _shadow2 = CreateShadow(ShadowSpread2, ShadowColor2);
        AddChild(_shadow2);
        _shadow1 = CreateShadow(ShadowSpread1, ShadowColor1);
        AddChild(_shadow1);

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = Color.White;

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "BubblegumFill";
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
        fill.Color = BubblegumColors.Accent;
        return fill;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        // Sized to (parent + 2*FocusRingInset) per axis, stroked with
        // FocusRingThickness translucent accent. Visible only when focused.
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "BubblegumFocusRing";
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
        ring.Color = new Color(255, 107, 157, 90); // translucent accent
        ring.Visible = false;
        return ring;
    }

    private static RoundedRectangleRuntime CreateShadow(float spread, Color color)
    {
        RoundedRectangleRuntime shadow = new RoundedRectangleRuntime();
        shadow.Name = "BubblegumShadow";
        shadow.X = 0;
        shadow.Y = ShadowOffsetY;
        shadow.XUnits = GeneralUnitType.PixelsFromMiddle;
        shadow.YUnits = GeneralUnitType.PixelsFromMiddle;
        shadow.XOrigin = HorizontalAlignment.Center;
        shadow.YOrigin = VerticalAlignment.Center;
        shadow.Width = spread * 2f;
        shadow.Height = spread * 2f;
        shadow.WidthUnits = DimensionUnitType.RelativeToParent;
        shadow.HeightUnits = DimensionUnitType.RelativeToParent;
        shadow.CornerRadius = CornerRadius + spread;
        shadow.IsFilled = true;
        shadow.Color = color;
        return shadow;
    }

    private void WireStates()
    {
        // Replace (don't append to) the base callbacks. The base
        // SetValuesForState targets the now-detached NineSlice background and
        // underline FocusedIndicator, neither of which exists in this visual's
        // render tree any more.
        States.Enabled.Apply = () => ApplyPalette(
            fill: BubblegumColors.Accent,
            text: Color.White,
            showShadow: true,
            showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: BubblegumColors.AccentHover,
            text: Color.White,
            showShadow: true,
            showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            fill: BubblegumColors.Accent,
            text: Color.White,
            showShadow: true,
            showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            fill: BubblegumColors.AccentHover,
            text: Color.White,
            showShadow: true,
            showFocusRing: true);

        States.Pushed.Apply = () => ApplyPalette(
            fill: BubblegumColors.AccentDark,
            text: Color.White,
            showShadow: false,
            showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            fill: BubblegumColors.Disabled,
            text: Color.White,
            showShadow: false,
            showFocusRing: false);

        States.DisabledFocused.Apply = () => ApplyPalette(
            fill: BubblegumColors.Disabled,
            text: Color.White,
            showShadow: false,
            showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color text, bool showShadow, bool showFocusRing)
    {
        _fill.Color = fill;
        TextInstance.Color = text;
        _shadow1.Visible = showShadow;
        _shadow2.Visible = showShadow;
        _shadow3.Visible = showShadow;
        _focusRing.Visible = showFocusRing;
    }
}
