using System.Globalization;

namespace Gum.Controls.DataUi
{
    /// <summary>
    /// Pure parsing/formatting helpers for the hex color text box on <see cref="ColorDisplay"/>.
    /// Extracted from the WPF control so the logic is unit-testable.
    /// </summary>
    public static class HexColorParser
    {
        /// <summary>
        /// Parses a hex color string into its red, green, and blue byte components.
        /// Accepts 6-digit (RRGGBB) and 8-digit (RRGGBBAA) hex, with or without a leading '#',
        /// and ignores surrounding whitespace. Any alpha component (the AA in 8-digit form) is
        /// parsed but discarded — callers manage alpha separately.
        /// </summary>
        /// <returns>True if the value was a valid 6- or 8-digit hex color; otherwise false.</returns>
        public static bool TryParse(string? hex, out byte r, out byte g, out byte b)
        {
            r = 0;
            g = 0;
            b = 0;

            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            string trimmed = hex.Trim();
            if (trimmed.StartsWith("#"))
            {
                trimmed = trimmed.Substring(1);
            }

            // Only RRGGBB and RRGGBBAA are supported; alpha is parsed below but discarded.
            if (trimmed.Length != 6 && trimmed.Length != 8)
            {
                return false;
            }

            if (!TryParseComponent(trimmed.Substring(0, 2), out byte parsedR) ||
                !TryParseComponent(trimmed.Substring(2, 2), out byte parsedG) ||
                !TryParseComponent(trimmed.Substring(4, 2), out byte parsedB))
            {
                return false;
            }

            r = parsedR;
            g = parsedG;
            b = parsedB;
            return true;
        }

        private static bool TryParseComponent(string twoDigitHex, out byte value)
        {
            // AllowHexSpecifier rejects signs and surrounding whitespace, so each two-character
            // component is validated strictly here.
            return byte.TryParse(twoDigitHex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Formats red, green, and blue components as a six-digit uppercase hex string
        /// (no leading '#'), matching the form <see cref="TryParse"/> displays and accepts.
        /// </summary>
        public static string ToHexRgb(byte r, byte g, byte b)
        {
            return $"{r:X2}{g:X2}{b:X2}";
        }
    }
}
