using Gum.Wireframe;
using RenderingLibrary.Content;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace RenderingLibrary;

/// <summary>
/// Skia backend for <see cref="GraphicalUiElement.ApplyCachedTextureFromPixelData"/> and
/// <see cref="GraphicalUiElement.ApplyPooledTextureFromPixelData"/>. Cached bitmaps live in the
/// <see cref="LoaderManager"/> (shared across instances, disposed on content unload). Pooled bitmaps
/// are reused across control instances, reclaiming entries whose owner has detached from the visual
/// tree, so repeated create/destroy cycles never grow the pool.
/// </summary>
internal static class PixelDataTextureApplier
{
    private static readonly PixelDataTexturePool<SKBitmap> Pool = new PixelDataTexturePool<SKBitmap>();

    public static void ApplyCached(GraphicalUiElement target, string cacheKey, byte[] rgba, int width, int height)
    {
        if (target?.RenderableComponent is not Sprite sprite)
        {
            return;
        }

        LoaderManager loader = LoaderManager.Self;
        SKBitmap? bitmap = loader.TryGetCachedDisposable<SKBitmap>(cacheKey);
        if (bitmap == null)
        {
            bitmap = CreateBitmap(rgba, width, height);
            loader.AddDisposable(cacheKey, bitmap, LoaderManager.ExistingContentBehavior.Replace);
        }

        sprite.Texture = bitmap;
    }

    public static void ApplyPooled(GraphicalUiElement target, GraphicalUiElement owner, byte[] rgba, int width, int height)
    {
        if (target?.RenderableComponent is not Sprite sprite)
        {
            return;
        }

        SKBitmap bitmap = Pool.GetOrCreate(owner, () => CreateBitmap(rgba, width, height));

        Upload(bitmap, rgba);
        sprite.Texture = bitmap;
    }

    private static SKBitmap CreateBitmap(byte[] rgba, int width, int height)
    {
        // Rgba8888 explicitly rather than the platform-native color type, so the callers' RGBA byte
        // layout copies in directly instead of coming out channel-swapped where BGRA is native.
        SKBitmap bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        Upload(bitmap, rgba);
        return bitmap;
    }

    private static void Upload(SKBitmap bitmap, byte[] rgba)
    {
        Marshal.Copy(rgba, 0, bitmap.GetPixels(), rgba.Length);
        bitmap.NotifyPixelsChanged();
    }
}
