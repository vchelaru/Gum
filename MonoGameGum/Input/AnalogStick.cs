using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Input;

public class AnalogStick
{
    // The curren time, as of the last time Update was called
    double currentTime;

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

}
