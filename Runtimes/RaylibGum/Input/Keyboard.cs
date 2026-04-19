using Gum.Wireframe;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GumKeys = Gum.Forms.Input.Keys;

namespace RaylibGum.Input;

/// <summary>
/// Keyboard implementation for Raylib.
/// </summary>
public class Keyboard : IInputReceiverKeyboard
{
    double _lastGameTime;
    double _lastGetStringTypedCall = -999;
    string _lastStringTyped = "";

    #region Key translation tables

    /// <summary>
    /// Maps <see cref="GumKeys"/> (Gum/XNA key space) to <see cref="KeyboardKey"/> (Raylib key space).
    /// Keys present in Gum but not in Raylib (e.g. Attn, Crsel, Exsel, Pa1, OemClear, ChatPad keys,
    /// Kana/Kanji/IME keys, media-launch keys, browser keys, F13-F24, Separator, Help, Sleep, Select,
    /// Print, Execute) are intentionally omitted — queries for those keys return <c>false</c>.
    /// </summary>
    private static readonly Dictionary<GumKeys, KeyboardKey> _gumToRaylib = new()
    {
        { GumKeys.Back, KeyboardKey.Backspace },
        { GumKeys.Tab, KeyboardKey.Tab },
        { GumKeys.Enter, KeyboardKey.Enter },
        { GumKeys.Pause, KeyboardKey.Pause },
        { GumKeys.CapsLock, KeyboardKey.CapsLock },
        { GumKeys.Escape, KeyboardKey.Escape },
        { GumKeys.Space, KeyboardKey.Space },
        { GumKeys.PageUp, KeyboardKey.PageUp },
        { GumKeys.PageDown, KeyboardKey.PageDown },
        { GumKeys.End, KeyboardKey.End },
        { GumKeys.Home, KeyboardKey.Home },
        { GumKeys.Left, KeyboardKey.Left },
        { GumKeys.Up, KeyboardKey.Up },
        { GumKeys.Right, KeyboardKey.Right },
        { GumKeys.Down, KeyboardKey.Down },
        { GumKeys.PrintScreen, KeyboardKey.PrintScreen },
        { GumKeys.Insert, KeyboardKey.Insert },
        { GumKeys.Delete, KeyboardKey.Delete },

        { GumKeys.D0, KeyboardKey.Zero },
        { GumKeys.D1, KeyboardKey.One },
        { GumKeys.D2, KeyboardKey.Two },
        { GumKeys.D3, KeyboardKey.Three },
        { GumKeys.D4, KeyboardKey.Four },
        { GumKeys.D5, KeyboardKey.Five },
        { GumKeys.D6, KeyboardKey.Six },
        { GumKeys.D7, KeyboardKey.Seven },
        { GumKeys.D8, KeyboardKey.Eight },
        { GumKeys.D9, KeyboardKey.Nine },

        { GumKeys.A, KeyboardKey.A },
        { GumKeys.B, KeyboardKey.B },
        { GumKeys.C, KeyboardKey.C },
        { GumKeys.D, KeyboardKey.D },
        { GumKeys.E, KeyboardKey.E },
        { GumKeys.F, KeyboardKey.F },
        { GumKeys.G, KeyboardKey.G },
        { GumKeys.H, KeyboardKey.H },
        { GumKeys.I, KeyboardKey.I },
        { GumKeys.J, KeyboardKey.J },
        { GumKeys.K, KeyboardKey.K },
        { GumKeys.L, KeyboardKey.L },
        { GumKeys.M, KeyboardKey.M },
        { GumKeys.N, KeyboardKey.N },
        { GumKeys.O, KeyboardKey.O },
        { GumKeys.P, KeyboardKey.P },
        { GumKeys.Q, KeyboardKey.Q },
        { GumKeys.R, KeyboardKey.R },
        { GumKeys.S, KeyboardKey.S },
        { GumKeys.T, KeyboardKey.T },
        { GumKeys.U, KeyboardKey.U },
        { GumKeys.V, KeyboardKey.V },
        { GumKeys.W, KeyboardKey.W },
        { GumKeys.X, KeyboardKey.X },
        { GumKeys.Y, KeyboardKey.Y },
        { GumKeys.Z, KeyboardKey.Z },

        { GumKeys.LeftWindows, KeyboardKey.LeftSuper },
        { GumKeys.RightWindows, KeyboardKey.RightSuper },
        { GumKeys.Apps, KeyboardKey.KeyboardMenu },

        { GumKeys.NumPad0, KeyboardKey.Kp0 },
        { GumKeys.NumPad1, KeyboardKey.Kp1 },
        { GumKeys.NumPad2, KeyboardKey.Kp2 },
        { GumKeys.NumPad3, KeyboardKey.Kp3 },
        { GumKeys.NumPad4, KeyboardKey.Kp4 },
        { GumKeys.NumPad5, KeyboardKey.Kp5 },
        { GumKeys.NumPad6, KeyboardKey.Kp6 },
        { GumKeys.NumPad7, KeyboardKey.Kp7 },
        { GumKeys.NumPad8, KeyboardKey.Kp8 },
        { GumKeys.NumPad9, KeyboardKey.Kp9 },
        { GumKeys.Multiply, KeyboardKey.KpMultiply },
        { GumKeys.Add, KeyboardKey.KpAdd },
        { GumKeys.Subtract, KeyboardKey.KpSubtract },
        { GumKeys.Decimal, KeyboardKey.KpDecimal },
        { GumKeys.Divide, KeyboardKey.KpDivide },

        { GumKeys.F1, KeyboardKey.F1 },
        { GumKeys.F2, KeyboardKey.F2 },
        { GumKeys.F3, KeyboardKey.F3 },
        { GumKeys.F4, KeyboardKey.F4 },
        { GumKeys.F5, KeyboardKey.F5 },
        { GumKeys.F6, KeyboardKey.F6 },
        { GumKeys.F7, KeyboardKey.F7 },
        { GumKeys.F8, KeyboardKey.F8 },
        { GumKeys.F9, KeyboardKey.F9 },
        { GumKeys.F10, KeyboardKey.F10 },
        { GumKeys.F11, KeyboardKey.F11 },
        { GumKeys.F12, KeyboardKey.F12 },

        { GumKeys.NumLock, KeyboardKey.NumLock },
        { GumKeys.Scroll, KeyboardKey.ScrollLock },

        { GumKeys.LeftShift, KeyboardKey.LeftShift },
        { GumKeys.RightShift, KeyboardKey.RightShift },
        { GumKeys.LeftControl, KeyboardKey.LeftControl },
        { GumKeys.RightControl, KeyboardKey.RightControl },
        { GumKeys.LeftAlt, KeyboardKey.LeftAlt },
        { GumKeys.RightAlt, KeyboardKey.RightAlt },

        { GumKeys.VolumeDown, KeyboardKey.VolumeDown },
        { GumKeys.VolumeUp, KeyboardKey.VolumeUp },

        { GumKeys.OemSemicolon, KeyboardKey.Semicolon },
        { GumKeys.OemPlus, KeyboardKey.Equal },
        { GumKeys.OemComma, KeyboardKey.Comma },
        { GumKeys.OemMinus, KeyboardKey.Minus },
        { GumKeys.OemPeriod, KeyboardKey.Period },
        { GumKeys.OemQuestion, KeyboardKey.Slash },
        { GumKeys.OemTilde, KeyboardKey.Grave },
        { GumKeys.OemOpenBrackets, KeyboardKey.LeftBracket },
        { GumKeys.OemPipe, KeyboardKey.Backslash },
        { GumKeys.OemCloseBrackets, KeyboardKey.RightBracket },
        { GumKeys.OemQuotes, KeyboardKey.Apostrophe },
        { GumKeys.OemBackslash, KeyboardKey.Backslash },
    };

