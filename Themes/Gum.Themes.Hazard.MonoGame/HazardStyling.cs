#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
// Brings in ColorExtensions.Adjust (lighten/darken helper shipped with V3
// styling). Used below to COMPUTE derived state colors instead of
// hand-storing them.
using Gum.Forms.DefaultVisuals.V3;

namespace Gum.Themes.Hazard;

/// <summary>
/// Root of the Hazard theme's mutable per-theme styling. Mutate
/// <see cref="ActiveStyle"/>'s <see cref="Colors"/>/<see cref="Text"/> before calling
/// <see cref="HazardTheme.Apply"/> to restyle the theme without forking its source —
/// mirrors the "mutate before construct" ordering of Gum's own
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>.
/// </summary>
public class HazardStyling
{
    /// <summary>
    /// The active Hazard styling instance. Get-only from outside this assembly so ordinary
    /// code can't construct a second instance and forget to activate it; still a settable
    /// property internally so a future theme-variant loader has a path to replace it.
    /// </summary>
    public static HazardStyling ActiveStyle { get; private set; } = new();

    public HazardColors Colors { get; } = new();
    public HazardText Text { get; } = new();
}

/// <summary>
/// The Hazard theme's color tokens - the one place colors are declared. Every
/// visual in this theme reads its colors from <see cref="HazardStyling.ActiveStyle"/>,
/// so a restyle touches one file.
///
/// Transcribed from the "Salvage" design (an industrial space-salvage HUD inspired
/// by Hardspace: Shipbreaker): signature hazard-yellow on warm near-black, muted
/// gold borders, olive header bands. The base tokens below map 1:1 to that design's
/// CSS custom properties (the <c>.sv</c> :root block); each keeps its
/// <c>--var #hex</c> comment so the mapping back to the mockup stays auditable.
/// Derived state colors are computed from the base tokens via
/// <see cref="ColorExtensions.Adjust"/>.
/// </summary>
public class HazardColors
{
    // ---- Base tokens (transcribed from the design's .sv :root block) --------

    /// <summary>App background - near-black warm (<c>--bg</c>, <c>#0A0A08</c>).</summary>
    public Color Background { get; set; } = new Color(10, 10, 8);

    /// <summary>Black ink - text drawn on an accent-yellow fill (<c>--ink</c>, <c>#0A0A08</c>).</summary>
    public Color Ink { get; set; } = new Color(10, 10, 8);

    /// <summary>Olive header band - window title bars, top bands (<c>--band</c>, <c>#2C2810</c>).</summary>
    public Color Band { get; set; } = new Color(44, 40, 16);

    /// <summary>Surface tier 1 - default control fill, rows, fields (<c>--s1</c>, <c>#121007</c>).</summary>
    public Color Surface1 { get; set; } = new Color(18, 16, 7);

    /// <summary>Surface tier 2 - hovered / raised surface (<c>--s2</c>, <c>#1E1A0A</c>).</summary>
    public Color Surface2 { get; set; } = new Color(30, 26, 10);

    /// <summary>Default border - muted gold (<c>--bd</c>, <c>#4A3F16</c>).</summary>
    public Color Border { get; set; } = new Color(74, 63, 22);

    /// <summary>Hovered border - brighter gold (<c>--bdh</c>, <c>#8C751F</c>).</summary>
    public Color BorderHover { get; set; } = new Color(140, 117, 31);

    /// <summary>Accent - signature hazard yellow; focus rings, fills, selection
    /// (<c>--acc</c>, <c>#F4C81A</c>).</summary>
    public Color Accent { get; set; } = new Color(244, 200, 26);

    /// <summary>Selected-item background. This design fills a selected row / option
    /// with the full hazard <see cref="Accent"/> (and draws its text in
    /// <see cref="Ink"/>), rather than a muted selection tint.</summary>
    public Color Selection { get; set; } = new Color(244, 200, 26);

    /// <summary>Primary text - gold body / label text (<c>--txt</c>, <c>#E3B528</c>).</summary>
    public Color Text { get; set; } = new Color(227, 181, 40);

