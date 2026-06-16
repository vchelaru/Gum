#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Derived color values used across multiple Forest Glade visuals. Kept
/// separate from <see cref="ForestGladeColors"/> so the canonical CSS-token
/// palette stays 1:1 with the source mockup, and translucent /
/// state-derived colors live here.
/// </summary>
internal static class ForestGladePalette
{
    // ---- Button gradient stops (CSS .fg-btn uses three-stop linear gradients) ----
    // Apos.Shapes supports 2-stop linear gradients on RectangleRuntime;
    // we use the first and last CSS stops, dropping the middle. The middle
    // tones (kept here as the legacy ButtonRestFill/etc. for any consumer
    // that referenced them) are the median of the gradient and read fine as
    // a flat-fill fallback if a visual chooses not to use the gradient.

    // 2-stop gradient using the CSS first (light) and last (dark) stops.
    // The CSS 3-stop with mid at 55% has slightly different *shape* than
    // a 2-stop linear (the CSS gradient has a flatter mid-area; the 2-stop
    // is a constant slope), but the visible color range is the same — and
    // a top→mid 2-stop loses the visible bottom darkening entirely. To
    // approximate the CSS s-curve more faithfully we'd add inset edge
    // strips (top sun-pale highlight + bottom dark shadow), which the
    // ButtonVisual can layer on top of the gradient.

    /// <summary>Rest Button fill TOP — CSS <c>#0fc448</c> (1st stop).</summary>
    public static readonly Color ButtonRestFillTop = new Color(15, 196, 72);
    /// <summary>Rest Button fill BOTTOM — CSS <c>#008c2e</c> (3rd stop).</summary>
    public static readonly Color ButtonRestFillBottom = new Color(0, 140, 46);

    /// <summary>Hover Button fill TOP — CSS <c>#2cdb5c</c> (1st stop).</summary>
    public static readonly Color ButtonHoverFillTop = new Color(44, 219, 92);
    /// <summary>Hover Button fill BOTTOM — CSS <c>#08b23b</c> (3rd stop).</summary>
    public static readonly Color ButtonHoverFillBottom = new Color(8, 178, 59);

    /// <summary>Pushed Button fill TOP — CSS <c>#008c2e</c>.</summary>
    public static readonly Color ButtonPushedFillTop = new Color(0, 140, 46);
    /// <summary>Pushed Button fill BOTTOM — CSS <c>#007028</c>.</summary>
    public static readonly Color ButtonPushedFillBottom = new Color(0, 112, 40);

    /// <summary>Disabled Button fill TOP — CSS <c>#2c4438</c>.</summary>
    public static readonly Color ButtonDisabledFillTop = new Color(44, 68, 56);
    /// <summary>Disabled Button fill BOTTOM — CSS <c>#243a30</c>.</summary>
    public static readonly Color ButtonDisabledFillBottom = new Color(36, 58, 48);

    /// <summary>Legacy mid-tone Button fill — middle CSS stop. Kept for visuals that prefer a flat fill (ToggleButton, etc.).</summary>
    public static readonly Color ButtonRestFill = new Color(8, 178, 59);
    /// <summary>Legacy mid-tone Button hover fill.</summary>
    public static readonly Color ButtonHoverFill = new Color(22, 203, 70);
    /// <summary>Legacy mid-tone Button pushed fill.</summary>
    public static readonly Color ButtonPushedFill = new Color(0, 126, 41);
    /// <summary>Legacy mid-tone Button disabled fill.</summary>
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

    /// <summary>
    /// Dark drop shadow — CSS <c>rgba(0,60,30,.55)</c>, used as the
    /// primary "depth" shadow below a control (offset down, blurred).
    /// Reads as "lit from above" rather than the neon halo of <see cref="GlowMedium"/>.
    /// Alpha bumped beyond CSS literal per the gum-theming sRGB note.
    /// </summary>
    public static readonly Color DarkShadow = new Color(0, 60, 30, 200);

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
