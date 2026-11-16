using Gum;
using Raylib_cs;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace RaylibGum.Tests.Hot;

/// <summary>
/// Hot reload writes a changed image's pixels into the cached texture, which keeps its GPU id (#8160).
/// </summary>
public class HotReloadReplacedImageTests : BaseTestClass
{
    private readonly string _directory;
    private readonly bool _previousCacheTextures;

    public HotReloadReplacedImageTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "RaylibHotReloadReplacedImage8160_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _previousCacheTextures = LoaderManager.Self.CacheTextures;
        LoaderManager.Self.CacheTextures = true;
    }

    public override void Dispose()
    {
        LoaderManager.Self.CacheTextures = _previousCacheTextures;
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    private string WritePng(string name, int width, int height, Color color, PixelFormat format = PixelFormat.UncompressedR8G8B8A8)
    {
        string path = Path.Combine(_directory, name);
        Image image = Raylib.GenImageColor(width, height, color);
        try
        {
            Raylib.ImageFormat(ref image, format);
            ((bool)Raylib.ExportImage(image, path)).ShouldBeTrue();
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
        return path;
    }

    private static Color PixelOf(Texture2D texture)
    {
        Image image = Raylib.LoadImageFromTexture(texture);
        try
        {
            return Raylib.GetImageColor(image, 1, 1);
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }

    [Fact]
    public void ReplaceCachedTexture_SameSize_WritesTheNewPixelsIntoTheSameTexture()
    {
        string path = WritePng("art.png", 4, 4, Color.Red);
        Texture2D before = LoaderManager.Self.LoadContent<Texture2D>(path);

        WritePng("art.png", 4, 4, Color.Blue);

        global::Gum.GumService.ReplaceCachedTexture(path).ShouldBe(CachedImageReplacement.Replaced);
        Texture2D after = LoaderManager.Self.LoadContent<Texture2D>(path);
        after.Id.ShouldBe(before.Id);
        Color pixel = PixelOf(before);
        (pixel.R, pixel.G, pixel.B, pixel.A).ShouldBe(((byte)0, (byte)121, (byte)241, (byte)255));
    }

    [Fact]
    public void ReplaceCachedTexture_SizeChanged_KeepsTheOldPixels()
    {
        string path = WritePng("art.png", 4, 4, Color.Red);
        Texture2D before = LoaderManager.Self.LoadContent<Texture2D>(path);

        WritePng("art.png", 8, 8, Color.Blue);

        global::Gum.GumService.ReplaceCachedTexture(path).ShouldBe(CachedImageReplacement.SizeChanged);
        PixelOf(before).R.ShouldBe(Color.Red.R);
    }

    [Fact]
    public void ReplaceCachedTexture_PixelFormatChanged_IsNotReplaced()
    {
        string path = WritePng("art.png", 4, 4, Color.Red);
        Texture2D before = LoaderManager.Self.LoadContent<Texture2D>(path);

        WritePng("art.png", 4, 4, Color.Blue, PixelFormat.UncompressedR8G8B8);

        global::Gum.GumService.ReplaceCachedTexture(path).ShouldBe(CachedImageReplacement.NotReplaced);
        PixelOf(before).R.ShouldBe(Color.Red.R);
    }

    [Fact]
    public void ReplaceCachedTexture_FileNotCached_LeavesTheCacheEmpty()
    {
        string path = WritePng("never-loaded.png", 4, 4, Color.Red);
        int cachedBefore = LoaderManager.Self.CachedDisposables.Count;

        global::Gum.GumService.ReplaceCachedTexture(path).ShouldBe(CachedImageReplacement.NotReplaced);

        LoaderManager.Self.CachedDisposables.Count.ShouldBe(cachedBefore);
    }
}
