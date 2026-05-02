namespace Gum.Bundle;

/// <summary>
/// Constants describing the on-disk layout of a `.gumpkg` bundle file:
/// 4-byte magic ("GUMP") + 1-byte version + brotli-compressed ustar tar payload.
/// </summary>
public static class GumBundleFormat
{
    /// <summary>The four magic bytes that prefix every bundle file: ASCII "GUMP".</summary>
    public static readonly byte[] MagicBytes = new byte[] { 0x47, 0x55, 0x4D, 0x50 };

    /// <summary>The current bundle format version this implementation writes and reads.</summary>
    public const byte CurrentVersion = 0x01;

    /// <summary>Total length of the fixed header (magic bytes + version byte).</summary>
    public const int HeaderLength = 5;
}
