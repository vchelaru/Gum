using System;
using System.Collections.Generic;
#if NET7_0_OR_GREATER
using System.Formats.Tar;
#endif
using System.IO;
using System.IO.Compression;

namespace Gum.Bundle;

/// <summary>
/// Reads a `.gumpkg` bundle stream into a <see cref="GumBundle"/>: validates header,
/// decompresses brotli payload, and materializes every tar entry into memory.
/// </summary>
public static class GumBundleReader
{
    /// <summary>
    /// Reads a bundle from <paramref name="input"/>. Throws <see cref="GumBundleFormatException"/>
    /// for bad magic, unsupported version, or truncated/corrupt payload.
    /// </summary>
    public static GumBundle Read(Stream input)
    {
#if !NET7_0_OR_GREATER
        throw new NotSupportedException(
            "GumBundleReader.Read requires .NET 7 or later (System.Formats.Tar). " +
            "On older targets, .gumpkg bundle loading is not available — load loose files instead.");
#else
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
            using BrotliStream brotli = new BrotliStream(input, CompressionMode.Decompress, leaveOpen: true);
            using TarReader tar = new TarReader(brotli, leaveOpen: true);

            TarEntry? entry;
            while ((entry = tar.GetNextEntry(copyData: false)) != null)
            {
                if (entry.EntryType != TarEntryType.RegularFile && entry.EntryType != TarEntryType.V7RegularFile)
                {
                    continue;
                }

                byte[] content;
                if (entry.DataStream == null)
                {
                    content = Array.Empty<byte>();
                }
                else
                {
                    using MemoryStream buffer = new MemoryStream();
                    entry.DataStream.CopyTo(buffer);
                    content = buffer.ToArray();
                }

                entries[entry.Name] = content;
                orderedPaths.Add(entry.Name);
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
#endif
    }

#if NET7_0_OR_GREATER
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
#endif
}
