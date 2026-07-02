#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Meadow;

/// <summary>
/// Root of the Meadow theme's mutable per-theme styling. Mutate
/// <see cref="ActiveStyle"/>'s <see cref="Colors"/>/<see cref="Text"/> before calling
/// <see cref="MeadowTheme.Apply"/> to restyle the theme without forking its source —
/// mirrors the "mutate before construct" ordering of Gum's own
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>.
/// </summary>
public class MeadowStyling
{
    /// <summary>
    /// The active Meadow styling instance. Get-only from outside this assembly so ordinary
    /// code can't construct a second instance and forget to activate it; still a settable
    /// property internally so a future theme-variant loader has a path to replace it.
    /// </summary>
    public static MeadowStyling ActiveStyle { get; private set; } = new();

    public MeadowColors Colors { get; } = new();
    public MeadowText Text { get; } = new();
}

/// <summary>
/// Centralized color tokens for the Meadow theme — a cozy cottagecore palette of
/// cream, sage, peach, teal, sky-blue, and coral. Values mirror the CSS custom
/// properties in the source mockup (gum-styles-meadow.css <c>.pp</c> palette);
/// each carries the <c>--var #hex</c> it came from so the mapping back to the
/// mockup stays auditable, plus derived tints that used to live in an
/// internal-only <c>MeadowPalette</c> — merged here as ordinary instance
/// properties, public like the rest of the palette.
/// <para>
/// Meadow is intentionally polychromatic: unlike a monochrome accent theme, the
/// interactive roles are split across hue families — blue carries buttons / focus,
/// sage carries selection / check state, coral carries slider + scrollbar fills,
/// peach carries input chrome, teal carries text + title bars. The tokens are
/// therefore named by hue (e.g. <see cref="Blue"/>) rather than by a single
/// "Accent" role; each visual picks the family that fits its role.
/// </para>
/// </summary>
public class MeadowColors
{
    // ---- Cream (page + raised surfaces) -------------------------------------

    /// <summary>Page background (<c>--cream</c>, <c>#F7EDD6</c>).</summary>
    public Color Cream { get; set; } = new Color(247, 237, 214);

    /// <summary>Raised surface — window body, list/combo/splitter panels, tooltip
    /// (<c>--cream2</c>, <c>#FCF5E6</c>).</summary>
    public Color Cream2 { get; set; } = new Color(252, 245, 230);

    // ---- Peach (input chrome) ----------------------------------------------

    /// <summary>Peach mid tone (<c>--peach</c>, <c>#F7DBBA</c>).</summary>
    public Color Peach { get; set; } = new Color(247, 219, 186);

    /// <summary>Peach dark — input borders, slider / scrollbar track, dashed
    /// container outlines, splitter divider (<c>--peachd</c>, <c>#EFC8A0</c>).</summary>
    public Color PeachDark { get; set; } = new Color(239, 200, 160);

    /// <summary>Peach light — text-input / combo fill, hovered list row
    /// (<c>--peachl</c>, <c>#FBEAD4</c>).</summary>
    public Color PeachLight { get; set; } = new Color(251, 234, 212);

    // ---- Sage (selection + check state) ------------------------------------

    /// <summary>Sage — selected-row fill, check/radio focus ring
    /// (<c>--sage</c>, <c>#BCDDC9</c>).</summary>
    public Color Sage { get; set; } = new Color(188, 221, 201);

    /// <summary>Sage light (<c>--sagel</c>, <c>#D3E9DA</c>).</summary>
    public Color SageLight { get; set; } = new Color(211, 233, 218);

    /// <summary>Sage dark — checked CheckBox fill, selected RadioButton inner dot,
    /// selection inset border (<c>--saged</c>, <c>#84C2A6</c>).</summary>
    public Color SageDark { get; set; } = new Color(132, 194, 166);

    // ---- Teal (text + title bars) ------------------------------------------

    /// <summary>Teal — title bars, badges, accent text (<c>--teal</c>, <c>#2E8576</c>).</summary>
    public Color Teal { get; set; } = new Color(46, 133, 118);

