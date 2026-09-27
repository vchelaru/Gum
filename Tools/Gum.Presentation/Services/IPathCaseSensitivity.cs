using System;

namespace Gum.Services;

/// <summary>
/// Says how to compare paths at a location: ignoring case where the file system does (Windows,
/// default macOS volumes), exactly where it doesn't (Linux), so <c>Foo</c> and <c>foo</c> count
/// as one folder only where they are one folder.
/// </summary>
public interface IPathCaseSensitivity
{
    /// <summary>
    /// <see cref="StringComparison.OrdinalIgnoreCase"/> when the file system holding
    /// <paramref name="path"/> (or its nearest existing ancestor) ignores case, else
    /// <see cref="StringComparison.Ordinal"/>.
    /// </summary>
    StringComparison GetComparison(string path);
}
