using System;
using System.Diagnostics;

namespace RenderingLibrary.Graphics
{
    /// <summary>
    /// The one debug-only check every renderable uses for the floats layout writes into it (X, Y, Width, Height,
    /// Rotation), so each runtime rejects the same values the same way (#6477). The calls are compiled out unless
    /// FULL_DIAGNOSTICS is defined, so a release build pays nothing.
    /// </summary>
    public static class NonFiniteCheck
    {
        /// <summary>Throws an ArgumentException for NaN, positive infinity or negative infinity.</summary>
        [Conditional("FULL_DIAGNOSTICS")]
        public static void ThrowIfNotFinite(float value, string propertyName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentException("Invalid " + propertyName + ": " + value);
            }
        }

        /// <summary>As <see cref="ThrowIfNotFinite(float, string)"/>; null is allowed.</summary>
        [Conditional("FULL_DIAGNOSTICS")]
        public static void ThrowIfNotFinite(float? value, string propertyName)
        {
            if (value.HasValue && (float.IsNaN(value.Value) || float.IsInfinity(value.Value)))
            {
                throw new ArgumentException("Invalid " + propertyName + ": " + value.Value);
            }
        }
    }
}
