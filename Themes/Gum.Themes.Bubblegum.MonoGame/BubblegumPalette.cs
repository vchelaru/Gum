using Microsoft.Xna.Framework;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Derived color values used across multiple Bubblegum visuals. Kept separate
/// from <see cref="BubblegumColors"/> so the canonical CSS-token palette stays
/// 1:1 with the source mockup, and translucent / state-derived colors live here.
/// </summary>
internal static class BubblegumPalette
{
    /// <summary>
    /// Translucent accent for focus rings (matches CSS
    /// <c>rgba(255,107,157,.25)</c> ≈ alpha 64). The ring is stroked at 3 px in
    /// every visual; the alpha is what carries the "soft halo" reading.
    /// </summary>
    public static readonly Color FocusRing = new Color(255, 107, 157, 90);

    /// <summary>Hover background tint for list rows (<c>#FFF5FB</c> from .bb-lb-item.hov).</summary>
    public static readonly Color HoverRow = new Color(255, 245, 251);

    /// <summary>Hover background tint for combo-box / menu options (<c>#FFF0F6</c>).</summary>
    public static readonly Color HoverOption = new Color(255, 240, 246);

    /// <summary>Selected row fill — the same soft pink the CSS uses (<c>--accl</c>).</summary>
    public static readonly Color SelectedRow = BubblegumColors.AccentLight;

    /// <summary>
    /// Accent on selected-row text (<c>--accd</c>). Higher contrast against
    /// AccentLight than the default Text color, matching the CSS spec.
    /// </summary>
    public static readonly Color SelectedRowText = BubblegumColors.AccentDark;
}
