using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Input;

public class AnalogButton
{
    float mPosition = 0;


    float mLastPosition = 0;

    bool lastIsDownDown = false;


    double mLastButtonPush;


    /// <summary>
    /// The current value of the AnalogButton, with ranges 0 - 1
    /// </summary>
    public float Position
    {
        get { return mPosition; }
    }

    internal float LastPosition
    {
        get { return mLastPosition; }
    }

    public bool IsDown
    {
        get
        {
            if (lastIsDownDown)
            {
                return mPosition > AnalogStick.DPadOffValue;
            }
            else
            {
                return mPosition > AnalogStick.DPadOnValue;
            }

        }
    }

    public bool WasJustPressed => !lastIsDownDown && IsDown;

    public bool WasJustReleased => lastIsDownDown && !IsDown;



    public void Update(float newPosition, double time)
    {
        mLastPosition = mPosition;
        lastIsDownDown = IsDown;

        //if (TimeManager.SecondDifference != 0)
        //{
        //    mVelocity = (newPosition - mPosition) / TimeManager.SecondDifference;
        //}
        mPosition = newPosition;

        if (IsDown && !lastIsDownDown)
        {
            mLastButtonPush = time;
        }
    }

    /// <summary>
    /// Clears all analog button state, resetting it to initial values.
    /// This resets both current and previous states to prevent spurious push/release events.
    /// </summary>
    public void Clear()
    {
        mPosition = 0;
        mLastPosition = 0;
        lastIsDownDown = false;
        mLastButtonPush = 0;
    }

}
