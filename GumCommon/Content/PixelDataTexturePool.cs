#nullable enable

using Gum.Wireframe;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Content;

/// <summary>
/// Pooled-texture bookkeeping shared by every backend's pixel-data texture applier. Each owner keeps
/// its own texture while it is in the visual tree, and an entry whose owner has detached is recycled,
/// so repeated create/destroy cycles never grow the pool. Backends supply the texture type and how it
/// is allocated. This is the pooled counterpart to the shared, key-based caching in
/// <see cref="LoaderManager"/>.
/// </summary>
/// <typeparam name="TTexture">The backend's texture type.</typeparam>
public class PixelDataTexturePool<TTexture>
{
    private class PoolEntry
    {
        public PoolEntry(TTexture texture) => Texture = texture;
        public TTexture Texture { get; }
        public GraphicalUiElement? Owner { get; set; }
    }

    private readonly List<PoolEntry> _entries;

    /// <summary>
    /// Creates an empty pool. Backends hold one per pooled texture role.
    /// </summary>
    public PixelDataTexturePool()
    {
        _entries = new List<PoolEntry>();
    }

    /// <summary>
    /// Returns the texture belonging to <paramref name="owner"/>, reusing the texture of an entry
    /// whose owner has left the visual tree, or calling <paramref name="create"/> when neither is
    /// available. The returned texture holds whatever pixels it was last given, so callers upload
    /// their pixel data to it after this returns.
    /// </summary>
    /// <param name="owner">The element the texture is reserved for.</param>
    /// <param name="create">Allocates a texture. Only called when nothing can be reused.</param>
    public TTexture GetOrCreate(GraphicalUiElement owner, Func<TTexture> create)
    {
        PoolEntry? entry = null;
        PoolEntry? reclaimable = null;
        foreach (PoolEntry candidate in _entries)
        {
            if (candidate.Owner == owner)
            {
                entry = candidate;
                break;
            }
            // An entry whose owner has left the visual tree (removed from root) can be reused.
            if (reclaimable == null && (candidate.Owner == null || candidate.Owner.Parent == null))
            {
                reclaimable = candidate;
            }
        }

        if (entry == null)
        {
            entry = reclaimable ?? AddEntry(create());
            entry.Owner = owner;
        }

        return entry.Texture;
    }

    private PoolEntry AddEntry(TTexture texture)
    {
        PoolEntry entry = new PoolEntry(texture);
        _entries.Add(entry);
        return entry;
    }
}
