#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
// Brings in ColorExtensions.Adjust (lighten/darken helper shipped with V3
// styling). Used below to COMPUTE derived state colors instead of
// hand-storing them.
using Gum.Forms.DefaultVisuals.V3;

namespace Gum.Themes.Template;

/// <summary>
/// Root of the Template theme's mutable per-theme styling. Mutate
/// <see cref="ActiveStyle"/>'s <see cref="Colors"/>/<see cref="Text"/> before calling
/// <see cref="TemplateTheme.Apply"/> to restyle the theme without forking its source —
/// mirrors the "mutate before construct" ordering of Gum's own
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>.
/// </summary>
public class TemplateStyling
{
    /// <summary>
    /// The active Template styling instance. Get-only from outside this assembly so ordinary
    /// code can't construct a second instance and forget to activate it; still a settable
    /// property internally so a future theme-variant loader has a path to replace it.
    /// </summary>
    public static TemplateStyling ActiveStyle { get; private set; } = new();

    public TemplateColors Colors { get; } = new();
    public TemplateText Text { get; } = new();
}

/// <summary>
/// The Template theme's color tokens - the one place colors are declared. Every visual
/// in this theme reads its colors from <see cref="TemplateStyling.ActiveStyle"/>, so a
/// restyle touches one file - or, from outside this assembly, no file at all: a
/// consumer can mutate <c>TemplateStyling.ActiveStyle.Colors</c> and call
/// <see cref="TemplateTheme.Apply"/> to restyle the theme without forking its source.
///
/// <para>
/// This is the standard shape every Gum theme's styling follows: a mutable
/// <c>XyzStyling</c> root (see <see cref="TemplateStyling"/>) exposing an
/// <c>XyzColors</c> instance (this class) via <c>.Colors</c> and an <c>XyzText</c>
/// instance via <c>.Text</c>, reached through <c>XyzStyling.ActiveStyle</c>. A few
/// rules make up that shape:
/// </para>
///
/// <list type="number">
/// <item><b>Base tokens are settable instance properties</b>, transcribed 1:1 from
/// the source design's CSS custom properties (the <c>:root { --bg: ...; }</c> block).
/// Each carries the CSS variable name and hex it came from so the mapping back to the
/// mockup stays auditable. These are the colors that define the theme's identity -
/// the standard slots below are a starting vocabulary, not a fixed set: add tokens
/// for anything your design defines (extra accents, success/danger, ...) and remove
/// any you don't use.</item>
/// <item><b>Derived colors are get-only computed properties</b> - hover / pressed /
/// selection tints computed from the base tokens via <see cref="ColorExtensions.Adjust"/>
/// (lighten/darken by a percentage) rather than stored separately. Because they're
/// computed from a mutable base token instead of cached, changing e.g.
/// <see cref="Accent"/> after construction automatically changes
/// <see cref="AccentHover"/> too - no re-<c>Apply()</c> needed, since visuals already
/// read these properties live from each state's <c>.Apply</c> lambda rather than
/// snapshotting them at construction. If your design specifies an exact value for one
/// of these (rather than "the base color, a bit lighter"), use a settable property
/// with an explicit default instead - see <see cref="DisabledFill"/> for the explicit
/// form.</item>
/// <item><b>The 4-token guardrail is the one shared contract across themes.</b> Every
/// theme's <c>XyzColors</c> must expose <c>TextPrimary</c>/<c>TextMuted</c>/
/// <c>Primary</c>/<c>Accent</c> as <c>Color</c>-typed properties, because every
/// theme's <c>ConfigureStyling()</c> pushes exactly these 4 into
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>'s <c>Colors</c> for
/// the stock, un-subclassed V3 visuals (e.g. <c>Label</c>) the theme leaves in place.
/// Template's own vocabulary already has a token named <see cref="Accent"/>; the
/// other three are get-only aliases onto Template's real tokens
/// (<see cref="TextPrimary"/>, <see cref="TextMuted"/>, <see cref="Primary"/> below).
/// This is enforced by a reflection-based test in <c>Tests/Gum.Themes.Tests</c>
/// rather than a shared interface/base type - see point 4.</item>
/// <item><b>No shared base type across themes - by design.</b> <see cref="TemplateColors"/>
/// is a plain class, not a subclass of (or reference to) any cross-theme type. It is
/// the theme's equivalent of V3's own <see cref="Gum.Forms.DefaultVisuals.V3.Colors"/>,
/// but each theme owns its full, independent vocabulary so the theme stays a
/// self-contained, copyable reference - the only exception is the 4-token guardrail
/// above, and even that is enforced by a test, not an interface.</item>
/// </list>
/// </summary>
public class TemplateColors
{
    // ---- Base tokens (transcribe from the design's :root block) -------------
    // These slots are a starting vocabulary, not a fixed set: add tokens for
    // anything your design defines (extra accents, success/danger, ...) and remove
    // any you don't use. Every visual reads its colors from here.

