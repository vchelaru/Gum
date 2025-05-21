using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditorTabPlugin_XNA.ExtensionMethods;
public static class KeyCombinationExtensionMethods
{
    public static bool IsPressed(this KeyCombination keyCombination, InputLibrary.Keyboard keyboard)
    {
        if (keyCombination.IsShiftDown &&
            !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) &&
            !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
        {
            return false;
        }

        if (keyCombination.IsCtrlDown &&
            !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) &&
            !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl))
        {
            return false;
        }

        if (keyCombination.IsAltDown &&
            !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) &&
            !keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt))
        {
            return false;
        }

        return keyCombination.Key == null ||
            // Most keys are the same in XNA - is this enough?
            keyboard.KeyDown((Microsoft.Xna.Framework.Input.Keys)keyCombination.Key);

    }
}
