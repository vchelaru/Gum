using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Input;

public enum DeadzoneType
{
    Radial = 0,
    //BoundingBox = 1, // Not currently supported
    Cross
}

public enum DeadzoneInterpolationType
{
    /// <summary>
    /// No interpolation is performed. Values less than the deadzone are set to 0.
    /// </summary>
    Instant,
    /// <summary>
    /// Linear interpolation is performed for values greater than the deadzone. 
    /// </summary>
    Linear,
    /// <summary>
    /// Quadratic (in) interpolation is performed for values greater than the deadzone. This increases
    /// accuracy at lower values (values closer to the deadzone) so that small movements are easier to perform.
    /// </summary>
    Quadratic
}


public class AnalogStick
{
    // The curren time, as of the last time Update was called
    double currentTime;

    Vector2 mRawPosition;
    Vector2 mPosition;

    double mTimeAfterPush = DefaultTimeAfterPush;
    double mTimeBetweenRepeating = DefaultTimeBetweenRepeating;

    /// <summary>
    /// The DPadOnValue and DPadOffValue
    /// values are used to simulate D-Pad control
    /// with the analog stick.  When the user is above
    /// the absolute value of the mDPadOnValue then it is
    /// as if the DPad is held down.  To release the DPad the
    /// value must come under the off value.  If there was only
    /// one value then the user could hold the stick near the threshold
    /// and get rapid on/off values due to the inaccuracy of the analog stick. 
    /// Therefore the DPadOnValue must be larger than DPadOffValue. The spread between
    /// these values should be large enough to prevent rapid on/off values, but small enough
    /// so the user can still release the analog stick and have it feel like a DPad.
    /// </summary>
    internal const float DPadOnValue = .550f;
    internal const float DPadOffValue = .450f;

    /// <summary>
    /// Number of seconds to wait after a push before repeating.
    /// </summary>
    public const double DefaultTimeAfterPush = .35;


    /// <summary>
    /// Number of seconds between repeating.
    /// </summary>
    public const double DefaultTimeBetweenRepeating = .12;

    bool[] mLastDPadDown = new bool[4];
    bool[] mCurrentDPadDown = new bool[4];

    /* The following would be useful to add
     * float mAngularVelocity;
     * float mDeadzone;
     */

    // Start this at -1 instead of 0, otherwise the first frame will return "true" because
    // the last push on time 0 will match the TimeManager.CurrentTime
    double[] mLastDPadPush = new double[4] { -1, -1, -1, -1 };
    double[] mLastDPadRepeatRate = new double[4] { -1, -1, -1, -1 };

    public bool AsDPadDown(DPadDirection direction)
    {
        switch (direction)
        {
            case DPadDirection.Left:

                if (mLastDPadDown[(int)DPadDirection.Left])
                {
                    return mPosition.X < -DPadOffValue;
                }
                else
                {
                    return mPosition.X < -DPadOnValue;
                }

            //break;

            case DPadDirection.Right:

                if (mLastDPadDown[(int)DPadDirection.Right])
                {
                    return mPosition.X > DPadOffValue;
                }
                else
                {
                    return mPosition.X > DPadOnValue;
                }

            //break;

            case DPadDirection.Up:

                if (mLastDPadDown[(int)DPadDirection.Up])
                {
                    return mPosition.Y > DPadOffValue;
                }
                else
                {
                    return mPosition.Y > DPadOnValue;
                }

            //break;

            case DPadDirection.Down:

                if (mLastDPadDown[(int)DPadDirection.Down])
                {
                    return mPosition.Y < -DPadOffValue;
                }
                else
                {
                    return mPosition.Y < -DPadOnValue;
                }

            //break;
            default:

                return false;
                //break;
        }
    }
    public float Deadzone { get; set; } = .1f;
    public DeadzoneType DeadzoneType { get; set; } = DeadzoneType.Radial;// matches the behavior prior to May 22, 2022 when this property was introduced


    /// <summary>
    /// Whether Position values should be limited so that Position.Length is less than or equal to 1.
    /// This is recommended for top-down games.
    /// </summary>
    public bool IsMaxPositionNormalized { get; set; } = false;

    /// <summary>
    /// The type of interpolation to perform up to the max value when outside of the deadzone value.
    /// </summary>
    public DeadzoneInterpolationType DeadzoneInterpolation { get; set; }


