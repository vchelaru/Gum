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

        _focusRing = BubblegumShapes.FocusRing(
            color: new Color(255, 107, 157, 90), // translucent accent
            cornerRadius: CornerRadius,
            inset: FocusRingInset,
            thickness: FocusRingThickness,
            name: "BubblegumFocusRing");
        AddChild(_focusRing);

        _fill = BubblegumShapes.FillWithDropshadow(
            color: BubblegumStyling.ActiveStyle.Colors.Accent,
            cornerRadius: CornerRadius,
            shadowColor: ShadowColor,
            offsetX: 0f,
            offsetY: ShadowOffsetY,
            blur: ShadowBlur,
            name: "BubblegumFill");
        AddChild(_fill);

        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = new Color(255, 255, 255);

        WireStates();
    }

    private void WireStates()
    {
        // Replace (don't append to) the base callbacks. The base
        // SetValuesForState targets the now-detached NineSlice background and
        // underline FocusedIndicator, neither of which exists in this visual's
        // render tree any more.
        States.Enabled.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: new Color(255, 255, 255),
            showShadow: true,
            showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.AccentHover,
            text: new Color(255, 255, 255),
            showShadow: true,
            showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: new Color(255, 255, 255),
            showShadow: true,
            showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.AccentHover,
            text: new Color(255, 255, 255),
            showShadow: true,
            showFocusRing: true);

        States.Pushed.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.AccentDark,
            text: new Color(255, 255, 255),
            showShadow: false,
            showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: new Color(255, 255, 255),
            showShadow: false,
            showFocusRing: false);

        States.DisabledFocused.Apply = () => ApplyPalette(
            fill: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: new Color(255, 255, 255),
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
