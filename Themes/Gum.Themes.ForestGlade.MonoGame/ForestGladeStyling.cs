#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Root of the Forest Glade theme's mutable per-theme styling. Mutate
/// <see cref="ActiveStyle"/>'s <see cref="Colors"/>/<see cref="Text"/> before calling
/// <see cref="ForestGladeTheme.Apply"/> to restyle the theme without forking its source —
/// mirrors the "mutate before construct" ordering of Gum's own
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>.
/// </summary>
public class ForestGladeStyling
{
    /// <summary>
    /// The active Forest Glade styling instance. Get-only from outside this assembly so ordinary
    /// code can't construct a second instance and forget to activate it; still a settable
    /// property internally so a future theme-variant loader has a path to replace it.
    /// </summary>
    public static ForestGladeStyling ActiveStyle { get; private set; } = new();

    public ForestGladeColors Colors { get; } = new();
    public ForestGladeText Text { get; } = new();
}

/// <summary>
/// Centralized color tokens for the Forest Glade theme. Values mirror the CSS
/// custom properties in the source mockup (forest-glade.css <c>.fg</c> palette),
/// plus derived tints that used to live in an internal-only <c>ForestGladePalette</c>
/// — merged here as ordinary instance properties, public like the rest of the palette.
/// </summary>
public class ForestGladeColors
{
    /// <summary>Deep shadow under the trees (<c>--canopy-deep</c>, <c>#053239</c>). Page background.</summary>
    public Color CanopyDeep { get; set; } = new Color(5, 50, 57);

    /// <summary>Mossy mid-ground green (<c>--canopy-mid</c>, <c>#005f41</c>).</summary>
    public Color CanopyMid { get; set; } = new Color(0, 95, 65);

    /// <summary>Sunlit leaves green (<c>--canopy-lit</c>, <c>#08b23b</c>). Mid-tone in Button gradients.</summary>
    public Color CanopyLit { get; set; } = new Color(8, 178, 59);

    /// <summary>Vibrant new growth (<c>--leaf-bright</c>, <c>#47f641</c>). Accent color; focus ring fill, slider track fill.</summary>
    public Color LeafBright { get; set; } = new Color(71, 246, 65);

    /// <summary>High sun pollen (<c>--sun-pale</c>, <c>#e8ff75</c>). Border tint base, caret, highlight ticks.</summary>
    public Color SunPale { get; set; } = new Color(232, 255, 117);

    /// <summary>Late-afternoon gold (<c>--sun-warm</c>, <c>#ecab11</c>). Optional warm accent.</summary>
    public Color SunWarm { get; set; } = new Color(236, 171, 17);

    /// <summary>Dappled warm light (<c>--sun-glow</c>, <c>#fbbe82</c>). Secondary light-beam tint.</summary>
    public Color SunGlow { get; set; } = new Color(251, 190, 130);

    /// <summary>Tree bark dark (<c>--bark</c>, <c>#461c14</c>). Disabled-well fill; Window border.</summary>
    public Color Bark { get; set; } = new Color(70, 28, 20);

    /// <summary>Wood midtone (<c>--bark-soft</c>, <c>#8a4926</c>). Window title-bar gradient mid stop.</summary>
    public Color BarkSoft { get; set; } = new Color(138, 73, 38);

    /// <summary>Wildflower pink (<c>--petal</c>, <c>#f78d8d</c>). Wax-seal close button on Window.</summary>
    public Color Petal { get; set; } = new Color(247, 141, 141);

    /// <summary>Primary text — pale leaf-white (<c>--txt</c>, <c>#f1fff0</c>).</summary>
    public Color Text { get; set; } = new Color(241, 255, 240);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#9bbaa3</c>) — desaturated sage.</summary>
    public Color Muted { get; set; } = new Color(155, 186, 163);

    /// <summary>Disabled text / fills (<c>--dis</c>, <c>#4a6a58</c>) — moss undergrowth.</summary>
    public Color Disabled { get; set; } = new Color(74, 106, 88);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#7d9c87</c>).</summary>
    public Color Placeholder { get; set; } = new Color(125, 156, 135);

    /// <summary>Default border tint — <c>rgba(232,255,117,.18)</c>, sun-pale at 18% alpha.</summary>
    public Color Border { get; set; } = new Color(232, 255, 117, 46);

    /// <summary>Hover border tint — <c>rgba(232,255,117,.42)</c>, sun-pale at 42% alpha.</summary>
    public Color BorderHover { get; set; } = new Color(232, 255, 117, 107);

    /// <summary>Translucent accent fill — <c>rgba(71,246,65,.18)</c>, leaf-bright at 18% alpha. Selected rows, pushed states.</summary>
    public Color AccentDim { get; set; } = new Color(71, 246, 65, 46);

    /// <summary>Translucent accent halo — <c>rgba(71,246,65,.30)</c>, leaf-bright at 30%. Focus ring around inputs.</summary>
    public Color AccentHalo { get; set; } = new Color(71, 246, 65, 76);

