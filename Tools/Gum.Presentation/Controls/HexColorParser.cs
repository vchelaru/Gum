using Gum.ColorPicking;

namespace Gum.Controls.DataUi
{
    /// <summary>
    /// The color row's hex field helpers, without the leading '#'. The parsing rules live in
    /// <see cref="HexColor"/>, which the color picker shares.
    /// </summary>
    public static class HexColorParser
    {
        /// <summary>
        /// Parses #RGB, #RRGGBB or #RRGGBBAA, with or without a leading '#', ignoring surrounding
        /// whitespace. Any alpha component is discarded; callers manage alpha separately.
        /// </summary>
        /// <returns>True if the value was a valid hex color; otherwise false.</returns>
        public static bool TryParse(string? hex, out byte r, out byte g, out byte b)
        {
            HexColorParseResult result = HexColor.Parse(hex);
            r = result.IsValid ? result.Red : (byte)0;
            g = result.IsValid ? result.Green : (byte)0;
            b = result.IsValid ? result.Blue : (byte)0;
            return result.IsValid;
        }

        /// <summary>
        /// Formats red, green, and blue components as a six-digit uppercase hex string
        /// (no leading '#'), matching the form <see cref="TryParse"/> displays and accepts.
        /// </summary>
        public static string ToHexRgb(byte r, byte g, byte b) => HexColor.Format(r, g, b).Substring(1);
    }
}
