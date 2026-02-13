using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Input;

#region DPadDirection Enum
/// <summary>
/// Represents directional input for D-Pad and analog stick direction queries.
/// </summary>
public enum DPadDirection
{
    /// <summary>
    /// Upward direction.
    /// </summary>
    Up,
    /// <summary>
    /// Downward direction.
    /// </summary>
    Down,
    /// <summary>
    /// Leftward direction.
    /// </summary>
    Left,
    /// <summary>
    /// Rightward direction.
    /// </summary>
    Right
}
#endregion

public class GamePad
{
    #region Fields/Properties

    GamePadState mGamePadState = new GamePadState();
    GamePadState mLastGamePadState = new GamePadState();

    /// <summary>
    /// Returns whether the gamepad is currently connected.
    /// </summary>
    public bool IsConnected => mGamePadState.IsConnected;

    /// <summary>
    /// Returns whether the gamepad was disconnected this frame (was connected last frame, but not connected this frame).
    /// </summary>
    public bool WasDisconnectedThisFrame
    {
        get
        {
            return mLastGamePadState.IsConnected && !mGamePadState.IsConnected;
        }
    }

    AnalogStick mLeftStick;
    AnalogStick mRightStick;

    // The curren time, as of the last time Update was called
    double currentTime;

    /// <summary>
    /// Returns a reference to the left analog stick. This value will always be non-null even if the gamepad doesn't have a physical analog stick.
    /// </summary>
    public AnalogStick LeftStick => mLeftStick;


    /// <summary>
    /// Returns a reference to the right analog stick. This value will always be non-null even if the gamepad doesn't have a physical analog stick.
    /// </summary>
    public AnalogStick RightStick => mRightStick;


    // The Buttons enum uses bitfield values (powers of 2), so we need to map bit positions (0-30) to array indices
    double[] mLastButtonPush = new double[31];
    double[] mLastRepeatRate = new double[31];

    const float AnalogOnThreshold = .5f;

    /// <summary>
    /// The left trigger values as reported directly by the gamepad, not flipped for Gamecube
    /// </summary>
    AnalogButton mLeftTrigger;

    /// <summary>
    /// The right trigger values as reported directly by the gamepad, not flipped for Gamecube
    /// </summary>
    AnalogButton mRightTrigger;

    #endregion

    /// <summary>
    /// Creates a new GamePad instance with all inputs initialized to neutral/default states.
    /// </summary>
    public GamePad()
    {
        mLeftStick = new AnalogStick();
        mRightStick = new AnalogStick();

        mLeftTrigger = new AnalogButton();
        mRightTrigger = new AnalogButton();
    }

    /// <summary>
    /// Converts a Buttons enum value (which is a bitfield) to an array index based on bit position.
    /// For example: DPadUp (0x1) -> index 0, DPadDown (0x2) -> index 1, DPadLeft (0x4) -> index 2, etc.
    /// </summary>
    private static int GetButtonIndex(Buttons button)
    {
        return System.Numerics.BitOperations.TrailingZeroCount((uint)button);
    }

    /// <summary>
    /// Clears all gamepad input state while preserving connection status.
    /// This resets both current and previous states to prevent spurious button release events.
    /// </summary>
    public void Clear()
    {
        // Preserve connection state by keeping the current GamePadState's connection info
        // but creating a neutral state with no inputs
        var wasConnected = IsConnected;

        // If we were connected, preserve the current state's packet number to maintain connection
        // Otherwise use a default disconnected state
        if (wasConnected)
        {
            // Create a neutral but connected state by preserving the packet number from current state
            mGamePadState = new GamePadState(
                Microsoft.Xna.Framework.Vector2.Zero,
                Microsoft.Xna.Framework.Vector2.Zero, 
                0, 
                0, 
                new Buttons[0]);
        }
        else
        {
            mGamePadState = new GamePadState();
        }

        // Set last state to match current to prevent spurious events
        mLastGamePadState = mGamePadState;

        Array.Clear(mLastButtonPush, 0, mLastButtonPush.Length);
        Array.Clear(mLastRepeatRate, 0, mLastRepeatRate.Length);

        currentTime = 0;

        // Clear existing instances instead of creating new ones to preserve references
        mLeftStick.Clear();
        mRightStick.Clear();

        mLeftTrigger.Clear();
        mRightTrigger.Clear();
    }