    /// <summary>
    /// Returns whether the user has pressed a particular direction, returning true only once per push similar to a d-pad. This can be used for UI navigation.
    /// </summary>
    /// <seealso cref="AsDPadDown"/>
    /// <param name="direction">The direction to test</param>
    /// <returns>Whether the direction was pressed.</returns>
    public bool AsDPadPushed(DPadDirection direction)
    {
        // If the last was not down and this one is, then report a push.
        return mLastDPadDown[(int)direction] == false && AsDPadDown(direction);
    }

    public bool AsDPadPushedRepeatRate(DPadDirection direction)
    {
        // Ignoring is performed inside this call.
        return AsDPadPushedRepeatRate(direction, mTimeAfterPush, mTimeBetweenRepeating);
    }


    public bool AsDPadPushedRepeatRate(DPadDirection direction, double timeAfterPush, double timeBetweenRepeating)
    {
        if (AsDPadPushed(direction))
            return true;

        // If this method is called multiple times per frame this line
        // of code guarantees that the user will get true every time until
        // the next TimeManager.Update (next frame).
        // The very first frame of FRB would have CurrentTime == 0. 
        // The repeat cannot happen on the first frame, so we check for that:
        bool repeatedThisFrame = currentTime > 0 && mLastDPadPush[(int)direction] == currentTime;

        if (repeatedThisFrame ||
            (
            AsDPadDown(direction) &&
            currentTime - mLastDPadPush[(int)direction] > timeAfterPush &&
            currentTime - mLastDPadRepeatRate[(int)direction] > timeBetweenRepeating)
            )
        {
            mLastDPadRepeatRate[(int)direction] = currentTime;
            return true;
        }

        return false;
    }

    public void Update(Vector2 newPosition, double time)
    {
        currentTime = time;
        mRawPosition = newPosition;
        if (Deadzone > 0)
        {
            switch (DeadzoneType)
            {
                case DeadzoneType.Radial:
                    newPosition = GetRadialDeadzoneValue(newPosition);
                    break;
                case DeadzoneType.Cross:
                    newPosition = GetCrossDeadzoneValue(newPosition);
                    break;
            }

        }

        if (IsMaxPositionNormalized && newPosition.LengthSquared() > 1)
        {
            newPosition.Normalize();
        }

        mLastDPadDown[(int)DPadDirection.Up] = AsDPadDown(DPadDirection.Up);
        mLastDPadDown[(int)DPadDirection.Down] = AsDPadDown(DPadDirection.Down);
        mLastDPadDown[(int)DPadDirection.Left] = AsDPadDown(DPadDirection.Left);
        mLastDPadDown[(int)DPadDirection.Right] = AsDPadDown(DPadDirection.Right);

        //mVelocity = Vector2.Multiply((newPosition - mPosition), 1 / TimeManager.SecondDifference);
        mPosition = newPosition;

        UpdateAccordingToPosition();

        for (int i = 0; i < 4; i++)
        {
            if (AsDPadPushed((DPadDirection)i))
            {
                mLastDPadPush[i] = currentTime;
            }
        }

        // todo...
        //leftAsButton.Update(-System.Math.Min(0, mPosition.X));
        //rightAsButton.Update(System.Math.Max(0, mPosition.X));

        //downAsButton.Update(-System.Math.Min(0, mPosition.Y));
        //upAsButton.Update(System.Math.Max(0, mPosition.Y));
    }

    /// <summary>
    /// Clears all analog stick state, resetting it to initial values.
    /// This resets both current and previous states to prevent spurious push/release events.
    /// </summary>
    public void Clear()
    {
        currentTime = 0;
        mRawPosition = Vector2.Zero;
        mPosition = Vector2.Zero;
        mTimeAfterPush = DefaultTimeAfterPush;
        mTimeBetweenRepeating = DefaultTimeBetweenRepeating;

        Array.Clear(mLastDPadDown, 0, mLastDPadDown.Length);
        Array.Clear(mCurrentDPadDown, 0, mCurrentDPadDown.Length);

        for (int i = 0; i < mLastDPadPush.Length; i++)
        {
            mLastDPadPush[i] = -1;
            mLastDPadRepeatRate[i] = -1;
        }
    }

