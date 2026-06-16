#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Meadow;

/// <summary>
/// Centralized color tokens for the Meadow theme — a cozy cottagecore palette of
/// cream, sage, peach, teal, sky-blue, and coral. Values mirror the CSS custom
/// properties in the source mockup (gum-styles-meadow.css <c>.pp</c> palette);
/// each carries the <c>--var #hex</c> it came from so the mapping back to the
/// mockup stays auditable.
/// <para>
/// Meadow is intentionally polychromatic: unlike a monochrome accent theme, the
/// interactive roles are split across hue families — blue carries buttons / focus,
/// sage carries selection / check state, coral carries slider + scrollbar fills,
/// peach carries input chrome, teal carries text + title bars. The tokens are
/// therefore named by hue (e.g. <see cref="Blue"/>) rather than by a single
/// "Accent" role; each visual picks the family that fits its role.
/// </para>
/// </summary>
public static class MeadowColors
{
    // ---- Cream (page + raised surfaces) -------------------------------------

    /// <summary>Page background (<c>--cream</c>, <c>#F7EDD6</c>).</summary>
    public static readonly Color Cream = new Color(247, 237, 214);

    /// <summary>Raised surface — window body, list/combo/splitter panels, tooltip
    /// (<c>--cream2</c>, <c>#FCF5E6</c>).</summary>
    public static readonly Color Cream2 = new Color(252, 245, 230);

    // ---- Peach (input chrome) ----------------------------------------------

    /// <summary>Peach mid tone (<c>--peach</c>, <c>#F7DBBA</c>).</summary>
    public static readonly Color Peach = new Color(247, 219, 186);

    /// <summary>Peach dark — input borders, slider / scrollbar track, dashed
    /// container outlines, splitter divider (<c>--peachd</c>, <c>#EFC8A0</c>).</summary>
    public static readonly Color PeachDark = new Color(239, 200, 160);

    /// <summary>Peach light — text-input / combo fill, hovered list row
    /// (<c>--peachl</c>, <c>#FBEAD4</c>).</summary>
    public static readonly Color PeachLight = new Color(251, 234, 212);

    // ---- Sage (selection + check state) ------------------------------------

    /// <summary>Sage — selected-row fill, check/radio focus ring
    /// (<c>--sage</c>, <c>#BCDDC9</c>).</summary>
    public static readonly Color Sage = new Color(188, 221, 201);

    /// <summary>Sage light (<c>--sagel</c>, <c>#D3E9DA</c>).</summary>
    public static readonly Color SageLight = new Color(211, 233, 218);

    /// <summary>Sage dark — checked CheckBox fill, selected RadioButton inner dot,
    /// selection inset border (<c>--saged</c>, <c>#84C2A6</c>).</summary>
    public static readonly Color SageDark = new Color(132, 194, 166);

    // ---- Teal (text + title bars) ------------------------------------------

    /// <summary>Teal — title bars, badges, accent text (<c>--teal</c>, <c>#2E8576</c>).</summary>
    public static readonly Color Teal = new Color(46, 133, 118);

    /// <summary>Teal dark — primary control / label text (<c>--teald</c>, <c>#1E6A5B</c>).</summary>
    public static readonly Color TealDark = new Color(30, 106, 91);

    // ---- Blue (buttons + focus) --------------------------------------------

    /// <summary>Sky blue — Button fill, focus border (<c>--blue</c>, <c>#46ADE6</c>).</summary>
    public static readonly Color Blue = new Color(70, 173, 230);

    /// <summary>Blue dark — Button drop-shadow edge + pressed fill
    /// (<c>--blued</c>, <c>#2E93D2</c>).</summary>
    public static readonly Color BlueDark = new Color(46, 147, 210);

    /// <summary>Blue hover — lifted Button fill (<c>#5EBBF0</c>, from <c>.pp-btn.hov</c>).</summary>
    public static readonly Color BlueHover = new Color(94, 187, 240);

    // ---- Coral (slider + scrollbar fills) ----------------------------------

    /// <summary>Coral — slider fill, scrollbar thumb (<c>--coral</c>, <c>#ED9A78</c>).</summary>
    public static readonly Color Coral = new Color(237, 154, 120);

    /// <summary>Coral dark — slider chevrons, combo / scrollbar arrows, scrollbar
    /// thumb hover (<c>--corald</c>, <c>#DE7E58</c>).</summary>
    public static readonly Color CoralDark = new Color(222, 126, 88);

    // ---- Neutrals + disabled -----------------------------------------------

    /// <summary>Muted / secondary + placeholder text (<c>--mu</c>, <c>#B49C84</c>).</summary>
    public static readonly Color Muted = new Color(180, 156, 132);

    /// <summary>Disabled fill / border (<c>--dis</c>, <c>#D8CDBA</c>).</summary>
    public static readonly Color Disabled = new Color(216, 205, 186);

    /// <summary>Disabled text ink (<c>--disink</c>, <c>#B7A88E</c>).</summary>
    public static readonly Color DisabledInk = new Color(183, 168, 142);

    /// <summary>White — CheckBox / RadioButton fill, Button label, check glyph,
    /// slider thumb (<c>--white</c>, <c>#FFFFFF</c>).</summary>
    public static readonly Color White = new Color(255, 255, 255);

    /// <summary>Disabled slider fill — a desaturated coral/tan (<c>#C9BCA8</c>,
    /// from <c>.pp-sldr.dis .pp-sldr-fill</c>).</summary>
    public static readonly Color DisabledSliderFill = new Color(201, 188, 168);
}
