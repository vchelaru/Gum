#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Neon;

/// <summary>
/// Root of the Neon theme's mutable per-theme styling. Mutate
/// <see cref="ActiveStyle"/>'s <see cref="Colors"/>/<see cref="Text"/> before calling
/// <see cref="NeonTheme.Apply"/> to restyle the theme without forking its source —
/// mirrors the "mutate before construct" ordering of Gum's own
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>.
/// </summary>
public class NeonStyling
{
    /// <summary>
    /// The active Neon styling instance. Get-only from outside this assembly so ordinary
    /// code can't construct a second instance and forget to activate it; still a settable
    /// property internally so a future theme-variant loader has a path to replace it.
    /// </summary>
    public static NeonStyling ActiveStyle { get; private set; } = new();

    public NeonColors Colors { get; } = new();
    public NeonText Text { get; } = new();
}

/// <summary>
/// Centralized color tokens for the Neon / Cyberpunk theme. Values mirror the
/// CSS custom properties in the source mockup (gum-styles.css <c>.nc</c>
/// palette), plus derived/state colors that used to live in an internal-only
/// <c>NeonPalette</c> — merged here as ordinary instance properties, public
/// like the rest of the palette.
/// </summary>
public class NeonColors
{
    /// <summary>Page background (<c>--bg</c>, <c>#060612</c>) — near-black with a faint blue cast.</summary>
    public Color Background { get; set; } = new Color(6, 6, 18);

    /// <summary>Surface tier 1 — control fills (<c>--s1</c>, <c>#0D0D22</c>).</summary>
    public Color Surface1 { get; set; } = new Color(13, 13, 34);

    /// <summary>Surface tier 2 — title bars, dropdown headers, scroll buttons (<c>--s2</c>, <c>#131330</c>).</summary>
    public Color Surface2 { get; set; } = new Color(19, 19, 48);

    /// <summary>Default border (<c>--bd</c>, <c>#1E1E50</c>) — saturated indigo.</summary>
    public Color Border { get; set; } = new Color(30, 30, 80);

    /// <summary>Hover border (<c>--bdh</c>, <c>#003355</c>) — dim cyan stand-in for sub-accent hover.</summary>
    public Color BorderHover { get; set; } = new Color(0, 51, 85);

    /// <summary>Accent — focus, fill, selection (<c>--acc</c>, <c>#00E5FF</c>) — bright cyan.</summary>
    public Color Accent { get; set; } = new Color(0, 229, 255);

    /// <summary>Accent translucent fill (<c>--accd</c>, <c>rgba(0,229,255,.12)</c>) — used for selected rows and pushed states.</summary>
    public Color AccentDim { get; set; } = new Color(0, 229, 255, 31);

    /// <summary>Primary text — pale cyan-white (<c>--txt</c>, <c>#C8FAFF</c>).</summary>
    public Color Text { get; set; } = new Color(200, 250, 255);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#4A6080</c>) — desaturated steel-blue.</summary>
    public Color Muted { get; set; } = new Color(74, 96, 128);

    /// <summary>Disabled fill / disabled border (<c>--dis</c>, <c>#151530</c>) — near-bg dark indigo.</summary>
    public Color Disabled { get; set; } = new Color(21, 21, 48);

    /// <summary>Disabled border (slightly lighter than <see cref="Disabled"/>) — used uniformly across controls (<c>#181838</c>).</summary>
    public Color DisabledBorder { get; set; } = new Color(24, 24, 56);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#2A3850</c>).</summary>
    public Color Placeholder { get; set; } = new Color(42, 56, 80);

    /// <summary>Row divider between list items (<c>#0F0F28</c>, derived from CSS <c>.nc-mrow</c>).</summary>
    public Color Divider { get; set; } = new Color(15, 15, 40);

    /// <summary>
    /// Hot pink danger accent — used on the Window close button only
    /// (<c>#FF0099</c>). CSS bleeds magenta in for visual contrast against
    /// the cyan-dominated chrome.
    /// </summary>
    public Color Danger { get; set; } = new Color(255, 0, 153);

    /// <summary>Pure white — used for pressed-state body text on Button/Slider thumb (CSS <c>color:#fff</c>).</summary>
    public Color White { get; set; } = new Color(255, 255, 255);