    /// <summary>Pure white — used for pressed-state text on a few controls.</summary>
    public Color White { get; set; } = Color.White;

    // --- Promoted from the former internal ForestGladePalette ---------------
    // Button gradient stops (CSS .fg-btn uses three-stop linear gradients).
    // Apos.Shapes supports 2-stop linear gradients on RectangleRuntime; we
    // use the first and last CSS stops, dropping the middle. The middle
    // tones (kept here as the legacy ButtonRestFill/etc. for any consumer
    // that referenced them) are the median of the gradient and read fine as
    // a flat-fill fallback if a visual chooses not to use the gradient.

    /// <summary>Rest Button fill TOP — CSS <c>#0fc448</c> (1st stop).</summary>
    public Color ButtonRestFillTop { get; set; } = new Color(15, 196, 72);

    /// <summary>Rest Button fill BOTTOM — CSS <c>#008c2e</c> (3rd stop).</summary>
    public Color ButtonRestFillBottom { get; set; } = new Color(0, 140, 46);

    /// <summary>Hover Button fill TOP — CSS <c>#2cdb5c</c> (1st stop).</summary>
    public Color ButtonHoverFillTop { get; set; } = new Color(44, 219, 92);

    /// <summary>Hover Button fill BOTTOM — CSS <c>#08b23b</c> (3rd stop).</summary>
    public Color ButtonHoverFillBottom { get; set; } = new Color(8, 178, 59);

    /// <summary>Pushed Button fill TOP — CSS <c>#008c2e</c>.</summary>
    public Color ButtonPushedFillTop { get; set; } = new Color(0, 140, 46);

    /// <summary>Pushed Button fill BOTTOM — CSS <c>#007028</c>.</summary>
    public Color ButtonPushedFillBottom { get; set; } = new Color(0, 112, 40);

    /// <summary>Disabled Button fill TOP — CSS <c>#2c4438</c>.</summary>
    public Color ButtonDisabledFillTop { get; set; } = new Color(44, 68, 56);

    /// <summary>Disabled Button fill BOTTOM — CSS <c>#243a30</c>.</summary>
    public Color ButtonDisabledFillBottom { get; set; } = new Color(36, 58, 48);

    /// <summary>Legacy mid-tone Button fill — middle CSS stop. Kept for visuals that prefer a flat fill (ToggleButton, etc.).</summary>
    public Color ButtonRestFill { get; set; } = new Color(8, 178, 59);

    /// <summary>Legacy mid-tone Button hover fill.</summary>
    public Color ButtonHoverFill { get; set; } = new Color(22, 203, 70);

    /// <summary>Legacy mid-tone Button pushed fill.</summary>
    public Color ButtonPushedFill { get; set; } = new Color(0, 126, 41);

    /// <summary>Legacy mid-tone Button disabled fill.</summary>
    public Color ButtonDisabledFill { get; set; } = new Color(40, 63, 52);

    // Glow / drop shadow colors. Alpha is bumped above CSS literals because
    // Apos.Shapes composites in sRGB while browsers composite in linear
    // light — same alpha math reads markedly fainter in-engine. See
    // `gum-theming` skill for the rationale.

    /// <summary>Resting button/checkbox glow — CSS uses <c>rgba(71,246,65,.18)</c> → ~46. Bumped for sRGB.</summary>
    public Color GlowMedium { get; set; } = new Color(71, 246, 65, 110);

    /// <summary>Hover/focus glow — CSS spec around <c>.32</c> alpha. Bumped.</summary>
    public Color GlowStrong { get; set; } = new Color(71, 246, 65, 170);

    /// <summary>Subtle resting glow used on smaller chrome (slider thumb, etc.).</summary>
    public Color GlowSubtle { get; set; } = new Color(71, 246, 65, 80);

    /// <summary>
    /// Dark drop shadow — CSS <c>rgba(0,60,30,.55)</c>, used as the
    /// primary "depth" shadow below a control (offset down, blurred).
    /// Reads as "lit from above" rather than the neon halo of <see cref="GlowMedium"/>.
    /// Alpha bumped beyond CSS literal per the gum-theming sRGB note.
    /// </summary>
    public Color DarkShadow { get; set; } = new Color(0, 60, 30, 200);

    /// <summary>Sun-pale glow — used on hover for elements that read warm rather than green.</summary>
    public Color GlowSunPale { get; set; } = new Color(232, 255, 117, 90);

    // TextBox / input.

    /// <summary>TextBox at-rest fill — translucent dark "glassy pool" effect, opaque baked over canopy bg.</summary>
    public Color InputFill { get; set; } = new Color(3, 32, 36);

    /// <summary>TextBox focused fill — same baseline; the focus ring carries the change.</summary>
    public Color InputFillFocused { get; set; } = new Color(3, 32, 36);

