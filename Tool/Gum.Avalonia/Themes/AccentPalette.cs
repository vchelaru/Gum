using System;
using Avalonia.Media;

namespace Gum.Avalonia.Themes;

/// <summary>
/// The colors the WPF <c>ThemingService</c> derives from the accent when it switches: a lighter
/// and a darker tint, and the foreground that reads on the darker one (its default button text).
/// </summary>
public static class AccentPalette
{
    // MaterialDesign's ColorAssist thresholds: lightness steps of 10 percent, and black text once
    // the relative luminance passes 0.179.
    private const double LightnessStep = 0.10;
    private const double BlackTextLuminance = 0.179;

    /// <summary>A tint of <paramref name="color"/> ten percent lighter.</summary>
    public static Color Lighten(Color color) => ShiftLightness(color, LightnessStep);

    /// <summary>A shade of <paramref name="color"/> ten percent darker.</summary>
    public static Color Darken(Color color) => ShiftLightness(color, -LightnessStep);

    /// <summary>Black or white, whichever reads on <paramref name="background"/>.</summary>
    public static Color ContrastingForeground(Color background) =>
        RelativeLuminance(background) > BlackTextLuminance ? Colors.Black : Colors.White;

    private static Color ShiftLightness(Color color, double amount)
    {
        HslColor hsl = color.ToHsl();
        double lightness = Math.Clamp(hsl.L + amount, 0, 1);
        return new HslColor(hsl.A, hsl.H, hsl.S, lightness).ToRgb();
    }

    private static double RelativeLuminance(Color color)
    {
        double r = Linear(color.R);
        double g = Linear(color.G);
        double b = Linear(color.B);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;

        static double Linear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
    }
}
