using System;
using System.Collections.Generic;
#if NET7_0_OR_GREATER
using System.Formats.Tar;
#endif
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Gum.Bundle;

/// <summary>
/// Writes a set of (path, content) entries as a `.gumpkg` bundle: header + brotli(tar(entries)).
/// Output is deterministic given identical inputs (sorted entries, fixed timestamps/uid/gid/mode).
/// </summary>
public static class GumBundleWriter
{
    /// <summary>
    /// Writes the supplied entries to <paramref name="output"/> as a complete bundle stream.
    /// Entries are sorted lexicographically (ordinal) before writing.
    /// </summary>
    public static void Write(Stream output, IEnumerable<(string path, byte[] content)> entries)
    {
#if !NET7_0_OR_GREATER
        throw new NotSupportedException(
            "GumBundleWriter.Write requires .NET 7 or later (System.Formats.Tar). " +
            "On older targets, .gumpkg bundle creation is not available.");
#else
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }
        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        output.Write(GumBundleFormat.MagicBytes, 0, GumBundleFormat.MagicBytes.Length);
        output.WriteByte(GumBundleFormat.CurrentVersion);

        List<(string path, byte[] content)> sorted = entries
            .OrderBy(e => e.path, StringComparer.Ordinal)
            .ToList();

        // leaveOpen so the caller still owns `output` after we dispose the brotli/tar wrappers.
        using (BrotliStream brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            using (TarWriter tar = new TarWriter(brotli, TarEntryFormat.Ustar, leaveOpen: true))
            {
                foreach ((string path, byte[] content) in sorted)
                {
                    UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, path)
                    {
                        // Fixed metadata so two writes of identical input produce byte-identical output.
                        ModificationTime = DateTimeOffset.UnixEpoch,
                        Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead,
                        Uid = 0,
                        Gid = 0,
                    };
                    entry.DataStream = new MemoryStream(content, writable: false);
                    tar.WriteEntry(entry);
                }
            }

            if (sorted.Count == 0)
            {
                // TarWriter does not emit the end-of-archive marker (two 512-byte zero blocks)
                // when no entries were written. Emit it ourselves so the reader can distinguish
                // an intentionally empty archive from a truncated one.
                byte[] endOfArchive = new byte[1024];
                brotli.Write(endOfArchive, 0, endOfArchive.Length);
            }
        }
#endif
    }
}
