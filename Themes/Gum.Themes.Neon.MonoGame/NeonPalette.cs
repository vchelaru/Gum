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
    /// Button hover fill — CSS spec is <c>rgba(0,229,255,.07)</c> over Surface1.
    /// Stored OPAQUE (pre-blended against Surface1) rather than translucent so
    /// the body's drop-shadow halo doesn't show through and wash the text.
    /// Computed: 0×0.07 + 13×0.93, 229×0.07 + 13×0.93, 255×0.07 + 34×0.93.
    /// </summary>
    public static readonly Color ButtonHoverFill = new Color(12, 28, 50);

    /// <summary>
    /// Pushed Button fill — pre-blend of <c>rgba(0,229,255,.18)</c> over
    /// Surface1. Same opaque-fill rationale as <see cref="ButtonHoverFill"/>.
    /// </summary>
    public static readonly Color ButtonPushedFill = new Color(11, 52, 74);

    /// <summary>
    /// CheckBox checked fill — pre-blend of <c>rgba(0,229,255,.12)</c>
    /// (<c>--accd</c>) over Surface1. Opaque.
    /// </summary>
    public static readonly Color CheckedFill = new Color(11, 39, 61);

    /// <summary>
    /// CheckBox hover-while-checked — brighter pre-blend (alpha ~64). Without
    /// this, the highlighted-on state was visually identical to the resting
    /// checked state, hiding hover feedback.
    /// </summary>
    public static readonly Color CheckedHoverFill = new Color(10, 67, 89);

    /// <summary>
    /// CheckBox pushed-while-checked — same opaque tier as the hover fill so
    /// the press doesn't darken to invisibility.
    /// </summary>
    public static readonly Color CheckedPushedFill = new Color(10, 67, 89);

    /// <summary>
    /// Pure-white focus indicator. Painted as a 1 px ring sitting ~4 px
    /// outside the body. Distinct shape from the body glow (which carries
    /// hover) and unmistakable against the cyan-on-dark palette.
    /// </summary>
    public static readonly Color FocusRing = Color.White;

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