    /// <summary>
    /// Cyan glow color used by the native Apos.Shapes dropshadow. The CSS
    /// spec uses <c>rgba(0,229,255,.5)</c> through <c>.8</c> on most glows;
    /// alpha is set per visual via the dedicated glow tokens below, while the
    /// RGB stays fixed to the accent.
    /// </summary>
    public Color Glow { get; set; } = new Color(0, 229, 255);

    // --- Promoted from the former internal NeonPalette ----------------------

    /// <summary>
    /// Button hover fill — CSS spec is <c>rgba(0,229,255,.07)</c> over Surface1.
    /// Stored OPAQUE (pre-blended against Surface1) rather than translucent so
    /// the body's drop-shadow halo doesn't show through and wash the text.
    /// Computed: 0×0.07 + 13×0.93, 229×0.07 + 13×0.93, 255×0.07 + 34×0.93.
    /// </summary>
    public Color ButtonHoverFill { get; set; } = new Color(12, 28, 50);

    /// <summary>
    /// Pushed Button fill — pre-blend of <c>rgba(0,229,255,.18)</c> over
    /// Surface1. Same opaque-fill rationale as <see cref="ButtonHoverFill"/>.
    /// </summary>
    public Color ButtonPushedFill { get; set; } = new Color(11, 52, 74);

    /// <summary>
    /// CheckBox checked fill — pre-blend of <c>rgba(0,229,255,.12)</c>
    /// (<c>--accd</c>) over Surface1. Opaque.
    /// </summary>
    public Color CheckedFill { get; set; } = new Color(11, 39, 61);

    /// <summary>
    /// CheckBox hover-while-checked — brighter pre-blend (alpha ~64). Without
    /// this, the highlighted-on state was visually identical to the resting
    /// checked state, hiding hover feedback.
    /// </summary>
    public Color CheckedHoverFill { get; set; } = new Color(10, 67, 89);

    /// <summary>
    /// CheckBox pushed-while-checked — same opaque tier as the hover fill so
    /// the press doesn't darken to invisibility.
    /// </summary>
    public Color CheckedPushedFill { get; set; } = new Color(10, 67, 89);

    /// <summary>
    /// Pure-white focus indicator. Painted as a 1 px ring sitting ~4 px
    /// outside the body. Distinct shape from the body glow (which carries
    /// hover) and unmistakable against the cyan-on-dark palette.
    /// </summary>
    public Color FocusRing { get; set; } = new Color(255, 255, 255);

    /// <summary>
    /// ScrollBar thumb fill at rest. Intentionally muted (steel-blue
    /// <see cref="Muted"/>) instead of bright cyan so a scroll bar — chrome
    /// the user rarely interacts with — doesn't draw the eye away from
    /// primary content.
    /// </summary>
    public Color ScrollThumb => Muted;

    /// <summary>
    /// ScrollBar thumb hover/push fill. Brightens toward cyan to confirm
    /// the interaction, but stays a step below full Accent so the bar still
    /// reads as secondary chrome.
    /// </summary>
    public Color ScrollThumbHover { get; set; } = new Color(120, 170, 200);

    /// <summary>
    /// Hover background tint for ListBox / Menu rows — CSS uses
    /// <c>--s2</c> on <c>.nc-lb-item.hov</c> (raised surface tier).
    /// </summary>
    public Color HoverRow => Surface2;

    /// <summary>
    /// Selected row fill — translucent accent (<c>--accd</c>).
    /// </summary>
    public Color SelectedRow => AccentDim;

    /// <summary>
    /// Hot-pink translucent fill on the Window close button — CSS
    /// <c>rgba(255,0,153,.15)</c>.
    /// </summary>
    public Color CloseFill { get; set; } = new Color(255, 0, 153, 38);

    // Glow alpha is bumped well above CSS literals because Apos.Shapes
    // composites in sRGB while browsers composite in linear light — same
    // alpha math reads markedly fainter in-engine. See `gum-theming` skill
    // for the long-form note. These values were tuned by eye against the
    // mockup at https://github.com/vchelaru/Gum (Gum Styles.html, .nc block).

    /// <summary>Resting glow.</summary>
    public Color GlowMedium { get; set; } = new Color(0, 229, 255, 160);

