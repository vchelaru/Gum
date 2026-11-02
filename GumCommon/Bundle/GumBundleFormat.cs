namespace Gum.Bundle;

/// <summary>
/// Constants describing the on-disk layout of a `.gumpkg` bundle file:
/// 4-byte magic ("GUMP") + 1-byte version + payload. Version 2 (written today) is a zip archive
/// with one Deflate-compressed entry per file. Version 1 (still read) is a brotli-compressed ustar tar.
/// </summary>
public static class GumBundleFormat
{
    /// <summary>The four magic bytes that prefix every bundle file: ASCII "GUMP".</summary>
    public static readonly byte[] MagicBytes = new byte[] { 0x47, 0x55, 0x4D, 0x50 };

    /// <summary>The bundle format version this implementation writes, and the highest it reads.</summary>
    public const byte CurrentVersion = 0x02;

    /// <summary>The original format version: a brotli-compressed ustar tar payload.</summary>
    public const byte TarBrotliVersion = 0x01;

    /// <summary>Total length of the fixed header (magic bytes + version byte).</summary>
    public const int HeaderLength = 5;

    /// <summary>
    /// Minimum length of a valid decompressed version 1 tar payload: every well-formed tar — including
    /// the empty case — ends with two 512-byte zero blocks (the end-of-archive marker), so
    /// any decompressed payload shorter than this is truncated/corrupt.
    /// </summary>
    public const int MinimumTarPayloadLength = 1024;
}
