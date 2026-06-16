#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Neon;

/// <summary>
/// Centralized color tokens for the Neon / Cyberpunk theme. Values mirror the
/// CSS custom properties in the source mockup (gum-styles.css <c>.nc</c>
/// palette).
/// </summary>
public static class NeonColors
{
    /// <summary>Page background (<c>--bg</c>, <c>#060612</c>) — near-black with a faint blue cast.</summary>
    public static readonly Color Background = new Color(6, 6, 18);

    /// <summary>Surface tier 1 — control fills (<c>--s1</c>, <c>#0D0D22</c>).</summary>
    public static readonly Color Surface1 = new Color(13, 13, 34);

    /// <summary>Surface tier 2 — title bars, dropdown headers, scroll buttons (<c>--s2</c>, <c>#131330</c>).</summary>
    public static readonly Color Surface2 = new Color(19, 19, 48);

    /// <summary>Default border (<c>--bd</c>, <c>#1E1E50</c>) — saturated indigo.</summary>
    public static readonly Color Border = new Color(30, 30, 80);

    /// <summary>Hover border (<c>--bdh</c>, <c>#003355</c>) — dim cyan stand-in for sub-accent hover.</summary>
    public static readonly Color BorderHover = new Color(0, 51, 85);

    /// <summary>Accent — focus, fill, selection (<c>--acc</c>, <c>#00E5FF</c>) — bright cyan.</summary>
    public static readonly Color Accent = new Color(0, 229, 255);

    /// <summary>Accent translucent fill (<c>--accd</c>, <c>rgba(0,229,255,.12)</c>) — used for selected rows and pushed states.</summary>
    public static readonly Color AccentDim = new Color(0, 229, 255, 31);

    /// <summary>Primary text — pale cyan-white (<c>--txt</c>, <c>#C8FAFF</c>).</summary>
    public static readonly Color Text = new Color(200, 250, 255);

    /// <summary>Muted / secondary text (<c>--mu</c>, <c>#4A6080</c>) — desaturated steel-blue.</summary>
    public static readonly Color Muted = new Color(74, 96, 128);

    /// <summary>Disabled fill / disabled border (<c>--dis</c>, <c>#151530</c>) — near-bg dark indigo.</summary>
    public static readonly Color Disabled = new Color(21, 21, 48);

    /// <summary>Disabled border (slightly lighter than <see cref="Disabled"/>) — used uniformly across controls (<c>#181838</c>).</summary>
    public static readonly Color DisabledBorder = new Color(24, 24, 56);

    /// <summary>Placeholder text (<c>--ph</c>, <c>#2A3850</c>).</summary>
    public static readonly Color Placeholder = new Color(42, 56, 80);

    /// <summary>Row divider between list items (<c>#0F0F28</c>, derived from CSS <c>.nc-mrow</c>).</summary>
    public static readonly Color Divider = new Color(15, 15, 40);

    /// <summary>
    /// Hot pink danger accent — used on the Window close button only
    /// (<c>#FF0099</c>). CSS bleeds magenta in for visual contrast against
    /// the cyan-dominated chrome.
    /// </summary>
    public static readonly Color Danger = new Color(255, 0, 153);

    /// <summary>Pure white — used for pressed-state body text on Button/Slider thumb (CSS <c>color:#fff</c>).</summary>
    public static readonly Color White = Color.White;

    /// <summary>
    /// Cyan glow color used by the native Apos.Shapes dropshadow. The CSS
    /// spec uses <c>rgba(0,229,255,.5)</c> through <c>.8</c> on most glows;
    /// alpha is set per visual (see <see cref="NeonPalette"/> constants),
    /// while the RGB stays fixed to the accent.
    /// </summary>
    public static readonly Color Glow = new Color(0, 229, 255);
}
