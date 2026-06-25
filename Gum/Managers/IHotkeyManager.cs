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

    bool PreviewKeyDownAppWide(System.Windows.Input.KeyEventArgs e, bool enableEntireAppZoom = true);
    void HandleKeyDownElementTreeView(System.Windows.Forms.KeyEventArgs e);
    void HandleEditorKeyDown(System.Windows.Forms.KeyEventArgs e);
    void HandleKeyUpWireframe(System.Windows.Forms.KeyEventArgs e);

    /// <summary>
    /// Handles wireframe command keys (arrow-key nudging) using Gum's framework-neutral
    /// <see cref="Gum.Input.GumKey"/> so this interface stays free of WinForms. Callers at the WinForms
    /// boundary translate <c>System.Windows.Forms.Keys</c> via <c>ToGumKey()</c> and pass the modifier
    /// state explicitly. Returns true if the key was handled.
    /// </summary>
    bool ProcessCmdKeyWireframe(Gum.Input.GumKey? key, bool isShiftDown, bool isCtrlDown, bool isAltDown);
}
