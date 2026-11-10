using Shouldly;
using SkiaSharp;

// SkiaResourceManager is what the default ContentLoader loads bitmaps through.
#pragma warning disable CS0618

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// What a load returns after the caller disposed the bitmap the loaded-bitmap cache holds.
/// </summary>
public class SharedTextureDisposedCacheTests : SharedTextureTestBase
{
    [Fact]
    public void LoadContent_AfterTheCachedBitmapWasDisposed_ReturnsAUsableBitmap()
    {
        // Undocumented: the cache has no Remove or Clear, so a bitmap a caller disposed stays cached and
        // every later load of that file hands the disposed object back. Intended behavior needs discussion;
        // the defensible one is a bitmap that can be drawn.
        string path = WriteGridPng(GridSize, seed: 0);
        LoadFile(path).Dispose();

        SKBitmap reloaded = LoadFile(path);

        reloaded.Handle.ShouldNotBe(IntPtr.Zero);
        reloaded.Width.ShouldBe(GridSize);
    }
}
