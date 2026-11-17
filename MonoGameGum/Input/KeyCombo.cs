using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
using Microsoft.Xna.Framework.Input;
#else
using Keys = Gum.Forms.Input.Keys;
#endif

// Needs to be in this namespace since it was originally
// added as a class in this namespace in the FrameworkElement.cs
// file and now we don't want to break backwards compatability.
namespace Gum.Forms.Controls;
public struct KeyCombo
{
    public Keys PushedKey;

    /// <summary>
    /// A key that must be down for the combo to match. When null, the combo matches only while no
    /// modifier (Shift, Control or Alt) is down, so a plain Tab combo does not also match Shift+Tab.
    /// </summary>
    public Keys? HeldKey;
    public bool IsTriggeredOnRepeat;
}


public static class KeyComboExtensions
{
    // Converts the platform-local Keys alias (XNA Keys under FRB, Gum Keys everywhere else)
    // to the shared Gum.Forms.Input.Keys that IInputReceiverKeyboard's interface methods accept.
    // Values align across both enums, so an int round-trip is safe.
    private static Gum.Forms.Input.Keys ToGumKey(Keys key) => (Gum.Forms.Input.Keys)(int)key;

    private static readonly Gum.Forms.Input.Keys[] ModifierKeys = new[]
    {
        Gum.Forms.Input.Keys.LeftShift, Gum.Forms.Input.Keys.RightShift,
        Gum.Forms.Input.Keys.LeftControl, Gum.Forms.Input.Keys.RightControl,
        Gum.Forms.Input.Keys.LeftAlt, Gum.Forms.Input.Keys.RightAlt
    };

    // Only the start of a combo (IsComboPushed) is gated by this: a key that was already pushed
    // still reads as down and released when a modifier arrives later, so a pushed state can end.
    // A combo with no HeldKey requires that no modifier is down. A modifier that is the combo's own
    // PushedKey is ignored so a combo on Shift itself can still match. Indexed loop: this runs per
    // frame and must not allocate.
    private static bool IsHeldRequirementMet(KeyCombo keyCombo, IInputReceiverKeyboard keyboard)
    {
        if (keyCombo.HeldKey != null)
        {
            return keyboard.KeyDown(ToGumKey(keyCombo.HeldKey.Value));
        }

        Gum.Forms.Input.Keys pushedKey = ToGumKey(keyCombo.PushedKey);
        for (int i = 0; i < ModifierKeys.Length; i++)
        {
            Gum.Forms.Input.Keys modifier = ModifierKeys[i];
            if (modifier != pushedKey && keyboard.KeyDown(modifier))
            {
                return false;
            }
        }
        return true;
    }

    public static bool IsComboPushed(this KeyCombo keyCombo)
    {
        foreach (var keyboard in FrameworkElement.KeyboardsForUiControl)
        {
            var isHeld = IsHeldRequirementMet(keyCombo, keyboard);
            if (isHeld)
            {
                return keyboard.KeyPushed(ToGumKey(keyCombo.PushedKey)) ||
                    (keyboard.KeysTyped?.Contains(ToGumKey(keyCombo.PushedKey)) == true && keyCombo.IsTriggeredOnRepeat);
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
