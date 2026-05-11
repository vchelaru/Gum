using Microsoft.Xna.Framework;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Centralized color tokens for the Dark Pro theme. Values mirror the CSS
/// custom properties in the source mockup (gum-styles.css). Held as a
/// theme-local palette for now; once more visuals land this will be promoted
/// into a typed extension of <see cref="Gum.Forms.DefaultVisuals.V3.Colors"/>.
/// </summary>
public static class DarkProColors
{
    /// <summary>App background (<c>--bg</c>, <c>#1A1B1E</c>).</summary>
    public static readonly Color Background = new Color(26, 27, 30);

    /// <summary>Surface tier 1 — control fills (<c>--s1</c>, <c>#252526</c>).</summary>
    public static readonly Color Surface1 = new Color(37, 37, 38);

    /// <summary>Surface tier 2 — hovered control / dropdown header (<c>--s2</c>, <c>#2D2D30</c>).</summary>
    public static readonly Color Surface2 = new Color(45, 45, 48);

    /// <summary>Default border (<c>--bd</c>, <c>#3C3C3C</c>).</summary>
    public static readonly Color Border = new Color(60, 60, 60);

    /// <summary>Hovered border (<c>--bdh</c>, <c>#5A5A5A</c>).</summary>
    public static readonly Color BorderHover = new Color(90, 90, 90);

    /// <summary>Accent — focus, fill, selection (<c>--acc</c>, <c>#007ACC</c>).</summary>
    public static readonly Color Accent = new Color(0, 122, 204);

    /// <summary>Accent dark — pressed / pushed (<c>--accd</c>, <c>#094771</c>).</summary>
    public static readonly Color AccentDark = new Color(9, 71, 113);

    /// <summary>Primary text (<c>--txt</c>, <c>#D4D4D4</c>).</summary>
    public static readonly Color Text = new Color(212, 212, 212);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#888888</c>).</summary>
    public static readonly Color Muted = new Color(136, 136, 136);

    /// <summary>Disabled text (<c>--dis</c>, <c>#454545</c>).</summary>
    public static readonly Color DisabledText = new Color(69, 69, 69);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#6A6A6A</c>).</summary>
    public static readonly Color Placeholder = new Color(106, 106, 106);

    /// <summary>Disabled control fill — slightly darker than Surface1 (<c>#1C1C1C</c> from .dp-btn.dis).</summary>
    public static readonly Color DisabledFill = new Color(28, 28, 28);

    /// <summary>Disabled border (<c>#292929</c> from .dp-btn.dis).</summary>
    public static readonly Color DisabledBorder = new Color(41, 41, 41);

    /// <summary>
    /// Pressed-state fill — a step down from <see cref="Surface1"/> so press reads as a transient
    /// interaction rather than a state change. The source mockup specified <c>--accd</c>
    /// (full accent blue) for press, but a fully-blue press makes every button look "toggled"
    /// rather than "you just clicked me." Accent-fill-on-press is preserved for any future
    /// primary/default Button variant.
    /// </summary>
    public static readonly Color PressedFill = new Color(29, 29, 30);

    /// <summary>
    /// Light-blue text color from the source mockup's accent-fill press state (<c>#9DCFEE</c>).
    /// Currently unused — the active <see cref="PressedFill"/> uses normal <see cref="Text"/>.
    /// Retained for a future primary/default Button variant that paints accent on press.
    /// </summary>
    public static readonly Color PressedText = new Color(157, 207, 238);
}
