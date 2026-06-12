// Companion enum to Gum.GumService (issue #3119) — lives in the same namespace so
// EnableZoomToWindow callers need only `using Gum;`.
namespace Gum;

/// <summary>
/// Controls which window axis drives the zoom factor in
/// <see cref="GumService.ZoomToWindow(WindowZoomMode, float)"/>.
/// </summary>
public enum WindowZoomMode
{
    /// <summary>
    /// Window height divided by reference height drives the zoom. The vertical axis fully
    /// fills the window; the horizontal axis gets extra space (wider window) or is cropped
    /// (narrower window). Recommended default for game UI, since monitor aspect ratios vary
    /// more horizontally than vertically.
    /// </summary>
    HeightDominant,

    /// <summary>
    /// Window width divided by reference width drives the zoom. The horizontal axis fully
    /// fills the window; the vertical axis gets extra space or is cropped depending on the
    /// window's aspect ratio.
    /// </summary>
    WidthDominant,
}
