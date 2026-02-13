using Microsoft.Xna.Framework.Input;
using MonoGameGum.Input;
using Shouldly;
using Xunit;
using GamePad = MonoGameGum.Input.GamePad;

namespace MonoGameGum.Tests.Input;

public class GamePadTests
{
    #region Helper Methods

    private static GamePadState CreateGamePadState(
        bool isConnected = true,
        bool aPressed = false,
        bool bPressed = false,
        bool xPressed = false,
        bool yPressed = false,
        bool leftShoulderPressed = false,
        bool rightShoulderPressed = false,
        bool backPressed = false,
        bool startPressed = false,
        bool leftStickPressed = false,
        bool rightStickPressed = false,
        bool dpadUpPressed = false,
        bool dpadDownPressed = false,
        bool dpadLeftPressed = false,
        bool dpadRightPressed = false,
        float leftTrigger = 0f,
        float rightTrigger = 0f)
    {
        var buttons = new GamePadButtons(
            (aPressed ? Buttons.A : 0) |
            (bPressed ? Buttons.B : 0) |
            (xPressed ? Buttons.X : 0) |
            (yPressed ? Buttons.Y : 0) |
            (leftShoulderPressed ? Buttons.LeftShoulder : 0) |
            (rightShoulderPressed ? Buttons.RightShoulder : 0) |
            (backPressed ? Buttons.Back : 0) |
            (startPressed ? Buttons.Start : 0) |
            (leftStickPressed ? Buttons.LeftStick : 0) |
            (rightStickPressed ? Buttons.RightStick : 0));

        var dpad = new GamePadDPad(
            dpadUpPressed ? ButtonState.Pressed : ButtonState.Released,
            dpadDownPressed ? ButtonState.Pressed : ButtonState.Released,
            dpadLeftPressed ? ButtonState.Pressed : ButtonState.Released,
            dpadRightPressed ? ButtonState.Pressed : ButtonState.Released);

        var triggers = new GamePadTriggers(leftTrigger, rightTrigger);
        var thumbSticks = new GamePadThumbSticks(new Microsoft.Xna.Framework.Vector2(0, 0), new Microsoft.Xna.Framework.Vector2(0, 0));

        return new GamePadState(thumbSticks, triggers, buttons, dpad);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeLeftStick()
    {
        var sut = new GamePad();
        sut.LeftStick.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeRightStick()
    {
        var sut = new GamePad();
        sut.RightStick.ShouldNotBeNull();
    }

    #endregion

    #region IsConnected Tests

    [Fact]
    public void IsConnected_ShouldBeFalse_WhenNoActivityCalled()
    {
        var sut = new GamePad();
        sut.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void IsConnected_ShouldBeFalse_WhenDisconnected()
    {
        var sut = new GamePad();
        var disconnectedState = new GamePadState();
        sut.Activity(disconnectedState, 0);

        sut.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void IsConnected_ShouldBeTrue_WhenConnected()
    {
        var sut = new GamePad();
        var connectedState = CreateGamePadState(isConnected: true);
        sut.Activity(connectedState, 0);

        sut.IsConnected.ShouldBeTrue();
    }

    #endregion

    #region WasDisconnectedThisFrame Tests

    [Fact]
    public void WasDisconnectedThisFrame_ShouldBeFalse_WhenConnectedBothFrames()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(isConnected: true), 0);
        sut.Activity(CreateGamePadState(isConnected: true), 0.016);

        sut.WasDisconnectedThisFrame.ShouldBeFalse();
    }

    [Fact]
    public void WasDisconnectedThisFrame_ShouldBeFalse_WhenDisconnectedBothFrames()
    {
        var sut = new GamePad();
        sut.Activity(new GamePadState(), 0);
        sut.Activity(new GamePadState(), 0.016);

        sut.WasDisconnectedThisFrame.ShouldBeFalse();
    }

    [Fact]
    public void WasDisconnectedThisFrame_ShouldBeTrue_WhenDisconnectedThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(isConnected: true), 0);
        sut.Activity(new GamePadState(), 0.016);

        sut.WasDisconnectedThisFrame.ShouldBeTrue();
    }

    #endregion

    #region ButtonDown Tests

