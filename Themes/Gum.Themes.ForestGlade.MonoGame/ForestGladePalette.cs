using Microsoft.Xna.Framework;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Derived color values used across multiple Forest Glade visuals. Kept
/// separate from <see cref="ForestGladeColors"/> so the canonical CSS-token
/// palette stays 1:1 with the source mockup, and translucent /
/// state-derived colors live here.
/// </summary>
internal static class ForestGladePalette
{
    // ---- Button gradients (CSS .fg-btn uses three-stop linear gradients) ----
    // We approximate the three-stop CSS gradient with a single mid-tone fill
    // and rely on the per-state alpha tweaks for the lighting feel. Picking
    // the middle stop of each CSS gradient gives the closest match.

    /// <summary>Rest Button fill — middle stop of CSS <c>linear-gradient(180deg, #0fc448, #08b23b, #008c2e)</c>.</summary>
    public static readonly Color ButtonRestFill = new Color(8, 178, 59);

    /// <summary>Hover Button fill — middle stop of CSS <c>linear-gradient(180deg, #2cdb5c, #16cb46, #08b23b)</c>.</summary>
    public static readonly Color ButtonHoverFill = new Color(22, 203, 70);

    /// <summary>Pushed Button fill — middle stop of CSS <c>linear-gradient(180deg, #008c2e, #007028)</c>.</summary>
    public static readonly Color ButtonPushedFill = new Color(0, 126, 41);

    /// <summary>Disabled Button fill — CSS <c>linear-gradient(180deg, #2c4438, #243a30)</c> midpoint.</summary>
    public static readonly Color ButtonDisabledFill = new Color(40, 63, 52);

    // ---- Glow / drop shadow colors --------------------------------------
    // Alpha is bumped above CSS literals because Apos.Shapes composites in
    // sRGB while browsers composite in linear light — same alpha math reads
    // markedly fainter in-engine. See `gum-theming` skill for the rationale.

    /// <summary>Resting button/checkbox glow — CSS uses <c>rgba(71,246,65,.18)</c> → ~46. Bumped for sRGB.</summary>
    public static readonly Color GlowMedium = new Color(71, 246, 65, 110);

    /// <summary>Hover/focus glow — CSS spec around <c>.32</c> alpha. Bumped.</summary>
    public static readonly Color GlowStrong = new Color(71, 246, 65, 170);

    /// <summary>Subtle resting glow used on smaller chrome (slider thumb, etc.).</summary>
    public static readonly Color GlowSubtle = new Color(71, 246, 65, 80);

    /// <summary>Sun-pale glow — used on hover for elements that read warm rather than green.</summary>
    public static readonly Color GlowSunPale = new Color(232, 255, 117, 90);

    // ---- TextBox / input ------------------------------------------------

    /// <summary>TextBox at-rest fill — translucent dark "glassy pool" effect, opaque baked over canopy bg.</summary>
    public static readonly Color InputFill = new Color(3, 32, 36);

    /// <summary>TextBox focused fill — same baseline; the focus ring carries the change.</summary>
    public static readonly Color InputFillFocused = new Color(3, 32, 36);

    /// <summary>TextBox disabled fill — pre-blend with disabled fade.</summary>
    public static readonly Color InputFillDisabled = new Color(8, 25, 28);

    // ---- ListBox / ComboBox / Menu rows ---------------------------------

    /// <summary>Hover row tint (~<c>rgba(232,255,117,.08)</c> over canopy bg).</summary>
    public static readonly Color HoverRow = new Color(20, 60, 60);

    /// <summary>Selected row tint — accent dim (translucent).</summary>
    public static readonly Color SelectedRow = ForestGladeColors.AccentDim;

    /// <summary>Selection edge stripe — the inset 3px leaf-bright stripe on the left of selected list items (CSS <c>box-shadow: inset 3px 0 0</c>).</summary>
    public static readonly Color SelectionStripe = ForestGladeColors.LeafBright;

    // ---- ScrollBar ------------------------------------------------------

    /// <summary>ScrollBar thumb base color — semitransparent leaf-bright over track. Pre-blended toward dark.</summary>
    public static readonly Color ScrollThumb = new Color(35, 175, 60);

    /// <summary>ScrollBar thumb hover — brighter.</summary>
    public static readonly Color ScrollThumbHover = new Color(60, 210, 80);

    /// <summary>ScrollBar track fill.</summary>
    public static readonly Color ScrollTrack = new Color(4, 22, 25);

    /// <summary>ScrollBar arrow button fill — barely-tinted track variant.</summary>
    public static readonly Color ScrollButton = new Color(20, 50, 50);

    // ---- Window ---------------------------------------------------------

    /// <summary>Window body fill — translucent canopy with backdrop blur replaced by an opaque pre-blend.</summary>
    public static readonly Color WindowBody = new Color(10, 45, 38);

    /// <summary>Window title bar mid color — middle of CSS bark gradient <c>linear-gradient(180deg, #6b3520, #4a2516, #3a1c10)</c>.</summary>
    public static readonly Color WindowTitleBar = new Color(74, 37, 22);

    /// <summary>Close-button wax-seal fill (CSS radial pink/red).</summary>
    public static readonly Color CloseSeal = new Color(193, 36, 88);

    // ---- Slider ---------------------------------------------------------

    /// <summary>Slider track at rest.</summary>
    public static readonly Color SliderTrack = new Color(4, 22, 25);

    /// <summary>Slider fill (left of thumb) — mid-stop of CSS green gradient.</summary>
    public static readonly Color SliderFill = ForestGladeColors.LeafBright;

    /// <summary>Slider disabled track + fill (CSS muted moss).</summary>
    public static readonly Color SliderDisabled = new Color(44, 68, 56);

    // ---- Splitter -------------------------------------------------------

    /// <summary>Splitter vine cord color — mid-stop of CSS bark gradient.</summary>
    public static readonly Color VineCord = new Color(90, 53, 32);
}
