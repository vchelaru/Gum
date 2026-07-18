using System;
using TextureCoordinateSelectionPlugin.Models;

namespace TextureCoordinateSelectionPlugin.Logic;

/// <summary>
/// The subset of <c>TextureCoordinateDisplayController</c> that <c>MainControlViewModel</c>
/// depends on. The concrete controller holds WPF-specific state (a <c>ScrollBarLogicWpf</c> and a
/// <c>MainControl</c> view) and stays in the Gum tool project; this interface lets the view model
/// live in the headless <c>Gum.Presentation</c> assembly (ADR-0005).
/// </summary>
public interface ITextureCoordinateDisplayController
{
    /// <summary>
    /// Raised when the zoom level changes as a result of user interaction with the display
    /// (e.g. mouse-wheel zoom), so the view model can keep its own zoom selection in sync.
    /// </summary>
    event Action<int>? ZoomLevelChanged;

    /// <summary>Applies the given zoom level to the display.</summary>
    void UpdateZoom(int zoomLevel);

    /// <summary>Applies the snap-to-grid state and grid size to the display.</summary>
    void UpdateSnapGrid(bool isEnabled, int gridSize);

    /// <summary>Sets which exposed texture coordinate source the display currently edits.</summary>
    void SetCurrentExposedSource(ExposedTextureCoordinateSet? source);

    /// <summary>Refreshes the display to reflect the current selection and settings.</summary>
    void Refresh();
}
