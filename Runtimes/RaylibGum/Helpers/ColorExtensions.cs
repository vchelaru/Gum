using Raylib_cs;

namespace RaylibGum.Helpers;

/// <summary>
/// Extension and helper methods for working with <see cref="Color"/> values on the Raylib backend.
/// Mirrors the surface of the XNA-side ToolsUtilitiesStandard.Helpers.ColorExtensions so shared
/// runtime code can call the same members on either backend.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Gets an opaque white color.
    /// </summary>
    public static Color White => Color.White;

    /// <summary>
    /// Returns a copy of the color with its alpha channel replaced by the specified value.
    /// </summary>
    public static Color WithAlpha(this Color color, byte value)
    {
        return new Color(color.R, color.G, color.B, value);
    }

    /// <summary>
    /// Returns a copy of the color with its red channel replaced by the specified value.
    /// </summary>
    public static Color WithRed(this Color color, byte value)
    {
        return new Color(value, color.G, color.B, color.A);
    }

    /// <summary>
    /// Returns a copy of the color with its green channel replaced by the specified value.
    /// </summary>
    public static Color WithGreen(this Color color, byte value)
    {
        return new Color(color.R, value, color.B, color.A);
    }

    /// <summary>
    /// Returns a copy of the color with its blue channel replaced by the specified value.
    /// </summary>
    public static Color WithBlue(this Color color, byte value)
    {
        return new Color(color.R, color.G, value, color.A);
    }

    /// <summary>
    /// Converts a <see cref="System.Drawing.Color"/> to its Raylib equivalent. Mirror of
    /// <c>ToolsUtilitiesStandard.Helpers.ColorExtensions.ToXNA</c> for the Raylib backend.
    /// </summary>
    public static Color ToRaylib(this System.Drawing.Color value)
    {
        return new Color(value.R, value.G, value.B, value.A);
    }

    // Identity overloads of the container/user color round-trip helpers added in #2757 for
    // shared runtime files (e.g. PolygonRuntime). On Raylib the contained renderable's color
    // type matches the user-facing Color, so no conversion is needed — but exposing the same
    // method names lets the unified runtime call them without #if-gating.
    public static Color ToUserColor(this Color color) => color;
    public static Color ToContainerColor(this Color color) => color;
}
