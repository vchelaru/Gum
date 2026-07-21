namespace Gum.Managers;

/// <summary>
/// Live OS keyboard-modifier state (Ctrl/Shift/Alt currently held), independent of any specific key
/// event. Framework-neutral (ADR-0005) so <see cref="HotkeyManager.IsPressedInControl"/> can stay in
/// headless <c>Gum.Presentation</c>; the WinForms read (<c>Control.ModifierKeys</c>) lives in the
/// tool-side implementation, injected in from there.
/// </summary>
public interface IModifierKeyState
{
    /// <summary>Whether a Ctrl key is currently held.</summary>
    bool IsCtrlDown { get; }

    /// <summary>Whether a Shift key is currently held.</summary>
    bool IsShiftDown { get; }

    /// <summary>Whether an Alt key is currently held.</summary>
    bool IsAltDown { get; }
}
