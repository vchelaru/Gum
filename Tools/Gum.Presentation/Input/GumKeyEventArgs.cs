namespace Gum.Input;

/// <summary>
/// Gum's framework-neutral key-down/up event payload, used by <see cref="Gum.Managers.IHotkeyManager"/>
/// so the interface can describe key handling without referencing WinForms or WPF.
/// </summary>
/// <remarks>
/// Mirrors the read/write shape of the framework key-event args it replaces: callers at the WinForms/WPF
/// boundary populate the key and modifier state on the way in, and read <see cref="Handled"/> /
/// <see cref="SuppressKeyPress"/> back out to decide whether to swallow the key. Because it is consumed
/// in/out, it is a reference type.
/// </remarks>
public class GumKeyEventArgs
{
    /// <summary>The pressed key, or null if it is not a key Gum maps (see <see cref="GumKey"/>).</summary>
    public GumKey? Key { get; set; }

    /// <summary>Whether a Shift key is held.</summary>
    public bool IsShiftDown { get; set; }

    /// <summary>Whether a Ctrl key is held.</summary>
    public bool IsCtrlDown { get; set; }

    /// <summary>Whether an Alt key is held.</summary>
    public bool IsAltDown { get; set; }

    /// <summary>Set by the handler when it consumes the key; read back by the boundary caller.</summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Set by the handler to suppress the subsequent key-press (WinForms semantics); read back by the
    /// boundary caller. Has no effect on the WPF path.
    /// </summary>
    public bool SuppressKeyPress { get; set; }
}
