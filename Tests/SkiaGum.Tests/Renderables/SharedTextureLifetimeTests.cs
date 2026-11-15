using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Runtime.InteropServices;

// SkiaResourceManager is what the default ContentLoader loads bitmaps through.
#pragma warning disable CS0618

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// How long the one bitmap that many Sprites and NineSlices share lives, and what its cache does with it
/// (#7876). Skia refcounts an immutable bitmap's pixels, so an image outlives the bitmap it was built
/// from; the loaded-bitmap cache never evicts.
/// </summary>
public class SharedTextureLifetimeTests : SharedTextureTestBase
{
    private sealed class ReleaseFlag
    {
        public volatile bool Released;
    }

    // A bitmap over native memory that tells the test when Skia freed the pixels. The release callback runs
    // when the last reference to the pixel buffer goes away, which is the exact event "native memory
    // released" (SKBitmap and SKImage expose no handle count).
    private static SKBitmap CreateTrackedBitmap(int size, SKColor color, ReleaseFlag flag)
    {
        SKImageInfo info = new(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        IntPtr memory = Marshal.AllocHGlobal(info.BytesSize);
        byte[] rgba = new byte[info.BytesSize];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = color.Red;
            rgba[i + 1] = color.Green;
            rgba[i + 2] = color.Blue;
            rgba[i + 3] = 255;
        }
        Marshal.Copy(rgba, 0, memory, rgba.Length);

        SKBitmap bitmap = new();
        bool installed = bitmap.InstallPixels(info, memory, info.RowBytes,
            (address, context) =>
            {
                Marshal.FreeHGlobal(address);
                flag.Released = true;
            }, null);
        installed.ShouldBeTrue();
        return bitmap;
    }

    #region The bitmap cache

    [Fact]
    public void LoadContent_SameFileManyTimes_ReturnsOneImmutableBitmap()
    {
        string path = WriteGridPng(GridSize, seed: 0);

        HashSet<SKBitmap> bitmaps = new(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < 50; i++)
        {
            bitmaps.Add(LoadFile(path));
        }

        bitmaps.Count.ShouldBe(1);
        bitmaps.Single().IsImmutable.ShouldBeTrue();
    }

    [Fact]
    public void LoaderManagerDispose_OfALoadedFilePath_LeavesTheCachedBitmapUsable()
    {
        // GumService.EnableHotReload passes LoaderManager.Dispose(path) as its cache eviction. Skia keeps
        // loaded images in SkiaResourceManager's own cache, which that call does not reach, so an image
        // stays as loaded until the game restarts (docs/code/debugging/hot-reload.md).
        string path = WriteGridPng(GridSize, seed: 0);
        SKBitmap loaded = LoadFile(path);
        GraphicalUiElement runtime = CreateRuntime(nameof(Sprite), loaded, x: 4);
        byte[] before = Render(runtime);

        LoaderManager.Self.Dispose(path);
        SKBitmap reloaded = LoadFile(path);

        reloaded.ShouldBeSameAs(loaded);
        IsDisposed(reloaded).ShouldBeFalse();
        Render(runtime).ShouldBe(before);
    }

    [Fact]
    public void LoadContent_FileChangedOnDiskAfterLoading_KeepsTheLoadedPixels()
    {
        // Documented: "the cached texture is still used. Restart the game to pick up texture changes."
        string path = WriteGridPng(GridSize, seed: 0);
        SKBitmap loaded = LoadFile(path);
        SKColor original = loaded.GetPixel(3, 5);

        using (SKBitmap replacement = CreateGrid(GridSize, seed: 1))
        using (SKImage image = SKImage.FromBitmap(replacement))
        using (SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100))
        {
            File.WriteAllBytes(path, encoded.ToArray());
        }
        SKBitmap reloaded = LoadFile(path);