    /// <summary>Hover/focus glow.</summary>
    public Color GlowStrong { get; set; } = new Color(0, 229, 255, 200);

    /// <summary>Subtle resting glow for chrome controls.</summary>
    public Color GlowSubtle { get; set; } = new Color(0, 229, 255, 100);

    /// <summary>
    /// Slider thumb drop shadow — CSS spec is
    /// <c>box-shadow:0 0 10px rgba(0,229,255,.5)</c>; bumped per the
    /// gum-theming skill's sRGB note so the halo reads as bright in-engine.
    /// Was a class-load-time <c>static readonly</c> field on
    /// <see cref="SliderThumbVisual"/> before the theme promoted its palette
    /// to instance state; now a live, restylable token like every other color.
    /// </summary>
    public Color SliderThumbShadow { get; set; } = new Color(0, 229, 255, 180);

    /// <summary>
    /// Native Gaussian cyan halo under floating Windows, standing in for the
    /// layered CSS spec (<c>0 0 20px rgba(0,229,255,.2)</c>). Bumped per the
    /// gum-theming skill's sRGB-vs-linear note so the window reads as glowing
    /// rather than flat. Was a class-load-time <c>static readonly</c> field on
    /// <see cref="WindowVisual"/> before the theme promoted its palette to
    /// instance state; now a live, restylable token like every other color.
    /// </summary>
    public Color WindowShadow { get; set; } = new Color(0, 229, 255, 130);

    // --- 4-token guardrail ---------------------------------------------------
    // Every theme's Apply() pushes these into V3.Styling.ActiveStyle.Colors for the stock,
    // un-subclassed V3 visuals (e.g. Label) Neon leaves in place. Neon already carries a
    // token named "Accent" that matches the guardrail name exactly, so only the other 3
    // need get-only aliases onto Neon's real, settable tokens (Text/Muted/Surface1) —
    // mutating those is reflected automatically, the same "reactivity is free" behavior as
    // any other derived color.

    /// <summary>Alias for <see cref="Text"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextPrimary</c>.</summary>
    public Color TextPrimary => Text;

    /// <summary>Alias for <see cref="Muted"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextMuted</c>.</summary>
    public Color TextMuted => Muted;

    /// <summary>Alias for <see cref="Surface1"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Primary</c>.</summary>
    public Color Primary => Surface1;
}

/// <summary>
/// Font selection for the Neon theme. Registration of the bundled TTF bytes is
/// separate (see <see cref="NeonTheme.RegisterBundledFonts"/>) — reassigning
/// these before <see cref="NeonTheme.Apply"/> only changes which
/// already-registered family visuals select.
/// <para>
/// Neon ships one body typeface, Share Tech Mono (<see cref="FontFamily"/>),
/// which flows to every control that reads
/// <c>Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text</c>. Orbitron
/// (<see cref="TitleFontFamily"/>) is registered for the CSS spec's window /
/// header titles but is not currently wired into any Neon visual — kept
/// available (mirrors ForestGlade's Fraunces) for a future title-bar treatment
/// or a consumer's own <c>Font</c> override.
/// </para>
/// </summary>
public class NeonText
{
    /// <summary>
    /// Family visuals use for body/control text. Defaults to the bundled
    /// Share Tech Mono family.
    /// </summary>
    public string FontFamily { get; set; } = NeonTheme.BundledFontFamily;

    /// <summary>
    /// Family registered for the CSS spec's title typeface. Defaults to the
    /// bundled Orbitron family. Not currently applied by any Neon visual; set
    /// <c>TextInstance.Font = NeonStyling.ActiveStyle.Text.TitleFontFamily</c>
    /// to opt a custom visual into it.
    /// </summary>
    public string TitleFontFamily { get; set; } = NeonTheme.BundledTitleFontFamily;

    /// <summary>
    /// Family used for glyphs Share Tech Mono doesn't cover (check marks,
    /// dropdown chevrons, scroll-bar arrows — Dingbats and Geometric Shapes
    /// blocks). Defaults to the bundled DejaVu Sans Mono icon family.
    /// </summary>
    public string IconFontFamily { get; set; } = NeonTheme.BundledIconFontFamily;

    /// <summary>Default text size used by the theme. Matches the source mockup's <c>--fs</c> token (13px).</summary>
    public int FontSize { get; set; } = 13;
}
