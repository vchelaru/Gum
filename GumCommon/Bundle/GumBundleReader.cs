using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Gum.Bundle;

/// <summary>
/// Reads a `.gumpkg` bundle stream into a <see cref="GumBundle"/>: validates header,
/// decompresses brotli payload, and materializes every tar entry into memory.
/// <para>
/// Both the brotli decompression (<c>BrotliSharpLib</c>) and the tar parsing
/// (<c>SharpCompress</c>) are pure-managed rather than the BCL equivalents
/// (<c>System.IO.Compression.BrotliStream</c> / <c>System.Formats.Tar</c>).
/// The framework versions both throw <c>PlatformNotSupportedException</c> on
/// Blazor WebAssembly — brotli because Mono does not link the native brotli
/// library, and tar because the formats package depends on POSIX file metadata
/// APIs that aren't surfaced on browser-WASM. Blazor is the primary target the
/// `.gumpkg` format was meant to optimize for, so the reader path must be fully
/// managed. The on-disk format is unchanged — pure-managed and BCL implementations
/// read the same byte stream.
/// </para>
/// </summary>
public static class GumBundleReader
{
    /// <summary>
    /// Reads a bundle from <paramref name="input"/>. Throws <see cref="GumBundleFormatException"/>
    /// for bad magic, unsupported version, or truncated/corrupt payload.
    /// </summary>
    public static GumBundle Read(Stream input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        byte[] header = new byte[GumBundleFormat.HeaderLength];
        int read = ReadFully(input, header, 0, header.Length);
        if (read < GumBundleFormat.HeaderLength)
        {
            throw new GumBundleFormatException(
                $"Bundle stream is too short to contain a header (expected {GumBundleFormat.HeaderLength} bytes, got {read}).");
        }

        for (int i = 0; i < GumBundleFormat.MagicBytes.Length; i++)
        {
            if (header[i] != GumBundleFormat.MagicBytes[i])
            {
                throw new GumBundleFormatException(
                    "Bundle stream does not begin with the expected 'GUMP' magic bytes.");
            }
        }

        byte version = header[4];
        if (version > GumBundleFormat.CurrentVersion)
        {
            throw new GumBundleFormatException(
                $"Bundle version {version} is newer than the highest supported version {GumBundleFormat.CurrentVersion}.");
        }

        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        List<string> orderedPaths = new List<string>();

        try
        {
            // BrotliSharpLib + SharpCompress (not System.IO.Compression / System.Formats.Tar)
            // — see class XML doc for the Blazor WASM rationale. Same wire format; these are
            // purely the decoder choices.
            //
            // SharpCompress's TarReader.OpenReader does a format-detection rewind that
            // requires a seekable input. BrotliStream is forward-only, so we materialize
            // the decompressed payload into a MemoryStream first. Bundles are small
            // (typically <1MB compressed, a few MB decompressed) so the buffering cost is
            // negligible compared to the runtime asset loads that follow.
            MemoryStream decompressed = new MemoryStream();
            using (BrotliSharpLib.BrotliStream brotli = new BrotliSharpLib.BrotliStream(input, CompressionMode.Decompress, leaveOpen: true))
            {
                brotli.CopyTo(decompressed);
            }
            decompressed.Position = 0;

            using SharpCompress.Readers.IReader tar = SharpCompress.Readers.Tar.TarReader.OpenReader(
                decompressed, new SharpCompress.Readers.ReaderOptions());

            while (tar.MoveToNextEntry())
            {
                if (tar.Entry.IsDirectory)
                {
                    continue;
                }

                byte[] content;
                using (SharpCompress.Common.EntryStream entryStream = tar.OpenEntryStream())
                using (MemoryStream buffer = new MemoryStream())
                {
                    entryStream.CopyTo(buffer);
                    content = buffer.ToArray();
                }

                string key = tar.Entry.Key ?? string.Empty;
                entries[key] = content;
                orderedPaths.Add(key);
            }
        }
        catch (GumBundleFormatException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or IOException or FormatException)
        {
            throw new GumBundleFormatException(
                "Bundle payload is truncated or corrupt; failed to decode the brotli/tar stream.", ex);
        }

        return new GumBundle(version, entries, orderedPaths);
    }

    private static int ReadFully(Stream stream, byte[] buffer, int offset, int count)
    {
        int total = 0;
        while (total < count)
        {
            int n = stream.Read(buffer, offset + total, count - total);
            if (n <= 0)
            {
                break;
            }
            total += n;
        }
        return total;
    }
}
