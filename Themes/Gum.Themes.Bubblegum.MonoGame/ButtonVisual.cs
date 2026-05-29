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
    /// Drop shadow rendered natively by Apos.Shapes — matches the CSS
    /// <c>box-shadow: 0 3px 10px rgba(255,107,157,.4)</c>. OffsetY=3 pushes the
    /// shadow below the body, BlurX/Y=10 produces the soft Gaussian falloff,
    /// alpha 102 ≈ 40% (matches the CSS .4). Toggled per state via
    /// <c>_fill.HasDropshadow</c>.
    /// </summary>
    // Pink-tinted Gaussian halo. CSS spec is 0 3px 10px rgba(255,107,157,.4)
    // but ~40% alpha read nearly invisible in practice against the pastel page
    // background. Bumped to alpha 160 (~62%) and blur 12 for a casual-game
    // "lift" reading without crossing into harsh-shadow territory.
    private const float ShadowOffsetY = 3f;
    private const float ShadowBlur = 12f;
    private static readonly Color ShadowColor = new Color(255, 107, 157, 160);

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;

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

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = Color.White;

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
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
        fill.FillColor = BubblegumColors.Accent;
        fill.StrokeWidth = 0;
        // Native Gaussian drop shadow — replaces the old three-layer stack.
        // Toggled per state via WireStates (off when pressed/disabled).
        fill.HasDropshadow = true;
        fill.DropshadowColor = ShadowColor;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = ShadowOffsetY;
        fill.DropshadowBlur = ShadowBlur;
        return fill;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        // Sized to (parent + 2*FocusRingInset) per axis, stroked with
        // FocusRingThickness translucent accent. Visible only when focused.
        RectangleRuntime ring = new RectangleRuntime();
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
        ring.StrokeColor = new Color(255, 107, 157, 90); // translucent accent
        ring.Visible = false;
        return ring;
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
        _fill.FillColor = fill;
        _fill.HasDropshadow = showShadow;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
