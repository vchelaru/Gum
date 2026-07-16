using SkiaSharp;

namespace SkiaGum.Helpers;

/// <summary>
/// Extension and helper methods for working with <see cref="SKColor"/> values on the Skia backend.
/// Mirrors the surface of <c>RaylibGum.Helpers.ColorExtensions</c> and the XNA-side
/// <c>ToolsUtilitiesStandard.Helpers.ColorExtensions</c> so shared runtime code can call the same
/// members on any backend.
/// </summary>
public static class ColorExtensions
{
    /// <summary>Gets an opaque white color.</summary>
    public static SKColor White => SKColors.White;

    /// <summary>
    /// Returns a copy of the color with its alpha channel replaced by the specified value.
    /// </summary>
    public static SKColor WithAlpha(this SKColor color, byte value)
    {
        return new SKColor(color.Red, color.Green, color.Blue, value);
    }

    /// <summary>
    /// Returns a copy of the color with its red channel replaced by the specified value.
    /// </summary>
    public static SKColor WithRed(this SKColor color, byte value)
    {
        return new SKColor(value, color.Green, color.Blue, color.Alpha);
    }

    /// <summary>
    /// Returns a copy of the color with its green channel replaced by the specified value.
    /// </summary>
    public static SKColor WithGreen(this SKColor color, byte value)
    {
        return new SKColor(color.Red, value, color.Blue, color.Alpha);
    }

    /// <summary>
    /// Returns a copy of the color with its blue channel replaced by the specified value.
    /// </summary>
    public static SKColor WithBlue(this SKColor color, byte value)
    {
        return new SKColor(color.Red, color.Green, value, color.Alpha);
    }

    /// <summary>
    /// Converts a <see cref="System.Drawing.Color"/> to its Skia equivalent. Mirror of
    /// <c>RaylibGum.Helpers.ColorExtensions.ToRaylib</c> for the Skia backend.
    /// </summary>
    public static SKColor ToSkia(this System.Drawing.Color value)
    {
        return new SKColor(value.R, value.G, value.B, value.A);
    }
}
