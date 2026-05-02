using System.Collections.Generic;

namespace Gum.Bundle;

/// <summary>
/// In-memory representation of a decoded `.gumpkg` bundle: the format version
/// plus all entries keyed by their bundle-relative path.
/// </summary>
public class GumBundle
{
    /// <summary>Format version byte read from the bundle header.</summary>
    public byte Version { get; }

    /// <summary>All entries in the bundle, keyed by bundle-relative path (forward slashes).</summary>
    public IReadOnlyDictionary<string, byte[]> Entries { get; }

    /// <summary>
    /// Entry paths in the order they appeared in the underlying tar stream.
    /// Writers sort lexicographically before emitting, so this is the canonical sorted order.
    /// </summary>
    public IReadOnlyList<string> EntryPathsInOrder { get; }

    /// <summary>Initializes a new <see cref="GumBundle"/> with the given version and entries.</summary>
    public GumBundle(byte version, IReadOnlyDictionary<string, byte[]> entries, IReadOnlyList<string> entryPathsInOrder)
    {
        Version = version;
        Entries = entries;
        EntryPathsInOrder = entryPathsInOrder;
    }
}
