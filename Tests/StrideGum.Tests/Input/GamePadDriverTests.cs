using Gum.Input;
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
}
