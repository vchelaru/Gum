using Gum;
using Gum.Input;
using RenderingLibrary;
using Moq;
using Shouldly;
using Stride.Input;
using GumGamepadButton = Gum.Input.GamepadButton;

namespace StrideGum.Tests.Input;

/// <summary>
/// Pins the button/stick/trigger mapping of <see cref="GamePadDriver"/> (issue #4600). Unlike Silk
/// (an <see cref="Silk.NET.Input.IGamepad"/> instance with live per-button queries), Stride hands out
/// a single <see cref="GamePadState"/> snapshot (a button bitmask + axis floats) via
/// <see cref="IGamePadDevice.State"/>, so a mocked <see cref="IGamePadDevice"/> returning that
/// snapshot is the seam here.
/// </summary>
public class GamePadDriverTests
{
    private static Mock<IGamePadDevice> CreateGamePad(GamePadState state)
    {
        var device = new Mock<IGamePadDevice>();
        device.SetupGet(g => g.State).Returns(state);
        return device;
    }

    [Fact]
    public void Apply_MapsAButtonPressed_ToA()
    {
        GamePad sut = new GamePad();
        var strideGamePad = CreateGamePad(new GamePadState { Buttons = GamePadButton.A });

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);

        sut.ButtonDown(GumGamepadButton.A).ShouldBeTrue();
        sut.ButtonDown(GumGamepadButton.B).ShouldBeFalse();
    }

    [Fact]
    public void Apply_MapsLeftShoulder_NotRightShoulder()
    {
        GamePad sut = new GamePad();
        var strideGamePad = CreateGamePad(new GamePadState { Buttons = GamePadButton.LeftShoulder });

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);

        sut.ButtonDown(GumGamepadButton.LeftShoulder).ShouldBeTrue();
        sut.ButtonDown(GumGamepadButton.RightShoulder).ShouldBeFalse();
    }

    [Fact]
    public void Apply_ThresholdsLeftTrigger_ToLeftTrigger()
    {
        GamePad sut = new GamePad();
        var strideGamePad = CreateGamePad(new GamePadState { LeftTrigger = 0.75f, RightTrigger = 0f });

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);

        sut.ButtonDown(GumGamepadButton.LeftTrigger).ShouldBeTrue();
        sut.ButtonDown(GumGamepadButton.RightTrigger).ShouldBeFalse();
    }

    [Fact]
    public void Apply_PassesThumbstickYStraightThrough_AlreadyXnaGumConvention()
    {
        GamePad sut = new GamePad();
        // Unlike Silk (SDL native, positive-down, needs flipping), Stride's LeftThumb.Y is already
        // XNA/Gum convention (positive-up) -- confirmed against Stride's XInput backend, which passes
        // the raw XInput axis value through unchanged.
        var strideGamePad = CreateGamePad(new GamePadState { LeftThumb = new(0f, 1f) });

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);

        sut.LeftStick.AsDPadDown(DPadDirection.Up).ShouldBeTrue();
    }

    [Fact]
    public void Apply_SetsConnectedTrue()
    {
        GamePad sut = new GamePad();
        var strideGamePad = CreateGamePad(new GamePadState());

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);

        sut.IsConnected.ShouldBeTrue();
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
    [InlineData(GamePadButton.PadUp, GumGamepadButton.DPadUp)]
    [InlineData(GamePadButton.PadDown, GumGamepadButton.DPadDown)]
    [InlineData(GamePadButton.PadLeft, GumGamepadButton.DPadLeft)]
    [InlineData(GamePadButton.PadRight, GumGamepadButton.DPadRight)]
    [InlineData(GamePadButton.A, GumGamepadButton.A)]
    [InlineData(GamePadButton.B, GumGamepadButton.B)]
    [InlineData(GamePadButton.X, GumGamepadButton.X)]
    [InlineData(GamePadButton.Y, GumGamepadButton.Y)]
    [InlineData(GamePadButton.LeftShoulder, GumGamepadButton.LeftShoulder)]
    [InlineData(GamePadButton.RightShoulder, GumGamepadButton.RightShoulder)]
    [InlineData(GamePadButton.Start, GumGamepadButton.Start)]
    [InlineData(GamePadButton.Back, GumGamepadButton.Back)]
    [InlineData(GamePadButton.LeftThumb, GumGamepadButton.LeftStick)]
    [InlineData(GamePadButton.RightThumb, GumGamepadButton.RightStick)]
    public void Apply_MapsEachStrideButton_ToExactlyOneGumButton(GamePadButton strideButton, GumGamepadButton expected)
    {
        GamePad sut = new GamePad();
        var strideGamePad = CreateGamePad(new GamePadState { Buttons = strideButton });

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);

        foreach (GumGamepadButton button in MappedButtons)
        {
            sut.ButtonDown(button).ShouldBe(button == expected, button.ToString());
        }
    }

    [Theory]
    [InlineData(0.49f, false)]
    [InlineData(0.5f, true)]
    public void Apply_ThresholdsBothTriggers_AtHalf(float value, bool expected)
    {
        GamePad sut = new GamePad();
        var strideGamePad = CreateGamePad(new GamePadState { LeftTrigger = value, RightTrigger = value });

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);

        sut.ButtonDown(GumGamepadButton.LeftTrigger).ShouldBe(expected);
        sut.ButtonDown(GumGamepadButton.RightTrigger).ShouldBe(expected);
    }

    [Fact]
    public void Apply_PassesRightStickThrough_YPositiveUp()
    {
        GamePad sut = new GamePad();
        var strideGamePad = CreateGamePad(new GamePadState { RightThumb = new(0.9f, 0.9f) });

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);

        sut.RightStick.X.ShouldBeGreaterThan(0.5f);
        sut.RightStick.Y.ShouldBeGreaterThan(0.5f);
    }

    [Fact]
    public void Apply_CommitsTheFrame_SoPushIsReportedOnlyOnce()
    {
        GamePad sut = new GamePad();
        var strideGamePad = CreateGamePad(new GamePadState { Buttons = GamePadButton.A });

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 1);
        sut.ButtonPushed(GumGamepadButton.A).ShouldBeTrue();

        GamePadDriver.Apply(sut, strideGamePad.Object, time: 2);
        sut.ButtonPushed(GumGamepadButton.A).ShouldBeFalse();
        sut.ButtonDown(GumGamepadButton.A).ShouldBeTrue();
    }

    [Fact]
    public void ApplyGamePadState_WithNoStrideGamePadAtIndex_DisconnectsButKeepsLastPushedButtons()
    {
        GamePad sut = new GamePad();
        sut.SetConnected(true);
        sut.SetButtonState(GumGamepadButton.A, isDown: true);
        sut.Activity(1);
        sut.IsConnected.ShouldBeTrue();

        // The bootstrap InputManager has no game pads, so every index is "not present".
        ((IGumService)GumService.Default).ApplyGamePadState(sut, 0, 2);

        sut.IsConnected.ShouldBeFalse();
        // Unlike the Unity host, nothing releases the buttons on disconnect; they keep their last pushed state.
        sut.ButtonDown(GumGamepadButton.A).ShouldBeTrue();
    }
}
