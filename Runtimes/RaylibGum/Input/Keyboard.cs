using Gum.Wireframe;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Input;
public class Keyboard : IInputReceiverKeyboard
{
    public bool IsShiftDown => 
        Raylib.IsKeyDown(KeyboardKey.LeftShift) ||
        Raylib.IsKeyDown(KeyboardKey.RightShift);

    public bool IsCtrlDown =>
        Raylib.IsKeyDown(KeyboardKey.LeftControl) ||
        Raylib.IsKeyDown(KeyboardKey.RightControl);


    public bool IsAltDown => 
        Raylib.IsKeyDown(KeyboardKey.LeftAlt) ||
        Raylib.IsKeyDown(KeyboardKey.RightAlt);

    IEnumerable<int> IInputReceiverKeyboard.KeysTyped
    {
        get
        {
            int key = Raylib.GetKeyPressed();

            while (key > 0)
            {
                yield return key;
                key = Raylib.GetKeyPressed();
            }
        }
    }

    public void Activity(double gameTime)
    {
        // todo:
    }

    public string GetStringTyped()
    {
        string typedString = "";
        int codepoint = Raylib.GetCharPressed();

        while (codepoint > 0)
        {
            // Convert the Unicode codepoint to a string
            typedString += char.ConvertFromUtf32(codepoint);
            codepoint = Raylib.GetCharPressed();
        }
        return typedString;
    }
}
