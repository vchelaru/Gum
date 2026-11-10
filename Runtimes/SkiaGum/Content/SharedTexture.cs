#nullable enable
using SkiaSharp;

namespace SkiaGum.Content;

/// <summary>
/// Marks a bitmap that Gum loads and caches as read-only. Skia copies a mutable bitmap every time it
/// builds an <see cref="SKImage"/> from it, so every NineSlice and Sprite over one sprite sheet used to
/// own a private copy of the whole sheet, which overflows the GPU texture cache on a GPU surface (#7876).
/// Skia builds an image over an immutable bitmap without copying, so all of them share one pixel buffer.
/// A bitmap the caller creates and assigns is left alone and stays mutable.
/// </summary>
internal static class SharedTexture
{
    /// <summary>
    /// Makes <paramref name="bitmap"/> immutable and returns it, so images built from it share its pixels.
    /// Call it once the pixels are final.
    /// </summary>
    public static SKBitmap MarkShared(SKBitmap bitmap)
    {
        bitmap.SetImmutable();
        return bitmap;
    }
}
