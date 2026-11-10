using Shouldly;
using SkiaGum.Content;
using SkiaSharp;
using ToolsUtilities;

// SkiaResourceManager is what the default ContentLoader loads bitmaps through.
#pragma warning disable CS0618

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// What a load returns after the caller disposed the bitmap the loaded-bitmap cache holds (#7886): the cache
/// treats a disposed entry as a miss, drops it and loads the file again.
/// </summary>
public class SharedTextureDisposedCacheTests : SharedTextureTestBase
{
    [Fact]
    public void LoadContent_AfterTheCachedBitmapWasDisposed_ReturnsAUsableBitmap()
    {
        string path = WriteGridPng(GridSize, seed: 0);
        LoadFile(path).Dispose();

        SKBitmap reloaded = LoadFile(path);

        reloaded.Handle.ShouldNotBe(IntPtr.Zero);
        reloaded.Width.ShouldBe(GridSize);
    }

    [Fact]
    public void LoadContent_AfterTheCachedBitmapWasDisposed_ReturnsTheFilesPixelsAndSharesThemAgain()
    {
        string path = WriteGridPng(GridSize, seed: 0);
        LoadFile(path).Dispose();

        SKBitmap reloaded = LoadFile(path);

        // Reading a disposed bitmap aborts the process, so this is checked first.
        IsDisposed(reloaded).ShouldBeFalse();
        reloaded.IsImmutable.ShouldBeTrue();
        reloaded.GetPixel(3, 5).ShouldBe(TexelColor(3, 5, seed: 0));
        LoadFile(path).ShouldBeSameAs(reloaded);
    }

    [Fact]
    public void LoadContent_ABitmapThatWasNeverDisposed_StaysCachedAndIsNotLoadedAgain()
    {
        string path = WriteGridPng(GridSize, seed: 0);
        SKBitmap loaded = LoadFile(path);
        int hookCalls = 0;
        FileManager.CustomGetStreamFromFile = name =>
        {
            Interlocked.Increment(ref hookCalls);
            return File.OpenRead(name);
        };

        try
        {
            LoadFile(path).ShouldBeSameAs(loaded);
        }
        finally
        {
            FileManager.CustomGetStreamFromFile = null;
        }

        hookCalls.ShouldBe(0);
        IsDisposed(loaded).ShouldBeFalse();
    }

    [Fact]
    public void LoadContent_ABitmapDisposedTwice_ReloadsOnce()
    {
        string path = WriteGridPng(GridSize, seed: 0);
        SKBitmap loaded = LoadFile(path);
        loaded.Dispose();
        loaded.Dispose();

        SKBitmap reloaded = LoadFile(path);

        reloaded.ShouldNotBeSameAs(loaded);
        IsDisposed(reloaded).ShouldBeFalse();
        LoadFile(path).ShouldBeSameAs(reloaded);
    }

    [Fact]
    public void LoadContent_ABitmapDisposedAgainAfterAReload_ReloadsAgain()
    {
        string path = WriteGridPng(GridSize, seed: 0);
        LoadFile(path).Dispose();
        SKBitmap secondLoad = LoadFile(path);
        secondLoad.Dispose();

        SKBitmap thirdLoad = LoadFile(path);

        IsDisposed(thirdLoad).ShouldBeFalse();
        thirdLoad.GetPixel(3, 5).ShouldBe(TexelColor(3, 5, seed: 0));
    }

    [Fact]
    public void IsCached_AfterTheCachedBitmapWasDisposed_ReturnsFalse()
    {
        string path = WriteGridPng(GridSize, seed: 0);
        SKBitmap loaded = LoadFile(path);
        SkiaResourceManager.IsCached(path).ShouldBeTrue();

        loaded.Dispose();

        SkiaResourceManager.IsCached(path).ShouldBeFalse();
        LoadFile(path);
        SkiaResourceManager.IsCached(path).ShouldBeTrue();
    }

    [Fact]
    public void GetSKBitmapFromUrl_AfterTheCachedBitmapWasDisposed_DownloadsAgainInsteadOfReturningTheDisposedBitmap()
    {
        // Nothing listens on this port, so the download fails and the call returns null. A cache hit on
        // the disposed entry would return that disposed bitmap instead.
        string url = "http://127.0.0.1:1/" + Guid.NewGuid().ToString("N") + ".png";
        using MemoryStream stream = new(File.ReadAllBytes(WriteGridPng(GridSize, seed: 0)));
        SKBitmap cached = SkiaResourceManager.GetSKBitmap(url, stream);
        SkiaResourceManager.GetSKBitmapFromUrl(url).ShouldBeSameAs(cached);

        cached.Dispose();

        SkiaResourceManager.GetSKBitmapFromUrl(url).ShouldBeNull();
        SkiaResourceManager.IsCached(url).ShouldBeFalse();
    }

    [Fact]
    public void GetSKBitmap_ThreadsReloadingADisposedEntryTogether_ShareOneBitmapAndDecodeOnce()
    {
        string path = WriteGridPng(GridSize, seed: 0);
        LoadFile(path).Dispose();
        int hookCalls = 0;
        FileManager.CustomGetStreamFromFile = name =>
        {
            Interlocked.Increment(ref hookCalls);
            return new MemoryStream(File.ReadAllBytes(name));
        };
        const int threadCount = 8;
        using Barrier start = new(threadCount);

        SKBitmap[] results;
        try
        {
            Task<SKBitmap>[] tasks = Enumerable.Range(0, threadCount)
                .Select(_ => Task.Run(() =>
                {
                    start.SignalAndWait();
                    return SkiaResourceManager.GetSKBitmap(path);
                }))
                .ToArray();
            Task.WaitAll(tasks, TimeSpan.FromSeconds(60)).ShouldBeTrue("a thread did not finish");
            results = tasks.Select(task => task.Result).ToArray();
        }
        finally
        {
            FileManager.CustomGetStreamFromFile = null;
        }

        results.Distinct().Count().ShouldBe(1);
        IsDisposed(results[0]).ShouldBeFalse();
        hookCalls.ShouldBe(1);
    }

    [Fact]
    public void GetSKBitmap_ADisposedEntryWhoseFileCannotBeDecodedAnymore_ThrowsAndLeavesNothingCached()
    {
        string path = WriteGridPng(GridSize, seed: 0);
        byte[] original = File.ReadAllBytes(path);
        LoadFile(path).Dispose();
        File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });

        Should.Throw<FileNotFoundException>(() => SkiaResourceManager.GetSKBitmap(path));

        SkiaResourceManager.IsCached(path).ShouldBeFalse();

        File.WriteAllBytes(path, original);
        SKBitmap recovered = SkiaResourceManager.GetSKBitmap(path);
        IsDisposed(recovered).ShouldBeFalse();
        SkiaResourceManager.GetSKBitmap(path).ShouldBeSameAs(recovered);
    }
}
