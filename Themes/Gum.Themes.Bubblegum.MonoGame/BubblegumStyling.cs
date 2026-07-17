#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Root of the Bubblegum theme's mutable per-theme styling. Mutate
/// <see cref="ActiveStyle"/>'s <see cref="Colors"/>/<see cref="Text"/> before calling
/// <see cref="BubblegumTheme.Apply"/> to restyle the theme without forking its source —
/// mirrors the "mutate before construct" ordering of Gum's own
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>.
/// </summary>
public class BubblegumStyling
{
    /// <summary>
    /// The active Bubblegum styling instance. Get-only from outside this assembly so ordinary
    /// code can't construct a second instance and forget to activate it; still a settable
    /// property internally so a future theme-variant loader has a path to replace it.
    /// </summary>
    public static BubblegumStyling ActiveStyle { get; private set; } = new();

    public BubblegumColors Colors { get; } = new();
    public BubblegumText Text { get; } = new();
}

/// <summary>
/// Centralized color tokens for the Bubblegum theme. Values mirror the CSS
/// custom properties in the source mockup (gum-styles.css `.bb` palette), plus
/// derived tints that used to live in an internal-only <c>BubblegumPalette</c>
/// — merged here as ordinary instance properties, public like the rest of the palette.
/// </summary>
public class BubblegumColors
{
    /// <summary>Page background (<c>--bg</c>, <c>#FFF0F9</c>).</summary>
    public Color Background { get; set; } = new Color(255, 240, 249);

    /// <summary>Surface tier 1 — control fills (<c>--s1</c>, <c>#FFFFFF</c>).</summary>
    public Color Surface1 { get; set; } = new Color(255, 255, 255);

    /// <summary>Default border (<c>--bd</c>, <c>#F0AACC</c>).</summary>
    public Color Border { get; set; } = new Color(240, 170, 204);

    /// <summary>Focused border / hover (<c>--bdf</c>, <c>#FF6B9D</c>).</summary>
    public Color BorderFocused { get; set; } = new Color(255, 107, 157);

    /// <summary>Accent — focus, fill, selection (<c>--acc</c>, <c>#FF6B9D</c>).</summary>
    public Color Accent { get; set; } = new Color(255, 107, 157);

    /// <summary>Accent light — soft fills, ticks, badges (<c>--accl</c>, <c>#FFDEED</c>).</summary>
    public Color AccentLight { get; set; } = new Color(255, 222, 237);

    /// <summary>Accent dark — pressed / pushed (<c>--accd</c>, <c>#CC4475</c>).</summary>
    public Color AccentDark { get; set; } = new Color(204, 68, 117);

    /// <summary>Button hover fill — a lifted, lighter pink (<c>#FF8FB8</c>).</summary>
    public Color AccentHover { get; set; } = new Color(255, 143, 184);

    /// <summary>Primary text — deep eggplant for legibility on pastel surfaces (<c>--txt</c>, <c>#3D1155</c>).</summary>
    public Color Text { get; set; } = new Color(61, 17, 85);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#A07BB0</c>).</summary>
    public Color Muted { get; set; } = new Color(160, 123, 176);

    /// <summary>Disabled fill / disabled border (<c>--dis</c>, <c>#CDAEDD</c>).</summary>
    public Color Disabled { get; set; } = new Color(205, 174, 221);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#C9A8D9</c>).</summary>
    public Color Placeholder { get; set; } = new Color(201, 168, 217);

    /// <summary>Faint divider used between rows (<c>#F5D0E8</c>).</summary>
    public Color Divider { get; set; } = new Color(245, 208, 232);

    /// <summary>Disabled checkbox / radio fill (<c>#F5EAF8</c>).</summary>
    public Color DisabledFill { get; set; } = new Color(245, 234, 248);

    /// <summary>Soft pink-tinted shadow used under floating chrome (<c>rgba(255,107,157,.4)</c> ≈ <c>#FFB8D2</c>). Alpha is baked into the layered stack rather than the color itself.</summary>
    public Color Shadow { get; set; } = new Color(255, 107, 157, 100);

    // --- Promoted from the former internal BubblegumPalette ---------------

    /// <summary>
    /// Translucent accent for focus rings (matches CSS
    /// <c>rgba(255,107,157,.25)</c> ≈ alpha 64). The ring is stroked at 3 px in
    /// every visual; the alpha is what carries the "soft halo" reading.
    /// </summary>
    public Color FocusRing { get; set; } = new Color(255, 107, 157, 90);

    /// <summary>Hover background tint for list rows (<c>#FFF5FB</c> from .bb-lb-item.hov).</summary>
    public Color HoverRow { get; set; } = new Color(255, 245, 251);

    /// <summary>Hover background tint for combo-box / menu options (<c>#FFF0F6</c>).</summary>
    public Color HoverOption { get; set; } = new Color(255, 240, 246);

    /// <summary>Selected row fill — the same soft pink the CSS uses (<c>--accl</c>).</summary>
    public Color SelectedRow => AccentLight;

    /// <summary>
    /// Accent on selected-row text (<c>--accd</c>). Higher contrast against
    /// AccentLight than the default Text color, matching the CSS spec.
    /// </summary>
    public Color SelectedRowText => AccentDark;

    // --- 4-token guardrail -------------------------------------------------
    // Every theme's Apply() pushes these into V3.Styling.ActiveStyle.Colors for the stock,
    // un-subclassed V3 visuals (e.g. Label) Bubblegum leaves in place. Bubblegum's own
    // vocabulary already covers these concepts under different names, so they're exposed here
    // as get-only aliases — mutating Text/Muted/Surface1 (Bubblegum's real, settable tokens) is
    // reflected automatically, the same "reactivity is free" behavior as any other derived color.

    /// <summary>Alias for <see cref="Text"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextPrimary</c>.</summary>
    public Color TextPrimary => Text;

    /// <summary>Alias for <see cref="Muted"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextMuted</c>.</summary>
    public Color TextMuted => Muted;

    /// <summary>Alias for <see cref="Surface1"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Primary</c>.</summary>
    public Color Primary => Surface1;
}

/// <summary>
/// Font selection for the Bubblegum theme. Registration of the bundled TTF bytes is
/// separate (see <see cref="BubblegumTheme.RegisterBundledFonts"/>) — reassigning these
/// before <see cref="BubblegumTheme.Apply"/> only changes which already-registered
/// family visuals select.
/// </summary>
public class BubblegumText
{
    /// <summary>
    /// Family visuals use for body/control text. Defaults to the bundled Nunito family.
    /// </summary>
    public string FontFamily { get; set; } = BubblegumTheme.BundledFontFamily;

    /// <summary>
    /// Family used for glyphs Nunito doesn't cover (check marks, dropdown chevrons, arrow
    /// indicators). Defaults to the bundled DejaVu Sans Mono icon family.
    /// </summary>
    public string IconFontFamily { get; set; } = BubblegumTheme.BundledIconFontFamily;

    /// <summary>Default text size used by the theme. Matches the source mockup's <c>--fs</c> token (14px).</summary>
    public int FontSize { get; set; } = 14;
}
