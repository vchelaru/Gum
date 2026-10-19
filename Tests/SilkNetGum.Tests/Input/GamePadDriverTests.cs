using Gum;
using RenderingLibrary;
using Gum.Input;
using Moq;
using Shouldly;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using GumGamepadButton = Gum.Input.GamepadButton;

namespace SilkNetGum.Tests.Input;

/// <summary>
/// Pins the button/stick/trigger mapping of <see cref="GamePadDriver"/> (issue #3668). Unlike the
/// Raylib driver (static functions reading live hardware, needing a delegate-injection seam), Silk
/// hands out an <see cref="IGamepad"/> instance directly, so a mocked <see cref="IGamepad"/> is the
/// seam here.
/// </summary>
public class GamePadDriverTests
{
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
    public void Apply_MapsAButtonPressed_ToA()
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad(buttons: new[] { new Button(ButtonName.A, 0, pressed: true) });

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        sut.ButtonDown(GumGamepadButton.A).ShouldBeTrue();
        sut.ButtonDown(GumGamepadButton.B).ShouldBeFalse();
    }

    [Fact]
    public void Apply_MapsLeftBumper_ToLeftShoulder_NotRightShoulder()
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad(buttons: new[] { new Button(ButtonName.LeftBumper, 0, pressed: true) });

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        sut.ButtonDown(GumGamepadButton.LeftShoulder).ShouldBeTrue();
        sut.ButtonDown(GumGamepadButton.RightShoulder).ShouldBeFalse();
    }

    [Fact]
    public void Apply_ThresholdsLeftTriggerPosition_ToLeftTrigger()
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad(triggers: new[] { new Trigger(0, position: 0.75f), new Trigger(1, position: 0f) });

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        sut.ButtonDown(GumGamepadButton.LeftTrigger).ShouldBeTrue();
        sut.ButtonDown(GumGamepadButton.RightTrigger).ShouldBeFalse();
    }

    [Fact]
    public void Apply_FlipsLeftStickY_ToXnaGumConvention()
    {
        GamePad sut = new GamePad();
        // Silk reports stick Y as positive-down (SDL native); Gum/XNA convention is positive-up.
        var silkGamepad = CreateGamepad(thumbsticks: new[] { new Thumbstick(0, x: 0f, y: 1f) });

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        sut.LeftStick.AsDPadDown(DPadDirection.Down).ShouldBeTrue();
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

    private static readonly GumGamepadButton[] MappedButtons =
    {
        GumGamepadButton.DPadUp, GumGamepadButton.DPadDown, GumGamepadButton.DPadLeft, GumGamepadButton.DPadRight,
        GumGamepadButton.A, GumGamepadButton.B, GumGamepadButton.X, GumGamepadButton.Y,
        GumGamepadButton.LeftShoulder, GumGamepadButton.RightShoulder,
        GumGamepadButton.Start, GumGamepadButton.Back,
        GumGamepadButton.LeftStick, GumGamepadButton.RightStick,
        GumGamepadButton.LeftTrigger, GumGamepadButton.RightTrigger,
    };

    [Theory]
    [InlineData(ButtonName.DPadUp, GumGamepadButton.DPadUp)]
    [InlineData(ButtonName.DPadDown, GumGamepadButton.DPadDown)]
    [InlineData(ButtonName.DPadLeft, GumGamepadButton.DPadLeft)]
    [InlineData(ButtonName.DPadRight, GumGamepadButton.DPadRight)]
    [InlineData(ButtonName.A, GumGamepadButton.A)]
    [InlineData(ButtonName.B, GumGamepadButton.B)]
    [InlineData(ButtonName.X, GumGamepadButton.X)]
    [InlineData(ButtonName.Y, GumGamepadButton.Y)]
    [InlineData(ButtonName.LeftBumper, GumGamepadButton.LeftShoulder)]
    [InlineData(ButtonName.RightBumper, GumGamepadButton.RightShoulder)]
    [InlineData(ButtonName.Start, GumGamepadButton.Start)]
    [InlineData(ButtonName.Back, GumGamepadButton.Back)]
    [InlineData(ButtonName.LeftStick, GumGamepadButton.LeftStick)]
    [InlineData(ButtonName.RightStick, GumGamepadButton.RightStick)]
    public void Apply_MapsEachSilkButton_ToExactlyOneGumButton(ButtonName silkButton, GumGamepadButton expected)
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad(buttons: new[] { new Button(silkButton, 0, pressed: true) });

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        foreach (GumGamepadButton button in MappedButtons)
        {
            sut.ButtonDown(button).ShouldBe(button == expected, button.ToString());
        }
    }

    [Theory]
    [InlineData(0.49f, false)]
    [InlineData(0.5f, true)]
    public void Apply_ThresholdsBothTriggers_AtHalf(float position, bool expected)
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad(triggers: new[] { new Trigger(0, position), new Trigger(1, position) });

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        sut.ButtonDown(GumGamepadButton.LeftTrigger).ShouldBe(expected);
        sut.ButtonDown(GumGamepadButton.RightTrigger).ShouldBe(expected);
    }

    [Fact]
    public void Apply_FlipsRightStickY_AndPassesXThrough()
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad(thumbsticks: new[] { new Thumbstick(0, 0f, 0f), new Thumbstick(1, x: 0.9f, y: 0.9f) });

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        sut.RightStick.X.ShouldBeGreaterThan(0.5f);
        sut.RightStick.Y.ShouldBeLessThan(-0.5f);
    }

    [Fact]
    public void Apply_PassesLeftStickXThrough()
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad(thumbsticks: new[] { new Thumbstick(0, x: -0.9f, y: 0f) });

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);

        sut.LeftStick.X.ShouldBeLessThan(-0.5f);
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

    [Fact]
    public void Apply_CommitsTheFrame_SoPushIsReportedOnlyOnce()
    {
        GamePad sut = new GamePad();
        var silkGamepad = CreateGamepad(buttons: new[] { new Button(ButtonName.A, 0, pressed: true) });

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 1);
        sut.ButtonPushed(GumGamepadButton.A).ShouldBeTrue();

        GamePadDriver.Apply(sut, silkGamepad.Object, time: 2);
        sut.ButtonPushed(GumGamepadButton.A).ShouldBeFalse();
        sut.ButtonDown(GumGamepadButton.A).ShouldBeTrue();
    }

    [Fact]
    public void ApplyGamePadState_WithNoSilkGamepadAtIndex_DisconnectsButKeepsLastPushedButtons()
    {
        GamePad sut = new GamePad();
        sut.SetConnected(true);
        sut.SetButtonState(GumGamepadButton.A, isDown: true);
        sut.Activity(1);
        sut.IsConnected.ShouldBeTrue();

        // The bootstrap input context has no gamepads, so every index is "not present".
        ((IGumService)GumService.Default).ApplyGamePadState(sut, 0, 2);

        sut.IsConnected.ShouldBeFalse();
        // Unlike the Unity host, nothing releases the buttons on disconnect; they keep their last pushed state.
        sut.ButtonDown(GumGamepadButton.A).ShouldBeTrue();
    }
}
