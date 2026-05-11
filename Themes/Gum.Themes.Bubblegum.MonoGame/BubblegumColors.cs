using Microsoft.Xna.Framework;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Centralized color tokens for the Bubblegum theme. Values mirror the CSS
/// custom properties in the source mockup (gum-styles.css `.bb` palette).
/// </summary>
public static class BubblegumColors
{
    /// <summary>Page background (<c>--bg</c>, <c>#FFF0F9</c>).</summary>
    public static readonly Color Background = new Color(255, 240, 249);

    /// <summary>Surface tier 1 — control fills (<c>--s1</c>, <c>#FFFFFF</c>).</summary>
    public static readonly Color Surface1 = new Color(255, 255, 255);

    /// <summary>Default border (<c>--bd</c>, <c>#F0AACC</c>).</summary>
    public static readonly Color Border = new Color(240, 170, 204);

    /// <summary>Focused border / hover (<c>--bdf</c>, <c>#FF6B9D</c>).</summary>
    public static readonly Color BorderFocused = new Color(255, 107, 157);

    /// <summary>Accent — focus, fill, selection (<c>--acc</c>, <c>#FF6B9D</c>).</summary>
    public static readonly Color Accent = new Color(255, 107, 157);

    /// <summary>Accent light — soft fills, ticks, badges (<c>--accl</c>, <c>#FFDEED</c>).</summary>
    public static readonly Color AccentLight = new Color(255, 222, 237);

    /// <summary>Accent dark — pressed / pushed (<c>--accd</c>, <c>#CC4475</c>).</summary>
    public static readonly Color AccentDark = new Color(204, 68, 117);

    /// <summary>Button hover fill — a lifted, lighter pink (<c>#FF8FB8</c>).</summary>
    public static readonly Color AccentHover = new Color(255, 143, 184);

    /// <summary>Primary text — deep eggplant for legibility on pastel surfaces (<c>--txt</c>, <c>#3D1155</c>).</summary>
    public static readonly Color Text = new Color(61, 17, 85);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#A07BB0</c>).</summary>
    public static readonly Color Muted = new Color(160, 123, 176);

    /// <summary>Disabled fill / disabled border (<c>--dis</c>, <c>#CDAEDD</c>).</summary>
    public static readonly Color Disabled = new Color(205, 174, 221);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#C9A8D9</c>).</summary>
    public static readonly Color Placeholder = new Color(201, 168, 217);

    /// <summary>Faint divider used between rows (<c>#F5D0E8</c>).</summary>
    public static readonly Color Divider = new Color(245, 208, 232);

    /// <summary>Disabled checkbox / radio fill (<c>#F5EAF8</c>).</summary>
    public static readonly Color DisabledFill = new Color(245, 234, 248);

    /// <summary>Soft pink-tinted shadow used under floating chrome (<c>rgba(255,107,157,.4)</c> ≈ <c>#FFB8D2</c>). Alpha is baked into the layered stack rather than the color itself.</summary>
    public static readonly Color Shadow = new Color(255, 107, 157, 100);
}
