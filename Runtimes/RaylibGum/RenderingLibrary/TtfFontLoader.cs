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
    /// font's <see cref="Font.BaseSize"/> is its line height (ascent to descent), not <paramref name="fontSize"/>.
    /// A file whose head/hhea tables cannot be read loads at <paramref name="fontSize"/> unchanged.
    /// </summary>
    public static Font LoadFont(string fontFilePath, int fontSize)
    {
        return LoadFontEx(fontFilePath, GetRaylibLoadSize(fontFilePath, fontSize), null, 0);
    }

    /// <summary>
    /// The size to pass to <c>LoadFontEx</c> so the font's em is <paramref name="fontSize"/> pixels.
    /// </summary>
    internal static int GetRaylibLoadSize(string fontFilePath, int fontSize)
    {
        try
        {
            using FileStream stream = File.OpenRead(fontFilePath);
            if (TryReadEmMetrics(stream, out int unitsPerEm, out int ascent, out int descent)
                && unitsPerEm > 0 && ascent - descent > 0)
            {
                return System.Math.Max(1, (int)MathF.Round(fontSize * (ascent - descent) / (float)unitsPerEm));
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        return fontSize;
    }

    /// <summary>
    /// Reads unitsPerEm (head table) and ascent/descent (hhea table, the metrics raylib's stb_truetype scales by)
    /// from an sfnt font. A collection (.ttc) reads its first face, which is the one <c>LoadFontEx</c> loads.
    /// </summary>
    internal static bool TryReadEmMetrics(Stream stream, out int unitsPerEm, out int ascent, out int descent)
    {
        unitsPerEm = 0;
        ascent = 0;
        descent = 0;

        Span<byte> buffer = stackalloc byte[16];
        long fontOffset = 0;
        if (!TryRead(stream, 0, buffer.Slice(0, 12)))
        {
            return false;
        }
        if (buffer[0] == 't' && buffer[1] == 't' && buffer[2] == 'c' && buffer[3] == 'f')
        {
            fontOffset = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(8));
            if (!TryRead(stream, fontOffset, buffer.Slice(0, 12)))
            {
                return false;
            }
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
