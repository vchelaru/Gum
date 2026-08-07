using System;

namespace Gum.Wireframe.Editors;

/// <summary>
/// Floor-based snapping to the nearest grid line at or below a value, with correct handling of
/// negative coordinates (which C# integer truncation gets wrong).
/// </summary>
public static class GridSnapper
{
    /// <summary>
    /// Returns <paramref name="value"/> snapped down to the nearest multiple of <paramref name="gridSize"/>.
    /// Returns <paramref name="value"/> unchanged if <paramref name="gridSize"/> is not positive.
    /// </summary>
    public static float Snap(float value, float gridSize)
    {
        if (gridSize <= 0)
        {
            return value;
        }

        return (float)Math.Floor(value / gridSize) * gridSize;
    }
}
