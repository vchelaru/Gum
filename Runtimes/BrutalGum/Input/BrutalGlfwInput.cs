using Brutal.GlfwApi;
using System;
using System.Collections.Generic;

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

    public bool IsMouseButtonDown(GlfwMouseButton button) => _window.GetMouseButton(button) == GlfwButtonAction.Press;

    // GetKey reports Press for a held key; Repeat is only delivered to key callbacks.
    public bool IsKeyDown(GlfwKey key) => _window.GetKey(key) == GlfwKeyAction.Press;

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

    public void SetCursorShape(GlfwStandardCursor shape)
    {
        if (!_cursors.TryGetValue(shape, out GlfwCursor cursor))
        {
            cursor = Glfw.CreateCursor(shape);
            _cursors[shape] = cursor;
        }
        _window.SetCursor(cursor);
    }

    public bool TryReadGamepad(int index, byte[] buttons, float[] axes)
    {
        if (!Glfw.JoystickIsGamepad(index) || !Glfw.GetGamepadState(index, out GlfwGamepadState state))
        {
            return false;
        }

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
        return true;
    }

    private void HandleScroll(GlfwWindow window, Brutal.Numerics.double2 offset) => _scrolled?.Invoke(offset.Y);

    private void HandleChar(GlfwWindow window, uint codepoint) => _charTyped?.Invoke(codepoint);
}
