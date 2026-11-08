using Brutal.GlfwApi;
using System.Collections.Generic;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Keyboard for BRUTAL's GLFW window. Down state is polled with <c>GlfwWindow.GetKey</c> and
/// <see cref="TablePolledKeyboard{TDeviceKey}"/> derives push/release edges and key repeat from it. Typed
/// text comes from the window's character callback.
/// </summary>
public class Keyboard : TablePolledKeyboard<GlfwKey>
{
    private readonly IBrutalInput? _input;

    internal Keyboard(IBrutalInput input) : base(GumToGlfw)
    {
        _input = input;
        _input.CharTyped += HandleChar;
        OnDispose(() => input.CharTyped -= HandleChar);
    }

    /// <summary>
    /// Device-less keyboard, so the Forms input pump still has a non-null keyboard to tick when the
    /// service has no window (tests).
    /// </summary>
    internal Keyboard() : base(GumToGlfw)
    {
    }

    private void HandleChar(uint codepoint)
    {
        // Lone surrogates and out-of-range values cannot be converted; drop them.
        if (codepoint <= 0x10FFFF && (codepoint < 0xD800 || codepoint > 0xDFFF))
        {
            AppendTypedText(char.ConvertFromUtf32((int)codepoint));
        }
    }

    /// <summary>
    /// Maps <see cref="GumKeys"/> to <see cref="GlfwKey"/>. Keys GLFW has no equivalent for (media, browser
    /// and IME keys, and so on) are omitted, so querying them returns false.
    /// </summary>
    internal static readonly IReadOnlyDictionary<GumKeys, GlfwKey> GumToGlfw = new Dictionary<GumKeys, GlfwKey>
    {
        { GumKeys.Back, GlfwKey.Backspace },
        { GumKeys.Tab, GlfwKey.Tab },
        { GumKeys.Enter, GlfwKey.Enter },
        { GumKeys.Pause, GlfwKey.Pause },
        { GumKeys.CapsLock, GlfwKey.CapsLock },
        { GumKeys.Escape, GlfwKey.Escape },
        { GumKeys.Space, GlfwKey.Space },
        { GumKeys.PageUp, GlfwKey.PageUp },
        { GumKeys.PageDown, GlfwKey.PageDown },
        { GumKeys.End, GlfwKey.End },
        { GumKeys.Home, GlfwKey.Home },
        { GumKeys.Left, GlfwKey.Left },
        { GumKeys.Up, GlfwKey.Up },
        { GumKeys.Right, GlfwKey.Right },
        { GumKeys.Down, GlfwKey.Down },
        { GumKeys.PrintScreen, GlfwKey.PrintScreen },
        { GumKeys.Insert, GlfwKey.Insert },
        { GumKeys.Delete, GlfwKey.Delete },

        { GumKeys.D0, GlfwKey.Number0 },
        { GumKeys.D1, GlfwKey.Number1 },
        { GumKeys.D2, GlfwKey.Number2 },
        { GumKeys.D3, GlfwKey.Number3 },
        { GumKeys.D4, GlfwKey.Number4 },
        { GumKeys.D5, GlfwKey.Number5 },
        { GumKeys.D6, GlfwKey.Number6 },
        { GumKeys.D7, GlfwKey.Number7 },
        { GumKeys.D8, GlfwKey.Number8 },
        { GumKeys.D9, GlfwKey.Number9 },

        { GumKeys.A, GlfwKey.A },
        { GumKeys.B, GlfwKey.B },
        { GumKeys.C, GlfwKey.C },
        { GumKeys.D, GlfwKey.D },
        { GumKeys.E, GlfwKey.E },
        { GumKeys.F, GlfwKey.F },
        { GumKeys.G, GlfwKey.G },
        { GumKeys.H, GlfwKey.H },
        { GumKeys.I, GlfwKey.I },
        { GumKeys.J, GlfwKey.J },
        { GumKeys.K, GlfwKey.K },
        { GumKeys.L, GlfwKey.L },
        { GumKeys.M, GlfwKey.M },
        { GumKeys.N, GlfwKey.N },
        { GumKeys.O, GlfwKey.O },
        { GumKeys.P, GlfwKey.P },
        { GumKeys.Q, GlfwKey.Q },
        { GumKeys.R, GlfwKey.R },
        { GumKeys.S, GlfwKey.S },
        { GumKeys.T, GlfwKey.T },
        { GumKeys.U, GlfwKey.U },
        { GumKeys.V, GlfwKey.V },
        { GumKeys.W, GlfwKey.W },
        { GumKeys.X, GlfwKey.X },
        { GumKeys.Y, GlfwKey.Y },
        { GumKeys.Z, GlfwKey.Z },

        { GumKeys.LeftWindows, GlfwKey.LeftSuper },
        { GumKeys.RightWindows, GlfwKey.RightSuper },
        { GumKeys.Apps, GlfwKey.Menu },

        { GumKeys.NumPad0, GlfwKey.Kp0 },
        { GumKeys.NumPad1, GlfwKey.Kp1 },
        { GumKeys.NumPad2, GlfwKey.Kp2 },
        { GumKeys.NumPad3, GlfwKey.Kp3 },
        { GumKeys.NumPad4, GlfwKey.Kp4 },
        { GumKeys.NumPad5, GlfwKey.Kp5 },
        { GumKeys.NumPad6, GlfwKey.Kp6 },
        { GumKeys.NumPad7, GlfwKey.Kp7 },
        { GumKeys.NumPad8, GlfwKey.Kp8 },
        { GumKeys.NumPad9, GlfwKey.Kp9 },
        { GumKeys.Multiply, GlfwKey.KpMultiply },
        { GumKeys.Add, GlfwKey.KpAdd },
        { GumKeys.Subtract, GlfwKey.KpSubtract },
        { GumKeys.Decimal, GlfwKey.KpDecimal },
        { GumKeys.Divide, GlfwKey.KpDivide },

        { GumKeys.F1, GlfwKey.F1 },
        { GumKeys.F2, GlfwKey.F2 },
        { GumKeys.F3, GlfwKey.F3 },
        { GumKeys.F4, GlfwKey.F4 },
        { GumKeys.F5, GlfwKey.F5 },
        { GumKeys.F6, GlfwKey.F6 },
        { GumKeys.F7, GlfwKey.F7 },
        { GumKeys.F8, GlfwKey.F8 },
        { GumKeys.F9, GlfwKey.F9 },
        { GumKeys.F10, GlfwKey.F10 },
        { GumKeys.F11, GlfwKey.F11 },
        { GumKeys.F12, GlfwKey.F12 },

        { GumKeys.NumLock, GlfwKey.NumLock },
        { GumKeys.Scroll, GlfwKey.ScrollLock },

        { GumKeys.LeftShift, GlfwKey.LeftShift },
        { GumKeys.RightShift, GlfwKey.RightShift },
        { GumKeys.LeftControl, GlfwKey.LeftControl },
        { GumKeys.RightControl, GlfwKey.RightControl },
        { GumKeys.LeftAlt, GlfwKey.LeftAlt },
        { GumKeys.RightAlt, GlfwKey.RightAlt },

        { GumKeys.OemSemicolon, GlfwKey.Semicolon },
        { GumKeys.OemPlus, GlfwKey.Equal },
        { GumKeys.OemComma, GlfwKey.Comma },
        { GumKeys.OemMinus, GlfwKey.Minus },
        { GumKeys.OemPeriod, GlfwKey.Period },
        { GumKeys.OemQuestion, GlfwKey.Slash },
        { GumKeys.OemTilde, GlfwKey.GraveAccent },
        { GumKeys.OemOpenBrackets, GlfwKey.LeftBracket },
        { GumKeys.OemPipe, GlfwKey.Backslash },
        { GumKeys.OemCloseBrackets, GlfwKey.RightBracket },
        { GumKeys.OemQuotes, GlfwKey.Apostrophe },
        { GumKeys.OemBackslash, GlfwKey.Backslash },
    };

    /// <inheritdoc/>
    protected override bool IsDeviceKeyPressed(GlfwKey key) => IsKeyPressed(key);

    /// <summary>
    /// Whether the given GLFW key is held. Virtual so tests can drive the table and edge detection without
    /// a window.
    /// </summary>
    protected virtual bool IsKeyPressed(GlfwKey key) => _input?.IsKeyDown(key) == true;
}
