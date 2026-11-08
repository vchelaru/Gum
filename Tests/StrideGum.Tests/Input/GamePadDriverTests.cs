using Gum.Input;
using Moq;
using Shouldly;
using SkiaHostTests;
using Stride.Core.Mathematics;
using Stride.Input;
using System;
using GumGamepadButton = Gum.Input.GamepadButton;

namespace StrideGum.Tests.Input;

/// <summary>
/// Pins the button/stick/trigger mapping of <see cref="GamePadDriver"/> (issue #4600). Unlike Silk
/// (an <see cref="Silk.NET.Input.IGamepad"/> instance with live per-button queries), Stride hands out
/// a single <see cref="GamePadState"/> snapshot (a button bitmask + axis floats) via
/// <see cref="IGamePadDevice.State"/>, so a mocked <see cref="IGamePadDevice"/> returning that
/// snapshot is the seam here. The mapping cases every Skia host shares live in
/// <see cref="GamePadDriverStickTestsBase"/>; this keeps the Stride-specific ones.
/// </summary>
public class GamePadDriverTests : GamePadDriverStickTestsBase
{
    private sealed class StrideGamePadRig : IHostStickGamePadRig
    {
        private GamePadState _state = new GamePadState();

        public GamePad GamePad { get; } = new GamePad();

        public void Press(GumGamepadButton button) => _state.Buttons = ToStride(button);

        public void SetTriggers(float left, float right)
        {
            _state.LeftTrigger = left;
            _state.RightTrigger = right;
        }

        // Stride's thumbstick Y is already Gum's positive-up.
        public void SetLeftStick(float x, float y) => _state.LeftThumb = new Vector2(x, y);

        public void SetRightStick(float x, float y) => _state.RightThumb = new Vector2(x, y);

        public void Apply(double time) => GamePadDriver.Apply(GamePad, CreateGamePad(_state).Object, time);

        // The bootstrap InputManager has no game pads, so every index is already "not present".
        public void RemoveDevice()
        {
        }

        private static GamePadButton ToStride(GumGamepadButton button) => button switch
        {
            GumGamepadButton.DPadUp => GamePadButton.PadUp,
            GumGamepadButton.DPadDown => GamePadButton.PadDown,
            GumGamepadButton.DPadLeft => GamePadButton.PadLeft,
            GumGamepadButton.DPadRight => GamePadButton.PadRight,
            GumGamepadButton.A => GamePadButton.A,
            GumGamepadButton.B => GamePadButton.B,
            GumGamepadButton.X => GamePadButton.X,
            GumGamepadButton.Y => GamePadButton.Y,
            GumGamepadButton.LeftShoulder => GamePadButton.LeftShoulder,
            GumGamepadButton.RightShoulder => GamePadButton.RightShoulder,
            GumGamepadButton.Start => GamePadButton.Start,
            GumGamepadButton.Back => GamePadButton.Back,
            GumGamepadButton.LeftStick => GamePadButton.LeftThumb,
            GumGamepadButton.RightStick => GamePadButton.RightThumb,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Not a Stride button."),
        };
    }

    protected override IHostStickGamePadRig CreateStickRig() => new StrideGamePadRig();

    private static Mock<IGamePadDevice> CreateGamePad(GamePadState state)
    {
        var device = new Mock<IGamePadDevice>();
        device.SetupGet(g => g.State).Returns(state);
        return device;
    }

    [Fact]
    public void Apply_SetsConnectedTrue()
    {
        GamePad sut = new GamePad();
        var strideGamePad = CreateGamePad(new GamePadState());

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);

        sut.IsConnected.ShouldBeTrue();
    }
}
