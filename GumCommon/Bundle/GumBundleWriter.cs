using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Gum.Bundle;

/// <summary>
/// Writes a set of (path, content) entries as a `.gumpkg` bundle: header + a zip archive with one
/// Deflate-compressed entry per file. Output is deterministic given identical inputs (sorted entries,
/// fixed timestamps).
/// </summary>
public static class GumBundleWriter
{
    // Earliest timestamp a zip entry can store; fixed so identical input gives identical bytes.
    private static readonly DateTimeOffset EntryTimestamp = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Writes the supplied entries to <paramref name="output"/> as a complete bundle stream.
    /// Entries are sorted lexicographically (ordinal) before writing.
    /// </summary>
    public static void Write(Stream output, IEnumerable<(string path, byte[] content)> entries)
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }
        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        List<(string path, byte[] content)> sorted = entries
            .OrderBy(e => e.path, StringComparer.Ordinal)
            .ToList();

        // The zip is built separately so its offsets start at 0, independent of the header.
        MemoryStream payload = new MemoryStream();
        using (ZipArchive zip = new ZipArchive(payload, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach ((string path, byte[] content) in sorted)
            {
                ZipArchiveEntry entry = zip.CreateEntry(path, CompressionLevel.Optimal);
                entry.LastWriteTime = EntryTimestamp;
                using Stream entryStream = entry.Open();
                entryStream.Write(content, 0, content.Length);
            }
        }

        output.Write(GumBundleFormat.MagicBytes, 0, GumBundleFormat.MagicBytes.Length);
        output.WriteByte(GumBundleFormat.CurrentVersion);
        payload.Position = 0;
        payload.CopyTo(output);
    }
}
