using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Gum.Bundle;

/// <summary>
/// Reads a `.gumpkg` bundle stream into a <see cref="GumBundle"/>: validates header,
/// decompresses the payload, and materializes every entry into memory.
/// <para>
/// Version 2 payloads are zip archives with Deflate entries; Deflate is native on every target,
/// browser WebAssembly included. For version 1 payloads, both the brotli decompression (<c>BrotliSharpLib</c>) and the tar parsing
/// (<c>SharpCompress</c>) are pure-managed rather than the BCL equivalents
/// (<c>System.IO.Compression.BrotliStream</c> / <c>System.Formats.Tar</c>).
/// The framework versions both throw <c>PlatformNotSupportedException</c> on
/// Blazor WebAssembly — brotli because Mono does not link the native brotli
/// library, and tar because the formats package depends on POSIX file metadata
/// APIs that aren't surfaced on browser-WASM. Blazor is the primary target the
/// `.gumpkg` format was meant to optimize for, so the version 1 reader path must be fully
/// managed.
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
            if (version == GumBundleFormat.TarBrotliVersion)
            {
                ReadTarBrotliPayload(input, entries, orderedPaths);
            }
            else
            {
                ReadZipPayload(input, entries, orderedPaths);
            }
        }
        catch (GumBundleFormatException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or IOException or FormatException)
        {
            throw new GumBundleFormatException(
                $"Bundle payload is truncated or corrupt; failed to decode the version {version} payload.", ex);
        }

        return new GumBundle(version, entries, orderedPaths);
    }

    private static void ReadZipPayload(Stream input, Dictionary<string, byte[]> entries, List<string> orderedPaths)
    {
        // ZipArchive reads the central directory at the end, so it needs a seekable stream whose
        // offsets start at the payload.
        MemoryStream payload = new MemoryStream();
        input.CopyTo(payload);
        payload.Position = 0;

        using ZipArchive zip = new ZipArchive(payload, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in zip.Entries)
        {
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                continue;
            }

            byte[] content;
            using (Stream entryStream = entry.Open())
            using (MemoryStream buffer = new MemoryStream())
            {
                entryStream.CopyTo(buffer);
                content = buffer.ToArray();
            }

            entries[entry.FullName] = content;
            orderedPaths.Add(entry.FullName);
        }
    }

    private static void ReadTarBrotliPayload(Stream input, Dictionary<string, byte[]> entries, List<string> orderedPaths)
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

        // BrotliSharpLib decodes truncated input silently, returning whatever it managed
        // to produce instead of throwing — so a half-bundle decompresses to 0 bytes and
        // the tar reader below sees an empty stream and reports zero entries. Detect that
        // here: every well-formed bundle (including the intentionally-empty one) carries
        // at least the 1024-byte tar end-of-archive marker, so anything shorter is truncated.
        if (decompressed.Length < GumBundleFormat.MinimumTarPayloadLength)
        {
            throw new GumBundleFormatException(
                $"Bundle payload is truncated; decompressed tar stream is {decompressed.Length} bytes, " +
                $"shorter than the {GumBundleFormat.MinimumTarPayloadLength}-byte tar end-of-archive marker required for any valid bundle.");
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
