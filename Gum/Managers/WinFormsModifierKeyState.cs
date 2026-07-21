using System.Windows.Forms;

namespace Gum.Managers;

/// <summary>
/// Reads the live OS modifier-key state via WinForms' <see cref="Control.ModifierKeys"/>. Kept in the
/// tool layer so <see cref="IModifierKeyState"/> (headless <c>Gum.Presentation</c>, ADR-0005) stays
/// framework-neutral; see <see cref="HotkeyManager.IsPressedInControl"/>, its only consumer.
/// </summary>
public class WinFormsModifierKeyState : IModifierKeyState
{
    /// <inheritdoc/>
    public bool IsCtrlDown => (Control.ModifierKeys & Keys.Control) == Keys.Control;

    /// <inheritdoc/>
    public bool IsShiftDown => (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

    /// <inheritdoc/>
    public bool IsAltDown => (Control.ModifierKeys & Keys.Alt) == Keys.Alt;
}
