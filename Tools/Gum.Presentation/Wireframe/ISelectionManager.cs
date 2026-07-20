using System.Collections.Generic;

namespace Gum.Wireframe;

/// <summary>
/// The subset of the tool-side <c>SelectionManager</c> that the headless wireframe-editing family
/// (<see cref="EditorContext"/>, <see cref="RectangleSelector"/>, <see cref="WireframeEditor"/> and
/// its input handlers) needs. <c>SelectionManager</c> itself stays tool-side (it directly reads
/// <c>System.Windows.Forms.Control.ModifierKeys</c> and sets a WinForms
/// <c>System.Windows.Forms.Cursor</c>), but implements this interface so its consumers here don't
/// need the concrete type.
/// </summary>
public interface ISelectionManager
{
    bool IsOverBody { get; set; }

    /// <summary>
    /// Whether any object is currently selected.
    /// </summary>
    bool HasSelection { get; }

    /// <summary>
    /// The first currently-selected object, or null if nothing is selected.
    /// </summary>
    GraphicalUiElement? SelectedGue { get; }

    /// <summary>
    /// All currently-selected objects.
    /// </summary>
    List<GraphicalUiElement> SelectedGues { get; }

    void DeselectAll();
    void ToggleSelection(GraphicalUiElement element);
    void Select(IEnumerable<GraphicalUiElement> elements);
}