    /// <summary>App background (<c>--bg</c>, <c>#1A1B1E</c>).</summary>
    public Color Background { get; set; } = new Color(26, 27, 30);

    /// <summary>Surface tier 1 - default control fill (<c>--s1</c>, <c>#252526</c>).</summary>
    public Color Surface1 { get; set; } = new Color(37, 37, 38);

    /// <summary>Surface tier 2 - hovered / raised surface (<c>--s2</c>, <c>#2D2D30</c>).</summary>
    public Color Surface2 { get; set; } = new Color(45, 45, 48);

    /// <summary>Default border (<c>--bd</c>, <c>#3C3C3C</c>).</summary>
    public Color Border { get; set; } = new Color(60, 60, 60);

    /// <summary>Hovered border (<c>--bdh</c>, <c>#5A5A5A</c>).</summary>
    public Color BorderHover { get; set; } = new Color(90, 90, 90);

    /// <summary>Accent - focus, fills, selection (<c>--acc</c>, <c>#007ACC</c>).</summary>
    public Color Accent { get; set; } = new Color(0, 122, 204);

    /// <summary>Dark accent used as the background of a selected / highlighted item
    /// (a selected ListBoxItem, an open MenuItem submenu). Muted enough that an
    /// unfocused list still reads as "this row is selected" without competing with
    /// the brighter <see cref="Accent"/> focus fill (<c>--accd</c>, <c>#094771</c>).</summary>
    public Color Selection { get; set; } = new Color(9, 71, 113);

    /// <summary>Primary text (<c>--txt</c>, <c>#D4D4D4</c>).</summary>
    public Color Text { get; set; } = new Color(212, 212, 212);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#888888</c>).</summary>
    public Color Muted { get; set; } = new Color(136, 136, 136);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#6A6A6A</c>).</summary>
    public Color Placeholder { get; set; } = new Color(106, 106, 106);

    /// <summary>Light-blue text used on an accent-filled element (e.g. a ToggleButton
    /// in its On state) so the label stays legible against the saturated
    /// <see cref="Accent"/> fill (<c>#9DCFEE</c>).</summary>
    public Color PressedText { get; set; } = new Color(157, 207, 238);

    // ---- Disabled tokens (explicit - muted, not derived) --------------------

    /// <summary>Disabled control fill (<c>#1C1C1C</c>).</summary>
    public Color DisabledFill { get; set; } = new Color(28, 28, 28);

    /// <summary>Disabled border (<c>#292929</c>).</summary>
    public Color DisabledBorder { get; set; } = new Color(41, 41, 41);

    /// <summary>Disabled text (<c>#454545</c>).</summary>
    public Color DisabledText { get; set; } = new Color(69, 69, 69);

    // ---- Derived colors (computed from the base tokens via Adjust) ----------
    // Replace any of these with a settable property if your design pins an
    // exact value rather than "the base color, lighter/darker by N%".

    /// <summary>Fill for a hovered control - one step up from <see cref="Surface1"/>.</summary>
    public Color HoverFill => Surface2;

    /// <summary>Fill for a pressed control - a step darker than <see cref="Surface1"/>
    /// so a press reads as a transient interaction.</summary>
    public Color PressedFill => Surface1.Adjust(-20f);

    /// <summary>Brighter accent for hover on an accent-filled element (e.g. a slider thumb).</summary>
    public Color AccentHover => Accent.Adjust(+15f);

