#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Retro95;

/// <summary>
/// Centralized color tokens for the Retro95 theme. Values mirror the CSS
/// custom properties in the source mockup (gum-styles.css <c>.rc</c> palette —
/// the Win95-era "Classic" block).
/// </summary>
public static class Retro95Colors
{
    /// <summary>Battleship-gray control surface (<c>--bg</c>, <c>#C0C0C0</c>). The default
    /// fill color for buttons, scroll bars, window chrome, menu bars, etc.</summary>
    public static readonly Color Surface = new Color(192, 192, 192);

    /// <summary>White inset / text-input fill (<c>--win</c>, <c>#FFFFFF</c>).</summary>
    public static readonly Color WhiteFill = new Color(255, 255, 255);

    /// <summary>Primary text — pure black (<c>--txt</c>, <c>#000000</c>).</summary>
    public static readonly Color Text = new Color(0, 0, 0);

    /// <summary>Selection background — navy blue (<c>--sel</c>, <c>#000080</c>). Used for the
    /// title bar, menu-bar active item, and ListBox / ComboBox selected row.</summary>
    public static readonly Color Selection = new Color(0, 0, 128);

    /// <summary>Text-on-selection color (<c>--stx</c>, <c>#FFFFFF</c>).</summary>
    public static readonly Color SelectionText = new Color(255, 255, 255);

    /// <summary>Lighter selection band used inside <see cref="TextBoxVisual"/> /
    /// <see cref="PasswordBoxVisual"/>. The real Win95 selection is solid navy (<see cref="Selection"/>)
    /// and the OS inverted the text under it to white. Gum's V3 TextBox renders selection as a
    /// background <c>NineSlice</c> behind a single <c>TextRuntime</c> (no per-range text color), so
    /// painting it navy makes the black text underneath unreadable. We use a translucent navy here
    /// (alpha ~80) so the band still reads as "selection" but the black glyphs remain legible.
    /// ListBox / Menu / ComboBox-option selection use <see cref="Selection"/> directly because their
    /// item visuals swap text color to <see cref="SelectionText"/> on the Selected state.</summary>
    public static readonly Color TextBoxSelection = new Color(0, 0, 128, 80);

    /// <summary>Inner highlight, 1 px in from the outer white (<c>--hi2</c>, <c>#DFDFDF</c>).</summary>
    public static readonly Color HighlightInner = new Color(223, 223, 223);

    /// <summary>Outer highlight on raised bevels (<c>--hi</c>, <c>#FFFFFF</c>).</summary>
    public static readonly Color HighlightOuter = new Color(255, 255, 255);

    /// <summary>Inner shadow, 1 px in from the outer dark (<c>--sha</c>, <c>#808080</c>).</summary>
    public static readonly Color ShadowInner = new Color(128, 128, 128);

    /// <summary>Outer shadow on raised bevels (<c>--sha2</c>, <c>#404040</c>).</summary>
    public static readonly Color ShadowOuter = new Color(64, 64, 64);

    /// <summary>Disabled text — mid-gray (<c>--dis</c>, <c>#808080</c>). The Win95 convention is
    /// to also stamp a 1 px white drop-shadow under disabled labels to mimic the etched look,
    /// but the runtime has no equivalent — we just gray the text.</summary>
    public static readonly Color DisabledText = new Color(128, 128, 128);

    /// <summary>Hairline divider used between menu bar and client area (<c>#808080</c>).</summary>
    public static readonly Color HairlineDivider = new Color(128, 128, 128);

    /// <summary>Hover background tint for buttons (slight off-white over the surface,
    /// <c>#CACACA</c> — barely perceptible but it is what the CSS specifies).</summary>
    public static readonly Color SurfaceHover = new Color(202, 202, 202);

    /// <summary>Hover background for text input fields (<c>#F8F8F8</c>).</summary>
    public static readonly Color WhiteHover = new Color(248, 248, 248);

    /// <summary>"Black" window-shadow color (Win95 uses a solid 2 px hard offset shadow on
    /// floating windows — <c>2px 2px 0 #000</c>).</summary>
    public static readonly Color WindowShadow = new Color(0, 0, 0);

    /// <summary>Disabled-thumb fill for the slider (<c>#A8A8A8</c>).</summary>
    public static readonly Color DisabledThumb = new Color(168, 168, 168);
}
