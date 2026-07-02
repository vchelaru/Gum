#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Editor;

/// <summary>
/// Root of the Editor theme's mutable per-theme styling. Mutate
/// <see cref="ActiveStyle"/>'s <see cref="Colors"/>/<see cref="Text"/> before calling
/// <see cref="EditorTheme.Apply"/> to restyle the theme without forking its source —
/// mirrors the "mutate before construct" ordering of Gum's own
/// <see cref="Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle"/>.
/// </summary>
public class EditorStyling
{
    /// <summary>
    /// The active Editor styling instance. Get-only from outside this assembly so ordinary
    /// code can't construct a second instance and forget to activate it; still a settable
    /// property internally so a future theme-variant loader has a path to replace it.
    /// </summary>
    public static EditorStyling ActiveStyle { get; private set; } = new();

    public EditorColors Colors { get; } = new();
    public EditorText Text { get; } = new();
}

/// <summary>
/// Centralized color tokens for the Editor theme. Editor previously had no palette class at
/// all — every visual inlined its own <c>new Color(...)</c> literals. These properties collect
/// the literals that were duplicated across two or more call sites (plus the 4-token guardrail,
/// which every theme's <c>Colors</c> must expose regardless of duplication — see below). Values
/// that appeared only once (e.g. the PropertyGridVisual row-stripe colors, ExpanderVisual's
/// header fill) were left as local literals; they aren't part of a shared vocabulary yet.
/// </summary>
public class EditorColors
{
    // --- 4-token guardrail -------------------------------------------------
    // Every theme's Apply()/ConfigureStyling() pushes these into V3.Styling.ActiveStyle.Colors
    // for the stock, un-subclassed V3 visuals (e.g. Label) Editor leaves in place. Editor never
    // had its own distinct vocabulary before this migration, so these are the theme's real,
    // settable tokens directly (no alias indirection needed, unlike DarkPro/Bubblegum which
    // already had differently-named equivalents to alias).

    /// <summary>Primary text color. Previously inlined in <c>EditorTheme.Apply</c>.</summary>
    public Color TextPrimary { get; set; } = new Color(180, 180, 180);

    /// <summary>Muted / secondary text color. Previously inlined in <c>EditorTheme.Apply</c>.</summary>
    public Color TextMuted { get; set; } = new Color(88, 88, 88);

    /// <summary>
    /// Primary surface/border gray. Previously inlined in <c>EditorTheme.Apply</c>
    /// (<c>styling.Colors.Primary</c>) and also used directly as the resting-state stroke/fill
    /// color on <c>ListBoxVisual</c>'s border and <c>ScrollBarVisual</c>'s thumb.
    /// </summary>
    public Color Primary { get; set; } = new Color(60, 60, 60);

    /// <summary>
    /// Accent — focus ring / caret. Previously inlined twice in <c>TextBoxVisual</c>
    /// (<c>CaretColor</c> and the Focused-state outline stroke) but never pushed into
    /// <c>V3.Styling.ActiveStyle.Colors.Accent</c> — see the Accent-sync bug fix in
    /// <see cref="EditorTheme.ConfigureStyling"/>.
    /// </summary>
    public Color Accent { get; set; } = new Color(192, 222, 255);

    // --- Additional deduplicated tokens -------------------------------------

    /// <summary>
    /// Hover/highlighted outline stroke. Previously inlined in <c>ButtonVisual</c>,
    /// <c>CheckBoxVisual</c> (both On/Off), <c>ComboBoxVisual</c>, and <c>TextBoxVisual</c>'s
    /// Highlighted-state stroke.
    /// </summary>
    public Color BorderHover { get; set; } = new Color(150, 150, 150);

    /// <summary>
    /// Pushed/pressed outline stroke. Previously inlined in <c>ButtonVisual</c>,
    /// <c>CheckBoxVisual</c> (both On/Off), and <c>ComboBoxVisual</c>.
    /// </summary>
    public Color BorderPushed { get; set; } = new Color(255, 255, 255);

    /// <summary>
    /// Selected/highlighted background fill. Previously inlined in
    /// <c>ListBoxItemVisual.SelectedBackgroundColor</c> and
    /// <c>TextBoxVisual.SelectionBackgroundColor</c>.
    /// </summary>
    public Color Selection { get; set; } = new Color(0, 92, 128);

    /// <summary>
    /// Panel/container background. Previously inlined in <c>ListBoxVisual.BackgroundColor</c>
    /// and <c>ScrollViewerVisual.BackgroundColor</c>.
    /// </summary>
    public Color PanelBackground { get; set; } = new Color(27, 27, 27);

    /// <summary>
    /// Recessed/sunken background — the darkest surface, used for content wells. Previously
    /// inlined in <c>ScrollBarVisual.TrackBackgroundColor</c> and
    /// <c>TextBoxVisual.BackgroundColor</c>.
    /// </summary>
    public Color RecessedBackground { get; set; } = new Color(10, 10, 10);
}

/// <summary>
/// Font selection for the Editor theme. Unlike the other shipped themes, Editor never bundles
/// or registers a TTF — it uses the host platform's system font ("Arial") as-is, so there is no
/// registration/selection split (no fixed internal <c>Bundled*FontFamily</c> constant) and no
/// icon font: <see cref="FontFamily"/> is just a plain, freely reassignable string.
/// </summary>
public class EditorText
{
    /// <summary>Family visuals use for body/control text. Defaults to the system "Arial" font.</summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>Default text size used by the theme. Matches the pre-migration literal (15px).</summary>
    public int FontSize { get; set; } = 15;
}