    /// <summary>Bright gold - hovered / emphasized text and values (<c>--txtb</c>, <c>#F8D43B</c>).</summary>
    public Color TextBright { get; set; } = new Color(248, 212, 59);

    /// <summary>Muted gold - secondary text, labels (<c>--mu</c>, <c>#786626</c>).</summary>
    public Color Muted { get; set; } = new Color(120, 102, 38);

    /// <summary>Placeholder text - the muted gold (<c>--mu</c>, <c>#786626</c>).</summary>
    public Color Placeholder { get; set; } = new Color(120, 102, 38);

    /// <summary>Text drawn on an accent-yellow fill (e.g. a ToggleButton in its On
    /// state, a selected item, a pressed button). Black, so the label stays legible
    /// on hazard yellow (<c>--ink</c>, <c>#0A0A08</c>).</summary>
    public Color PressedText { get; set; } = new Color(10, 10, 8);

    // ---- Disabled tokens (explicit - muted, not derived) --------------------

    /// <summary>Disabled control fill (<c>#100E07</c>).</summary>
    public Color DisabledFill { get; set; } = new Color(16, 14, 7);

    /// <summary>Disabled border (<c>#2A2410</c>).</summary>
    public Color DisabledBorder { get; set; } = new Color(42, 36, 16);

    /// <summary>Disabled text (<c>--dis</c>, <c>#45391A</c>).</summary>
    public Color DisabledText { get; set; } = new Color(69, 57, 26);

    /// <summary>Fill for a disabled accent element (e.g. slider thumb / fill) - the
    /// accent drained to muted gold (<c>--dis</c>, <c>#45391A</c>).</summary>
    public Color DisabledAccent { get; set; } = new Color(69, 57, 26);

    // ---- Derived / state colors --------------------------------------------

    /// <summary>Deep amber for a pressed accent element - pressed checked CheckBox,
    /// pressed slider thumb, text selection highlight (<c>--accd</c>, <c>#C9A30C</c>).</summary>
    public Color AccentPressed { get; set; } = new Color(201, 163, 12);

    /// <summary>Fill for a hovered control - one warm step up from <see cref="Surface1"/>,
    /// reading as the design's faint accent wash on hover.</summary>
    public Color HoverFill => Surface2;

    /// <summary>Fill for a pressed (but not "on") control - a step darker than
    /// <see cref="Surface1"/> so a press reads as a transient interaction.</summary>
    public Color PressedFill => Surface1.Adjust(-20f);

    /// <summary>Brighter accent for hover on an accent-filled element (e.g. a slider thumb).</summary>
    public Color AccentHover => Accent.Adjust(+15f);

    // --- 4-token guardrail ---------------------------------------------------
    // Every theme's Apply() pushes these into V3.Styling.ActiveStyle.Colors for the stock,
    // un-subclassed V3 visuals (e.g. Label) Hazard leaves in place. Hazard already carries a
    // token named "Accent" that matches the guardrail name exactly, so only the other 3
    // need get-only aliases onto Hazard's real, settable tokens (Text/Muted/Surface1) —
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
/// Font selection for the Hazard theme. Registration of the bundled TTF bytes is
/// separate (see <see cref="HazardTheme.RegisterBundledFonts"/>) — reassigning
/// these before <see cref="HazardTheme.Apply"/> only changes which
/// already-registered family visuals select.
/// </summary>
public class HazardText
{
    /// <summary>
    /// Family visuals use for body/control text. Defaults to the bundled
    /// Saira Condensed family.
    /// </summary>
    public string FontFamily { get; set; } = HazardTheme.BundledFontFamily;

    /// <summary>
    /// Family used for glyphs Saira Condensed doesn't cover (check marks,
    /// combo/scrollbar arrows — Dingbats and Geometric Shapes blocks). Defaults
    /// to the bundled DejaVu Sans Mono icon family.
    /// </summary>
    public string IconFontFamily { get; set; } = HazardTheme.BundledIconFontFamily;

    /// <summary>Default text size used by the theme (the design's <c>--fs: 15px</c>).</summary>
    public int FontSize { get; set; } = 15;
}