        reloaded.ShouldBeSameAs(loaded);
        reloaded.GetPixel(3, 5).ShouldBe(original);
        original.ShouldBe(TexelColor(3, 5, seed: 0));
    }

    #endregion

    #region Skia's refcount of the pixels

    [Fact]
    public void ImageOfAnImmutableBitmap_AfterTheBitmapIsDisposed_StillHoldsTheBitmapsPixels()
    {
        // The claim the sharing rests on: SKImage.FromBitmap of an immutable bitmap refs the pixels, so
        // disposing the bitmap does not free them.
        SKBitmap bitmap = CreateGrid(GridSize, seed: 0);
        bitmap.SetImmutable();
        IntPtr bitmapPixels = bitmap.GetPixels();
        SKImage image = SKImage.FromBitmap(bitmap);
        PixelAddress(image).ShouldBe(bitmapPixels);

        bitmap.Dispose();

        IntPtr afterDispose = PixelAddress(image);
        afterDispose.ShouldBe(bitmapPixels);
        using SKSurface surface = SKSurface.Create(new SKImageInfo(GridSize, GridSize));
        surface.Canvas.DrawImage(image, 0, 0);
        using SKImage snapshot = surface.Snapshot();
        using SKBitmap drawn = SKBitmap.FromImage(snapshot);
        drawn.GetPixel(4, 7).ShouldBe(TexelColor(4, 7, seed: 0));
        image.Dispose();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void NativePixels_OfAnImmutableBitmap_AreFreedOnlyAfterEveryImageAndTheBitmapAreDisposed(string name)
    {
        ReleaseFlag released = new();
        SKBitmap bitmap = CreateTrackedBitmap(GridSize, SKColors.Orange, released);
        bitmap.SetImmutable();
        List<RenderableShapeBase> renderables = new();
        for (int i = 0; i < 5; i++)
        {
            renderables.Add(CreateRenderable(name, bitmap));
        }

        bitmap.Dispose();
        released.Released.ShouldBeFalse("the five images still reference the pixels");
        for (int i = 0; i < 4; i++)
        {
            ((IDisposable)renderables[i]).Dispose();
        }
        released.Released.ShouldBeFalse("one image still references the pixels");
        DrawDirect(renderables[4]).Any(value => value != 0).ShouldBeTrue();
        ((IDisposable)renderables[4]).Dispose();

        released.Released.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void NativePixels_OfAnImmutableBitmap_AreFreedWhenTheBitmapIsDisposedLast(string name)
    {
        ReleaseFlag released = new();
        SKBitmap bitmap = CreateTrackedBitmap(GridSize, SKColors.Orange, released);
        bitmap.SetImmutable();
        RenderableShapeBase renderable = CreateRenderable(name, bitmap);

        ((IDisposable)renderable).Dispose();
        released.Released.ShouldBeFalse("the bitmap still references the pixels");
        bitmap.Dispose();

        released.Released.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void NativePixels_OfAMutableBitmap_AreFreedWithTheBitmapWhileRenderablesKeepTheirCopies(string name)
    {
        // A bitmap the caller creates is copied per renderable (the documented limit), so the images own
        // their own pixels and the caller's bitmap frees its pixels on dispose.
        ReleaseFlag released = new();
        SKBitmap bitmap = CreateTrackedBitmap(GridSize, SKColors.Orange, released);
        RenderableShapeBase renderable = CreateRenderable(name, bitmap);

        bitmap.Dispose();

        released.Released.ShouldBeTrue();
        DrawDirect(renderable).Any(value => value != 0).ShouldBeTrue();
        ((IDisposable)renderable).Dispose();
    }

    #endregion

    #region Renderables outliving the bitmap

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Dispose_OfEveryRenderableAndTheBitmapInAnyOrder_DoesNotThrow(string name)
    {
        for (int bitmapPosition = 0; bitmapPosition <= 3; bitmapPosition++)
        {
            SKBitmap sheet = Load(GridSize, seed: 0);
            List<RenderableShapeBase> renderables = Enumerable.Range(0, 3)
                .Select(_ => CreateRenderable(name, sheet)).ToList();

            for (int step = 0; step <= 3; step++)
            {
                if (step == bitmapPosition)
                {
                    sheet.Dispose();
                }
                if (step < 3)
                {
                    ((IDisposable)renderables[step]).Dispose();
                }
            }

            renderables.ForEach(renderable => ((IDisposable)renderable).Dispose());
        }
    }

    #endregion

    #region Pixel data cached by key

    [Fact]
    public void ApplyCached_AfterTheLoaderEvictedTheBitmap_BuildsAFreshUsableBitmap()
    {
        // LoaderManager.Dispose(key) disposes the cached bitmap; renderables built from it keep drawing
        // (their images hold the pixels) and the next apply must not hand the disposed bitmap back.
        string cacheKey = "SharedTextureLifetimeTests_" + Guid.NewGuid().ToString("N");
        byte[] green = new byte[4 * 4 * 4];
        for (int i = 0; i < green.Length; i += 4)
        {
            green[i + 1] = 255;
            green[i + 3] = 255;
        }
        SpriteRuntime first = new();
        SpriteRuntime second = new();
        PixelDataTextureApplier.ApplyCached(first, cacheKey, green, 4, 4);
        SKBitmap firstBitmap = ((Sprite)first.RenderableComponent).Texture!;
        SKImage firstImage = ((Sprite)first.RenderableComponent).Image!;

        LoaderManager.Self.Dispose(cacheKey);
        PixelDataTextureApplier.ApplyCached(second, cacheKey, green, 4, 4);
        SKBitmap secondBitmap = ((Sprite)second.RenderableComponent).Texture!;

        IsDisposed(firstBitmap).ShouldBeTrue();
        IsDisposed(firstImage).ShouldBeFalse();
        using (SKBitmap firstPixels = SKBitmap.FromImage(firstImage))
        {
            firstPixels.GetPixel(1, 1).ShouldBe(SKColors.Lime);
        }
        secondBitmap.ShouldNotBeSameAs(firstBitmap);
        IsDisposed(secondBitmap).ShouldBeFalse();
        secondBitmap.IsImmutable.ShouldBeTrue();
        secondBitmap.GetPixel(1, 1).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void ApplyCached_SameKeyOnTwoSprites_SharesOneBitmapAndOnePixelBuffer()
    {
        string cacheKey = "SharedTextureLifetimeTests_" + Guid.NewGuid().ToString("N");
        byte[] pixels = new byte[4 * 4 * 4];
        SpriteRuntime first = new();
        SpriteRuntime second = new();

        PixelDataTextureApplier.ApplyCached(first, cacheKey, pixels, 4, 4);
        PixelDataTextureApplier.ApplyCached(second, cacheKey, pixels, 4, 4);

        ((Sprite)second.RenderableComponent).Texture.ShouldBeSameAs(((Sprite)first.RenderableComponent).Texture);
        PixelAddress(GetImage(second)).ShouldBe(PixelAddress(GetImage(first)));
        LoaderManager.Self.Dispose(cacheKey);
    }

    #endregion
}
