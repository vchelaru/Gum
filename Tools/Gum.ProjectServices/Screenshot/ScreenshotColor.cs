using System;
using System.Globalization;

namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// An RGBA color for <see cref="ScreenshotRequest.BackgroundColor"/>, parsed from a hex string so
/// a screenshot can be rendered against an opaque backdrop instead of the default transparent one.
/// Transparent vs. opaque compositing look very different once viewed, which made raylib-vs-tool
/// screenshot comparisons misleading (#4172).
/// </summary>
public readonly struct ScreenshotColor
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public ScreenshotColor(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Parses a hex color string: 6-digit RRGGBB (alpha defaults to fully opaque) or 8-digit
    /// RRGGBBAA, with or without a leading '#'.
    /// </summary>
    public static bool TryParse(string? hex, out ScreenshotColor color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        string trimmed = hex.Trim();
        if (trimmed.StartsWith("#"))
        {
            trimmed = trimmed.Substring(1);
        }

        if (trimmed.Length != 6 && trimmed.Length != 8)
        {
            return false;
        }

        if (!TryParseComponent(trimmed, 0, out byte r) ||
            !TryParseComponent(trimmed, 2, out byte g) ||
            !TryParseComponent(trimmed, 4, out byte b))
        {
            return false;
        }

        byte a = 255;
        if (trimmed.Length == 8 && !TryParseComponent(trimmed, 6, out a))
        {
            return false;
        }

        color = new ScreenshotColor(r, g, b, a);
        return true;
    }

    private static bool TryParseComponent(string hex, int start, out byte value) =>
        byte.TryParse(hex.AsSpan(start, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
}
