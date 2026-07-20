using Gum.Input;
using System.Windows.Forms;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace Gum.Managers;

/// <summary>
/// Framework-specific <see cref="KeyCombination"/> matching that reads WinForms/WPF key events. These
/// live in the WinForms tool layer so <see cref="KeyCombination"/> itself can stay headless in the
/// Gum.Presentation assembly (ADR-0005). Call sites that already <c>using Gum.Managers;</c> pick these
/// up with no further changes.
/// </summary>
public static class KeyCombinationExtensions
{
    // GumKey values are defined equal to Win32 virtual-key codes (pinned by GumKeyTests),
    // so converting the bound key to the WinForms Keys enum is a pure cast.
    private static Keys ToWinFormsKey(GumKey key) => (Keys)(int)key;

    public static bool IsPressed(this KeyCombination kc, KeyEventArgs args)
    {
        if (kc.IsCtrlDown && (args.Modifiers & Keys.Control) != Keys.Control) return false;
        if (kc.IsShiftDown && (args.Modifiers & Keys.Shift) != Keys.Shift) return false;
        if (kc.IsAltDown && (args.Modifiers & Keys.Alt) != Keys.Alt) return false;

        return kc.Key == null || args.KeyCode == ToWinFormsKey(kc.Key.Value);
    }

    /// <summary>
    /// Matches against Gum's framework-neutral <see cref="GumKeyEventArgs"/>, for callers that have
    /// already translated the framework key event at the editor-host boundary (e.g.
    /// <c>CameraController.HandleKeyPress</c>).
    /// </summary>
    public static bool IsPressed(this KeyCombination kc, GumKeyEventArgs args)
    {
        if (kc.IsCtrlDown && !args.IsCtrlDown) return false;
        if (kc.IsShiftDown && !args.IsShiftDown) return false;
        if (kc.IsAltDown && !args.IsAltDown) return false;

        return kc.Key == null || args.Key == kc.Key;
    }

    public static bool IsPressed(this KeyCombination kc, System.Windows.Input.KeyEventArgs args)
    {
        if (kc.IsCtrlDown && (args.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Control) != System.Windows.Input.ModifierKeys.Control) return false;
        if (kc.IsShiftDown && (args.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Shift) != System.Windows.Input.ModifierKeys.Shift) return false;
        if (kc.IsAltDown && (args.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Alt) != System.Windows.Input.ModifierKeys.Alt) return false;

        if (kc.Key == null)
        {
            return true;
        }
        else
        {
            var expectedWpfKey = System.Windows.Input.KeyInterop.KeyFromVirtualKey((int)kc.Key.Value);
            return args.Key == expectedWpfKey || args.SystemKey == expectedWpfKey;
        }
    }

    public static bool IsPressed(this KeyCombination kc, Keys keyData)
    {
        Keys extracted = keyData;

        if (kc.IsAltDown)
        {
            if (kc.Key == null)
            {
                return keyData == Keys.Alt;
            }
            else
            {
                return keyData == (Keys.Alt | ToWinFormsKey(kc.Key.Value));
            }
        }
        else
        {
            if (keyData >= Keys.KeyCode)
            {
                int value = (int)(keyData) + (int)Keys.Modifiers;

                extracted = (Keys)value;
            }

            bool shiftDown = (keyData & Keys.Shift) == Keys.Shift;
            bool ctrlDown = (keyData & Keys.Control) == Keys.Control;
            bool altDown = (keyData & Keys.Alt) == Keys.Alt;

            if (kc.IsCtrlDown && !ctrlDown) return false;
            if (kc.IsShiftDown && !shiftDown) return false;
            if (kc.IsAltDown && !altDown) return false;

            return kc.Key == null || extracted == ToWinFormsKey(kc.Key.Value);
        }
    }
}