    /// <summary>Darker accent for press on an accent-filled element.</summary>
    public Color AccentPressed => Accent.Adjust(-20f);

    /// <summary>Fill for a disabled accent-filled element (e.g. slider thumb) - the
    /// accent drained to a neutral gray.</summary>
    public Color DisabledAccent { get; set; } = new Color(62, 62, 66);

    /// <summary>Translucent <see cref="Accent"/> used for the Variants gallery's soft
    /// focus-ring glow (CheckBox, RadioButton, TextBox/PasswordBox). Computed - not
    /// cached in a <c>static readonly</c> field - so a restyle of <see cref="Accent"/>
    /// is picked up by any visual constructed after it, rather than being permanently
    /// baked to whatever <see cref="Accent"/> was the first time the type was touched.</summary>
    public Color AccentGlow => new Color((int)Accent.R, (int)Accent.G, (int)Accent.B, 110);

    /// <summary>Translucent <see cref="AccentPressed"/> used for the Variants
    /// gallery's slider-thumb drop shadow. Computed live for the same reason as
    /// <see cref="AccentGlow"/>.</summary>
    public Color AccentPressedGlow => new Color((int)AccentPressed.R, (int)AccentPressed.G, (int)AccentPressed.B, 130);

    // --- 4-token guardrail ---------------------------------------------------
    // Every theme's ConfigureStyling() pushes these into V3.Styling.ActiveStyle.Colors for
    // the stock, un-subclassed V3 visuals (e.g. Label) Template leaves in place. Template
    // already carries a token named "Accent" that matches the guardrail name exactly, so
    // only the other 3 need get-only aliases onto Template's real, settable tokens
    // (Text/Muted/Surface1) - mutating those is reflected automatically, the same
    // "reactivity is free" behavior as any other derived color.

    /// <summary>Alias for <see cref="Text"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextPrimary</c>.</summary>
    public Color TextPrimary => Text;

    /// <summary>Alias for <see cref="Muted"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextMuted</c>.</summary>
    public Color TextMuted => Muted;

    /// <summary>Alias for <see cref="Surface1"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Primary</c>.</summary>
    public Color Primary => Surface1;
}

/// <summary>
/// Font selection for the Template theme. Registration of the bundled TTF bytes is
/// separate (see <see cref="TemplateTheme.RegisterBundledFonts"/>) — reassigning
/// these before <see cref="TemplateTheme.Apply"/> only changes which
/// already-registered family visuals select.
/// <para>
/// Demonstrates the common "display + body" split: a personality face for
/// buttons/labels/titles (<see cref="FontFamily"/>) and a quieter face for
/// typed/list content (<see cref="BodyFontFamily"/>). Controls that render entered or
/// tabular text opt into the body face explicitly via
/// <c>TextInstance.Font = TemplateStyling.ActiveStyle.Text.BodyFontFamily</c> (see
/// TextBox, ComboBox, ListBoxItem, MenuItem, Tooltip in this theme). Omit
/// <see cref="BodyFontFamily"/> entirely if your theme uses a single family.
/// </para>
/// </summary>
public class TemplateText
{
    /// <summary>
    /// Family visuals use for display text (buttons, labels, titles). Defaults to
    /// the bundled DM Mono family.
    /// </summary>
    public string FontFamily { get; set; } = TemplateTheme.BundledFontFamily;

    /// <summary>
    /// Family visuals use for typed/list/menu content - the quieter face. Defaults
    /// to the bundled Nunito family.
    /// </summary>
    public string BodyFontFamily { get; set; } = TemplateTheme.BundledBodyFontFamily;

    /// <summary>
    /// Family used for glyphs the display/body fonts don't cover (check marks,
    /// combo/scrollbar arrows — Dingbats and Geometric Shapes blocks). Defaults to
    /// the bundled DejaVu Sans Mono icon family.
    /// </summary>
    public string IconFontFamily { get; set; } = TemplateTheme.BundledIconFontFamily;

    /// <summary>Default text size used by the theme.</summary>
    public int FontSize { get; set; } = 14;
}