    /// <summary>
    /// Returns whether the specified button is currently pressed down.
    /// </summary>
    /// <param name="button">The button to check, including face buttons, shoulders, triggers, and DPad directions.</param>
    /// <returns>True if the button is currently pressed, false otherwise.</returns>
    public bool ButtonDown(Buttons button)
    {
        //if (mButtonsIgnoredForThisFrame[(int)button] || InputManager.CurrentFrameInputSuspended)
        //    return false;

        bool returnValue = false;

        #region Handle the buttons if there isn't a ButtonMap (this can happen even if there is a ButtonMap)


        bool areShouldersAndTriggersFlipped =
            //AreShoulderAndTriggersFlipped;
            false;


        switch (button)
        {
            case Buttons.A:
                returnValue |= mGamePadState.Buttons.A == ButtonState.Pressed;
                break;
            case Buttons.B:
                returnValue |= mGamePadState.Buttons.B == ButtonState.Pressed;
                break;
            case Buttons.X:
                returnValue |= mGamePadState.Buttons.X == ButtonState.Pressed;
                break;
            case Buttons.Y:
                returnValue |= mGamePadState.Buttons.Y == ButtonState.Pressed;
                break;
            case Buttons.LeftShoulder:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mLeftTrigger.Position >= AnalogOnThreshold;
                }
                else
                {
                    returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
                }
                break;
            case Buttons.RightShoulder:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mRightTrigger.Position >= AnalogOnThreshold;
                }
                else
                {
                    returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
                }
                break;
            case Buttons.Back:
                returnValue |= mGamePadState.Buttons.Back == ButtonState.Pressed;
                break;
            case Buttons.Start:
                returnValue |= mGamePadState.Buttons.Start == ButtonState.Pressed;
                break;
            case Buttons.LeftStick:
                returnValue |= mGamePadState.Buttons.LeftStick == ButtonState.Pressed;
                break;
            case Buttons.RightStick:
                returnValue |= mGamePadState.Buttons.RightStick == ButtonState.Pressed;
                break;
            case Buttons.DPadUp:
                returnValue |= mGamePadState.DPad.Up == ButtonState.Pressed;
                break;
            case Buttons.DPadDown:
                returnValue |= mGamePadState.DPad.Down == ButtonState.Pressed;
                break;
            case Buttons.DPadLeft:
                returnValue |= mGamePadState.DPad.Left == ButtonState.Pressed;
                break;
            case Buttons.DPadRight:
                returnValue |= mGamePadState.DPad.Right == ButtonState.Pressed;
                break;
            case Buttons.LeftTrigger:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
                }
                else
                {
                    returnValue |= mLeftTrigger.Position >= AnalogOnThreshold;
                }
                break;
            case Buttons.RightTrigger:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
                }
                else
                {
                    returnValue |= mRightTrigger.Position >= AnalogOnThreshold;
                }
                break;
        }

        #endregion

        return returnValue;

    }

    /// <summary>
    /// Returns whether the specified button was pushed this frame (pressed this frame but not pressed last frame).
    /// </summary>
    /// <param name="button">The button to check, including face buttons, shoulders, triggers, DPad directions, and thumbstick directions.</param>
    /// <returns>True if the button was pushed this frame, false otherwise.</returns>
    public bool ButtonPushed(Buttons button)
    {
        //if (InputManager.mIgnorePushesThisFrame || mButtonsIgnoredForThisFrame[(int)button] || InputManager.CurrentFrameInputSuspended || ignoredNextPushes[(int)button])
        //    return false;

        bool returnValue = false;

        bool areShouldersAndTriggersFlipped =
            false;
        //AreShoulderAndTriggersFlipped;

        switch (button)
        {
            case Buttons.A:
                returnValue |= mGamePadState.Buttons.A == ButtonState.Pressed && mLastGamePadState.Buttons.A == ButtonState.Released;
                break;
            case Buttons.B:
                returnValue |= mGamePadState.Buttons.B == ButtonState.Pressed && mLastGamePadState.Buttons.B == ButtonState.Released;
                break;
            case Buttons.X:
                returnValue |= mGamePadState.Buttons.X == ButtonState.Pressed && mLastGamePadState.Buttons.X == ButtonState.Released;
                break;
            case Buttons.Y:
                returnValue |= mGamePadState.Buttons.Y == ButtonState.Pressed && mLastGamePadState.Buttons.Y == ButtonState.Released;
                break;
            case Buttons.LeftShoulder:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mLeftTrigger.Position >= AnalogOnThreshold && mLeftTrigger.LastPosition < AnalogOnThreshold;
                }
                else
                {
                    returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Pressed && mLastGamePadState.Buttons.LeftShoulder == ButtonState.Released;
                }
                break;
            case Buttons.RightShoulder:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mRightTrigger.Position >= AnalogOnThreshold && mRightTrigger.LastPosition < AnalogOnThreshold;
                }
                else
                {
                    returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Pressed && mLastGamePadState.Buttons.RightShoulder == ButtonState.Released;
                }
                break;
            case Buttons.Back:
                returnValue |= mGamePadState.Buttons.Back == ButtonState.Pressed && mLastGamePadState.Buttons.Back == ButtonState.Released;
                break;
            case Buttons.Start:
                returnValue |= mGamePadState.Buttons.Start == ButtonState.Pressed && mLastGamePadState.Buttons.Start == ButtonState.Released;
                break;
            case Buttons.LeftStick:
                returnValue |= mGamePadState.Buttons.LeftStick == ButtonState.Pressed && mLastGamePadState.Buttons.LeftStick == ButtonState.Released;
                break;
            case Buttons.RightStick:
                returnValue |= mGamePadState.Buttons.RightStick == ButtonState.Pressed && mLastGamePadState.Buttons.RightStick == ButtonState.Released;
                break;
            case Buttons.DPadUp:
                returnValue |= mGamePadState.DPad.Up == ButtonState.Pressed && mLastGamePadState.DPad.Up == ButtonState.Released;
                break;
            case Buttons.DPadDown:
                returnValue |= mGamePadState.DPad.Down == ButtonState.Pressed && mLastGamePadState.DPad.Down == ButtonState.Released;
                break;
            case Buttons.DPadLeft:
                returnValue |= mGamePadState.DPad.Left == ButtonState.Pressed && mLastGamePadState.DPad.Left == ButtonState.Released;
                break;
            case Buttons.DPadRight:
                returnValue |= mGamePadState.DPad.Right == ButtonState.Pressed && mLastGamePadState.DPad.Right == ButtonState.Released;
                break;
            case Buttons.LeftTrigger:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Pressed && mLastGamePadState.Buttons.LeftShoulder == ButtonState.Released;
                }
                else
                {
                    returnValue |= mLeftTrigger.Position >= AnalogOnThreshold && mLeftTrigger.LastPosition < AnalogOnThreshold;
                }
                break;
            case Buttons.RightTrigger:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Pressed && mLastGamePadState.Buttons.RightShoulder == ButtonState.Released;
                }
                else
                {
                    returnValue |= mRightTrigger.Position >= AnalogOnThreshold && mRightTrigger.LastPosition < AnalogOnThreshold;
                }
                break;
            case Buttons.LeftThumbstickUp:
                returnValue |= LeftStick.AsDPadPushed(DPadDirection.Up);
                break;
            case Buttons.LeftThumbstickDown:
                returnValue |= LeftStick.AsDPadPushed(DPadDirection.Down);
                break;
            case Buttons.LeftThumbstickLeft:
                returnValue |= LeftStick.AsDPadPushed(DPadDirection.Left);
                break;
            case Buttons.LeftThumbstickRight:
                returnValue |= LeftStick.AsDPadPushed(DPadDirection.Right);
                break;
        }

        return returnValue;
    }

    /// <summary>
    /// Returns whether the specified button was released this frame (released this frame but pressed last frame).
    /// </summary>
    /// <param name="button">The button to check, including face buttons, shoulders, triggers, and DPad directions.</param>
    /// <returns>True if the button was released this frame, false otherwise.</returns>
    public bool ButtonReleased(Buttons button)
    {
        //if (mButtonsIgnoredForThisFrame[(int)button] || InputManager.CurrentFrameInputSuspended)
        //    return false;

        bool returnValue = false;

        bool areShouldersAndTriggersFlipped =
            //AreShoulderAndTriggersFlipped;
            false;

        switch (button)
        {
            case Buttons.A:
                returnValue |= mGamePadState.Buttons.A == ButtonState.Released && mLastGamePadState.Buttons.A == ButtonState.Pressed;
                break;
            case Buttons.B:
                returnValue |= mGamePadState.Buttons.B == ButtonState.Released && mLastGamePadState.Buttons.B == ButtonState.Pressed;
                break;
            case Buttons.X:
                returnValue |= mGamePadState.Buttons.X == ButtonState.Released && mLastGamePadState.Buttons.X == ButtonState.Pressed;
                break;
            case Buttons.Y:
                returnValue |= mGamePadState.Buttons.Y == ButtonState.Released && mLastGamePadState.Buttons.Y == ButtonState.Pressed;
                break;
            case Buttons.LeftShoulder:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mLeftTrigger.Position < AnalogOnThreshold && mLeftTrigger.LastPosition >= AnalogOnThreshold;
                }
                else
                {
                    returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Released && mLastGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
                }
                break;
            case Buttons.RightShoulder:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mRightTrigger.Position < AnalogOnThreshold && mRightTrigger.LastPosition >= AnalogOnThreshold;
                }
                else
                {
                    returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Released && mLastGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
                }
                break;
            case Buttons.Back:
                returnValue |= mGamePadState.Buttons.Back == ButtonState.Released && mLastGamePadState.Buttons.Back == ButtonState.Pressed;
                break;
            case Buttons.Start:
                returnValue |= mGamePadState.Buttons.Start == ButtonState.Released && mLastGamePadState.Buttons.Start == ButtonState.Pressed;
                break;
            case Buttons.LeftStick:
                returnValue |= mGamePadState.Buttons.LeftStick == ButtonState.Released && mLastGamePadState.Buttons.LeftStick == ButtonState.Pressed;
                break;
            case Buttons.RightStick:
                returnValue |= mGamePadState.Buttons.RightStick == ButtonState.Released && mLastGamePadState.Buttons.RightStick == ButtonState.Pressed;
                break;
            case Buttons.DPadUp:
                returnValue |= mGamePadState.DPad.Up == ButtonState.Released && mLastGamePadState.DPad.Up == ButtonState.Pressed;
                break;
            case Buttons.DPadDown:
                returnValue |= mGamePadState.DPad.Down == ButtonState.Released && mLastGamePadState.DPad.Down == ButtonState.Pressed;
                break;
            case Buttons.DPadLeft:
                returnValue |= mGamePadState.DPad.Left == ButtonState.Released && mLastGamePadState.DPad.Left == ButtonState.Pressed;
                break;
            case Buttons.DPadRight:
                returnValue |= mGamePadState.DPad.Right == ButtonState.Released && mLastGamePadState.DPad.Right == ButtonState.Pressed;
                break;
            case Buttons.LeftTrigger:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Released && mLastGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
                }
                else
                {
                    returnValue |= mLeftTrigger.Position < AnalogOnThreshold && mLeftTrigger.LastPosition >= AnalogOnThreshold;
                }
                break;
            case Buttons.RightTrigger:
                if (areShouldersAndTriggersFlipped)
                {
                    returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Released && mLastGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
                }
                else
                {
                    returnValue |= mRightTrigger.Position < AnalogOnThreshold && mRightTrigger.LastPosition >= AnalogOnThreshold;
                }
                break;
        }

        return returnValue;

    }


    /// <summary>
    /// Returns whether the argument was pushed this frame, or whether it is continually being held down and a "repeat" press
    /// has occurred.
    /// </summary>
    /// <param name="button">The button to test, which includes DPad directions.</param>
    /// <param name="timeAfterPush">The number of seconds after initial push to wait before raising repeat rates. This value is typically larger than timeBetweenRepeating.</param>
    /// <param name="timeBetweenRepeating">The number of seconds between repeats once the timeAfterPush. This value is typically smaller than timeAfterPush.</param>
    /// <returns>Whether the button was pushed or repeated this frame.</returns>
    public bool ButtonRepeatRate(Buttons button, double timeAfterPush = .35, double timeBetweenRepeating = .12)
    {
        //if (mButtonsIgnoredForThisFrame[(int)button])
        //    return false;

        if (ButtonPushed(button))
            return true;

        // If this method is called multiple times per frame this line
        // of code guarantees that the user will get true every time until
        // the next TimeManager.Update (next frame).
        // The very first frame of FRB would have CurrentTime == 0.
        // The repeat cannot happen on the first frame, so we check for that:
        int buttonIndex = GetButtonIndex(button);
        bool repeatedThisFrame = currentTime > 0 && mLastRepeatRate[buttonIndex] == currentTime;

        if (repeatedThisFrame ||
            (
            ButtonDown(button) &&
            currentTime - mLastButtonPush[buttonIndex] > timeAfterPush &&
            currentTime - mLastRepeatRate[buttonIndex] > timeBetweenRepeating)
            )
        {
            mLastRepeatRate[buttonIndex] = currentTime;
            return true;
        }

        return false;

    }

    internal void Activity(GamePadState gamepadState, double time)
    {
        currentTime = time;
        mLastGamePadState = mGamePadState;
        mGamePadState = gamepadState;

        if (IsConnected || WasDisconnectedThisFrame)
        {
            UpdateAnalogStickAndTriggerValues(time);
            UpdateLastButtonPushedValues(time);
        }

    }

    private void UpdateAnalogStickAndTriggerValues(double time)
    {
        var leftStick = mGamePadState.ThumbSticks.Left;
        var rightStick = mGamePadState.ThumbSticks.Right;

        mLeftStick.Update(leftStick, time);
        mRightStick.Update(rightStick, time);

        //if (AreShoulderAndTriggersFlipped)
        //{
        //    mFlippedLeftTrigger.Update((int)mGamePadState.Buttons.LeftShoulder);
        //    mFlippedRightTrigger.Update((int)mGamePadState.Buttons.RightShoulder);

        //}

        // Even if using Gamecube, record these values as they are used above in button maps
        mLeftTrigger.Update(mGamePadState.Triggers.Left, time);
        mRightTrigger.Update(mGamePadState.Triggers.Right, time);

        
    }

    private void UpdateLastButtonPushedValues(double currentTime)
    {
        // Set the last pushed and clear the ignored input
        // We need to check each button bit position, not iterate 0-30
        var buttonsToCheck = new[]
        {
            Buttons.DPadUp, Buttons.DPadDown, Buttons.DPadLeft, Buttons.DPadRight,
            Buttons.Start, Buttons.Back, Buttons.LeftStick, Buttons.RightStick,
            Buttons.LeftShoulder, Buttons.RightShoulder, Buttons.BigButton,
            Buttons.A, Buttons.B, Buttons.X, Buttons.Y,
            Buttons.LeftTrigger, Buttons.RightTrigger,
            Buttons.LeftThumbstickUp, Buttons.LeftThumbstickDown, Buttons.LeftThumbstickLeft, Buttons.LeftThumbstickRight,
            Buttons.RightThumbstickUp, Buttons.RightThumbstickDown, Buttons.RightThumbstickLeft, Buttons.RightThumbstickRight
        };

        foreach (var button in buttonsToCheck)
        {
            if (ButtonPushed(button))
            {
                mLastButtonPush[GetButtonIndex(button)] = currentTime;
            }
        }
    }

}
