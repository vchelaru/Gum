using System;

namespace Gum.ColorPicking;

/// <summary>HSV and RGB conversion for the color picker. Hue is in degrees, saturation and value in 0 to 1.</summary>
public static class HsvRgb
{
    /// <summary>Converts HSV to RGB. The hue wraps, so -120 is 240 and 480 is 120.</summary>
    public static (byte Red, byte Green, byte Blue) ToRgb(double hue, double saturation, double value)
    {
        double wrapped = ((hue % 360) + 360) % 360;
        double saturationClamped = System.Math.Clamp(saturation, 0, 1);
        double valueClamped = System.Math.Clamp(value, 0, 1);

        double chroma = valueClamped * saturationClamped;
        double sector = wrapped / 60;
        double secondLargest = chroma * (1 - System.Math.Abs(sector % 2 - 1));
        double offset = valueClamped - chroma;

        (double r, double g, double b) = (int)sector switch
        {
            0 => (chroma, secondLargest, 0d),
            1 => (secondLargest, chroma, 0d),
            2 => (0d, chroma, secondLargest),
            3 => (0d, secondLargest, chroma),
            4 => (secondLargest, 0d, chroma),
            _ => (chroma, 0d, secondLargest),
        };
        return (ToByte(r + offset), ToByte(g + offset), ToByte(b + offset));
    }

    /// <summary>Converts RGB to HSV. Gray and black report a hue of 0.</summary>
    public static (double H, double S, double V) ToHsv(byte red, byte green, byte blue)
    {
        double r = red / 255d;
        double g = green / 255d;
        double b = blue / 255d;
        double max = System.Math.Max(r, System.Math.Max(g, b));
        double delta = max - System.Math.Min(r, System.Math.Min(g, b));

        double hue = 0;
        if (delta > 0)
        {
            if (max == r)
            {
                hue = ((g - b) / delta + 6) % 6;
            }
            else if (max == g)
            {
                hue = (b - r) / delta + 2;
            }
            else
            {
                hue = (r - g) / delta + 4;
            }
            hue *= 60;
        }
        return (hue, max == 0 ? 0 : delta / max, max);
    }

    private static byte ToByte(double channel) => (byte)System.Math.Round(channel * 255);
}
