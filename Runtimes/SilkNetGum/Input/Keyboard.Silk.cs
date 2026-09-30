using Silk.NET.Input;
using System.Collections.Generic;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Keyboard implementation for Silk.NET.Input. Down-state is polled from an <see cref="IKeyboard"/>;
/// <see cref="PolledKeyboard"/> derives push/release edges and key repeat from it (Silk has no
/// "pressed this frame" query). Typed text comes from the <see cref="IKeyboard.KeyChar"/> event.
/// </summary>
public class Keyboard : PolledKeyboard
{
    private readonly IKeyboard? _keyboard;

    /// <summary>
    /// Constructs a keyboard backed by the supplied Silk device (from
    /// <see cref="IInputContext.Keyboards"/>). Subscribes to <see cref="IKeyboard.KeyChar"/> for
    /// typed-text capture.
    /// </summary>
    public Keyboard(IKeyboard keyboard)
    {
        _keyboard = keyboard;
        _keyboard.KeyChar += (_, character) => AppendTypedText(character);
    }

    /// <summary>
    /// Device-less constructor. Used by <c>GumService.CreateKeyboard</c> for the degenerate case
    /// where the input context exposes no keyboard (headless), so the Forms input pump still has a
    /// non-null keyboard to tick (all queries return false / empty text) rather than crashing on a
    /// null. Also used by unit tests to drive the translation table and edge detection through the
    /// <see cref="IsKeyPressed"/> seam without a live Silk device.
    /// </summary>
    internal Keyboard()
    {
    }

    #region Key translation table

