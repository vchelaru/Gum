using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using Keys = Gum.Forms.Input.Keys;
#else
using Microsoft.Xna.Framework.Input;
#endif

// Needs to be in this namespace since it was originally
// added as a class in this namespace in the FrameworkElement.cs
// file and now we don't want to break backwards compatability.
namespace Gum.Forms.Controls;
public struct KeyCombo
{
    public Keys PushedKey;
    public Keys? HeldKey;
    public bool IsTriggeredOnRepeat;
}


public static class KeyComboExtensions
{
    // Converts the platform-local Keys alias (XNA Keys on MonoGame, Gum Keys on Raylib/Sokol)
    // to the shared Gum.Forms.Input.Keys that IInputReceiverKeyboard's interface methods accept.
    // Values align across both enums, so an int round-trip is safe.
    private static Gum.Forms.Input.Keys ToGumKey(Keys key) => (Gum.Forms.Input.Keys)(int)key;

    public static bool IsComboPushed(this KeyCombo keyCombo)
    {
        foreach (var keyboard in FrameworkElement.KeyboardsForUiControl)
        {
            var isHeld = keyCombo.HeldKey == null || keyboard.KeyDown(ToGumKey(keyCombo.HeldKey.Value));
            if (isHeld)
            {
                // Access KeysTyped via the base interface so we consistently get the
                // IEnumerable<int> overload. IInputReceiverKeyboardMonoGame hides this with
                // an XNA-typed collection, which would not expose LINQ Contains(int).
                IEnumerable<int>? keysTyped = ((IInputReceiverKeyboard)keyboard).KeysTyped;
                return keyboard.KeyPushed(ToGumKey(keyCombo.PushedKey)) ||
                    (keysTyped?.Contains((int)keyCombo.PushedKey) == true && keyCombo.IsTriggeredOnRepeat);
            }
        }
        return false;
    }

    public static bool IsComboReleased(this KeyCombo keyCombo)
    {
        foreach (var keyboard in FrameworkElement.KeyboardsForUiControl)
        {
            if (keyCombo.HeldKey == null)
            {
                // see if the normal key was just released:
                if (keyboard.KeyReleased(ToGumKey(keyCombo.PushedKey)))
                {
                    return true;
                }
            }
            else
            {
                return (keyboard.KeyReleased(ToGumKey(keyCombo.HeldKey.Value)) &&
                       keyboard.KeyDown(ToGumKey(keyCombo.PushedKey))) ||

                       (keyboard.KeyReleased(ToGumKey(keyCombo.PushedKey)) &&
                        keyboard.KeyDown(ToGumKey(keyCombo.HeldKey.Value)));
            }
        }
        return false;
    }

    public static bool IsComboDown(this KeyCombo keyCombo)
    {
        foreach (var keyboard in FrameworkElement.KeyboardsForUiControl)
        {
            var isHeld = keyCombo.HeldKey == null || keyboard.KeyDown(ToGumKey(keyCombo.HeldKey.Value));

            return isHeld && keyboard.KeyDown(ToGumKey(keyCombo.PushedKey));
        }
        return false;
    }
}
