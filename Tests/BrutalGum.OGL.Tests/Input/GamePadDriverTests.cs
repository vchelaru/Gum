using Brutal.GlfwApi;
using Gum.Input;
using Shouldly;
using SkiaHostTests;
using System;
using GumGamepadButton = Gum.Input.GamepadButton;

namespace BrutalGum.Tests.Input;

/// <summary>
/// Pins the mapping of <see cref="GamePadDriver"/> from GLFW's gamepad layout. The cases every host shares
/// are in <see cref="GamePadDriverStickTestsBase"/>.
/// </summary>
public class GamePadDriverTests : GamePadDriverStickTestsBase
{
    private sealed class BrutalGamePadRig : IHostStickGamePadRig
    {
        private readonly byte[] _buttons = new byte[GamePadDriver.ButtonCount];
        private readonly float[] _axes = NeutralAxes();
        private (float X, float Y) _left;
        private (float X, float Y) _right;

        public GamePad GamePad { get; } = new GamePad();

        public void Press(GumGamepadButton button)
        {
            Array.Clear(_buttons);
            _buttons[(int)ToGlfw(button)] = 1;
        }

        public void SetTriggers(float left, float right)
        {
            // GLFW triggers run -1 (released) to +1.
            _axes[(int)GlfwGamepadAxis.LeftTrigger] = left * 2f - 1f;
            _axes[(int)GlfwGamepadAxis.RightTrigger] = right * 2f - 1f;
        }

        // GLFW reports stick Y positive-down; the rig takes Gum's positive-up.
        public void SetLeftStick(float x, float y) => _left = (x, -y);

        public void SetRightStick(float x, float y) => _right = (x, -y);

        public void Apply(double time)
        {
            _axes[(int)GlfwGamepadAxis.LeftX] = _left.X;
            _axes[(int)GlfwGamepadAxis.LeftY] = _left.Y;
            _axes[(int)GlfwGamepadAxis.RightX] = _right.X;
            _axes[(int)GlfwGamepadAxis.RightY] = _right.Y;
            GamePadDriver.Apply(GamePad, _buttons, _axes, time);
        }

        // The bootstrap window has no gamepad, so index 0 is already absent.
        public void RemoveDevice()
        {
        }

        private static float[] NeutralAxes()
        {
            float[] axes = new float[GamePadDriver.AxisCount];
            axes[(int)GlfwGamepadAxis.LeftTrigger] = -1f;
            axes[(int)GlfwGamepadAxis.RightTrigger] = -1f;
            return axes;
        }

        private static GlfwGamepadButton ToGlfw(GumGamepadButton button) => button switch
        {
            GumGamepadButton.DPadUp => GlfwGamepadButton.DpadUp,
            GumGamepadButton.DPadDown => GlfwGamepadButton.DpadDown,
            GumGamepadButton.DPadLeft => GlfwGamepadButton.DpadLeft,
            GumGamepadButton.DPadRight => GlfwGamepadButton.DpadRight,
            GumGamepadButton.A => GlfwGamepadButton.A,
            GumGamepadButton.B => GlfwGamepadButton.B,
            GumGamepadButton.X => GlfwGamepadButton.X,
            GumGamepadButton.Y => GlfwGamepadButton.Y,
            GumGamepadButton.LeftShoulder => GlfwGamepadButton.LeftBumper,
            GumGamepadButton.RightShoulder => GlfwGamepadButton.RightBumper,
            GumGamepadButton.Start => GlfwGamepadButton.Start,
            GumGamepadButton.Back => GlfwGamepadButton.Back,
            GumGamepadButton.LeftStick => GlfwGamepadButton.LeftThumb,
            GumGamepadButton.RightStick => GlfwGamepadButton.RightThumb,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Not a GLFW button."),
        };
    }

    protected override IHostStickGamePadRig CreateStickRig() => new BrutalGamePadRig();

    [Fact]
    public void Apply_RestingTriggers_ReadZero()
    {
        GamePad sut = new GamePad();
        float[] axes = new float[GamePadDriver.AxisCount];
        axes[(int)GlfwGamepadAxis.LeftTrigger] = -1f;
        axes[(int)GlfwGamepadAxis.RightTrigger] = -1f;

        GamePadDriver.Apply(sut, new byte[GamePadDriver.ButtonCount], axes, 1);

        sut.IsConnected.ShouldBeTrue();
        sut.ButtonDown(GumGamepadButton.LeftTrigger).ShouldBeFalse();
        sut.ButtonDown(GumGamepadButton.RightTrigger).ShouldBeFalse();
    }
}
