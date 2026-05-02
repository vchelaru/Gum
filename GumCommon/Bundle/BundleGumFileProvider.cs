using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Gum.Bundle;

/// <summary>
/// <see cref="IGumFileProvider"/> backed by an in-memory <see cref="GumBundle"/>.
/// Lookups are case-sensitive, matching the bundle's production runtime semantics.
/// </summary>
public class BundleGumFileProvider : IGumFileProvider
{
    private readonly GumBundle _bundle;

    /// <summary>Initializes a new <see cref="BundleGumFileProvider"/> wrapping <paramref name="bundle"/>.</summary>
    public BundleGumFileProvider(GumBundle bundle)
    {
        if (bundle == null)
        {
            throw new ArgumentNullException(nameof(bundle));
        }
        _bundle = bundle;
    }

    /// <inheritdoc/>
    public bool Exists(string relativePath)
    {
        return _bundle.Entries.ContainsKey(Normalize(relativePath));
    }

    /// <inheritdoc/>
    public Stream OpenRead(string relativePath)
    {
        string key = Normalize(relativePath);
        if (!_bundle.Entries.TryGetValue(key, out byte[]? bytes))
        {
            throw new FileNotFoundException($"Bundle does not contain entry '{relativePath}'.", relativePath);
        }
        return new MemoryStream(bytes, writable: false);
    }

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string searchPattern)
    {
        if (searchPattern == null)
        {
            throw new ArgumentNullException(nameof(searchPattern));
        }

        string normalizedPattern = searchPattern.Replace('\\', '/');
        bool patternHasSlash = GlobMatcher.PatternHasPathSeparator(normalizedPattern);
        Regex regex = GlobMatcher.Compile(normalizedPattern);

        foreach (string path in _bundle.EntryPathsInOrder)
        {
            string candidate = patternHasSlash ? path : GetFileName(path);
            if (regex.IsMatch(candidate))
            {
                yield return path;
            }
        }
    }

    private static string Normalize(string relativePath)
    {
        if (relativePath == null)
        {
            throw new ArgumentNullException(nameof(relativePath));
        }
        return relativePath.Replace('\\', '/');
    }

    private static string GetFileName(string path)
    {
        int slash = path.LastIndexOf('/');
        return slash < 0 ? path : path.Substring(slash + 1);
    }
}
