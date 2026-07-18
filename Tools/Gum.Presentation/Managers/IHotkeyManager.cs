namespace Gum.Managers;

/// <summary>
/// Interface for HotkeyManager to enable dependency injection and testing.
/// Provides access to keyboard shortcuts used throughout the application.
/// </summary>
public interface IHotkeyManager
{
    KeyCombination Delete { get; }
    KeyCombination Copy { get; }
    KeyCombination Paste { get; }
    KeyCombination Cut { get; }
    KeyCombination Undo { get; }
    KeyCombination Redo { get; }
    KeyCombination RedoAlt { get; }
    KeyCombination ReorderUp { get; }
    KeyCombination ReorderDown { get; }
    KeyCombination GoToDefinition { get; }
    KeyCombination Search { get; }
    KeyCombination NudgeUp { get; }
    KeyCombination NudgeDown { get; }
    KeyCombination NudgeRight { get; }
    KeyCombination NudgeLeft { get; }
    KeyCombination NudgeUp5 { get; }
    KeyCombination NudgeDown5 { get; }
    KeyCombination NudgeRight5 { get; }
    KeyCombination NudgeLeft5 { get; }
    KeyCombination LockMovementToAxis { get; }
    KeyCombination MaintainResizeAspectRatio { get; }
    KeyCombination SnapRotationTo15Degrees { get; }
    KeyCombination MultiSelect { get; }
    KeyCombination ResizeFromCenter { get; }
    KeyCombination MoveCameraLeft { get; }
    KeyCombination MoveCameraRight { get; }
    KeyCombination MoveCameraUp { get; }
    KeyCombination MoveCameraDown { get; }
    KeyCombination ZoomCameraIn { get; }
    KeyCombination ZoomCameraInAlternative { get; }
    KeyCombination ZoomCameraOut { get; }
    KeyCombination ZoomCameraOutAlternative { get; }
    KeyCombination Rename { get; }
    KeyCombination NavigateBack { get; }
    KeyCombination NavigateForward { get; }

    /// <summary>
    /// Handles application-wide hotkeys (search, undo/redo, zoom). Callers at the WinForms/WPF boundary
    /// translate their framework key event into a <see cref="Gum.Input.GumKeyEventArgs"/> and read
    /// <see cref="Gum.Input.GumKeyEventArgs.Handled"/> back. Returns true if the key was handled.
    /// </summary>
    bool PreviewKeyDownAppWide(Gum.Input.GumKeyEventArgs e, bool enableEntireAppZoom = true);

    /// <summary>
    /// Handles element-tree-view key presses (copy/cut/paste, delete, reorder, rename, etc.). The caller
    /// reads <see cref="Gum.Input.GumKeyEventArgs.Handled"/> / <see cref="Gum.Input.GumKeyEventArgs.SuppressKeyPress"/>
    /// back to decide whether to swallow the key.
    /// </summary>
    void HandleKeyDownElementTreeView(Gum.Input.GumKeyEventArgs e);

    /// <summary>
    /// Handles wireframe/editor key presses (copy/cut/paste, delete, reorder, go-to-definition). The caller
    /// reads <see cref="Gum.Input.GumKeyEventArgs.Handled"/> / <see cref="Gum.Input.GumKeyEventArgs.SuppressKeyPress"/>
    /// back to decide whether to swallow the key.
    /// </summary>
    void HandleEditorKeyDown(Gum.Input.GumKeyEventArgs e);

    /// <summary>Finalizes a nudge gesture (records undo, auto-saves) when a wireframe key is released.</summary>
    void HandleKeyUpWireframe();

    /// <summary>
    /// Returns true if <paramref name="combo"/>'s modifiers are currently held according to the live OS
    /// modifier state. The framework read lives in the implementation; this stays on the interface (rather
    /// than becoming a <c>KeyCombination</c> extension like the <c>IsPressed</c> overloads) so it remains
    /// mockable.
    /// </summary>
    bool IsPressedInControl(KeyCombination combo);

    /// <summary>
    /// Handles wireframe command keys (arrow-key nudging) using Gum's framework-neutral
    /// <see cref="Gum.Input.GumKey"/> so this interface stays free of WinForms. Callers at the WinForms
    /// boundary translate <c>System.Windows.Forms.Keys</c> via <c>ToGumKey()</c> and pass the modifier
    /// state explicitly. Returns true if the key was handled.
    /// </summary>
    bool ProcessCmdKeyWireframe(Gum.Input.GumKey? key, bool isShiftDown, bool isCtrlDown, bool isAltDown);
}
