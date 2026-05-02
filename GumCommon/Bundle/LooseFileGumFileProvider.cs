using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Gum.Bundle;

/// <summary>
/// <see cref="IGumFileProvider"/> backed by loose files in a directory on disk.
/// Lookups are case-sensitive at the API layer even on case-insensitive filesystems (Windows/macOS),
/// so that dev-time casing bugs surface here instead of exploding in case-sensitive production environments.
/// </summary>
public class LooseFileGumFileProvider : IGumFileProvider
{
    private readonly string _rootDirectory;
    private readonly string _normalizedRoot;

    /// <summary>Initializes a new <see cref="LooseFileGumFileProvider"/> rooted at <paramref name="rootDirectory"/>.</summary>
    public LooseFileGumFileProvider(string rootDirectory)
    {
        if (rootDirectory == null)
        {
            throw new ArgumentNullException(nameof(rootDirectory));
        }
        _rootDirectory = rootDirectory;
        _normalizedRoot = Path.GetFullPath(rootDirectory).Replace('\\', '/').TrimEnd('/');
    }

    /// <inheritdoc/>
    public bool Exists(string relativePath)
    {
        if (relativePath == null)
        {
            throw new ArgumentNullException(nameof(relativePath));
        }
        string fullPath = Path.Combine(_rootDirectory, relativePath);
        return File.Exists(fullPath) && CasingMatchesOnDisk(fullPath, relativePath);
    }

    /// <inheritdoc/>
    public Stream OpenRead(string relativePath)
    {
        if (relativePath == null)
        {
            throw new ArgumentNullException(nameof(relativePath));
        }
        string fullPath = Path.Combine(_rootDirectory, relativePath);
        if (!File.Exists(fullPath) || !CasingMatchesOnDisk(fullPath, relativePath))
        {
            throw new FileNotFoundException($"File '{relativePath}' was not found under '{_rootDirectory}'.", relativePath);
        }
        return File.OpenRead(fullPath);
    }

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string searchPattern)
    {
        if (searchPattern == null)
        {
            throw new ArgumentNullException(nameof(searchPattern));
        }

        if (!Directory.Exists(_rootDirectory))
        {
            yield break;
        }

        string normalizedPattern = searchPattern.Replace('\\', '/');
        bool patternHasSlash = GlobMatcher.PatternHasPathSeparator(normalizedPattern);
        Regex regex = GlobMatcher.Compile(normalizedPattern);

        foreach (string fullPath in Directory.EnumerateFiles(_rootDirectory, "*", SearchOption.AllDirectories))
        {
            string relative = ToRelativeForwardSlash(fullPath);
            string candidate = patternHasSlash ? relative : GetFileName(relative);
            if (regex.IsMatch(candidate))
            {
                yield return relative;
            }
        }
    }

    private string ToRelativeForwardSlash(string fullPath)
    {
        string normalizedFull = Path.GetFullPath(fullPath).Replace('\\', '/');
        if (normalizedFull.StartsWith(_normalizedRoot + "/", StringComparison.Ordinal))
        {
            return normalizedFull.Substring(_normalizedRoot.Length + 1);
        }
        return normalizedFull;
    }

    private static string GetFileName(string path)
    {
        int slash = path.LastIndexOf('/');
        return slash < 0 ? path : path.Substring(slash + 1);
    }

    private bool CasingMatchesOnDisk(string fullPath, string requestedRelativePath)
    {
        string[] requestedSegments = requestedRelativePath
            .Replace('\\', '/')
            .TrimStart('/')
            .Split('/');

        try
        {
            string current = _rootDirectory;
            for (int i = 0; i < requestedSegments.Length; i++)
            {
                string segment = requestedSegments[i];
                if (segment.Length == 0)
                {
                    return false;
                }

                bool isLast = i == requestedSegments.Length - 1;
                string[] matches = isLast
                    ? Directory.GetFiles(current, segment)
                    : Directory.GetDirectories(current, segment);

                if (matches.Length == 0)
                {
                    return false;
                }
                string actualName = Path.GetFileName(matches[0]);
                if (!string.Equals(actualName, segment, StringComparison.Ordinal))
                {
                    return false;
                }
                current = matches[0];
            }
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
