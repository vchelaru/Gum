using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Math
{
    public class MathFunctions
    {
        public static int RoundToInt(float floatToRound)
        {
            // System.Math.Round should give us a number very close to the decimal
            // Of course, it may give us something like 3.99999, which would become
            // 3 when converted to an int.  We want that to be 4, so we add a small amount
            // But...why .5 and not something smaller?  Well, we want to minimize error due
            // to floating point inaccuracy, so we add a value that is big enough to get us into
            // the next integer value even when the float is really large (has a lot of error).
            return (int)(System.Math.Round(floatToRound) + (System.Math.Sign(floatToRound) * .5f));

        }

        public static int RoundToInt(double doubleToRound)
        {
            // see the other RoundToInt for information on why we add .5
            return (int)(System.Math.Round(doubleToRound) + (System.Math.Sign(doubleToRound) * .5f));

        }
    }
}