    /// <summary>
    /// Inverse of <see cref="_gumToRaylib"/>, built lazily for use by <see cref="KeysTyped"/>
    /// translation. Entries where multiple Gum keys map to the same Raylib key (e.g.
    /// <see cref="GumKeys.OemPipe"/> and <see cref="GumKeys.OemBackslash"/> both map to
    /// <see cref="KeyboardKey.Backslash"/>) resolve to the first Gum key encountered.
    /// </summary>
    private static readonly Dictionary<KeyboardKey, GumKeys> _raylibToGum = BuildRaylibToGum();

    private static Dictionary<KeyboardKey, GumKeys> BuildRaylibToGum()
    {
        Dictionary<KeyboardKey, GumKeys> result = new();
        foreach (KeyValuePair<GumKeys, KeyboardKey> pair in _gumToRaylib)
        {
            if (!result.ContainsKey(pair.Value))
            {
                result[pair.Value] = pair.Key;
            }
        }
        return result;
    }

    #endregion

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
                // Translate Raylib key value to Gum/XNA key value. If the Raylib key has no
                // Gum counterpart (e.g. Raylib's Menu, or a Raylib-only code), drop it silently —
                // callers cast to GumKeys and wouldn't know what to do with an unmapped value.
                if (_raylibToGum.TryGetValue((KeyboardKey)key, out GumKeys gumKey))
                {
                    yield return (int)gumKey;
                }
                key = Raylib.GetKeyPressed();
            }
        }
    }

    /// <inheritdoc/>
    public bool KeyDown(GumKeys key)
    {
        if (_gumToRaylib.TryGetValue(key, out KeyboardKey raylibKey))
        {
            return Raylib.IsKeyDown(raylibKey);
        }
        return false;
    }

    /// <inheritdoc/>
    public bool KeyPushed(GumKeys key)
    {
        if (_gumToRaylib.TryGetValue(key, out KeyboardKey raylibKey))
        {
            return Raylib.IsKeyPressed(raylibKey);
        }
        return false;
    }

    /// <inheritdoc/>
    public bool KeyReleased(GumKeys key)
    {
        if (_gumToRaylib.TryGetValue(key, out KeyboardKey raylibKey))
        {
            return Raylib.IsKeyReleased(raylibKey);
        }
        return false;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to <see cref="Raylib.IsKeyPressedRepeat(KeyboardKey)"/>, which returns true on
    /// initial press and on OS-driven repeat intervals. This matches MonoGame's
    /// <c>Keyboard.KeyTyped</c> semantics (push + repeat-rate) closely enough for
    /// keyboard-driven Forms controls.
    /// </remarks>
    public bool KeyTyped(GumKeys key)
    {
        if (_gumToRaylib.TryGetValue(key, out KeyboardKey raylibKey))
        {
            return Raylib.IsKeyPressed(raylibKey) || Raylib.IsKeyPressedRepeat(raylibKey);
        }
        return false;
    }

    /// <summary>
    /// Performs every-frame activity for the keyboard. This is automatically called by Gum.
    /// </summary>
    /// <param name="gameTime">The number of seconds since the start of the game.</param>
    public void Activity(double gameTime)
    {
        _lastGameTime = gameTime;
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
        if(_lastGameTime != _lastGetStringTypedCall)
        {
            _lastGetStringTypedCall = _lastGameTime;
            // todo this needs to be fixed: https://github.com/vchelaru/Gum/issues/1958
            _lastStringTyped = "";
            int codepoint = GetCharPressed();

            while (codepoint > 0)
            {
                // Convert the Unicode codepoint to a string
                _lastStringTyped += char.ConvertFromUtf32(codepoint);
                codepoint = GetCharPressed();
            }
        }


        return _lastStringTyped;
    }

    /// <summary>
    /// Retrieves the next Unicode character code typed by the user since the last call to this method.
    /// </summary>
    /// <remarks>
    /// This method exists for unit testing purposes.
    /// </remarks>
    /// <returns>The unicode character as an int</returns>
    protected virtual int GetCharPressed() => Raylib.GetCharPressed();
}
