using Gum.Input;
using Shouldly;

namespace RaylibGum.Tests.Inputs;

/// <summary>
/// Tests for the platform-neutral <see cref="GamePad"/> data holder in GumCommon.
/// The holder stores state pushed in by a platform driver (on Raylib,
/// <c>GamePadDriver.Apply(GamePad, int, double)</c>, dispatched from
/// <c>FormsUtilities.UpdateGamepads</c>) via <see cref="GamePad.SetButtonState"/> /
/// <see cref="GamePad.SetLeftStickPosition"/> and exposes it through the
/// <see cref="IGamePad"/> query API that Forms navigation consumes. Before this holder
/// was implemented every query returned false, so controller tabbing did nothing on
/// Raylib (issue #3046).
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

    [Fact]
    public void IGamePad_RightStick_ReturnsSameInstance_AsConcreteRightStick()
    {
        GamePad sut = new GamePad();

        ((IGamePad)sut).RightStick.ShouldBeSameAs(sut.RightStick);
    }

    [Fact]
    public void RightStick_XY_ReflectsSetRightStickPosition()
    {
        GamePad sut = new GamePad();
        sut.SetRightStickPosition(0.4f, -0.8f);
        sut.Activity(1);

        sut.RightStick.X.ShouldBe(0.4f);
        sut.RightStick.Y.ShouldBe(-0.8f);
    }

    // ---- Phase 1 member-parity additions (issue #3137) ----

    [Fact]
    public void Clear_ResetsButtonDown_Immediately()
    {
        GamePad sut = new GamePad();
        sut.SetButtonState(GamepadButton.A, true);
        sut.Activity(1);

        sut.Clear();

        sut.ButtonDown(GamepadButton.A).ShouldBeFalse();
    }

    [Fact]
    public void Clear_PreservesConnectionState_WhenConnected()
    {
        GamePad sut = new GamePad();
        sut.SetConnected(true);
        sut.Activity(1);

        sut.Clear();

        sut.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public void Clear_PreservesStickReferences()
    {
        GamePad sut = new GamePad();
        AnalogStick left = sut.LeftStick;
        AnalogStick right = sut.RightStick;

        sut.Clear();

        ReferenceEquals(sut.LeftStick, left).ShouldBeTrue();
        ReferenceEquals(sut.RightStick, right).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_InitializesRightStick()
    {
        GamePad sut = new GamePad();
        sut.RightStick.ShouldNotBeNull();
    }

    [Fact]
    public void IsConnected_False_WhenNoActivityCalled()
    {
        GamePad sut = new GamePad();
        sut.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void IsConnected_True_WhenDriverReportsConnected()
    {
        GamePad sut = new GamePad();
        sut.SetConnected(true);
        sut.Activity(1);

        sut.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public void RightStick_AsDPadDown_ReturnsTrue_WhenPushedRight()
    {
        GamePad sut = new GamePad();
        sut.SetRightStickPosition(1, 0);
        sut.Activity(1);

        sut.RightStick.AsDPadDown(DPadDirection.Right).ShouldBeTrue();
    }

    [Fact]
    public void WasDisconnectedThisFrame_True_OnTheFrameConnectionDrops()
    {
        GamePad sut = new GamePad();
        sut.SetConnected(true);
        sut.Activity(1);

        sut.SetConnected(false);
        sut.Activity(2);

        sut.WasDisconnectedThisFrame.ShouldBeTrue();
    }

    [Fact]
    public void WasDisconnectedThisFrame_False_WhenConnectedBothFrames()
    {
        GamePad sut = new GamePad();
        sut.SetConnected(true);
        sut.Activity(1);
        sut.SetConnected(true);
        sut.Activity(2);

        sut.WasDisconnectedThisFrame.ShouldBeFalse();
    }
}
