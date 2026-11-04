using System.IO.Compression;
using System.Text;

namespace Gum.Avalonia.Tests.TextureCoordinates;

/// <summary>
/// A procedural 320x192 sprite sheet written as a PNG, with the named regions a plugin would report
/// for it. Shared by the region scenario tests and the PR screenshots.
/// </summary>
internal static class SampleSpriteSheet
{
    public const int Width = 320;
    public const int Height = 192;

    /// <summary>A handful of differently sized sprites, as a sprite-sheet plugin would report them.</summary>
    public static IReadOnlyList<(string Name, int X, int Y, int Width, int Height)> Few { get; } = new[]
    {
        ("Hero_Idle", 8, 8, 48, 64),
        ("Hero_Run", 64, 8, 48, 64),
        ("Coin", 128, 12, 24, 24),
        ("Heart", 160, 12, 24, 24),
        ("Key", 192, 12, 16, 32),
        ("Slime", 8, 88, 64, 40),
        ("Door", 96, 80, 48, 96),
        ("Button_Normal", 160, 80, 96, 32),
        ("Button_Pressed", 160, 120, 96, 32),
        ("Panel_Frame", 264, 8, 48, 48),
        ("Torch", 272, 80, 32, 64),
    };

    /// <summary>Sixty sprites of varying size, the "many regions" case.</summary>
    public static IReadOnlyList<(string Name, int X, int Y, int Width, int Height)> Sixty()
    {
        List<(string, int, int, int, int)> sprites = new List<(string, int, int, int, int)>();
        for (int row = 0; row < 6; row++)
        {
            int x = 3;
            for (int column = 0; column < 10; column++)
            {
                int width = 20 + (row * 7 + column * 5) % 10;
                int height = 22 + (row + column * 3) % 8;
                sprites.Add(($"Sprite_{row * 10 + column:00}", x, row * 32 + 1 + column % 3, width, height));
                x += width + 3;
            }
        }
        return sprites;
    }

    /// <summary>Writes the sheet: a dark background with a tinted block per <see cref="Few"/> region.</summary>
    public static void Write(string path) => Write(path, Few);

    /// <summary>Writes the sheet with a tinted block per region in <paramref name="regions"/>.</summary>
    public static void Write(string path, IReadOnlyList<(string Name, int X, int Y, int Width, int Height)> regions)
    {
        byte[] pixels = new byte[Width * Height * 4];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int shade = ((x / 8 + y / 8) % 2 == 0) ? 58 : 66;
                Set(pixels, x, y, (byte)shade, (byte)shade, (byte)(shade + 6));
            }
        }

        for (int i = 0; i < regions.Count; i++)
        {
            var region = regions[i];
            byte red = (byte)(90 + (i * 53) % 150);
            byte green = (byte)(80 + (i * 97) % 160);
            byte blue = (byte)(100 + (i * 29) % 140);
            for (int y = region.Y; y < region.Y + region.Height; y++)
            {
                for (int x = region.X; x < region.X + region.Width; x++)
                {
                    bool border = x == region.X || y == region.Y || x == region.X + region.Width - 1 || y == region.Y + region.Height - 1;
                    Set(pixels, x, y, border ? (byte)30 : red, border ? (byte)30 : green, border ? (byte)30 : blue);
                }
            }
        }

        File.WriteAllBytes(path, EncodePng(pixels, Width, Height));
    }

    private static void Set(byte[] pixels, int x, int y, byte red, byte green, byte blue)
    {
        int index = (y * Width + x) * 4;
        pixels[index] = red;
        pixels[index + 1] = green;
        pixels[index + 2] = blue;
        pixels[index + 3] = 255;
    }

    private static byte[] EncodePng(byte[] rgba, int width, int height)
    {
        using MemoryStream output = new MemoryStream();
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        byte[] header = new byte[13];
        WriteInt(header, 0, width);
        WriteInt(header, 4, height);
        header[8] = 8; // bit depth
        header[9] = 6; // RGBA
        WriteChunk(output, "IHDR", header);

        using MemoryStream raw = new MemoryStream();
        for (int y = 0; y < height; y++)
        {
            raw.WriteByte(0); // filter: none
            raw.Write(rgba, y * width * 4, width * 4);
        }
        using MemoryStream compressed = new MemoryStream();
        using (ZLibStream zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw.ToArray());
        }
        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        byte[] length = new byte[4];
        WriteInt(length, 0, data.Length);
        output.Write(length);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);
        uint crc = 0xFFFFFFFF;
        foreach (byte value in typeBytes.Concat(data))
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }
        byte[] crcBytes = new byte[4];
        WriteInt(crcBytes, 0, (int)~crc);
        output.Write(crcBytes);
    }
}
