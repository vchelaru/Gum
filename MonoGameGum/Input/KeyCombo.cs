using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public static bool IsComboPushed(this KeyCombo keyCombo)
    {
        foreach (var keyboard in FrameworkElement.KeyboardsForUiControl)
        {
            var isHeld = keyCombo.HeldKey == null || keyboard.KeyDown(keyCombo.HeldKey.Value);
            if (isHeld)
            {
                return keyboard.KeyPushed(keyCombo.PushedKey) ||
                    (keyboard.KeysTyped?.Contains(keyCombo.PushedKey) == true && keyCombo.IsTriggeredOnRepeat);
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
                if (keyboard.KeyReleased(keyCombo.PushedKey))
                {
                    return true;
                }
            }
            else
            {
                return (keyboard.KeyReleased(keyCombo.HeldKey.Value) &&
                       keyboard.KeyDown(keyCombo.PushedKey)) ||

                       (keyboard.KeyReleased(keyCombo.PushedKey) &&
                        keyboard.KeyDown(keyCombo.HeldKey.Value));
            }
        }
        return false;
    }

    public static bool IsComboDown(this KeyCombo keyCombo)
    {
        foreach (var keyboard in FrameworkElement.KeyboardsForUiControl)
        {
            var isHeld = keyCombo.HeldKey == null || keyboard.KeyDown(keyCombo.HeldKey.Value);

            return isHeld && keyboard.KeyDown(keyCombo.PushedKey);
        }
        return false;
    }
}