using System.Collections.Generic;
using System.IO;

namespace Gum.Bundle;

/// <summary>
/// Abstraction over the file source backing a loaded Gum project: either loose files on disk
/// (<see cref="LooseFileGumFileProvider"/>) or entries inside a `.gumpkg` bundle
/// (<see cref="BundleGumFileProvider"/>). All paths are relative to the provider's root and
/// use forward slashes.
/// </summary>
public interface IGumFileProvider
{
    /// <summary>Returns true if a file at <paramref name="relativePath"/> exists in the provider.</summary>
    bool Exists(string relativePath);

    /// <summary>
    /// Opens a readable stream for <paramref name="relativePath"/>.
    /// Throws <see cref="FileNotFoundException"/> if the file is not present.
    /// </summary>
    Stream OpenRead(string relativePath);

    /// <summary>
    /// Enumerates files matching <paramref name="searchPattern"/> recursively across the provider's root.
    /// Supports `*` (any chars except `/`) and `?` (single char except `/`). When the pattern contains a
    /// `/`, it is matched against the full forward-slash-normalized relative path; otherwise it matches
    /// against the filename only. Returned paths are forward-slash-normalized and relative to the root.
    /// </summary>
    IEnumerable<string> EnumerateFiles(string searchPattern);
}
