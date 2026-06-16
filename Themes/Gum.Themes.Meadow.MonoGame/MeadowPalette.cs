#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Meadow;

/// <summary>
/// Derived / translucent color values used across multiple Meadow visuals. Kept
/// separate from <see cref="MeadowColors"/> so the canonical CSS-token palette
/// stays 1:1 with the source mockup, while focus-ring alphas, hover tints, and
/// drop-shadow colors (which combine or fade the base tokens) live here.
/// </summary>
internal static class MeadowPalette
{
    /// <summary>
    /// Soft sage halo for CheckBox / RadioButton focus rings (CSS
    /// <c>box-shadow: 0 0 0 3px var(--sage)</c>). Rendered as a 3 px stroke; the
    /// near-opaque sage carries the cozy "selected glow" reading.
    /// </summary>
    public static readonly Color SageFocusRing = new Color(188, 221, 201);

    /// <summary>
    /// Translucent sky-blue halo for text-input / combo focus rings (CSS
    /// <c>box-shadow: 0 0 0 3px rgba(70,173,230,.25)</c> ≈ alpha 64).
    /// </summary>
    public static readonly Color BlueFocusRing = new Color(70, 173, 230, 70);

    /// <summary>Hover background tint for list rows (<c>--peachl</c>, from
    /// <c>.pp-lb-item.hov</c>).</summary>
    public static readonly Color HoverRow = MeadowColors.PeachLight;

    /// <summary>Hover background tint for combo / menu options
    /// (<c>--peachl</c>, from <c>.pp-cbo-opt.hov</c>).</summary>
    public static readonly Color HoverOption = MeadowColors.PeachLight;

    /// <summary>Selected-row fill — soft sage band (<c>--sage</c>, from
    /// <c>.pp-lb-item.sel</c>).</summary>
    public static readonly Color SelectedRow = MeadowColors.Sage;

    /// <summary>Inset outline on a selected row (<c>--saged</c>, from
    /// <c>.pp-lb-item.sel box-shadow: inset 0 0 0 2px var(--saged)</c>).</summary>
    public static readonly Color SelectedRowInset = MeadowColors.SageDark;

    /// <summary>Selected-row text — deep teal for contrast on the sage band
    /// (<c>--teald</c>).</summary>
    public static readonly Color SelectedRowText = MeadowColors.TealDark;

    /// <summary>
    /// Warm brown drop shadow under the slider thumb (CSS
    /// <c>box-shadow: 0 2px 5px rgba(160,110,70,.35)</c>). Per the gum-theming
    /// skill, the CSS-literal alpha (~89) reads too faint in sRGB-composited
    /// Apos.Shapes, so it is bumped to ~120.
    /// </summary>
    public static readonly Color ThumbShadow = new Color(160, 110, 70, 120);

    /// <summary>
    /// Warm brown drop shadow under floating Windows (CSS
    /// <c>box-shadow: 0 14px 30px rgba(150,110,70,.22)</c> ≈ alpha 56). Bumped
    /// ~1.6× to alpha 90 so the window reads as clearly lifted off the page.
    /// </summary>
    public static readonly Color WindowShadow = new Color(150, 110, 70, 90);
}
