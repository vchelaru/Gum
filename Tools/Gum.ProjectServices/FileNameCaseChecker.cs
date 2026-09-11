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
/// <remarks>Directory listings are cached for the life of the instance, so create one per check pass.</remarks>
public class FileNameCaseChecker : IFileNameCaseChecker
{
    private readonly Dictionary<string, string[]?> _listings;

    /// <summary>Creates a checker with an empty listing cache.</summary>
    public FileNameCaseChecker()
    {
        _listings = new Dictionary<string, string[]?>(StringComparer.Ordinal);
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
        if (!_listings.TryGetValue(directory, out string[]? names))
        {
            names = Directory.Exists(directory)
                ? Directory.EnumerateFileSystemEntries(directory).Select(Path.GetFileName).OfType<string>().ToArray()
                : null;
            _listings[directory] = names;
        }
        return names;
    }
}
