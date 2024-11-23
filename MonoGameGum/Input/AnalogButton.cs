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

}
