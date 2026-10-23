using Raylib_cs;
using RenderingLibrary.Content;

namespace RaylibGum.Tests;

/// <summary>
/// Identifies which Nunito cut a loaded raylib font is, by the advance and atlas size of a few glyphs, which differ
/// between the cuts of one family.
/// </summary>
internal static class NunitoGlyphSignature
{
    public static string PathOf(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Nunito", fileName);

    public static string Of(Font font)
    {
        List<string> parts = new();
        foreach (char character in "HWamg")
        {
            GlyphInfo glyph = Raylib.GetGlyphInfo(font, character);
            Rectangle rec = Raylib.GetGlyphAtlasRec(font, character);
            parts.Add($"{character}:{glyph.AdvanceX}/{glyph.OffsetX}/{rec.Width}x{rec.Height}");
        }
        return string.Join(" ", parts);
    }

    /// <summary>
    /// The signature of the font file at <paramref name="path"/> loaded at <paramref name="fontSize"/>.
    /// </summary>
    public static string OfFile(string path, int fontSize)
    {
        Font font = TtfFontLoader.LoadFont(path, fontSize);
        try
        {
            return Of(font);
        }
        finally
        {
            TtfFontLoader.Unload(font);
        }
    }
}