    /// <summary>Teal dark — primary control / label text (<c>--teald</c>, <c>#1E6A5B</c>).</summary>
    public Color TealDark { get; set; } = new Color(30, 106, 91);

    // ---- Blue (buttons + focus) --------------------------------------------

    /// <summary>Sky blue — Button fill, focus border (<c>--blue</c>, <c>#46ADE6</c>).</summary>
    public Color Blue { get; set; } = new Color(70, 173, 230);

    /// <summary>Blue dark — Button drop-shadow edge + pressed fill
    /// (<c>--blued</c>, <c>#2E93D2</c>).</summary>
    public Color BlueDark { get; set; } = new Color(46, 147, 210);

    /// <summary>Blue hover — lifted Button fill (<c>#5EBBF0</c>, from <c>.pp-btn.hov</c>).</summary>
    public Color BlueHover { get; set; } = new Color(94, 187, 240);

    // ---- Coral (slider + scrollbar fills) ----------------------------------

    /// <summary>Coral — slider fill, scrollbar thumb (<c>--coral</c>, <c>#ED9A78</c>).</summary>
    public Color Coral { get; set; } = new Color(237, 154, 120);

    /// <summary>Coral dark — slider chevrons, combo / scrollbar arrows, scrollbar
    /// thumb hover (<c>--corald</c>, <c>#DE7E58</c>).</summary>
    public Color CoralDark { get; set; } = new Color(222, 126, 88);

    // ---- Neutrals + disabled -----------------------------------------------

    /// <summary>Muted / secondary + placeholder text (<c>--mu</c>, <c>#B49C84</c>).</summary>
    public Color Muted { get; set; } = new Color(180, 156, 132);

    /// <summary>Disabled fill / border (<c>--dis</c>, <c>#D8CDBA</c>).</summary>
    public Color Disabled { get; set; } = new Color(216, 205, 186);

    /// <summary>Disabled text ink (<c>--disink</c>, <c>#B7A88E</c>).</summary>
    public Color DisabledInk { get; set; } = new Color(183, 168, 142);

    /// <summary>White — CheckBox / RadioButton fill, Button label, check glyph,
    /// slider thumb (<c>--white</c>, <c>#FFFFFF</c>).</summary>
    public Color White { get; set; } = new Color(255, 255, 255);

    /// <summary>Disabled slider fill — a desaturated coral/tan (<c>#C9BCA8</c>,
    /// from <c>.pp-sldr.dis .pp-sldr-fill</c>).</summary>
    public Color DisabledSliderFill { get; set; } = new Color(201, 188, 168);

    // --- Promoted from the former internal MeadowPalette --------------------

    /// <summary>
    /// Soft sage halo for CheckBox / RadioButton focus rings (CSS
    /// <c>box-shadow: 0 0 0 3px var(--sage)</c>). Rendered as a 3 px stroke; the
    /// near-opaque sage carries the cozy "selected glow" reading.
    /// </summary>
    public Color SageFocusRing { get; set; } = new Color(188, 221, 201);

    /// <summary>
    /// Translucent sky-blue halo for text-input / combo focus rings (CSS
    /// <c>box-shadow: 0 0 0 3px rgba(70,173,230,.25)</c> ≈ alpha 64).
    /// </summary>
    public Color BlueFocusRing { get; set; } = new Color(70, 173, 230, 70);

    /// <summary>Hover background tint for list rows (<c>--peachl</c>, from
    /// <c>.pp-lb-item.hov</c>).</summary>
    public Color HoverRow => PeachLight;

    /// <summary>Hover background tint for combo / menu options
    /// (<c>--peachl</c>, from <c>.pp-cbo-opt.hov</c>).</summary>
    public Color HoverOption => PeachLight;

    /// <summary>Selected-row fill — soft sage band (<c>--sage</c>, from
    /// <c>.pp-lb-item.sel</c>).</summary>
    public Color SelectedRow => Sage;

    /// <summary>Inset outline on a selected row (<c>--saged</c>, from
    /// <c>.pp-lb-item.sel box-shadow: inset 0 0 0 2px var(--saged)</c>).</summary>
    public Color SelectedRowInset => SageDark;

