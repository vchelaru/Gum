using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using RenderingLibrary.Content;

namespace Gum.Bundle;

/// <summary>The pixel layout of a raw package texture.</summary>
public enum GumRawTexturePixelFormat : byte
{
    /// <summary>Four bytes per texel in R, G, B, A order, rows top to bottom.</summary>
    Rgba8 = 1,
}

/// <summary>The fixed header in front of a raw package texture's pixels.</summary>
public readonly struct GumRawTextureHeader
{
    /// <summary>Initializes a new <see cref="GumRawTextureHeader"/>.</summary>
    public GumRawTextureHeader(int width, int height, GumRawTexturePixelFormat pixelFormat, bool isPremultiplied)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        IsPremultiplied = isPremultiplied;
    }

    /// <summary>Width in texels.</summary>
    public int Width { get; }

    /// <summary>Height in texels.</summary>
    public int Height { get; }

    /// <summary>The layout of the pixel bytes after the header.</summary>
    public GumRawTexturePixelFormat PixelFormat { get; }

    /// <summary>True when the color channels are already multiplied by alpha.</summary>
    public bool IsPremultiplied { get; }

    /// <summary>The number of pixel bytes that follow the header.</summary>
    public long PixelByteCount => (long)Width * Height * 4;
}

/// <summary>
/// A texture stored in a `.gumpkg` as decoded pixels instead of a PNG (issue #6514). The entry keeps
/// the image's original path ("Sprites/a.png") so references resolve unchanged; its bytes are a
/// <see cref="HeaderLength"/>-byte header followed by the pixels, and a loader tells it from an encoded
/// image by <see cref="IsRawTexture"/>.
/// </summary>
/// <remarks>
/// Header layout, integers little-endian: bytes 0-3 magic (0x89 'G' 'R' 'T'), 4 header version, 5
/// <see cref="GumRawTexturePixelFormat"/>, 6 flags (bit 0 = premultiplied), 7 reserved (0), 8-11
/// width, 12-15 height.
/// </remarks>
public static class GumRawTexture
{
    /// <summary>Total length of the header in front of the pixels.</summary>
    public const int HeaderLength = 16;

    /// <summary>The header version this implementation writes and reads.</summary>
    public const byte CurrentHeaderVersion = 1;

    private const byte PremultipliedFlag = 0x01;

    private static readonly byte[] MagicBytes = { 0x89, 0x47, 0x52, 0x54 };

    private static readonly HashSet<string> WarnedPackages = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Where the one-time alpha-mismatch warning goes. Gum has no runtime warning channel, so it is
    /// written to <see cref="System.Diagnostics.Trace"/>; tests replace it.
    /// </summary>
    internal static Action<string> ReportWarning { get; set; } = message => System.Diagnostics.Trace.WriteLine(message);

    /// <summary>Forgets which packages were already warned about.</summary>
    internal static void ResetWarnings()
    {
        lock (WarnedPackages)
        {
            WarnedPackages.Clear();
        }
    }

    /// <summary>True when <paramref name="data"/> begins with the raw texture magic bytes.</summary>
    public static bool IsRawTexture(ReadOnlySpan<byte> data)
    {
        return data.Length >= MagicBytes.Length && data.Slice(0, MagicBytes.Length).SequenceEqual(MagicBytes);
    }

