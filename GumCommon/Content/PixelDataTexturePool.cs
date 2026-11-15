#nullable enable

using Gum.Wireframe;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Content;

/// <summary>
/// Pooled-texture bookkeeping shared by every backend's pixel-data texture applier. Each owner keeps
/// its own texture while it is in the visual tree, and an entry whose owner has detached is recycled
/// for a caller that needs a texture of the same size, so repeated create/destroy cycles never grow
/// the pool. Backends supply the texture type and how it is allocated. This is the pooled counterpart
/// to the shared, key-based caching in <see cref="LoaderManager"/>.
/// </summary>
/// <remarks>
/// The pool never disposes a texture: a detached owner's sprite may still reference it. An entry that
/// does not fit a request is left in the pool for a later caller of its size, so the pool holds at most
/// one entry per size for each owner of that size that is in the visual tree at the same time.
/// </remarks>
/// <typeparam name="TTexture">The backend's texture type.</typeparam>
public class PixelDataTexturePool<TTexture>
{
    private class PoolEntry
    {
        public PoolEntry(TTexture texture) => Texture = texture;
        public TTexture Texture { get; }
        public GraphicalUiElement? Owner { get; set; }
    }

    private static readonly Func<TTexture, bool> AnyTextureFits = static _ => true;

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
    /// their pixel data to it after this returns. Every texture is assumed to fit; backends whose
    /// textures have a size use the overload that takes a fit check.
    /// </summary>
    /// <param name="owner">The element the texture is reserved for.</param>
    /// <param name="create">Allocates a texture. Only called when nothing can be reused.</param>
    public TTexture GetOrCreate(GraphicalUiElement owner, Func<TTexture> create)
    {
        return GetOrCreate(owner, AnyTextureFits, create);
    }

    /// <summary>
    /// Returns a texture reserved for <paramref name="owner"/> that satisfies <paramref name="fits"/>.
    /// The owner's own texture is returned when it fits. Otherwise the texture of a detached entry that
    /// fits is reused, or <paramref name="create"/> allocates one when none does, and only then does the
    /// owner give up its old entry (it stays in the pool for a caller it fits). If <paramref name="create"/>
    /// throws, the owner keeps its old entry.
    /// </summary>
    /// <param name="owner">The element the texture is reserved for.</param>
    /// <param name="fits">Whether an existing texture has the size this caller needs.</param>
    /// <param name="create">Allocates a texture of the size this caller needs. Only called when nothing fits.</param>
    public TTexture GetOrCreate(GraphicalUiElement owner, Func<TTexture, bool> fits, Func<TTexture> create)
    {
        PoolEntry? outgrown = null;
        foreach (PoolEntry candidate in _entries)
        {
            if (candidate.Owner != owner)
            {
                continue;
            }

            if (fits(candidate.Texture))
            {
                return candidate.Texture;
            }

            outgrown = candidate;
            break;
        }

        PoolEntry? replacement = null;
        foreach (PoolEntry candidate in _entries)
        {
            // An entry whose owner has left the visual tree (removed from root) can be reused.
            bool isReclaimable = candidate.Owner == null || candidate.Owner.Parent == null;
            if (isReclaimable && fits(candidate.Texture))
            {
                replacement = candidate;
                break;
            }
        }

        // create() can throw. Until the owner has its new texture it keeps the old entry, because the
        // owner's sprite is still showing that texture and must not hand it to another owner.
        replacement ??= AddEntry(create());
        replacement.Owner = owner;

        if (outgrown != null)
        {
            // The owner's pixel data changed size. Give the old entry up so a caller of that size can
            // reclaim it; the caller points the owner's sprite at the new texture.
            outgrown.Owner = null;
        }

        return replacement.Texture;
    }

    private PoolEntry AddEntry(TTexture texture)
    {
        PoolEntry entry = new PoolEntry(texture);
        _entries.Add(entry);
        return entry;
    }
}
