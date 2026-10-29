using System;
using System.Collections.Generic;
using System.Text;

namespace CrossRuntimeTests;

/// <summary>
/// The expected result of the shipped Outline shader on a nine-slice (issue #6984), shared by the
/// MonoGame, raylib and Skia tests. A test renders the same nine-slice twice, once plain and once
/// through the shader, and <see cref="Compare"/> checks the second against the first: content pixels
/// unchanged, every empty pixel within reach of content (a square, diagonals included) the outline
/// color, everything else still empty. Because the plain render is the oracle, the check does not
/// depend on how a backend lays the nine slices out, only on the outline following their alpha.
/// </summary>
public static class NineSliceOutlineOracle
{
    public const int TextureSize = 12;

    /// <summary>
    /// A 12x12 "window frame" texture (a third of it, 4 texels, is each border). Fully opaque except
    /// the three outermost texels of every corner, which are transparent like a rounded corner. The
    /// color varies per texel so a slice drawn in the wrong place would show.
    /// </summary>
    public static (byte R, byte G, byte B, byte A) Texel(int x, int y)
    {
        int distanceX = Math.Min(x, TextureSize - 1 - x);
        int distanceY = Math.Min(y, TextureSize - 1 - y);
        if (distanceX + distanceY < 2)
        {
            return (0, 0, 0, 0);
        }
        return ((byte)(60 + x * 15), (byte)(60 + y * 15), (byte)(100 + ((x + y) % 2) * 50), 255);
    }

    public static int CountContent(IReadOnlyList<(byte R, byte G, byte B, byte A)> pixels)
    {
        int count = 0;
        foreach ((byte R, byte G, byte B, byte A) pixel in pixels)
        {
            if (pixel.A > 0)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Returns a description of the first few pixels where <paramref name="outlined"/> is not what the
    /// shader should have produced from <paramref name="plain"/>, or an empty string when all match.
    /// Both are row-major, <paramref name="width"/> by <paramref name="height"/>, top row first.
    /// </summary>
    public static string Compare(
        IReadOnlyList<(byte R, byte G, byte B, byte A)> plain,
        IReadOnlyList<(byte R, byte G, byte B, byte A)> outlined,
        int width, int height, int reach, (byte R, byte G, byte B) outlineColor, int tolerance = 3)
    {
        StringBuilder problems = new();
        int problemCount = 0;

        bool IsContent(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && plain[(y * width) + x].A > 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (byte R, byte G, byte B, byte A) actual = outlined[(y * width) + x];
                string? problem = null;

                if (IsContent(x, y))
                {
                    (byte R, byte G, byte B, byte A) expected = plain[(y * width) + x];
                    if (Math.Abs(actual.R - expected.R) > tolerance || Math.Abs(actual.G - expected.G) > tolerance ||
                        Math.Abs(actual.B - expected.B) > tolerance || Math.Abs(actual.A - expected.A) > tolerance)
                    {
                        problem = $"content changed, expected {expected} got {actual}";
                    }
                }
                else
                {
                    bool nearContent = false;
                    for (int offsetY = -reach; offsetY <= reach && !nearContent; offsetY++)
                    {
                        for (int offsetX = -reach; offsetX <= reach && !nearContent; offsetX++)
                        {
                            nearContent = IsContent(x + offsetX, y + offsetY);
                        }
                    }

                    if (nearContent)
                    {
                        if (actual.A < 255 - tolerance || Math.Abs(actual.R - outlineColor.R) > tolerance ||
                            Math.Abs(actual.G - outlineColor.G) > tolerance || Math.Abs(actual.B - outlineColor.B) > tolerance)
                        {
                            problem = $"should be the outline {outlineColor}, got {actual}";
                        }
                    }
                    else if (actual.A > tolerance)
                    {
                        problem = $"should be empty, got {actual}";
                    }
                }

                if (problem != null)
                {
                    problemCount++;
                    if (problemCount <= 8)
                    {
                        problems.AppendLine($"({x},{y}): {problem}");
                    }
                }
            }
        }

        return problemCount == 0 ? "" : $"{problemCount} wrong pixels, first ones:\n{problems}";
    }
}
