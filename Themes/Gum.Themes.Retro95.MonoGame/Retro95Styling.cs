#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Retro95;

/// <summary>
/// Root of the Retro95 theme's mutable per-theme styling. Mutate
/// <see cref="ActiveStyle"/>'s <see cref="Colors"/>/<see cref="Text"/> before calling
/// <see cref="Retro95Theme.Apply"/> to restyle the theme without forking its source —
/// mirrors the "mutate before construct" ordering of Gum's own
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>.
/// </summary>
public class Retro95Styling
{
    /// <summary>
    /// The active Retro95 styling instance. Get-only from outside this assembly so ordinary
    /// code can't construct a second instance and forget to activate it; still a settable
    /// property internally so a future theme-variant loader has a path to replace it.
    /// </summary>
    public static Retro95Styling ActiveStyle { get; private set; } = new();

    public Retro95Colors Colors { get; } = new();
    public Retro95Text Text { get; } = new();
}

/// <summary>
/// Centralized color tokens for the Retro95 theme. Values mirror the CSS
/// custom properties in the source mockup (gum-styles.css <c>.rc</c> palette —
/// the Win95-era "Classic" block).
/// </summary>
public class Retro95Colors
{
    /// <summary>Battleship-gray control surface (<c>--bg</c>, <c>#C0C0C0</c>). The default
    /// fill color for buttons, scroll bars, window chrome, menu bars, etc.</summary>
    public Color Surface { get; set; } = new Color(192, 192, 192);

    /// <summary>White inset / text-input fill (<c>--win</c>, <c>#FFFFFF</c>).</summary>
    public Color WhiteFill { get; set; } = new Color(255, 255, 255);

    /// <summary>Primary text — pure black (<c>--txt</c>, <c>#000000</c>).</summary>
    public Color Text { get; set; } = new Color(0, 0, 0);

    /// <summary>Selection background — navy blue (<c>--sel</c>, <c>#000080</c>). Used for the
    /// title bar, menu-bar active item, and ListBox / ComboBox selected row.</summary>
    public Color Selection { get; set; } = new Color(0, 0, 128);

    /// <summary>Text-on-selection color (<c>--stx</c>, <c>#FFFFFF</c>).</summary>
    public Color SelectionText { get; set; } = new Color(255, 255, 255);

    /// <summary>Lighter selection band used inside <see cref="TextBoxVisual"/> /
    /// <see cref="PasswordBoxVisual"/>. The real Win95 selection is solid navy (<see cref="Selection"/>)
    /// and the OS inverted the text under it to white. Gum's V3 TextBox renders selection as a
    /// background <c>NineSlice</c> behind a single <c>TextRuntime</c> (no per-range text color), so
    /// painting it navy makes the black text underneath unreadable. We use a translucent navy here
    /// (alpha ~80) so the band still reads as "selection" but the black glyphs remain legible.
    /// ListBox / Menu / ComboBox-option selection use <see cref="Selection"/> directly because their
    /// item visuals swap text color to <see cref="SelectionText"/> on the Selected state.</summary>
    public Color TextBoxSelection { get; set; } = new Color(0, 0, 128, 80);

    /// <summary>Inner highlight, 1 px in from the outer white (<c>--hi2</c>, <c>#DFDFDF</c>).</summary>
    public Color HighlightInner { get; set; } = new Color(223, 223, 223);

    /// <summary>Outer highlight on raised bevels (<c>--hi</c>, <c>#FFFFFF</c>).</summary>
    public Color HighlightOuter { get; set; } = new Color(255, 255, 255);

    /// <summary>Inner shadow, 1 px in from the outer dark (<c>--sha</c>, <c>#808080</c>).</summary>
    public Color ShadowInner { get; set; } = new Color(128, 128, 128);

    /// <summary>Outer shadow on raised bevels (<c>--sha2</c>, <c>#404040</c>).</summary>
    public Color ShadowOuter { get; set; } = new Color(64, 64, 64);

    /// <summary>Disabled text — mid-gray (<c>--dis</c>, <c>#808080</c>). The Win95 convention is
    /// to also stamp a 1 px white drop-shadow under disabled labels to mimic the etched look,
    /// but the runtime has no equivalent — we just gray the text.</summary>
    public Color DisabledText { get; set; } = new Color(128, 128, 128);

    /// <summary>Hairline divider used between menu bar and client area (<c>#808080</c>).</summary>
    public Color HairlineDivider { get; set; } = new Color(128, 128, 128);

    /// <summary>Hover background tint for buttons (slight off-white over the surface,
    /// <c>#CACACA</c> — barely perceptible but it is what the CSS specifies).</summary>
    public Color SurfaceHover { get; set; } = new Color(202, 202, 202);

    /// <summary>Hover background for text input fields (<c>#F8F8F8</c>).</summary>
    public Color WhiteHover { get; set; } = new Color(248, 248, 248);

    /// <summary>"Black" window-shadow color (Win95 uses a solid 2 px hard offset shadow on
    /// floating windows — <c>2px 2px 0 #000</c>).</summary>
    public Color WindowShadow { get; set; } = new Color(0, 0, 0);

    /// <summary>Disabled-thumb fill for the slider (<c>#A8A8A8</c>).</summary>
    public Color DisabledThumb { get; set; } = new Color(168, 168, 168);

    // --- 4-token guardrail -------------------------------------------------
    // Every theme's Apply() pushes these into V3.Styling.ActiveStyle.Colors for the stock,
    // un-subclassed V3 visuals (e.g. Label) Retro95 leaves in place. Retro95's own vocabulary
    // already covers these concepts under different names, so they're exposed here as get-only
    // aliases — mutating Text/DisabledText/Surface/Selection (Retro95's real, settable tokens)
    // is reflected automatically, the same "reactivity is free" behavior as any other derived color.

    /// <summary>Alias for <see cref="Text"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextPrimary</c>.</summary>
    public Color TextPrimary => Text;

    /// <summary>Alias for <see cref="DisabledText"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextMuted</c>.</summary>
    public Color TextMuted => DisabledText;

    /// <summary>Alias for <see cref="Surface"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Primary</c>.</summary>
    public Color Primary => Surface;

    /// <summary>Alias for <see cref="Selection"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Accent</c>.</summary>
    public Color Accent => Selection;
}

/// <summary>
/// Font selection for the Retro95 theme. Registration of the bundled TTF bytes is
/// separate (see <see cref="Retro95Theme.RegisterBundledFonts"/> is what registers them) —
/// reassigning these before <see cref="Retro95Theme.Apply"/> only changes which
/// already-registered family visuals select.
/// </summary>
public class Retro95Text
{
    /// <summary>
    /// Family visuals use for body/control text. Defaults to the bundled Nunito family —
    /// a stand-in for the proprietary, non-redistributable MS Sans Serif.
    /// </summary>
    public string FontFamily { get; set; } = Retro95Theme.BundledFontFamily;

    /// <summary>
    /// Family used for glyphs Nunito doesn't cover (check marks, close buttons, combo/scrollbar
    /// arrows). Defaults to the bundled DejaVu Sans Mono icon family.
    /// </summary>
    public string IconFontFamily { get; set; } = Retro95Theme.BundledIconFontFamily;

    /// <summary>Default text size used by the theme. Matches the source mockup's <c>--fs</c> token (13px).</summary>
    public int FontSize { get; set; } = 13;
}
