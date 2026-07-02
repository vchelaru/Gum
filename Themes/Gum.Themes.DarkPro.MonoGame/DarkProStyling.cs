#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.DarkPro;

/// <summary>
/// Root of the Dark Pro theme's mutable per-theme styling. Mutate
/// <see cref="ActiveStyle"/>'s <see cref="Colors"/>/<see cref="Text"/> before calling
/// <see cref="DarkProTheme.Apply"/> to restyle the theme without forking its source —
/// mirrors the "mutate before construct" ordering of Gum's own
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>.
/// </summary>
public class DarkProStyling
{
    /// <summary>
    /// The active Dark Pro styling instance. Get-only from outside this assembly so ordinary
    /// code can't construct a second instance and forget to activate it; still a settable
    /// property internally so a future theme-variant loader has a path to replace it.
    /// </summary>
    public static DarkProStyling ActiveStyle { get; private set; } = new();

    public DarkProColors Colors { get; } = new();
    public DarkProText Text { get; } = new();
}

/// <summary>
/// Centralized color tokens for the Dark Pro theme. Values mirror the CSS
/// custom properties in the source mockup (gum-styles.css).
/// </summary>
public class DarkProColors
{
    /// <summary>App background (<c>--bg</c>, <c>#1A1B1E</c>).</summary>
    public Color Background { get; set; } = new Color(26, 27, 30);

    /// <summary>Surface tier 1 — control fills (<c>--s1</c>, <c>#252526</c>).</summary>
    public Color Surface1 { get; set; } = new Color(37, 37, 38);

    /// <summary>Surface tier 2 — hovered control / dropdown header (<c>--s2</c>, <c>#2D2D30</c>).</summary>
    public Color Surface2 { get; set; } = new Color(45, 45, 48);

    /// <summary>Default border (<c>--bd</c>, <c>#3C3C3C</c>).</summary>
    public Color Border { get; set; } = new Color(60, 60, 60);

    /// <summary>Hovered border (<c>--bdh</c>, <c>#5A5A5A</c>).</summary>
    public Color BorderHover { get; set; } = new Color(90, 90, 90);

    /// <summary>Accent — focus, fill, selection (<c>--acc</c>, <c>#007ACC</c>).</summary>
    public Color Accent { get; set; } = new Color(0, 122, 204);

    /// <summary>Accent dark — pressed / pushed (<c>--accd</c>, <c>#094771</c>).</summary>
    public Color AccentDark { get; set; } = new Color(9, 71, 113);

    /// <summary>
    /// Darker accent used when a "filled-with-accent" element is pressed — for
    /// example, a checked CheckBox being pushed (<c>#005A99</c> from .dp-chk.pre.chk).
    /// </summary>
    public Color AccentPressed { get; set; } = new Color(0, 90, 153);

    /// <summary>
    /// Brighter accent used for hover on accent-filled elements like the slider
    /// thumb (<c>#2299E0</c> from .sldr.hov .sldr-thumb).
    /// </summary>
    public Color HoverAccent { get; set; } = new Color(34, 153, 224);

    /// <summary>
    /// Fill color for disabled accent-filled elements (slider thumb, future
    /// progress indicators) — <c>#3E3E42</c> from .sldr.dis .sldr-thumb.
    /// </summary>
    public Color DisabledThumb { get; set; } = new Color(62, 62, 66);

    /// <summary>Primary text (<c>--txt</c>, <c>#D4D4D4</c>).</summary>
    public Color Text { get; set; } = new Color(212, 212, 212);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#888888</c>).</summary>
    public Color Muted { get; set; } = new Color(136, 136, 136);

    /// <summary>Disabled text (<c>--dis</c>, <c>#454545</c>).</summary>
    public Color DisabledText { get; set; } = new Color(69, 69, 69);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#6A6A6A</c>).</summary>
    public Color Placeholder { get; set; } = new Color(106, 106, 106);

    /// <summary>Disabled control fill — slightly darker than Surface1 (<c>#1C1C1C</c> from .dp-btn.dis).</summary>
    public Color DisabledFill { get; set; } = new Color(28, 28, 28);

    /// <summary>Disabled border (<c>#292929</c> from .dp-btn.dis).</summary>
    public Color DisabledBorder { get; set; } = new Color(41, 41, 41);

    /// <summary>
    /// Pressed-state fill — a step down from <see cref="Surface1"/> so press reads as a transient
    /// interaction rather than a state change. The source mockup specified <c>--accd</c>
    /// (full accent blue) for press, but a fully-blue press makes every button look "toggled"
    /// rather than "you just clicked me." Accent-fill-on-press is preserved for any future
    /// primary/default Button variant.
    /// </summary>
    public Color PressedFill { get; set; } = new Color(29, 29, 30);

    /// <summary>
    /// Light-blue text color from the source mockup's accent-fill press state (<c>#9DCFEE</c>).
    /// Currently unused — the active <see cref="PressedFill"/> uses normal <see cref="Text"/>.
    /// Retained for a future primary/default Button variant that paints accent on press.
    /// </summary>
    public Color PressedText { get; set; } = new Color(157, 207, 238);

    // --- 4-token guardrail -------------------------------------------------
    // Every theme's Apply() pushes these into V3.Styling.ActiveStyle.Colors for the stock,
    // un-subclassed V3 visuals (e.g. Label) Dark Pro leaves in place. Dark Pro's own vocabulary
    // already covers these concepts under different names, so they're exposed here as get-only
    // aliases — mutating Text/Muted/Surface1 (Dark Pro's real, settable tokens) is reflected
    // automatically, the same "reactivity is free" behavior as any other derived color.

    /// <summary>Alias for <see cref="Text"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextPrimary</c>.</summary>
    public Color TextPrimary => Text;

    /// <summary>Alias for <see cref="Muted"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextMuted</c>.</summary>
    public Color TextMuted => Muted;

    /// <summary>Alias for <see cref="Surface1"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Primary</c>.</summary>
    public Color Primary => Surface1;
}

/// <summary>
/// Font selection for the Dark Pro theme. Registration of the bundled TTF bytes is
/// separate (see <see cref="DarkProTheme.RegisterBundledFonts"/> is what registers them) —
/// reassigning these before <see cref="DarkProTheme.Apply"/> only changes which
/// already-registered family visuals select.
/// </summary>
public class DarkProText
{
    /// <summary>
    /// Family visuals use for body/control text. Defaults to the bundled DM Mono family.
    /// </summary>
    public string FontFamily { get; set; } = DarkProTheme.BundledFontFamily;

    /// <summary>
    /// Family used for glyphs DM Mono doesn't cover (check marks, close buttons, combo/scrollbar
    /// arrows). Defaults to the bundled DejaVu Sans Mono icon family.
    /// </summary>
    public string IconFontFamily { get; set; } = DarkProTheme.BundledIconFontFamily;

    /// <summary>Default text size used by the theme. Matches the Dark Pro mockup's <c>--fs</c> token (14px).</summary>
    public int FontSize { get; set; } = 14;
}
