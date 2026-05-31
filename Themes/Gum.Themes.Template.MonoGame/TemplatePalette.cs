using Microsoft.Xna.Framework;
// Brings in ColorExtensions.Adjust / ToGrayscale (lighten/darken helpers shipped
// with V3 styling). Used below to COMPUTE derived state colors instead of
// hand-storing them.
using Gum.Forms.DefaultVisuals.V3;

namespace Gum.Themes.Template;

/// <summary>
/// The Template theme's color palette - the one place colors are declared.
/// Every visual in this theme reads its colors from here, so restyling the
/// theme is (mostly) a matter of editing this file.
///
/// This is the standard shape for a Gum theme palette:
///
/// <list type="number">
/// <item><b>Base tokens</b> - transcribed 1:1 from the source design's CSS custom
/// properties (the <c>:root { --bg: ...; }</c> block). Each is documented with the
/// CSS variable name and hex it came from so the mapping back to the mockup stays
/// obvious. THIS is the section you fill in when cloning the template from an HTML
/// design.</item>
/// <item><b>Derived colors</b> - hover / pressed / selection tints computed from the
/// base tokens via <see cref="ColorExtensions.Adjust"/> (lighten/darken by a
/// percentage). Computing them keeps the palette small and keeps related colors in
/// sync. If your design specifies an exact value for one of these (rather than "the
/// base color, a bit lighter"), replace the computed property with an explicit
/// <c>static readonly Color</c> - see <see cref="DisabledFill"/> for the explicit
/// form.</item>
/// </list>
///
/// Kept as a <c>static</c> class because a theme has exactly one palette. It is the
/// theme's equivalent of V3's <see cref="Gum.Forms.DefaultVisuals.V3.Colors"/>, but
/// it is intentionally NOT a subclass of (or reference to) any shared base type -
/// each theme owns its full palette so the theme stays a self-contained, copyable
/// reference.
/// </summary>
public static class TemplatePalette
{
    // ---- Base tokens (transcribe from the design's :root block) -------------

    /// <summary>App background (<c>--bg</c>, <c>#1A1B1E</c>).</summary>
    public static readonly Color Background = new Color(26, 27, 30);

    /// <summary>Surface tier 1 - default control fill (<c>--s1</c>, <c>#252526</c>).</summary>
    public static readonly Color Surface1 = new Color(37, 37, 38);

    /// <summary>Surface tier 2 - hovered / raised surface (<c>--s2</c>, <c>#2D2D30</c>).</summary>
    public static readonly Color Surface2 = new Color(45, 45, 48);

    /// <summary>Default border (<c>--bd</c>, <c>#3C3C3C</c>).</summary>
    public static readonly Color Border = new Color(60, 60, 60);

    /// <summary>Hovered border (<c>--bdh</c>, <c>#5A5A5A</c>).</summary>
    public static readonly Color BorderHover = new Color(90, 90, 90);

    /// <summary>Accent - focus, fills, selection (<c>--acc</c>, <c>#007ACC</c>).</summary>
    public static readonly Color Accent = new Color(0, 122, 204);

    /// <summary>Primary text (<c>--txt</c>, <c>#D4D4D4</c>).</summary>
    public static readonly Color Text = new Color(212, 212, 212);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#888888</c>).</summary>
    public static readonly Color Muted = new Color(136, 136, 136);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#6A6A6A</c>).</summary>
    public static readonly Color Placeholder = new Color(106, 106, 106);

    // ---- Disabled tokens (explicit - muted, not derived) --------------------

    /// <summary>Disabled control fill (<c>#1C1C1C</c>).</summary>
    public static readonly Color DisabledFill = new Color(28, 28, 28);

    /// <summary>Disabled border (<c>#292929</c>).</summary>
    public static readonly Color DisabledBorder = new Color(41, 41, 41);

    /// <summary>Disabled text (<c>#454545</c>).</summary>
    public static readonly Color DisabledText = new Color(69, 69, 69);

    // ---- Derived colors (computed from the base tokens via Adjust) ----------
    // Replace any of these with a `static readonly Color` if your design pins an
    // exact value rather than "the base color, lighter/darker by N%".

    /// <summary>Fill for a hovered control - one step up from <see cref="Surface1"/>.</summary>
    public static Color HoverFill => Surface2;

    /// <summary>Fill for a pressed control - a step darker than <see cref="Surface1"/>
    /// so a press reads as a transient interaction.</summary>
    public static Color PressedFill => Surface1.Adjust(-20f);

    /// <summary>Brighter accent for hover on an accent-filled element (e.g. a slider thumb).</summary>
    public static Color AccentHover => Accent.Adjust(+15f);

    /// <summary>Darker accent for press on an accent-filled element.</summary>
    public static Color AccentPressed => Accent.Adjust(-20f);

    /// <summary>Fill for a disabled accent-filled element (e.g. slider thumb) - the
    /// accent drained to a neutral gray.</summary>
    public static Color DisabledAccent => new Color(62, 62, 66);
}