    private void UpdateAccordingToPosition()
    {
        // Atan2 of (0,0) returns 0
        //mAngle = System.Math.Atan2(mPosition.Y, mPosition.X);
        //mAngle = MathFunctions.RegulateAngle(mAngle);
        //mMagnitude = System.Math.Min(1, mPosition.Length());
    }

    Vector2 GetRadialDeadzoneValue(Vector2 originalValue)
    {
        var deadzoneSquared = Deadzone * Deadzone;

        var originalValueLengthSquared =
            (originalValue.X * originalValue.X) +
            (originalValue.Y * originalValue.Y);

        if (originalValueLengthSquared < deadzoneSquared)
        {
            return Vector2.Zero;
        }
        else
        {
            switch (DeadzoneInterpolation)
            {
                case DeadzoneInterpolationType.Instant:
                    return originalValue;
                case DeadzoneInterpolationType.Linear:
                    {
                        var range = (1 - Deadzone);
                        var distanceBeyondDeadzone = originalValue.Length() - Deadzone;
                        return NormalizedOrRight(originalValue) * (distanceBeyondDeadzone / range);
                    }
                case DeadzoneInterpolationType.Quadratic:
                    {
                        var range = (1 - Deadzone);
                        var distanceBeyondDeadzone = originalValue.Length() - Deadzone;
                        var ratio = (distanceBeyondDeadzone / range);

                        var modifiedRatio = EaseIn(ratio, 0, 1, 1);
                        return NormalizedOrRight(originalValue) * modifiedRatio;
                    }

            }
            return originalValue;
        }
    }


    static Vector2 NormalizedOrRight(Vector2 vector)
    {
        if (vector.X != 0 || vector.Y != 0)
        {
            vector.Normalize();
            return vector;
        }
        else
        {
            return new Vector2(1, 0);
        }
    }

    static float EaseIn(float timeElapsed, float startingValue, float amountToAdd, float durationInSeconds)
    {
        return amountToAdd * (timeElapsed /= durationInSeconds) * timeElapsed + startingValue;
    }

    Vector2 GetCrossDeadzoneValue(Vector2 originalValue)
    {
        if (originalValue.X < Deadzone && originalValue.X > -Deadzone)
        {
            originalValue.X = 0;
        }
        else
        {
            switch (DeadzoneInterpolation)
            {
                case DeadzoneInterpolationType.Instant:
                    // return originalValue;
                    // do nothing
                    break;
                case DeadzoneInterpolationType.Linear:
                    {
                        var range = (1 - Deadzone);
                        var distanceBeyondDeadzone = System.Math.Abs(originalValue.X) - Deadzone;
                        originalValue.X = System.Math.Sign(originalValue.X) * (float)(distanceBeyondDeadzone / range);
                        break;
                    }
                case DeadzoneInterpolationType.Quadratic:
                    {
                        var range = (1 - Deadzone);
                        var distanceBeyondDeadzone = System.Math.Abs(originalValue.X) - Deadzone;
                        var ratio = distanceBeyondDeadzone / range;
                        var modifiedRatio = EaseIn(ratio, 0, 1, 1);
                        originalValue.X = System.Math.Sign(originalValue.X) * (float)modifiedRatio;
                        break;
                    }
            }
        }


        if (originalValue.Y < Deadzone && originalValue.Y > -Deadzone)
        {
            originalValue.Y = 0;
        }
        else
        {
            switch (DeadzoneInterpolation)
            {
                case DeadzoneInterpolationType.Instant:
                    break;
                case DeadzoneInterpolationType.Linear:
                    {
                        var range = (1 - Deadzone);
                        var distanceBeyondDeadzone = System.Math.Abs(originalValue.Y) - Deadzone;
                        originalValue.Y = System.Math.Sign(originalValue.Y) * (float)(distanceBeyondDeadzone / range);
                        break;
                    }
                case DeadzoneInterpolationType.Quadratic:
                    {
                        var range = 1 - Deadzone;
                        var distanceBeyondDeadzone = System.Math.Abs(originalValue.Y) - Deadzone;
                        var ratio = distanceBeyondDeadzone / range;
                        var modifiedRatio = EaseIn(ratio, 0, 1, 1);
                        originalValue.Y = System.Math.Sign(originalValue.Y) * (float)modifiedRatio;
                        break;
                    }
            }
        }
        return originalValue;
    }

}
