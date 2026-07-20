namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Narrow seam for the editor-tab zoom steps <see cref="CameraController"/> drives from mouse wheel
/// and zoom hotkeys. Kept separate from the full <c>EditorViewModel</c> (which is still WPF/WinForms-side)
/// so <see cref="CameraController"/> can stay headless (ADR-0005) while depending only on the zoom
/// operations it actually calls.
/// </summary>
public interface IZoomController
{
    /// <summary>Steps the zoom level in (closer).</summary>
    void ZoomIn();

    /// <summary>Steps the zoom level out (farther).</summary>
    void ZoomOut();
}
