#nullable enable
using System.Runtime.CompilerServices;
using System.Threading;
using SkiaSharp;

namespace SkiaGum.Content;

/// <summary>
/// Counts how often Gum rewrote a cached bitmap's pixels in place (hot reload of a replaced image, #8160).
/// Sprites and NineSlices draw an <see cref="SKImage"/> made from their bitmap, and SkiaSharp exposes no
/// generation id on <see cref="SKBitmap"/>, so they compare this version with the one they made the image
/// from and make a new image when it differs. Anything still holding the bitmap shows the new pixels on its
/// next draw, whether or not it is in a GumService's tree.
/// </summary>
internal static class BitmapPixelVersion
{
    private static readonly ConditionalWeakTable<SKBitmap, StrongBox<int>> _versions = new();
    private static int _changes;

    /// <summary>Changes since startup to any bitmap. Unchanged means no renderable needs to look its bitmap up.</summary>
    public static int Changes => Volatile.Read(ref _changes);

    public static int Of(SKBitmap? bitmap) =>
        bitmap != null && _versions.TryGetValue(bitmap, out StrongBox<int>? version) ? Volatile.Read(ref version.Value) : 0;

    /// <summary>
    /// Records that <paramref name="bitmap"/>'s pixels were rewritten. NotifyPixelsChanged gives it a new
    /// Skia generation id, so an image made from it afterwards does not reuse a GPU copy of the old pixels.
    /// </summary>
    public static void MarkChanged(SKBitmap bitmap)
    {
        bitmap.NotifyPixelsChanged();
        Interlocked.Increment(ref _versions.GetOrCreateValue(bitmap).Value);
        Interlocked.Increment(ref _changes);
    }
}
