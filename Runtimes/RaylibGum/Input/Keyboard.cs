using Gum.Wireframe;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Input;

/// <summary>
/// Keyboard implementation for Raylib.
/// </summary>
public class Keyboard : IInputReceiverKeyboard
{
    /// <summary>
    /// Returns true if either the left or right shift key is currently pressed down.
    /// </summary>
    public bool IsShiftDown => 
        Raylib.IsKeyDown(KeyboardKey.LeftShift) ||
        Raylib.IsKeyDown(KeyboardKey.RightShift);

    /// <summary>
    /// Returns true if either the left or right control key is currently pressed down.
    /// </summary>
    public bool IsCtrlDown =>
        Raylib.IsKeyDown(KeyboardKey.LeftControl) ||
        Raylib.IsKeyDown(KeyboardKey.RightControl);

    /// <summary>
    /// Returns true if either the left or right alt key is currently pressed down.
    /// </summary>
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

    /// <summary>
    /// Performs every-frame activity for the keyboard. This is automatically called by Gum.
    /// </summary>
    /// <param name="gameTime">The number of seconds since the start of the game.</param>
    public void Activity(double gameTime)
    {
    }

    /// <summary>
    /// Retrieves a string containing all Unicode characters typed by the user since the last call to this method. 
    /// </summary>
    /// <remarks>This method collects all characters entered via keyboard input since the previous invocation.
    /// It is typically used within an input processing loop to capture user text input. The returned string may include
    /// any Unicode characters supported by the input system.</remarks>
    /// <returns>A string representing the sequence of characters typed by the user. Returns an empty string if no characters
    /// have been typed.</returns>
    public string GetStringTyped()
    {
        // todo this needs to be fixed: https://github.com/vchelaru/Gum/issues/1958
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
