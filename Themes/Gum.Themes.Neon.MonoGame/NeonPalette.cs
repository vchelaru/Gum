using Microsoft.Xna.Framework;

namespace Gum.Themes.Neon;

/// <summary>
/// Derived color values used across multiple Neon visuals. Kept separate from
/// <see cref="NeonColors"/> so the canonical CSS-token palette stays 1:1 with
/// the source mockup, and translucent / state-derived colors live here.
/// </summary>
internal static class NeonPalette
{
    /// <summary>
    /// Translucent cyan used for the Button hover fill — CSS
    /// <c>rgba(0,229,255,.07)</c> (alpha ≈ 18).
    /// </summary>
    public static readonly Color ButtonHoverFill = new Color(0, 229, 255, 18);

    /// <summary>
    /// Pushed-state Button fill — CSS <c>rgba(0,229,255,.18)</c> (alpha ≈ 46).
    /// </summary>
    public static readonly Color ButtonPushedFill = new Color(0, 229, 255, 46);

    /// <summary>
    /// CheckBox checked fill — same alpha as <see cref="NeonColors.AccentDim"/>
    /// (CSS <c>--accd</c>), restated here for symmetry with other state tints.
    /// </summary>
    public static readonly Color CheckedFill = new Color(0, 229, 255, 31);

    /// <summary>
    /// CheckBox pushed-while-checked — CSS <c>rgba(0,229,255,.25)</c>.
    /// </summary>
    public static readonly Color CheckedPushedFill = new Color(0, 229, 255, 64);

    /// <summary>
    /// Slider thumb pushed fill — same as <see cref="CheckedPushedFill"/>.
    /// </summary>
    public static readonly Color SliderThumbPushed = new Color(0, 229, 255, 64);

    /// <summary>
    /// ScrollBar thumb fill — CSS <c>rgba(0,229,255,.15)</c>.
    /// </summary>
    public static readonly Color ScrollThumb = new Color(0, 229, 255, 38);

    /// <summary>
    /// Hover background tint for ListBox / Menu rows — CSS uses
    /// <c>--s2</c> on <c>.nc-lb-item.hov</c> (raised surface tier).
    /// </summary>
    public static readonly Color HoverRow = NeonColors.Surface2;

    /// <summary>
    /// Selected row fill — translucent accent (<c>--accd</c>).
    /// </summary>
    public static readonly Color SelectedRow = NeonColors.AccentDim;

    /// <summary>
    /// Hot-pink translucent fill on the Window close button — CSS
    /// <c>rgba(255,0,153,.15)</c>.
    /// </summary>
    public static readonly Color CloseFill = new Color(255, 0, 153, 38);

    /// <summary>
    /// Glow shadow color at the default alpha used by most stationary
    /// visuals (matches CSS <c>rgba(0,229,255,.5)</c>).
    /// </summary>
    public static readonly Color GlowMedium = new Color(0, 229, 255, 128);

    /// <summary>
    /// Glow shadow color for hover/focus emphasis (matches CSS
    /// <c>rgba(0,229,255,.8)</c>).
    /// </summary>
    public static readonly Color GlowStrong = new Color(0, 229, 255, 204);

    /// <summary>
    /// Subdued glow for resting states (matches CSS
    /// <c>rgba(0,229,255,.2)</c>).
    /// </summary>
    public static readonly Color GlowSubtle = new Color(0, 229, 255, 51);
}
