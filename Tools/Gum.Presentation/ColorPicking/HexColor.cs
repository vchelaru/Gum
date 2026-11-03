using System;
using System.Globalization;

namespace Gum.ColorPicking;

/// <summary>The outcome of parsing a hex color the user typed or pasted.</summary>
/// <param name="IsValid">False when the text is not #RGB, #RRGGBB or #RRGGBBAA.</param>
/// <param name="Red">The red channel; meaningful only when <paramref name="IsValid"/>.</param>
/// <param name="Green">The green channel; meaningful only when <paramref name="IsValid"/>.</param>
/// <param name="Blue">The blue channel; meaningful only when <paramref name="IsValid"/>.</param>
/// <param name="AlphaIgnored">True when the text carried alpha digits (#RRGGBBAA), which the picker does not use.</param>
public readonly record struct HexColorParseResult(bool IsValid, byte Red, byte Green, byte Blue, bool AlphaIgnored);

/// <summary>Parses and formats the color picker's hex field. Alpha is never part of the picked color.</summary>
public static class HexColor
{
    /// <summary>
    /// Parses #RGB, #RRGGBB or #RRGGBBAA, with or without the leading #, in any case, ignoring
    /// surrounding whitespace. The alpha digits of #RRGGBBAA are dropped and reported.
    /// </summary>
    public static HexColorParseResult Parse(string? text)
    {
        string digits = (text ?? "").Trim();
        if (digits.StartsWith('#'))
        {
            digits = digits.Substring(1);
        }

        foreach (char character in digits)
        {
            if (!Uri.IsHexDigit(character))
            {
                return default;
            }
        }

        if (digits.Length == 3)
        {
            digits = string.Concat(digits[0], digits[0], digits[1], digits[1], digits[2], digits[2]);
        }
        else if (digits.Length != 6 && digits.Length != 8)
        {
            return default;
        }

        byte red = ParseByte(digits, 0);
        byte green = ParseByte(digits, 2);
        byte blue = ParseByte(digits, 4);
        return new HexColorParseResult(true, red, green, blue, AlphaIgnored: digits.Length == 8);
    }

    /// <summary>
    /// True when the text is not a color yet but more hex digits could make it one (a "#1234" on its
    /// way to "#123456"), so typing it should not be flagged as an error. Text that is a color, has a
    /// character that is not a hex digit, or is longer than eight digits is false.
    /// </summary>
    public static bool IsIncomplete(string? text)
    {
        string digits = (text ?? "").Trim();
        if (digits.StartsWith('#'))
        {
            digits = digits.Substring(1);
        }

        foreach (char character in digits)
        {
            if (!Uri.IsHexDigit(character))
            {
                return false;
            }
        }
        return digits.Length < 8 && !Parse(text).IsValid;
    }

    /// <summary>Formats a color as #RRGGBB in uppercase, without alpha.</summary>
    public static string Format(byte red, byte green, byte blue) => $"#{red:X2}{green:X2}{blue:X2}";

    private static byte ParseByte(string digits, int start) =>
        byte.Parse(digits.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
