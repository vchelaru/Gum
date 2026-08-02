using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary.Content;
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
    private static readonly PixelDataTexturePool<Texture2D> Pool = new PixelDataTexturePool<Texture2D>();

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

        Texture2D texture = Pool.GetOrCreate(owner, () => CreateTexture(rgba, width, height));

        UpdateTexture(texture, rgba);
        sprite.Texture = texture;
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
