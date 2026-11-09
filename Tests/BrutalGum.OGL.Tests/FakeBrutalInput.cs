using Brutal.GlfwApi;
using Gum.Input;
using System;
using System.Collections.Generic;

namespace BrutalGum.Tests;

/// <summary>A scriptable stand-in for a GLFW window, read through the same seam production code uses.</summary>
internal sealed class FakeBrutalInput : IBrutalInput
{
    public double CursorX { get; set; }
    public double CursorY { get; set; }
    public int WindowWidth { get; set; } = 800;
    public int WindowHeight { get; set; } = 600;
    public int FramebufferWidth { get; set; } = 800;
    public int FramebufferHeight { get; set; } = 600;
    public HashSet<GlfwMouseButton> HeldButtons { get; } = new();
    public HashSet<GlfwKey> HeldKeys { get; } = new();
    public string? ClipboardText { get; set; } = "";
    public GlfwStandardCursor? LastCursorShape { get; private set; }

    public int ScrolledSubscribers => _scrolled?.GetInvocationList().Length ?? 0;
    public int CharSubscribers => _charTyped?.GetInvocationList().Length ?? 0;

    private Action<double>? _scrolled;
    private Action<uint>? _charTyped;

    public event Action<double>? Scrolled { add => _scrolled += value; remove => _scrolled -= value; }
    public event Action<uint>? CharTyped { add => _charTyped += value; remove => _charTyped -= value; }

    public bool IsMouseButtonDown(GlfwMouseButton button) => HeldButtons.Contains(button);

    public bool IsKeyDown(GlfwKey key) => HeldKeys.Contains(key);

    public void SetCursorShape(GlfwStandardCursor shape) => LastCursorShape = shape;

    /// <summary>Gamepads present on the fake window, by index; none by default.</summary>
    public Dictionary<int, FakeGamepadReading> Gamepads { get; } = new();

    public bool TryReadGamepad(int index, byte[] buttons, float[] axes)
    {
        if (!Gamepads.TryGetValue(index, out FakeGamepadReading? reading))
        {
            return false;
        }
        reading.Buttons.CopyTo(buttons, 0);
        reading.Axes.CopyTo(axes, 0);
        return true;
    }

    public void RaiseScroll(double notches) => _scrolled?.Invoke(notches);

    public void RaiseChar(uint codepoint) => _charTyped?.Invoke(codepoint);
}

/// <summary>One gamepad's reading in GLFW's layout, with triggers at rest (-1) and sticks centered.</summary>
internal sealed class FakeGamepadReading
{
    public byte[] Buttons { get; } = new byte[GamePadDriver.ButtonCount];
    public float[] Axes { get; } = CreateRestingAxes();

    public void Hold(GlfwGamepadButton button) => Buttons[(int)button] = 1;

    public void Release(GlfwGamepadButton button) => Buttons[(int)button] = 0;

    /// <summary>Sets the left stick in GLFW's convention (positive Y is down).</summary>
    public void SetLeftStick(float x, float y)
    {
        Axes[(int)GlfwGamepadAxis.LeftX] = x;
        Axes[(int)GlfwGamepadAxis.LeftY] = y;
    }

    private static float[] CreateRestingAxes()
    {
        float[] axes = new float[GamePadDriver.AxisCount];
        axes[(int)GlfwGamepadAxis.LeftTrigger] = -1f;
        axes[(int)GlfwGamepadAxis.RightTrigger] = -1f;
        return axes;
    }
}
