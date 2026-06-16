#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
// Brings in ColorExtensions.Adjust / ToGrayscale (lighten/darken helpers shipped
// with V3 styling). Used below to COMPUTE derived state colors instead of
// hand-storing them.
using Gum.Forms.DefaultVisuals.V3;

namespace Gum.Themes.Hazard;

/// <summary>
/// The Hazard theme's color palette - the one place colors are declared. Every
/// visual in this theme reads its colors from here, so a restyle touches one file.
///
/// Transcribed from the "Salvage" design (an industrial space-salvage HUD inspired
/// by Hardspace: Shipbreaker): signature hazard-yellow on warm near-black, muted
/// gold borders, olive header bands. The base tokens below map 1:1 to that design's
/// CSS custom properties (the <c>.sv</c> :root block); each keeps its
/// <c>--var #hex</c> comment so the mapping back to the mockup stays auditable.
/// Derived state colors are computed from the base tokens via
/// <see cref="ColorExtensions.Adjust"/>.
/// </summary>
public static class HazardPalette
{
    // ---- Base tokens (transcribed from the design's .sv :root block) --------

    /// <summary>App background - near-black warm (<c>--bg</c>, <c>#0A0A08</c>).</summary>
    public static readonly Color Background = new Color(10, 10, 8);

    /// <summary>Black ink - text drawn on an accent-yellow fill (<c>--ink</c>, <c>#0A0A08</c>).</summary>
    public static readonly Color Ink = new Color(10, 10, 8);

    /// <summary>Olive header band - window title bars, top bands (<c>--band</c>, <c>#2C2810</c>).</summary>
    public static readonly Color Band = new Color(44, 40, 16);

    /// <summary>Surface tier 1 - default control fill, rows, fields (<c>--s1</c>, <c>#121007</c>).</summary>
    public static readonly Color Surface1 = new Color(18, 16, 7);

    /// <summary>Surface tier 2 - hovered / raised surface (<c>--s2</c>, <c>#1E1A0A</c>).</summary>
    public static readonly Color Surface2 = new Color(30, 26, 10);

    /// <summary>Default border - muted gold (<c>--bd</c>, <c>#4A3F16</c>).</summary>
    public static readonly Color Border = new Color(74, 63, 22);

    /// <summary>Hovered border - brighter gold (<c>--bdh</c>, <c>#8C751F</c>).</summary>
    public static readonly Color BorderHover = new Color(140, 117, 31);

    /// <summary>Accent - signature hazard yellow; focus rings, fills, selection
    /// (<c>--acc</c>, <c>#F4C81A</c>).</summary>
    public static readonly Color Accent = new Color(244, 200, 26);

    /// <summary>Selected-item background. This design fills a selected row / option
    /// with the full hazard <see cref="Accent"/> (and draws its text in
    /// <see cref="Ink"/>), rather than a muted selection tint.</summary>
    public static readonly Color Selection = new Color(244, 200, 26);

    /// <summary>Primary text - gold body / label text (<c>--txt</c>, <c>#E3B528</c>).</summary>
    public static readonly Color Text = new Color(227, 181, 40);

    /// <summary>Bright gold - hovered / emphasized text and values (<c>--txtb</c>, <c>#F8D43B</c>).</summary>
    public static readonly Color TextBright = new Color(248, 212, 59);

    /// <summary>Muted gold - secondary text, labels (<c>--mu</c>, <c>#786626</c>).</summary>
    public static readonly Color Muted = new Color(120, 102, 38);

    /// <summary>Placeholder text - the muted gold (<c>--mu</c>, <c>#786626</c>).</summary>
    public static readonly Color Placeholder = new Color(120, 102, 38);

    /// <summary>Text drawn on an accent-yellow fill (e.g. a ToggleButton in its On
    /// state, a selected item, a pressed button). Black, so the label stays legible
    /// on hazard yellow (<c>--ink</c>, <c>#0A0A08</c>).</summary>
    public static readonly Color PressedText = new Color(10, 10, 8);

    // ---- Disabled tokens (explicit - muted, not derived) --------------------

    /// <summary>Disabled control fill (<c>#100E07</c>).</summary>
    public static readonly Color DisabledFill = new Color(16, 14, 7);

    /// <summary>Disabled border (<c>#2A2410</c>).</summary>
    public static readonly Color DisabledBorder = new Color(42, 36, 16);

    /// <summary>Disabled text (<c>--dis</c>, <c>#45391A</c>).</summary>
    public static readonly Color DisabledText = new Color(69, 57, 26);

    /// <summary>Fill for a disabled accent element (e.g. slider thumb / fill) - the
    /// accent drained to muted gold (<c>--dis</c>, <c>#45391A</c>).</summary>
    public static readonly Color DisabledAccent = new Color(69, 57, 26);

    // ---- Derived / state colors --------------------------------------------

    /// <summary>Deep amber for a pressed accent element - pressed checked CheckBox,
    /// pressed slider thumb, text selection highlight (<c>--accd</c>, <c>#C9A30C</c>).</summary>
    public static readonly Color AccentPressed = new Color(201, 163, 12);

    /// <summary>Fill for a hovered control - one warm step up from <see cref="Surface1"/>,
    /// reading as the design's faint accent wash on hover.</summary>
    public static Color HoverFill => Surface2;

    /// <summary>Fill for a pressed (but not "on") control - a step darker than
    /// <see cref="Surface1"/> so a press reads as a transient interaction.</summary>
    public static Color PressedFill => Surface1.Adjust(-20f);

    /// <summary>Brighter accent for hover on an accent-filled element (e.g. a slider thumb).</summary>
    public static Color AccentHover => Accent.Adjust(+15f);
}
