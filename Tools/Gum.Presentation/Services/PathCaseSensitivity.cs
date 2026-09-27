using System;
using System.IO;
using System.Linq;

namespace Gum.Services;

/// <inheritdoc/>
/// <remarks>
/// Asks the file system directly, since case sensitivity is per volume (and per directory on
/// Windows), not per OS.
/// </remarks>
public class PathCaseSensitivity : IPathCaseSensitivity
{
    /// <inheritdoc/>
    public StringComparison GetComparison(string path)
    {
        string? current = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        while (!string.IsNullOrEmpty(current))
        {
            string name = Path.GetFileName(current);
            string? parent = Path.GetDirectoryName(current);
            string swapped = SwapCase(name);

            if (parent != null && swapped != name && Exists(current))
            {
                string[] entryNames = Directory.EnumerateFileSystemEntries(parent)
                    .Select(entry => Path.GetFileName(entry))
                    .ToArray();
                // This spelling resolved without being on disk, so the file system ignored its case.
                if (!entryNames.Contains(name, StringComparer.Ordinal))
                {
                    return StringComparison.OrdinalIgnoreCase;
                }
                // The other-case name resolves on a case-insensitive file system. On a case-sensitive
                // one it resolves only when it is a separate entry, which the listing shows.
                return Exists(Path.Combine(parent, swapped)) && !entryNames.Contains(swapped, StringComparer.Ordinal)
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
            }

            current = parent;
        }

        // Nothing to probe (no existing path segment with letters): fall back to the OS default.
        return OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    private static string SwapCase(string name) =>
        new string(name.Select(c => char.IsUpper(c) ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c)).ToArray());
}