    /// <summary>TextBox disabled fill — pre-blend with disabled fade.</summary>
    public Color InputFillDisabled { get; set; } = new Color(8, 25, 28);

    // ListBox / ComboBox / Menu rows.

    /// <summary>Hover row tint (~<c>rgba(232,255,117,.08)</c> over canopy bg).</summary>
    public Color HoverRow { get; set; } = new Color(20, 60, 60);

    /// <summary>Selected row tint — accent dim (translucent).</summary>
    public Color SelectedRow => AccentDim;

    /// <summary>Selection edge stripe — the inset 3px leaf-bright stripe on the left of selected list items (CSS <c>box-shadow: inset 3px 0 0</c>).</summary>
    public Color SelectionStripe => LeafBright;

    // ScrollBar.

    /// <summary>ScrollBar thumb base color — semitransparent leaf-bright over track. Pre-blended toward dark.</summary>
    public Color ScrollThumb { get; set; } = new Color(35, 175, 60);

    /// <summary>ScrollBar thumb hover — brighter.</summary>
    public Color ScrollThumbHover { get; set; } = new Color(60, 210, 80);

    /// <summary>ScrollBar track fill.</summary>
    public Color ScrollTrack { get; set; } = new Color(4, 22, 25);

    /// <summary>ScrollBar arrow button fill — barely-tinted track variant.</summary>
    public Color ScrollButton { get; set; } = new Color(20, 50, 50);

    // Window.

    /// <summary>Window body fill — translucent canopy with backdrop blur replaced by an opaque pre-blend.</summary>
    public Color WindowBody { get; set; } = new Color(10, 45, 38);

    /// <summary>Window title bar mid color — middle of CSS bark gradient <c>linear-gradient(180deg, #6b3520, #4a2516, #3a1c10)</c>.</summary>
    public Color WindowTitleBar { get; set; } = new Color(74, 37, 22);

    /// <summary>Close-button wax-seal fill (CSS radial pink/red).</summary>
    public Color CloseSeal { get; set; } = new Color(193, 36, 88);

    // Slider.

    /// <summary>Slider track at rest.</summary>
    public Color SliderTrack { get; set; } = new Color(4, 22, 25);

    /// <summary>Slider fill (left of thumb) — mid-stop of CSS green gradient.</summary>
    public Color SliderFill => LeafBright;

    /// <summary>Slider disabled track + fill (CSS muted moss).</summary>
    public Color SliderDisabled { get; set; } = new Color(44, 68, 56);

    // Splitter.

    /// <summary>Splitter vine cord color — mid-stop of CSS bark gradient.</summary>
    public Color VineCord { get; set; } = new Color(90, 53, 32);

    // --- 4-token guardrail -------------------------------------------------
    // Every theme's Apply() pushes these into V3.Styling.ActiveStyle.Colors for the stock,
    // un-subclassed V3 visuals (e.g. Label) Forest Glade leaves in place. Forest Glade's own
    // vocabulary already covers these concepts under different names, so they're exposed here
    // as get-only aliases — mutating Text/Muted/CanopyDeep/LeafBright (Forest Glade's real,
    // settable tokens) is reflected automatically, the same "reactivity is free" behavior as
    // any other derived color.

    /// <summary>Alias for <see cref="Text"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextPrimary</c>.</summary>
    public Color TextPrimary => Text;

    /// <summary>Alias for <see cref="Muted"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextMuted</c>.</summary>
    public Color TextMuted => Muted;

    /// <summary>Alias for <see cref="CanopyDeep"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Primary</c>.</summary>
    public Color Primary => CanopyDeep;

    /// <summary>Alias for <see cref="LeafBright"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Accent</c>.</summary>
    public Color Accent => LeafBright;
}

/// <summary>
/// Font selection for the Forest Glade theme. Registration of the bundled TTF bytes is
/// separate (see <see cref="ForestGladeTheme.RegisterBundledFonts"/>) — reassigning these
/// before <see cref="ForestGladeTheme.Apply"/> only changes which already-registered
/// family visuals select.
/// </summary>
public class ForestGladeText
{
    /// <summary>
    /// Family visuals use for body/control text. Defaults to the bundled Nunito family.
    /// </summary>
    public string FontFamily { get; set; } = ForestGladeTheme.BundledFontFamily;

    /// <summary>
    /// Family used for the Window title bar's display face. Defaults to the bundled
    /// Fraunces Bold Italic family.
    /// </summary>
    public string TitleFontFamily { get; set; } = ForestGladeTheme.BundledTitleFontFamily;

    /// <summary>
    /// Family used for glyphs Nunito doesn't cover (check marks, dropdown chevrons, arrow
    /// indicators). Defaults to the bundled DejaVu Sans Mono icon family.
    /// </summary>
    public string IconFontFamily { get; set; } = ForestGladeTheme.BundledIconFontFamily;

    /// <summary>Default text size used by the theme. Matches the source mockup's <c>--fs</c> token (14px).</summary>
    public int FontSize { get; set; } = 14;
}
