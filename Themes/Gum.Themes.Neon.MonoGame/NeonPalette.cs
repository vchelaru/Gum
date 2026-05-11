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
    /// ScrollBar thumb fill at rest. Intentionally muted (steel-blue
    /// <see cref="NeonColors.Muted"/>) instead of bright cyan so a scroll
    /// bar — chrome the user rarely interacts with — doesn't draw the eye
    /// away from primary content.
    /// </summary>
    public static readonly Color ScrollThumb = NeonColors.Muted;

    /// <summary>
    /// ScrollBar thumb hover/push fill. Brightens toward cyan to confirm
    /// the interaction, but stays a step below full Accent so the bar still
    /// reads as secondary chrome.
    /// </summary>
    public static readonly Color ScrollThumbHover = new Color(120, 170, 200);

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

    // Glow alpha is bumped well above CSS literals because Apos.Shapes
    // composites in sRGB while browsers composite in linear light — same
    // alpha math reads markedly fainter in-engine. See `gum-theming` skill
    // for the long-form note. These values were tuned by eye against the
    // mockup at https://github.com/vchelaru/Gum (Gum Styles.html, .nc block).

    /// <summary>Resting glow.</summary>
    public static readonly Color GlowMedium = new Color(0, 229, 255, 160);

    /// <summary>Hover/focus glow.</summary>
    public static readonly Color GlowStrong = new Color(0, 229, 255, 200);

    /// <summary>Subtle resting glow for chrome controls.</summary>
    public static readonly Color GlowSubtle = new Color(0, 229, 255, 100);
}