    /// <summary>Builds a raw texture entry: the header followed by a copy of <paramref name="rgba"/>.</summary>
    public static byte[] Encode(int width, int height, ReadOnlySpan<byte> rgba, bool isPremultiplied)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), $"Texture size {width}x{height} must be positive.");
        }
        long expected = (long)width * height * 4;
        if (rgba.Length != expected)
        {
            throw new ArgumentException(
                $"Expected {expected} RGBA bytes for a {width}x{height} texture, got {rgba.Length}.", nameof(rgba));
        }

        byte[] entry = new byte[HeaderLength + rgba.Length];
        MagicBytes.CopyTo(entry, 0);
        entry[4] = CurrentHeaderVersion;
        entry[5] = (byte)GumRawTexturePixelFormat.Rgba8;
        entry[6] = isPremultiplied ? PremultipliedFlag : (byte)0;
        BinaryPrimitives.WriteInt32LittleEndian(entry.AsSpan(8, 4), width);
        BinaryPrimitives.WriteInt32LittleEndian(entry.AsSpan(12, 4), height);
        rgba.CopyTo(entry.AsSpan(HeaderLength));
        return entry;
    }

    /// <summary>
    /// Reads and validates the header of a raw texture entry. Throws
    /// <see cref="GumBundleFormatException"/> when the magic is missing, the header or the pixels are
    /// truncated, or the header version or pixel format is unknown.
    /// </summary>
    public static GumRawTextureHeader ReadHeader(ReadOnlySpan<byte> data)
    {
        if (!IsRawTexture(data))
        {
            throw new GumBundleFormatException("Texture data does not begin with the raw texture magic bytes.");
        }
        if (data.Length < HeaderLength)
        {
            throw new GumBundleFormatException(
                $"Raw texture header is truncated: expected {HeaderLength} bytes, got {data.Length}.");
        }

        byte headerVersion = data[4];
        if (headerVersion != CurrentHeaderVersion)
        {
            throw new GumBundleFormatException(
                $"Raw texture header version {headerVersion} is not supported (expected {CurrentHeaderVersion}). Repack the package with this version of gumcli.");
        }

        GumRawTexturePixelFormat pixelFormat = (GumRawTexturePixelFormat)data[5];
        if (pixelFormat != GumRawTexturePixelFormat.Rgba8)
        {
            throw new GumBundleFormatException($"Raw texture pixel format {data[5]} is not supported.");
        }

        int width = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(8, 4));
        int height = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(12, 4));
        if (width <= 0 || height <= 0)
        {
            throw new GumBundleFormatException($"Raw texture size {width}x{height} is not valid.");
        }

        GumRawTextureHeader header = new GumRawTextureHeader(width, height, pixelFormat,
            isPremultiplied: (data[6] & PremultipliedFlag) != 0);
        long available = data.Length - HeaderLength;
        if (available != header.PixelByteCount)
        {
            string problem = available < header.PixelByteCount ? "truncated" : "longer than its size";
            throw new GumBundleFormatException(
                $"Raw texture pixel data is {problem}: a {width}x{height} texture needs {header.PixelByteCount} bytes, got {available}.");
        }
        return header;
    }

    /// <summary>
    /// Validates a raw texture entry and converts its pixels, in place, to the runtime's alpha mode.
    /// A straight package in a premultiplied runtime is premultiplied; a premultiplied package in a
    /// straight runtime is unpremultiplied and, when <paramref name="bleedTransparentEdges"/> is set,
    /// edge-bled. Either mismatch reports one warning per package. Matching modes leave the pixels
    /// alone: the packer already baked the bleed into straight packages.
    /// </summary>
    /// <param name="entry">The whole entry, header included.</param>
    /// <param name="entryName">The entry's path, used when <paramref name="packageName"/> is null.</param>
    /// <param name="packageName">The package the entry came from, named in the warning.</param>
    /// <param name="runtimeUsesPremultipliedAlpha">The runtime's alpha mode.</param>
    /// <param name="bleedTransparentEdges">Whether the runtime bleeds edges of straight textures.</param>
    /// <param name="warnOnMismatch">
    /// False for a runtime whose users cannot avoid the conversion by repacking (Skia is always
    /// premultiplied while gumcli packs straight alpha), so the warning would only be noise.
    /// </param>
    public static GumRawTextureHeader PrepareForRuntime(byte[] entry, string entryName, string? packageName,
        bool runtimeUsesPremultipliedAlpha, bool bleedTransparentEdges, bool warnOnMismatch = true)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        GumRawTextureHeader header = ReadHeader(entry);
        if (header.IsPremultiplied == runtimeUsesPremultipliedAlpha)
        {
            return header;
        }

        Span<byte> pixels = entry.AsSpan(HeaderLength);
        if (runtimeUsesPremultipliedAlpha)
        {
            RgbaPixels.Premultiply(pixels);
        }
        else
        {
            RgbaPixels.Unpremultiply(pixels);
            if (bleedTransparentEdges)
            {
                RgbaPixels.BleedTransparentEdges(pixels, header.Width, header.Height);
            }
        }

        if (warnOnMismatch)
        {
            WarnOnce(packageName ?? entryName, header.IsPremultiplied, entryName);
        }
        return header;
    }

    /// <summary>
    /// Returns <paramref name="stream"/> when it can seek, otherwise a seekable in-memory copy of the
    /// rest of it, so a loader can sniff it with <see cref="ReadForRuntime"/> and still decode it.
    /// </summary>
    public static Stream EnsureSeekable(Stream stream)
    {
        if (stream.CanSeek)
        {
            return stream;
        }
        MemoryStream buffered = new MemoryStream();
        stream.CopyTo(buffered);
        buffered.Position = 0;
        return buffered;
    }

    /// <summary>
    /// The texture loaders' shared entry point. When <paramref name="stream"/> holds a raw texture,
    /// reads all of it, converts it with <see cref="PrepareForRuntime"/> (naming the package it came
    /// from in the warning) and returns the whole entry, header included, with the pixels starting at
    /// <see cref="HeaderLength"/>. Otherwise returns null with the stream rewound, so the caller
    /// decodes it as an encoded image.
    /// </summary>
    /// <param name="stream">A seekable stream; see <see cref="EnsureSeekable"/>.</param>
    /// <param name="entryName">The texture's path, named in the warning.</param>
    /// <param name="runtimeUsesPremultipliedAlpha">The runtime's alpha mode.</param>
    /// <param name="bleedTransparentEdges">Whether the runtime bleeds edges of straight textures.</param>
    /// <param name="warnOnMismatch">See <see cref="PrepareForRuntime"/>.</param>
    /// <param name="header">The validated header, or default when the stream is not a raw texture.</param>
    /// <exception cref="GumBundleFormatException">The stream holds a corrupt raw texture.</exception>
    public static byte[]? ReadForRuntime(Stream stream, string entryName, bool runtimeUsesPremultipliedAlpha,
        bool bleedTransparentEdges, bool warnOnMismatch, out GumRawTextureHeader header)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must be seekable; wrap it with EnsureSeekable.", nameof(stream));
        }

        header = default;
        long start = stream.Position;
        byte[] prefix = new byte[MagicBytes.Length];
        int read = 0;
        while (read < prefix.Length)
        {
            int n = stream.Read(prefix, read, prefix.Length - read);
            if (n <= 0)
            {
                break;
            }
            read += n;
        }
        stream.Position = start;

        if (!IsRawTexture(prefix.AsSpan(0, read)))
        {
            return null;
        }

        MemoryStream all = new MemoryStream();
        stream.CopyTo(all);
        byte[] entry = all.ToArray();
        header = PrepareForRuntime(entry, entryName, (stream as GumBundleEntryStream)?.PackagePath,
            runtimeUsesPremultipliedAlpha, bleedTransparentEdges, warnOnMismatch);
        return entry;
    }

    private static void WarnOnce(string source, bool packageIsPremultiplied, string entryName)
    {
        string key = source + (packageIsPremultiplied ? "|premultiplied" : "|straight");
        lock (WarnedPackages)
        {
            if (!WarnedPackages.Add(key))
            {
                return;
            }
        }

        string packageMode = packageIsPremultiplied ? "premultiplied" : "straight";
        string runtimeMode = packageIsPremultiplied ? "straight" : "premultiplied";
        ReportWarning(
            $"Gum package '{source}' stores {packageMode}-alpha textures, but the runtime uses {runtimeMode} alpha " +
            $"(Renderer.UsePremultipliedAlpha), so each texture is converted when it loads (first: '{entryName}'). " +
            "Repack the package with the project's alpha mode to skip the conversion.");
    }
}