    /// <summary>
    /// Maps <see cref="GumKeys"/> (Gum/XNA key space) to <see cref="Key"/> (Silk key space). Keys
    /// present in Gum but not in Silk (media/browser keys, IME keys, Attn/Crsel/Exsel/Pa1,
    /// OemClear, VolumeUp/Down, F13-F24, etc.) are intentionally omitted -- queries for those
    /// return <c>false</c>.
    /// </summary>
    private static readonly Dictionary<GumKeys, Key> _gumToSilk = new()
    {
        { GumKeys.Back, Key.Backspace },
        { GumKeys.Tab, Key.Tab },
        { GumKeys.Enter, Key.Enter },
        { GumKeys.Pause, Key.Pause },
        { GumKeys.CapsLock, Key.CapsLock },
        { GumKeys.Escape, Key.Escape },
        { GumKeys.Space, Key.Space },
        { GumKeys.PageUp, Key.PageUp },
        { GumKeys.PageDown, Key.PageDown },
        { GumKeys.End, Key.End },
        { GumKeys.Home, Key.Home },
        { GumKeys.Left, Key.Left },
        { GumKeys.Up, Key.Up },
        { GumKeys.Right, Key.Right },
        { GumKeys.Down, Key.Down },
        { GumKeys.PrintScreen, Key.PrintScreen },
        { GumKeys.Insert, Key.Insert },
        { GumKeys.Delete, Key.Delete },

        { GumKeys.D0, Key.Number0 },
        { GumKeys.D1, Key.Number1 },
        { GumKeys.D2, Key.Number2 },
        { GumKeys.D3, Key.Number3 },
        { GumKeys.D4, Key.Number4 },
        { GumKeys.D5, Key.Number5 },
        { GumKeys.D6, Key.Number6 },
        { GumKeys.D7, Key.Number7 },
        { GumKeys.D8, Key.Number8 },
        { GumKeys.D9, Key.Number9 },

        { GumKeys.A, Key.A },
        { GumKeys.B, Key.B },
        { GumKeys.C, Key.C },
        { GumKeys.D, Key.D },
        { GumKeys.E, Key.E },
        { GumKeys.F, Key.F },
        { GumKeys.G, Key.G },
        { GumKeys.H, Key.H },
        { GumKeys.I, Key.I },
        { GumKeys.J, Key.J },
        { GumKeys.K, Key.K },
        { GumKeys.L, Key.L },
        { GumKeys.M, Key.M },
        { GumKeys.N, Key.N },
        { GumKeys.O, Key.O },
        { GumKeys.P, Key.P },
        { GumKeys.Q, Key.Q },
        { GumKeys.R, Key.R },
        { GumKeys.S, Key.S },
        { GumKeys.T, Key.T },
        { GumKeys.U, Key.U },
        { GumKeys.V, Key.V },
        { GumKeys.W, Key.W },
        { GumKeys.X, Key.X },
        { GumKeys.Y, Key.Y },
        { GumKeys.Z, Key.Z },

        { GumKeys.LeftWindows, Key.SuperLeft },
        { GumKeys.RightWindows, Key.SuperRight },
        { GumKeys.Apps, Key.Menu },

        { GumKeys.NumPad0, Key.Keypad0 },
        { GumKeys.NumPad1, Key.Keypad1 },
        { GumKeys.NumPad2, Key.Keypad2 },
        { GumKeys.NumPad3, Key.Keypad3 },
        { GumKeys.NumPad4, Key.Keypad4 },
        { GumKeys.NumPad5, Key.Keypad5 },
        { GumKeys.NumPad6, Key.Keypad6 },
        { GumKeys.NumPad7, Key.Keypad7 },
        { GumKeys.NumPad8, Key.Keypad8 },
        { GumKeys.NumPad9, Key.Keypad9 },
        { GumKeys.Multiply, Key.KeypadMultiply },
        { GumKeys.Add, Key.KeypadAdd },
        { GumKeys.Subtract, Key.KeypadSubtract },
        { GumKeys.Decimal, Key.KeypadDecimal },
        { GumKeys.Divide, Key.KeypadDivide },

        { GumKeys.F1, Key.F1 },
        { GumKeys.F2, Key.F2 },
        { GumKeys.F3, Key.F3 },
        { GumKeys.F4, Key.F4 },
        { GumKeys.F5, Key.F5 },
        { GumKeys.F6, Key.F6 },
        { GumKeys.F7, Key.F7 },
        { GumKeys.F8, Key.F8 },
        { GumKeys.F9, Key.F9 },
        { GumKeys.F10, Key.F10 },
        { GumKeys.F11, Key.F11 },
        { GumKeys.F12, Key.F12 },

        { GumKeys.NumLock, Key.NumLock },
        { GumKeys.Scroll, Key.ScrollLock },

        { GumKeys.LeftShift, Key.ShiftLeft },
        { GumKeys.RightShift, Key.ShiftRight },
        { GumKeys.LeftControl, Key.ControlLeft },
        { GumKeys.RightControl, Key.ControlRight },
        { GumKeys.LeftAlt, Key.AltLeft },
        { GumKeys.RightAlt, Key.AltRight },

        { GumKeys.OemSemicolon, Key.Semicolon },
        { GumKeys.OemPlus, Key.Equal },
        { GumKeys.OemComma, Key.Comma },
        { GumKeys.OemMinus, Key.Minus },
        { GumKeys.OemPeriod, Key.Period },
        { GumKeys.OemQuestion, Key.Slash },
        { GumKeys.OemTilde, Key.GraveAccent },
        { GumKeys.OemOpenBrackets, Key.LeftBracket },
        { GumKeys.OemPipe, Key.BackSlash },
        { GumKeys.OemCloseBrackets, Key.RightBracket },
        { GumKeys.OemQuotes, Key.Apostrophe },
        { GumKeys.OemBackslash, Key.BackSlash },
    };

    #endregion

    /// <inheritdoc/>
    protected override IEnumerable<GumKeys> SupportedKeys => _gumToSilk.Keys;

    /// <inheritdoc/>
    protected override bool IsDeviceKeyDown(GumKeys key) =>
        _gumToSilk.TryGetValue(key, out Key silkKey) && IsKeyPressed(silkKey);

    /// <summary>
    /// Returns whether the given Silk key is currently held down. Virtual so unit tests can drive
    /// the translation table and edge detection without a live Silk device.
    /// </summary>
    protected virtual bool IsKeyPressed(Key key) => _keyboard?.IsKeyPressed(key) == true;
}
