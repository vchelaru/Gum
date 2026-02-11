using System.Windows.Forms;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Defines a handler for a specific type of user input interaction
/// (e.g., moving, resizing, rotating).
/// </summary>
public interface IInputHandler
{
    /// <summary>
    /// Priority for handling input. Higher priority handlers are checked first.
    /// Typical values:
    /// - Rotation: 100
    /// - Resize: 90  
    /// - Move: 80
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Returns true if this handler is currently actively handling input
    /// (e.g., in the middle of a drag operation).
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Returns true if the cursor is over an interactive element managed by this handler.
    /// </summary>
    /// <param name="worldX">Cursor X position in world coordinates.</param>
    /// <param name="worldY">Cursor Y position in world coordinates.</param>
    bool HasCursorOver(float worldX, float worldY);

    /// <summary>
    /// Returns the cursor to display when over this handler's interactive area,
    /// or null if the default cursor should be used.
    /// </summary>
    Cursor? GetCursorToShow(float worldX, float worldY);

    /// <summary>
    /// Called when the primary mouse button is first pressed.
    /// The handler should determine if it wants to handle this input.
    /// </summary>
    /// <param name="worldX">Cursor X position in world coordinates.</param>
    /// <param name="worldY">Cursor Y position in world coordinates.</param>
    /// <returns>True if this handler is now handling the input.</returns>
    bool HandlePush(float worldX, float worldY);

    /// <summary>
    /// Called every frame while the primary mouse button is held.
    /// Only called on the active handler.
    /// </summary>
    void HandleDrag();

    /// <summary>
    /// Called when the primary mouse button is released.
    /// Only called on the active handler.
    /// </summary>
    void HandleRelease();

    /// <summary>
    /// Called every frame to update hover state, highlights, etc.
    /// Called on all handlers regardless of active state.
    /// </summary>
    /// <param name="worldX">Cursor X position in world coordinates.</param>
    /// <param name="worldY">Cursor Y position in world coordinates.</param>
    void UpdateHover(float worldX, float worldY);

    /// <summary>
    /// Called when the selection changes.
    /// </summary>
    void OnSelectionChanged();

    /// <summary>
    /// Attempt to handle a delete key press.
    /// </summary>
    /// <returns>True if the delete was handled.</returns>
    bool TryHandleDelete();
}