    /// <summary>Selected-row text — deep teal for contrast on the sage band
    /// (<c>--teald</c>).</summary>
    public Color SelectedRowText => TealDark;

    /// <summary>
    /// Warm brown drop shadow under the slider thumb (CSS
    /// <c>box-shadow: 0 2px 5px rgba(160,110,70,.35)</c>). Per the gum-theming
    /// skill, the CSS-literal alpha (~89) reads too faint in sRGB-composited
    /// Apos.Shapes, so it is bumped to ~120.
    /// </summary>
    public Color ThumbShadow { get; set; } = new Color(160, 110, 70, 120);

    /// <summary>
    /// Warm brown drop shadow under floating Windows (CSS
    /// <c>box-shadow: 0 14px 30px rgba(150,110,70,.22)</c> ≈ alpha 56). Bumped
    /// ~1.6× to alpha 90 so the window reads as clearly lifted off the page.
    /// </summary>
    public Color WindowShadow { get; set; } = new Color(150, 110, 70, 90);

    // --- 4-token guardrail ---------------------------------------------------
    // Every theme's Apply() pushes these into V3.Styling.ActiveStyle.Colors for the stock,
    // un-subclassed V3 visuals (e.g. Label) Meadow leaves in place. Meadow's own polychromatic
    // vocabulary already covers these concepts under different names, so they're exposed here
    // as get-only aliases — mutating TealDark/Muted/Cream2/Blue (Meadow's real, settable tokens)
    // is reflected automatically, the same "reactivity is free" behavior as any other derived color.

    /// <summary>Alias for <see cref="TealDark"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextPrimary</c>.</summary>
    public Color TextPrimary => TealDark;

    /// <summary>Alias for <see cref="Muted"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.TextMuted</c>.</summary>
    public Color TextMuted => Muted;

    /// <summary>Alias for <see cref="Cream2"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Primary</c>.</summary>
    public Color Primary => Cream2;

    /// <summary>Alias for <see cref="Blue"/> — the guardrail-canonical name synced to <c>V3.Styling.ActiveStyle.Colors.Accent</c>.</summary>
    public Color Accent => Blue;
}

/// <summary>
/// Font selection for the Meadow theme. Registration of the bundled TTF bytes is
/// separate (see <see cref="MeadowTheme.RegisterBundledFonts"/>) — reassigning these
/// before <see cref="MeadowTheme.Apply"/> only changes which already-registered
/// family visuals select.
/// <para>
/// Meadow ships two user-facing typefaces: <b>Baloo 2</b> (rounded display face)
/// for buttons, check / radio labels, and window titles — <see cref="FontFamily"/>,
/// which flows to controls that read <c>Styling.ActiveStyle.Text</c> — and
/// <b>Quicksand</b> (<see cref="BodyFontFamily"/>) for text-entry / list / menu
/// content, which the relevant visuals opt into explicitly via their
/// <c>TextInstance.Font</c>.
/// </para>
/// </summary>
public class MeadowText
{
    /// <summary>
    /// Family visuals use for display/label text (buttons, check/radio labels,
    /// window titles). Defaults to the bundled Baloo 2 family.
    /// </summary>
    public string FontFamily { get; set; } = MeadowTheme.BundledFontFamily;

    /// <summary>
    /// Family used for text-entry / list / menu content. Defaults to the bundled
    /// Quicksand family. Set <c>TextInstance.Font = MeadowStyling.ActiveStyle.Text.BodyFontFamily</c>
    /// in a visual to opt into it (the default flowing to most controls is <see cref="FontFamily"/>).
    /// </summary>
    public string BodyFontFamily { get; set; } = MeadowTheme.BundledBodyFontFamily;

    /// <summary>
    /// Family used for glyphs the body fonts don't cover (check marks, dropdown
    /// chevrons — Dingbats and Geometric Shapes blocks). Defaults to the bundled
    /// DejaVu Sans Mono icon family.
    /// </summary>
    public string IconFontFamily { get; set; } = MeadowTheme.BundledIconFontFamily;

    /// <summary>Default text size used by the theme. Matches the source mockup's <c>--fs</c> token (15px).</summary>
    public int FontSize { get; set; } = 15;
}
