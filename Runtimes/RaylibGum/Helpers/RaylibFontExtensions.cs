using System.Collections.Generic;
using System.Text;

namespace RaylibGum.Helpers;

/// <summary>
/// Missing-character detection for <see cref="Raylib_cs.Font"/> (issue #4546), mirroring
/// <c>RenderingLibrary.Graphics.Fonts.BitmapFont.HasCharacter</c>/<c>GetMissingCharacters</c> for the
/// MonoGame/KniGum/FnaGum side. A raylib <see cref="Raylib_cs.Font"/> has no lookup table -- character
/// presence is a linear scan of its own <see cref="Raylib_cs.Font.Glyphs"/> array.
/// </summary>
public static class RaylibFontExtensions
{
    /// <summary>
    /// Whether <paramref name="font"/> has real glyph data for <paramref name="character"/>, as
    /// opposed to falling back to raylib's own default-glyph behavior for an unknown codepoint.
    /// </summary>
    public static unsafe bool HasCharacter(this Raylib_cs.Font font, char character)
    {
        for (int i = 0; i < font.GlyphCount; i++)
        {
            if (font.Glyphs[i].Value == character)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the atlas pixel rectangle <paramref name="font"/> placed <paramref name="character"/>'s
    /// glyph at, or <c>null</c> when the font has no real glyph data for it (see
    /// <see cref="HasCharacter"/>).
    /// </summary>
    public static unsafe Raylib_cs.Rectangle? TryGetGlyphRectangle(this Raylib_cs.Font font, char character)
    {
        for (int i = 0; i < font.GlyphCount; i++)
        {
            if (font.Glyphs[i].Value == character)
            {
                return font.Recs[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the distinct characters in <paramref name="text"/> that <paramref name="font"/> has no
    /// real glyph data for (see <see cref="HasCharacter"/>), in first-encounter order, or an empty
    /// string when every character is already present. A one-shot pre-pass over the whole string, not
    /// a per-character check in the hot wrap/measure loop.
    /// </summary>
    public static string GetMissingCharacters(this Raylib_cs.Font font, string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        StringBuilder? missing = null;
        HashSet<char>? seen = null;
        foreach (char c in text)
        {
            if (!font.HasCharacter(c))
            {
                seen ??= new HashSet<char>();
                if (seen.Add(c))
                {
                    missing ??= new StringBuilder();
                    missing.Append(c);
                }
            }
        }

        return missing?.ToString() ?? string.Empty;
    }
}
