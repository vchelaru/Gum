using Gum.Input;

namespace Gum.Managers;

/// <summary>
/// A framework-neutral keyboard shortcut binding: a <see cref="GumKey"/> plus modifier flags. Headless
/// (ADR-0005) — the WinForms/WPF matching (<c>IsPressed</c>) lives in <c>KeyCombinationExtensions</c> in
/// the tool layer, and the live-modifier query is <see cref="IHotkeyManager.IsPressedInControl"/>.
/// </summary>
public class KeyCombination
{
    public GumKey? Key { get; set; }
    public bool IsCtrlDown { get; set; }
    public bool IsShiftDown { get; set; }
    public bool IsAltDown { get; set; }

    public static KeyCombination Pressed(GumKey key) => new KeyCombination { Key = key };
    public static KeyCombination Ctrl(GumKey key) => new KeyCombination { Key = key, IsCtrlDown = true };
    public static KeyCombination Alt(GumKey? key = null) => new KeyCombination { Key = key, IsAltDown = true };
    public static KeyCombination Shift(GumKey? key = null) => new KeyCombination { Key = key, IsShiftDown = true };

    public override string ToString()
    {
        string toReturn = "";

        if (IsCtrlDown)
        {
            toReturn += "Ctrl";
        }
        if (IsShiftDown)
        {
            if(toReturn.Length != 0)
            {
                toReturn += "+";
            }
            toReturn += "Shift";
        }
        if (IsAltDown)
        {
            if (toReturn.Length != 0)
            {
                toReturn += "+";
            }
            toReturn += "Alt";
        }

        if (Key != null)
        {
            if (toReturn.Length != 0)
            {
                toReturn += "+";
            }
            toReturn += Key.ToString();
        }

        return toReturn;
    }
}
