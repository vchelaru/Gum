using Gum.Input;
using Moq;
using Shouldly;
using Silk.NET.Input;
using SkiaHostTests;
using System;
using System.Collections.Generic;
using GumGamepadButton = Gum.Input.GamepadButton;

namespace SilkNetGum.Tests.Input;

/// <summary>
/// Pins the button/stick/trigger mapping of <see cref="GamePadDriver"/> (issue #3668). Unlike the
/// Raylib driver (static functions reading live hardware, needing a delegate-injection seam), Silk
/// hands out an <see cref="IGamepad"/> instance directly, so a mocked <see cref="IGamepad"/> is the
/// seam here. The mapping cases every Skia host shares live in <see cref="GamePadDriverStickTestsBase"/>;
/// this keeps the Silk-specific ones.
/// </summary>
public class GamePadDriverTests : GamePadDriverStickTestsBase
{
    private sealed class SilkGamePadRig : IHostStickGamePadRig
    {
        private ButtonName? _button;
        private float _leftTrigger;
        private float _rightTrigger;
        private (float X, float Y) _leftStick;
        private (float X, float Y) _rightStick;

        public GamePad GamePad { get; } = new GamePad();

        public void Press(GumGamepadButton button) => _button = ToSilk(button);

        public void SetTriggers(float left, float right)
        {
            _leftTrigger = left;
            _rightTrigger = right;
        }

        // Silk reports stick Y as positive-down (SDL native); the rig takes Gum's positive-up.
        public void SetLeftStick(float x, float y) => _leftStick = (x, -y);

        public void SetRightStick(float x, float y) => _rightStick = (x, -y);

        public void Apply(double time)
        {
            Mock<IGamepad> device = CreateGamepad(
                buttons: _button == null ? null : new[] { new Button(_button.Value, 0, pressed: true) },
                thumbsticks: new[]
                {
                    new Thumbstick(0, _leftStick.X, _leftStick.Y),
                    new Thumbstick(1, _rightStick.X, _rightStick.Y),
                },
                triggers: new[] { new Trigger(0, _leftTrigger), new Trigger(1, _rightTrigger) });

            GamePadDriver.Apply(GamePad, device.Object, time);
        }

        // The bootstrap input context has no gamepads, so every index is already "not present".
        public void RemoveDevice()
        {
        }

        private static ButtonName ToSilk(GumGamepadButton button) => button switch
        {
            GumGamepadButton.DPadUp => ButtonName.DPadUp,
            GumGamepadButton.DPadDown => ButtonName.DPadDown,
            GumGamepadButton.DPadLeft => ButtonName.DPadLeft,
            GumGamepadButton.DPadRight => ButtonName.DPadRight,
            GumGamepadButton.A => ButtonName.A,
            GumGamepadButton.B => ButtonName.B,
            GumGamepadButton.X => ButtonName.X,
            GumGamepadButton.Y => ButtonName.Y,
            GumGamepadButton.LeftShoulder => ButtonName.LeftBumper,
            GumGamepadButton.RightShoulder => ButtonName.RightBumper,
            GumGamepadButton.Start => ButtonName.Start,
            GumGamepadButton.Back => ButtonName.Back,
            GumGamepadButton.LeftStick => ButtonName.LeftStick,
            GumGamepadButton.RightStick => ButtonName.RightStick,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Not a Silk button."),
        };
    }

    protected override IHostStickGamePadRig CreateStickRig() => new SilkGamePadRig();

    private static Mock<IGamepad> CreateGamepad(
        bool connected = true,
        IReadOnlyList<Button>? buttons = null,
        IReadOnlyList<Thumbstick>? thumbsticks = null,
        IReadOnlyList<Trigger>? triggers = null)
    {
        var gamepad = new Mock<IGamepad>();
        gamepad.SetupGet(g => g.IsConnected).Returns(connected);
        gamepad.SetupGet(g => g.Buttons).Returns(buttons ?? new List<Button>());
        gamepad.SetupGet(g => g.Thumbsticks).Returns(thumbsticks ?? new List<Thumbstick>());
        gamepad.SetupGet(g => g.Triggers).Returns(triggers ?? new List<Trigger>());
        return gamepad;
    }

    [Fact]
    public void Apply_MapsIsConnected()
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad(connected: false);

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        sut.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void Apply_SubscribesButtonAndTriggerEventsExactlyOnce_AcrossRepeatedCalls()
    {
        // Silk.NET.Input.Sdl's SdlGamepad writes its internal _buttons/_triggers arrays as a side
        // effect INSIDE the argument list of `ButtonDown?.Invoke(...)` / `TriggerMoved?.Invoke(...)`.
        // C#'s null-conditional operator short-circuits the WHOLE call -- including evaluating its
        // arguments -- when the event has zero subscribers, so with nobody listening those arrays are
        // never actually updated and polling Buttons/Triggers reads permanently stale state. Apply
        // must force at least one subscriber so real hardware's button/trigger reads stay live. Called
        // every frame, so it must not pile up a new subscription each call.
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad();

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);
        GamePadDriver.Apply(sut, silkGamepad.Object, time: 2);

        silkGamepad.VerifyAdd(g => g.ButtonDown += It.IsAny<Action<IGamepad, Button>>(), Times.Once());
        silkGamepad.VerifyAdd(g => g.ButtonUp += It.IsAny<Action<IGamepad, Button>>(), Times.Once());
        silkGamepad.VerifyAdd(g => g.TriggerMoved += It.IsAny<Action<IGamepad, Trigger>>(), Times.Once());
    }

    [Fact]
    public void Apply_WithNoTriggersOrSticks_LeavesThemNeutral()
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad();

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        sut.ButtonDown(GumGamepadButton.LeftTrigger).ShouldBeFalse();
        sut.ButtonDown(GumGamepadButton.RightTrigger).ShouldBeFalse();
        sut.LeftStick.X.ShouldBe(0f);
        sut.RightStick.Y.ShouldBe(0f);
    }
}
