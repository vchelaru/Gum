using Raylib_cs;
using System;
using System.Buffers.Binary;
using System.IO;
using static Raylib_cs.Raylib;

namespace RenderingLibrary.Content;

/// <summary>
/// Loads a TTF/OTF font file as a raylib <see cref="Font"/> sized so its em equals the requested size, as
/// KernSmith (MonoGame, Raylib) and Skia size fonts. raylib's own <see cref="Raylib.LoadFontEx(string, int, int[], int)"/>
/// sizes a font so its ascent-to-descent height (the hhea table) equals the requested size, which draws most
/// fonts smaller than the same FontSize on the other runtimes.
/// </summary>
public static class TtfFontLoader
{
    /// <summary>
    /// Loads <paramref name="fontFilePath"/> so the font's em is <paramref name="fontSize"/> pixels. The returned
    /// font's <see cref="Font.BaseSize"/> is <paramref name="fontSize"/>, as for a KernSmith or .fnt font, and its
    /// line height and baseline are recorded for the <c>Text</c> renderable. Release it with <see cref="Unload"/>.
    /// A file whose head/hhea tables cannot be read loads as <c>LoadFontEx</c> loads it.
    /// </summary>
    public static Font LoadFont(string fontFilePath, int fontSize)
    {
        if (!TryReadEmMetrics(fontFilePath, out int unitsPerEm, out int ascent, out int descent))
        {
            return LoadFontEx(fontFilePath, fontSize, null, 0);
        }

        // raylib scales the font so ascent - descent spans the size it is given, and puts the baseline at the
        // truncated scaled ascent.
        int loadSize = GetRaylibLoadSize(fontSize, unitsPerEm, ascent, descent);
        Font font = LoadFontEx(fontFilePath, loadSize, null, 0);
        if (font.Texture.Id == 0 || font.BaseSize != loadSize)
        {
            return font;
        }

        int baselineY = (int)(ascent * (loadSize / (float)(ascent - descent)));
        RaylibFontMetricsRegistry.Register(font.Texture.Id, loadSize, baselineY);
        // raylib draws a glyph scaled by the requested draw size over BaseSize, so a BaseSize of the em draws
        // the atlas 1:1 at FontSize, and every size Gum derives from BaseSize is in em units.
        font.BaseSize = fontSize;
        return font;
    }

    /// <summary>
    /// Unloads a font returned by <see cref="LoadFont"/>, including the line metrics recorded for it.
    /// </summary>
    public static void Unload(Font font)
    {
        RaylibFontMetricsRegistry.Remove(font.Texture.Id);
        UnloadFont(font);
    }

    /// <summary>
    /// The size to pass to <c>LoadFontEx</c> so the font's em is <paramref name="fontSize"/> pixels.
    /// </summary>
    internal static int GetRaylibLoadSize(int fontSize, int unitsPerEm, int ascent, int descent)
    {
        return System.Math.Max(1, (int)MathF.Round(fontSize * (ascent - descent) / (float)unitsPerEm));
    }

    /// <summary>
    /// Reads the em metrics of a font file; false when the file cannot be read or its tables are missing or
    /// degenerate.
    /// </summary>
    internal static bool TryReadEmMetrics(string fontFilePath, out int unitsPerEm, out int ascent, out int descent)
    {
        unitsPerEm = 0;
        ascent = 0;
        descent = 0;
        try
        {
            using FileStream stream = File.OpenRead(fontFilePath);
            return TryReadEmMetrics(stream, out unitsPerEm, out ascent, out descent)
                && unitsPerEm > 0 && ascent - descent > 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads unitsPerEm (head table) and ascent/descent (hhea table, the metrics raylib's stb_truetype scales by)
    /// from a single-face sfnt font. raylib loads only .ttf/.otf files, reading the face at offset 0, so a font
    /// collection (.ttc) is never read here.
    /// </summary>
    internal static bool TryReadEmMetrics(Stream stream, out int unitsPerEm, out int ascent, out int descent)
    {
        unitsPerEm = 0;
        ascent = 0;
        descent = 0;

        Span<byte> buffer = stackalloc byte[16];
        const long fontOffset = 0;
        if (!TryRead(stream, fontOffset, buffer.Slice(0, 12)))
        {
            return false;
        }

        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(4));
        long headOffset = -1;
        long hheaOffset = -1;
        for (int i = 0; i < tableCount && (headOffset < 0 || hheaOffset < 0); i++)
        {
            if (!TryRead(stream, fontOffset + 12 + 16 * i, buffer))
            {
                return false;
            }
            uint tag = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            long offset = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(8));
            if (tag == 0x68656164) // 'head'
            {
                headOffset = offset;
            }
            else if (tag == 0x68686561) // 'hhea'
            {
                hheaOffset = offset;
            }
        }
        if (headOffset < 0 || hheaOffset < 0
            || !TryRead(stream, headOffset + 18, buffer.Slice(0, 2)))
        {
            return false;
        }
        unitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(buffer);

        if (!TryRead(stream, hheaOffset + 4, buffer.Slice(0, 4)))
        {
            return false;
        }
        ascent = BinaryPrimitives.ReadInt16BigEndian(buffer);
        descent = BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(2));
        return true;
    }

    private static bool TryRead(Stream stream, long position, Span<byte> destination)
    {
        if (position < 0 || position + destination.Length > stream.Length)
        {
            return false;
        }
        stream.Position = position;
        stream.ReadExactly(destination);
        return true;
    }
}
