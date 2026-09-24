using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices;

/// <summary>
/// Finds files a project references under a different letter case than the file on disk. Windows
/// resolves such a reference; a case-sensitive file system (Linux) does not, so the project loads
/// on one and not the other.
/// </summary>
public interface IFileNameCaseChecker
{
    /// <summary>
    /// Returns the on-disk spelling of <paramref name="relativePath"/> (relative to
    /// <paramref name="rootDirectory"/>, forward slashes) when a file or folder along the path
    /// exists only under a different case; null when the path matches exactly or does not exist
    /// under any case.
    /// </summary>
    string? FindCaseMismatch(string rootDirectory, string relativePath);
}

/// <inheritdoc/>
/// <remarks>
/// Directory listings are cached for the life of the instance and re-read when a directory's
/// last-write time changes, so one instance can serve every check (issue #4950).
/// </remarks>
public class FileNameCaseChecker : IFileNameCaseChecker
{
    // Wider than the coarsest common file-system timestamp (FAT's 2 seconds): a directory changed
    // this close to its listing may keep the same last-write time, so the listing is not reused.
    private static readonly TimeSpan UntrustedListingAge = TimeSpan.FromSeconds(3);

    private readonly Dictionary<string, Listing> _listings;

    /// <summary>Creates a checker with an empty listing cache.</summary>
    public FileNameCaseChecker()
    {
        _listings = new Dictionary<string, Listing>(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public string? FindCaseMismatch(string rootDirectory, string relativePath)
    {
        if (string.IsNullOrEmpty(rootDirectory) || string.IsNullOrEmpty(relativePath) || Path.IsPathRooted(relativePath))
        {
            return null;
        }

        string current = rootDirectory.TrimEnd('/', '\\');
        List<string> actualSegments = new List<string>();
        bool differs = false;

        foreach (string segment in relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }
            if (segment == "..")
            {
                current = Path.GetDirectoryName(current) ?? current;
                actualSegments.Add(segment);
                continue;
            }

            string[]? names = ListNames(current);
            if (names == null)
            {
                return null;
            }

            string? exact = names.FirstOrDefault(name => string.Equals(name, segment, StringComparison.Ordinal));
            string? actual = exact ?? names.FirstOrDefault(name => string.Equals(name, segment, StringComparison.OrdinalIgnoreCase));
            if (actual == null)
            {
                return null;
            }

            differs |= exact == null;
            actualSegments.Add(actual);
            current = Path.Combine(current, actual);
        }

        return differs ? string.Join("/", actualSegments) : null;
    }

    private string[]? ListNames(string directory)
    {
        // A missing directory reports a fixed placeholder time, which changes once it is created.
        DateTime lastWriteUtc = Directory.GetLastWriteTimeUtc(directory);
        if (_listings.TryGetValue(directory, out Listing? cached)
            && cached.LastWriteUtc == lastWriteUtc
            && cached.ListedAtUtc - lastWriteUtc > UntrustedListingAge)
        {
            return cached.Names;
        }

        DateTime listedAtUtc = DateTime.UtcNow;
        string[]? names = Directory.Exists(directory)
            ? Directory.EnumerateFileSystemEntries(directory).Select(Path.GetFileName).OfType<string>().ToArray()
            : null;
        _listings[directory] = new Listing(lastWriteUtc, listedAtUtc, names);
        return names;
    }

    private sealed record Listing(DateTime LastWriteUtc, DateTime ListedAtUtc, string[]? Names);
}
