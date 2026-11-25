using Brutal.GlfwApi;
using System;
using Gum.Wireframe;
using System.Collections.Generic;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// <see cref="IBrutalInput"/> over a BRUTAL <see cref="GlfwWindow"/>. Not covered by unit tests, which cannot
/// create a GLFW window; the seam above it is.
/// </summary>
internal sealed class BrutalGlfwInput : IBrutalInput
{
    private readonly GlfwWindow _window;
    // Standard cursors are created once per process and shared by every input, so a repeated Initialize
    // does not create (and leak) a new set; GLFW destroys the remaining ones at terminate.
    private static readonly Dictionary<GlfwStandardCursor, GlfwCursor> _cursors = new();
    private Action<double>? _scrolled;
    private Action<uint>? _charTyped;

    public BrutalGlfwInput(GlfwWindow window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public double CursorX => _window.CursorPos.X;

    public double CursorY => _window.CursorPos.Y;

    public int WindowWidth => _window.Size.X;

    public int WindowHeight => _window.Size.Y;

    public int FramebufferWidth => _window.FramebufferSize.X;

    public int FramebufferHeight => _window.FramebufferSize.Y;

    public float DisplayScale => _window.ContentScale.X;

    public bool IsMouseButtonDown(MouseButton button)
    {
        GlfwMouseButton glfwButton = button switch
        {
            MouseButton.Left => GlfwMouseButton.Left,
            MouseButton.Middle => GlfwMouseButton.Middle,
            _ => GlfwMouseButton.Right,
        };
        return _window.GetMouseButton(glfwButton) == GlfwButtonAction.Press;
    }

    // GetKey reports Press for a held key; Repeat is only delivered to key callbacks.
    public bool IsKeyDown(GumKeys key) =>
        GumToGlfw.TryGetValue(key, out GlfwKey glfwKey) && _window.GetKey(glfwKey) == GlfwKeyAction.Press;

    // The window callbacks are attached only while something listens, so a replaced cursor or keyboard
    // does not keep the old one subscribed.
    public event Action<double>? Scrolled
    {
        add
        {
            if (_scrolled == null)
            {
                _window.OnScroll += HandleScroll;
            }
            _scrolled += value;
        }
        remove
        {
            _scrolled -= value;
            if (_scrolled == null)
            {
                _window.OnScroll -= HandleScroll;
            }
        }
    }

    public event Action<uint>? CharTyped
    {
        add
        {
            if (_charTyped == null)
            {
                _window.OnChar += HandleChar;
            }
            _charTyped += value;
        }
        remove
        {
            _charTyped -= value;
            if (_charTyped == null)
            {
                _window.OnChar -= HandleChar;
            }
        }
    }

    // Glfw.Clipboard has no window argument: BRUTAL passes a null window to GLFW.
    public string? ClipboardText
    {
        get => Glfw.Clipboard;
        set
        {
            if (value != null)
            {
                Glfw.Clipboard = value;
            }
        }
    }

    public void SetCursorShape(Cursors shape)
    {
        GlfwStandardCursor glfwShape = shape switch
        {
            Cursors.SizeNS => GlfwStandardCursor.ResizeNs,
            Cursors.SizeWE => GlfwStandardCursor.ResizeEw,
            Cursors.SizeNWSE => GlfwStandardCursor.ResizeNwse,
            Cursors.SizeNESW => GlfwStandardCursor.ResizeNesw,
            _ => GlfwStandardCursor.Arrow,
        };

        if (!_cursors.TryGetValue(glfwShape, out GlfwCursor cursor))
        {
            cursor = Glfw.CreateCursor(glfwShape);
            _cursors[glfwShape] = cursor;
        }
        _window.SetCursor(cursor);
    }

    public bool TryReadGamepad(int index, out GamePadSnapshot snapshot)
    {
        snapshot = default;
        if (!Glfw.JoystickIsGamepad(index) || !Glfw.GetGamepadState(index, out GlfwGamepadState state))
        {
            return false;
        }

        byte[] buttons = new byte[GamePadDriver.ButtonCount];
        float[] axes = new float[GamePadDriver.AxisCount];
        unsafe
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = state.Buttons[i];
            }
            for (int i = 0; i < axes.Length; i++)
            {
                axes[i] = state.Axes[i];
            }
        }
        snapshot = GamePadDriver.ToSnapshot(buttons, axes);
        return true;
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

    private void HandleScroll(GlfwWindow window, Brutal.Numerics.double2 offset) => _scrolled?.Invoke(offset.Y);

    private void HandleChar(GlfwWindow window, uint codepoint) => _charTyped?.Invoke(codepoint);
}
