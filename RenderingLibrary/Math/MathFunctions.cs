using System;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Point = System.Drawing.Point;
using Matrix = System.Numerics.Matrix4x4;

namespace RenderingLibrary.Math
{
    public class MathFunctions
    {

        public static bool IsPowerOfTwo(int numberToCheck)
        {
            return numberToCheck == 1 ||
                numberToCheck == 2 ||
                ((numberToCheck & (numberToCheck - 1)) == 0);
        }

        /// <summary>
        /// Rounds the float to the nearest int, using MidpointRounding.AwayFromZero.
        /// </summary>
        /// <param name="floatToRound">The float to round.</param>
        /// <returns>The rounded int.</returns>
        /// <exception cref="ArgumentException">Thrown if the float is NaN (not a number)</exception>
        public static int RoundToInt(float floatToRound)
        {
#if FULL_DIAGNOSTICS
            if (float.IsNaN(floatToRound))
            {
                throw new ArgumentException("floatToRound cannot be NaN");
            }
#endif
            // System.Math.Round should give us a number very close to the decimal
            // Of course, it may give us something like 3.99999, which would become
            // 3 when converted to an int.  We want that to be 4, so we add a small amount
            // But...why .5 and not something smaller?  Well, we want to minimize error due
            // to floating point inaccuracy, so we add a value that is big enough to get us into
            // the next integer value even when the float is really large (has a lot of error).
            // Update March 6, 2024
            // Perform a MidpointRounding.AwayFromZero
            // so that rounding 0.5 always goes in the same
            // direction. If we don't do this, then rendering
            // Sprites can sometimes be off by 1 pixel.
            return (int)(System.Math.Round(floatToRound, MidpointRounding.AwayFromZero) + (System.Math.Sign(floatToRound) * .5f));

        }

        public static int RoundToInt(double doubleToRound)
        {
            // see the other RoundToInt for information on why we add .5, and why we use MidpointRounding.AwayFromZero.
            return (int)(System.Math.Round(doubleToRound, MidpointRounding.AwayFromZero) + (System.Math.Sign(doubleToRound) * .5f));

        }

        public static int RoundToInt(decimal decimalToRound)
        {
            // see the other RoundToInt for information on why we add .5, and why we use MidpointRounding.AwayFromZero.
            return (int)(System.Math.Round(decimalToRound, MidpointRounding.AwayFromZero) + (System.Math.Sign(decimalToRound) * .5m));
        }

        /// <summary>
        /// Rotates a Point around another Point by a given number of radians.
        /// </summary>
        /// <param name="basePoint">Point to rotate around.</param>
        /// <param name="pointToRotate">Point to rotate (changes position).</param>
        /// <param name="radiansToChangeBy">Radians to rotate by.</param>
        public static void RotatePointAroundPoint(Point basePoint, ref Point pointToRotate, float radiansToChangeBy)
        {
            double xDistance = pointToRotate.X - basePoint.X;
            double yDistance = pointToRotate.Y - basePoint.Y;
            if (xDistance == 0 && yDistance == 0)
                return;

            double distance = xDistance * xDistance + yDistance * yDistance;
            distance = (float)System.Math.Pow(distance, .5);

            double angle = System.Math.Atan(yDistance / xDistance);
            if (xDistance < 0.0f) angle += (float)System.Math.PI;
            angle += radiansToChangeBy;

            pointToRotate.X = (int)(System.Math.Cos(angle) * distance + basePoint.X);
            pointToRotate.Y = (int)(System.Math.Sin(angle) * distance + basePoint.Y);

        }

        public static void RotatePointAroundPoint(Vector3 basePoint, ref Vector3 pointToRotate, float radiansToChangeBy)
        {
            double xDistance = pointToRotate.X - basePoint.X;
            double yDistance = pointToRotate.Y - basePoint.Y;
            if (xDistance == 0 && yDistance == 0)
                return;

            double distance = xDistance * xDistance + yDistance * yDistance;
            distance = (float)System.Math.Pow(distance, .5);

            double angle = System.Math.Atan2(yDistance, xDistance);
            angle += radiansToChangeBy;

            pointToRotate.X = (float)(System.Math.Cos(angle) * distance + basePoint.X);
            pointToRotate.Y = (float)(System.Math.Sin(angle) * distance + basePoint.Y);

        }

        public static void RotatePointAroundPoint(Vector2 basePoint, ref Vector2 pointToRotate, float radiansToChangeBy)
        {
            double xDistance = pointToRotate.X - basePoint.X;
            double yDistance = pointToRotate.Y - basePoint.Y;
            if (xDistance == 0 && yDistance == 0)
                return;

            double distance = xDistance * xDistance + yDistance * yDistance;
            distance = (float)System.Math.Pow(distance, .5);

            double angle = System.Math.Atan2(yDistance, xDistance);
            angle += radiansToChangeBy;

            pointToRotate.X = (float)(System.Math.Cos(angle) * distance + basePoint.X);
            pointToRotate.Y = (float)(System.Math.Sin(angle) * distance + basePoint.Y);

        }

        public static float RoundFloat(float valueToRound, float multipleOf)
        {
            return ((int)(System.Math.Sign(valueToRound) * .5f + valueToRound / multipleOf)) * multipleOf;
        }

        public static double RoundDouble(double valueToRound, double multipleOf)
        {
            return ((int)(System.Math.Sign(valueToRound) * .5 + valueToRound / multipleOf)) * multipleOf;
        }

        public static double RoundDouble(double valueToRound, double multipleOf, double seed)
        {
            valueToRound -= seed;

            return seed + ((int)(System.Math.Sign(valueToRound) * .5f + valueToRound / multipleOf)) * multipleOf;
        }

        public static decimal RoundDecimal(decimal valueToRound, decimal multipleOf)
        {
            return ((int)(System.Math.Sign(valueToRound) * .5m + valueToRound / multipleOf)) * multipleOf;
        }

        /// <summary>
        /// Rotates (and modifies) the argument vector2 by the argument radians, where a positive value is clockwise.
        /// </summary>
        /// <param name="vector2">The vector to rotate.</param>
        /// <param name="radians">The radians to rotate counterclockwise.</param>
        public static void RotateVector(ref Vector2 vector2, float radians)
        {
            if(vector2.X == 0 && vector2.Y == 0)
            {
                // do nothing
            }
            else
            {
                var angle = System.Math.Atan2(vector2.Y, vector2.X);
                var length = vector2.Length();
                angle += radians;

                vector2.X = length * (float)System.Math.Cos(angle);
                vector2.Y = length * (float)System.Math.Sin(angle);
            }
        }

        public static double Loop(double value, double loopPeriod, out bool didLoop)
        {
            didLoop = false;
            if (loopPeriod == 0)
            {
                return 0;
            }

            int numberOfTimesIn = (int)(value / loopPeriod);
            if (value < 0)
            {
                didLoop = true;
                return value + (1 + numberOfTimesIn) * loopPeriod;
            }
            else
            {
                didLoop = numberOfTimesIn > 0;
                return value - numberOfTimesIn * loopPeriod;
            }
        }
    }
}