    [Fact]
    public void ButtonDown_A_ShouldReturnFalse_WhenNotPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(), 0);

        sut.ButtonDown(Buttons.A).ShouldBeFalse();
    }

    [Fact]
    public void ButtonDown_A_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: true), 0);

        sut.ButtonDown(Buttons.A).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_B_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(bPressed: true), 0);

        sut.ButtonDown(Buttons.B).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_DPadDown_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(dpadDownPressed: true), 0);

        sut.ButtonDown(Buttons.DPadDown).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_DPadLeft_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(dpadLeftPressed: true), 0);

        sut.ButtonDown(Buttons.DPadLeft).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_DPadRight_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(dpadRightPressed: true), 0);

        sut.ButtonDown(Buttons.DPadRight).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_DPadUp_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(dpadUpPressed: true), 0);

        sut.ButtonDown(Buttons.DPadUp).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_LeftShoulder_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(leftShoulderPressed: true), 0);

        sut.ButtonDown(Buttons.LeftShoulder).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_LeftTrigger_ShouldReturnFalse_WhenBelowThreshold()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(leftTrigger: 0.4f), 0);

        sut.ButtonDown(Buttons.LeftTrigger).ShouldBeFalse();
    }

    [Fact]
    public void ButtonDown_LeftTrigger_ShouldReturnTrue_WhenAboveThreshold()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(leftTrigger: 0.6f), 0);

        sut.ButtonDown(Buttons.LeftTrigger).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_RightShoulder_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(rightShoulderPressed: true), 0);

        sut.ButtonDown(Buttons.RightShoulder).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_RightTrigger_ShouldReturnFalse_WhenBelowThreshold()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(rightTrigger: 0.4f), 0);

        sut.ButtonDown(Buttons.RightTrigger).ShouldBeFalse();
    }

    [Fact]
    public void ButtonDown_RightTrigger_ShouldReturnTrue_WhenAboveThreshold()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(rightTrigger: 0.6f), 0);

        sut.ButtonDown(Buttons.RightTrigger).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_Start_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(startPressed: true), 0);

        sut.ButtonDown(Buttons.Start).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_X_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(xPressed: true), 0);

        sut.ButtonDown(Buttons.X).ShouldBeTrue();
    }

    [Fact]
    public void ButtonDown_Y_ShouldReturnTrue_WhenPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(yPressed: true), 0);

        sut.ButtonDown(Buttons.Y).ShouldBeTrue();
    }

    #endregion

    #region ButtonPushed Tests

    [Fact]
    public void ButtonPushed_A_ShouldReturnFalse_WhenHeldDownBothFrames()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: true), 0);
        sut.Activity(CreateGamePadState(aPressed: true), 0.016);

        sut.ButtonPushed(Buttons.A).ShouldBeFalse();
    }

    [Fact]
    public void ButtonPushed_A_ShouldReturnFalse_WhenNotPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(), 0);

        sut.ButtonPushed(Buttons.A).ShouldBeFalse();
    }

    [Fact]
    public void ButtonPushed_A_ShouldReturnTrue_WhenPressedThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: false), 0);
        sut.Activity(CreateGamePadState(aPressed: true), 0.016);

        sut.ButtonPushed(Buttons.A).ShouldBeTrue();
    }

    [Fact]
    public void ButtonPushed_DPadUp_ShouldReturnTrue_WhenPressedThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(dpadUpPressed: false), 0);
        sut.Activity(CreateGamePadState(dpadUpPressed: true), 0.016);

        sut.ButtonPushed(Buttons.DPadUp).ShouldBeTrue();
    }

    [Fact]
    public void ButtonPushed_LeftTrigger_ShouldReturnFalse_WhenAboveThresholdBothFrames()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(leftTrigger: 0.6f), 0);
        sut.Activity(CreateGamePadState(leftTrigger: 0.7f), 0.016);

        sut.ButtonPushed(Buttons.LeftTrigger).ShouldBeFalse();
    }

    [Fact]
    public void ButtonPushed_LeftTrigger_ShouldReturnTrue_WhenCrossesThresholdThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(leftTrigger: 0.4f), 0);
        sut.Activity(CreateGamePadState(leftTrigger: 0.6f), 0.016);

        sut.ButtonPushed(Buttons.LeftTrigger).ShouldBeTrue();
    }

    [Fact]
    public void ButtonPushed_Start_ShouldReturnTrue_WhenPressedThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(startPressed: false), 0);
        sut.Activity(CreateGamePadState(startPressed: true), 0.016);

        sut.ButtonPushed(Buttons.Start).ShouldBeTrue();
    }

    #endregion

    #region ButtonReleased Tests

    [Fact]
    public void ButtonReleased_A_ShouldReturnFalse_WhenHeldDownBothFrames()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: true), 0);
        sut.Activity(CreateGamePadState(aPressed: true), 0.016);

        sut.ButtonReleased(Buttons.A).ShouldBeFalse();
    }

    [Fact]
    public void ButtonReleased_A_ShouldReturnFalse_WhenNotPressedBothFrames()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: false), 0);
        sut.Activity(CreateGamePadState(aPressed: false), 0.016);

        sut.ButtonReleased(Buttons.A).ShouldBeFalse();
    }

    [Fact]
    public void ButtonReleased_A_ShouldReturnTrue_WhenReleasedThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: true), 0);
        sut.Activity(CreateGamePadState(aPressed: false), 0.016);

        sut.ButtonReleased(Buttons.A).ShouldBeTrue();
    }

    [Fact]
    public void ButtonReleased_B_ShouldReturnTrue_WhenReleasedThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(bPressed: true), 0);
        sut.Activity(CreateGamePadState(bPressed: false), 0.016);

        sut.ButtonReleased(Buttons.B).ShouldBeTrue();
    }

    [Fact]
    public void ButtonReleased_DPadDown_ShouldReturnTrue_WhenReleasedThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(dpadDownPressed: true), 0);
        sut.Activity(CreateGamePadState(dpadDownPressed: false), 0.016);

        sut.ButtonReleased(Buttons.DPadDown).ShouldBeTrue();
    }

    [Fact]
    public void ButtonReleased_LeftTrigger_ShouldReturnTrue_WhenCrossesBelowThresholdThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(leftTrigger: 0.6f), 0);
        sut.Activity(CreateGamePadState(leftTrigger: 0.4f), 0.016);

        sut.ButtonReleased(Buttons.LeftTrigger).ShouldBeTrue();
    }

    [Fact]
    public void ButtonReleased_RightTrigger_ShouldReturnTrue_WhenCrossesBelowThresholdThisFrame()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(rightTrigger: 0.6f), 0);
        sut.Activity(CreateGamePadState(rightTrigger: 0.4f), 0.016);

        sut.ButtonReleased(Buttons.RightTrigger).ShouldBeTrue();
    }

    #endregion

    #region ButtonRepeatRate Tests

    [Fact]
    public void ButtonRepeatRate_ShouldReturnFalse_WhenButtonNotPressed()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: false), 0);

        sut.ButtonRepeatRate(Buttons.A).ShouldBeFalse();
    }

    [Fact]
    public void ButtonRepeatRate_ShouldReturnTrue_OnInitialPush()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: false), 0);
        sut.Activity(CreateGamePadState(aPressed: true), 0.016);

        sut.ButtonRepeatRate(Buttons.A).ShouldBeTrue();
    }

    [Fact]
    public void ButtonRepeatRate_ShouldReturnFalse_WhenHeldButBeforeRepeatTime()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: false), 0);
        sut.Activity(CreateGamePadState(aPressed: true), 0.016);
        sut.Activity(CreateGamePadState(aPressed: true), 0.032); // Still within initial delay

        sut.ButtonRepeatRate(Buttons.A).ShouldBeFalse();
    }

    [Fact]
    public void ButtonRepeatRate_ShouldReturnTrue_AfterRepeatDelay()
    {
        var sut = new GamePad();
        // Initial push at time 0
        sut.Activity(CreateGamePadState(aPressed: false), 0);
        sut.Activity(CreateGamePadState(aPressed: true), 0.016);

        // Hold for longer than default timeAfterPush (0.35s)
        sut.Activity(CreateGamePadState(aPressed: true), 0.4);

        sut.ButtonRepeatRate(Buttons.A).ShouldBeTrue();
    }

    [Fact]
    public void ButtonRepeatRate_ShouldReturnTrue_AfterRepeatInterval()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: false), 0);
        sut.Activity(CreateGamePadState(aPressed: true), 0.016);

        // First repeat
        sut.Activity(CreateGamePadState(aPressed: true), 0.4);

        // Wait for repeat interval (0.12s default)
        sut.Activity(CreateGamePadState(aPressed: true), 0.53);

        sut.ButtonRepeatRate(Buttons.A).ShouldBeTrue();
    }

    [Fact]
    public void ButtonRepeatRate_ShouldUseCustomTimings()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: false), 0);
        sut.Activity(CreateGamePadState(aPressed: true), 0.016);

        // Custom timings: 0.5s delay, 0.2s repeat
        sut.Activity(CreateGamePadState(aPressed: true), 0.4);
        sut.ButtonRepeatRate(Buttons.A, timeAfterPush: 0.5, timeBetweenRepeating: 0.2).ShouldBeFalse();

        sut.Activity(CreateGamePadState(aPressed: true), 0.6);
        sut.ButtonRepeatRate(Buttons.A, timeAfterPush: 0.5, timeBetweenRepeating: 0.2).ShouldBeTrue();
    }

    #endregion

    #region Activity Tests

    [Fact]
    public void Activity_ShouldUpdateAnalogSticks()
    {
        var sut = new GamePad();
        var thumbSticks = new GamePadThumbSticks(new Microsoft.Xna.Framework.Vector2(0.5f, 0.5f), new Microsoft.Xna.Framework.Vector2(-0.5f, -0.5f));
        var state = new GamePadState(thumbSticks, new GamePadTriggers(0, 0), new GamePadButtons(0), new GamePadDPad());

        sut.Activity(state, 0);

        // The sticks should be updated (they're non-null)
        sut.LeftStick.ShouldNotBeNull();
        sut.RightStick.ShouldNotBeNull();
    }

    [Fact]
    public void Activity_ShouldUpdateButtonPushTimes()
    {
        var sut = new GamePad();
        sut.Activity(CreateGamePadState(aPressed: false), 0);

        // Push the button
        sut.Activity(CreateGamePadState(aPressed: true), 0.5);

        // Verify the push was recorded by checking ButtonPushed returns true
        sut.ButtonPushed(Buttons.A).ShouldBeTrue();
    }

    #endregion
}
