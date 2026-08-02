using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary.Content;
using System.Collections.Generic;
using static Raylib_cs.Raylib;

namespace RenderingLibrary;

/// <summary>
/// raylib backend for <see cref="GraphicalUiElement.ApplyCachedTextureFromPixelData"/> and
/// <see cref="GraphicalUiElement.ApplyPooledTextureFromPixelData"/>. Cached textures live in the
/// <see cref="LoaderManager"/> (shared across instances, unloaded on content unload). Pooled textures
/// are reused across control instances, reclaiming entries whose owner has detached from the visual
/// tree, so repeated create/destroy cycles never grow the pool.
/// </summary>
internal static class PixelDataTextureApplier
{
    private class PoolEntry
    {
        public PoolEntry(Texture2D texture) => Texture = texture;
        public Texture2D Texture { get; }
        public GraphicalUiElement? Owner { get; set; }
    }

    private static readonly List<PoolEntry> Pool = new List<PoolEntry>();

    public static void ApplyCached(GraphicalUiElement target, string cacheKey, byte[] rgba, int width, int height)
    {
        if (target?.RenderableComponent is not Sprite sprite)
        {
            return;
        }

        // Texture creation needs a live GL context.
        if (!IsWindowReady())
        {
            return;
        }

        LoaderManager loader = LoaderManager.Self;
        ManagedTexture? managed = loader.TryGetCachedDisposable<ManagedTexture>(cacheKey);
        if (managed == null)
        {
            managed = new ManagedTexture(CreateTexture(rgba, width, height));
            loader.AddDisposable(cacheKey, managed, LoaderManager.ExistingContentBehavior.Replace);
        }

        sprite.Texture = managed.Texture;
    }

    public static void ApplyPooled(GraphicalUiElement target, GraphicalUiElement owner, byte[] rgba, int width, int height)
    {
        if (target?.RenderableComponent is not Sprite sprite)
        {
            return;
        }

        if (!IsWindowReady())
        {
            return;
        }

        PoolEntry? entry = null;
        PoolEntry? reclaimable = null;
        foreach (PoolEntry candidate in Pool)
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
            entry = reclaimable ?? AddPoolEntry(rgba, width, height);
            entry.Owner = owner;
        }

        UpdateTexture(entry.Texture, rgba);
        sprite.Texture = entry.Texture;
    }

    private static PoolEntry AddPoolEntry(byte[] rgba, int width, int height)
    {
        PoolEntry entry = new PoolEntry(CreateTexture(rgba, width, height));
        Pool.Add(entry);
        return entry;
    }

    private static Texture2D CreateTexture(byte[] rgba, int width, int height)
    {
        // GenImageColor produces an UncompressedR8G8B8A8 image, matching the RGBA byte layout the
        // callers build, so UpdateTexture can push the pixels straight in.
        Image image = GenImageColor(width, height, Color.Blank);
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        UpdateTexture(texture, rgba);
        // These textures are stretched to their on-screen size, so filter smoothly rather than
        // showing the generation resolution as blocks.
        SetTextureFilter(texture, TextureFilter.Bilinear);

        return texture;
    }
}
