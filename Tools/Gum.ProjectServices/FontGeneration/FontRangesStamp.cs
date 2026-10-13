using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gum.ProjectServices.FontGeneration;

/// <summary>
/// Records the character ranges a font was baked with as one extra line at the end of its text
/// <c>.fnt</c> (<c>gum ranges="32-126,160-255"</c>), so a later pass can tell that the project's
/// ranges changed since the file was written.
/// </summary>
/// <remarks>
/// The glyphs present in the file can't answer that: a font that lacks a requested glyph just omits
/// it, so the ids in the file are not the requested ranges. The line is invisible to
/// <c>ParsedFontFile</c>, which skips tags it doesn't know; KernSmith already ends its own output
/// with a <c>kernsmith</c> line the same way. A font with no line (baked before this existed, or
/// written by bmfont.exe) has no recorded ranges and is never reported stale.
/// </remarks>
internal static class FontRangesStamp
{
    private const string Prefix = "gum ranges=\"";

    /// <summary>The ranges recorded in <paramref name="fntPath"/>, or null when there is no file or no line.</summary>
    public static string? Read(string fntPath)
    {
        if (!File.Exists(fntPath))
        {
            return null;
        }

        foreach (string line in File.ReadLines(fntPath))
        {
            if (line.StartsWith(Prefix, StringComparison.Ordinal) && line.EndsWith("\"", StringComparison.Ordinal))
            {
                return line.Substring(Prefix.Length, line.Length - Prefix.Length - 1);
            }
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="fntPath"/> recorded ranges that cover different codepoints than
    /// <paramref name="expectedRanges"/>. Ranges are compared as sets of codepoints, so spelling and
    /// order don't matter.
    /// </summary>
    public static bool IsStale(string fntPath, string expectedRanges)
    {
        string? recorded = Read(fntPath);
        if (recorded == null)
        {
            return false;
        }

        return !new HashSet<int>(BmfcSave.ParseCharRanges(recorded))
            .SetEquals(BmfcSave.ParseCharRanges(expectedRanges));
    }

    /// <summary>
    /// Replaces the recorded ranges in <paramref name="fntPath"/>. Does nothing when the file is
    /// missing or is not the text format (an XML or binary <c>.fnt</c> has no line to add).
    /// </summary>
    public static void Write(string fntPath, string ranges)
    {
        if (!File.Exists(fntPath))
        {
            return;
        }

        string contents = File.ReadAllText(fntPath);
        if (!contents.StartsWith("info", StringComparison.Ordinal))
        {
            return;
        }

        string newLine = contents.Contains("\r\n") ? "\r\n" : "\n";
        List<string> lines = contents
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Where(line => !line.StartsWith(Prefix, StringComparison.Ordinal))
            .ToList();

        while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        lines.Add(Prefix + ranges + "\"");
        File.WriteAllText(fntPath, string.Join(newLine, lines) + newLine, new UTF8Encoding(false));
    }
}
