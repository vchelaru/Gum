using RenderingLibrary.Graphics;
using System.Collections.Generic;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Defines a visual component that renders editor overlays
/// (e.g., resize handles, dimension displays, origin markers).
/// </summary>
public interface IEditorVisual
{
    /// <summary>
    /// Whether this visual is currently visible.
    /// </summary>
    bool Visible { get; set; }

    /// <summary>
    /// Update the visual's state every frame.
    /// Called regardless of selection state.
    /// </summary>
    void Update();

    /// <summary>
    /// Update the visual to reflect the current selection.
    /// Called when selection changes.
    /// </summary>
    /// <param name="selectedObjects">The currently selected GraphicalUiElements.</param>
    void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects);

    /// <summary>
    /// Clean up resources (shapes, sprites, text, etc.).
    /// </summary>
    void Destroy();
}
