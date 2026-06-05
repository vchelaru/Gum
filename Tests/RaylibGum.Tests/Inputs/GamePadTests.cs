using Gum.Input;
using Shouldly;

namespace RaylibGum.Tests.Inputs;

/// <summary>
/// Tests for the platform-neutral <see cref="GamePad"/> data holder in GumCommon.
/// The holder stores state pushed in by a platform driver (on Raylib, the
/// <c>#if RAYLIB</c> branch of <c>FormsUtilities.UpdateGamepads</c>) via
/// <see cref="GamePad.SetButtonState"/> / <see cref="GamePad.SetLeftStickPosition"/>
/// and exposes it through the <see cref="IGamePad"/> query API that Forms navigation
/// consumes. Before this holder was implemented every query returned false, so
/// controller tabbing did nothing on Raylib (issue #3046).
/// </summary>
public class GamePadTests : BaseTestClass
{
    [Fact]
    public void ButtonDown_ReturnsTrue_WhenButtonSetDownThisFrame()
    {
        GamePad sut = new GamePad();
        sut.SetButtonState(GamepadButton.A, true);
        sut.Activity(1);

        sut.ButtonDown(GamepadButton.A).ShouldBeTrue();
        sut.ButtonDown(GamepadButton.B).ShouldBeFalse();
    }

    [Fact]
    public void ButtonPushed_ReturnsTrueOnlyOnTheFrameTheButtonGoesDown()
    {
        GamePad sut = new GamePad();

        sut.SetButtonState(GamepadButton.A, true);
        sut.Activity(1);
        sut.ButtonPushed(GamepadButton.A).ShouldBeTrue();

        // Still held the next frame -> no longer "pushed".
        sut.SetButtonState(GamepadButton.A, true);
        sut.Activity(2);
        sut.ButtonPushed(GamepadButton.A).ShouldBeFalse();
    }

    [Fact]
    public void ButtonReleased_ReturnsTrue_OnTheFrameTheButtonGoesUp()
    {
        GamePad sut = new GamePad();

        sut.SetButtonState(GamepadButton.A, true);
        sut.Activity(1);

        sut.SetButtonState(GamepadButton.A, false);
        sut.Activity(2);

        sut.ButtonReleased(GamepadButton.A).ShouldBeTrue();
    }

    [Fact]
    public void ButtonRepeatRate_ReturnsTrue_OnInitialPush()
    {
        GamePad sut = new GamePad();
        sut.SetButtonState(GamepadButton.DPadDown, true);
        sut.Activity(1);

        sut.ButtonRepeatRate(GamepadButton.DPadDown).ShouldBeTrue();
    }

    [Fact]
    public void LeftStick_AsDPadPushedRepeatRate_ReturnsTrue_WhenPushedDown()
    {
        GamePad sut = new GamePad();
        // XNA/Gum stick convention: down is negative Y.
        sut.SetLeftStickPosition(0, -1);
        sut.Activity(1);

        ((IAnalogStick)sut.LeftStick).AsDPadPushedRepeatRate(DPadDirection.Down).ShouldBeTrue();
    }

    [Fact]
    public void LeftStick_AsDPadPushed_ReturnsFalse_WhenInsideDeadzone()
    {
        GamePad sut = new GamePad();
        sut.SetLeftStickPosition(0, -0.05f);
        sut.Activity(1);

        ((IAnalogStick)sut.LeftStick).AsDPadPushed(DPadDirection.Down).ShouldBeFalse();
    }
}
