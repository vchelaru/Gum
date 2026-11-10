using MonoGameGum.Tests;
using Shouldly;
using SkiaGum.Content;
using SkiaSharp;
using ToolsUtilities;

// SkiaResourceManager is what the default ContentLoader loads bitmaps through.
#pragma warning disable CS0618

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Two threads loading one name at the same time must end up sharing one cached bitmap (#7876).
/// </summary>
public class SharedTextureCacheRaceTests : SharedTextureTestBase
{
    // Blocks the first read until the test releases it, so one load is held inside the decode.
    private sealed class GatedStream : MemoryStream
    {
        private readonly ManualResetEventSlim _entered;
        private readonly ManualResetEventSlim _release;
        private int _gated;

        public GatedStream(byte[] bytes, ManualResetEventSlim entered, ManualResetEventSlim release) : base(bytes)
        {
            _entered = entered;
            _release = release;
        }

        private void Gate()
        {
            if (Interlocked.Exchange(ref _gated, 1) == 0)
            {
                _entered.Set();
                _release.Wait(TimeSpan.FromMilliseconds(500));
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Gate();
            return base.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            Gate();
            return base.Read(buffer);
        }

        public override int ReadByte()
        {
            Gate();
            return base.ReadByte();
        }
    }

    private static T WaitFor<T>(Task<T> task)
    {
        task.Wait(TimeSpan.FromSeconds(60)).ShouldBeTrue("a thread did not finish");
        return task.Result;
    }

    [Fact]
    public void GetSKBitmap_TwoLoadsOfOneNameOverlappingInTheDecode_ReturnOneBitmap()
    {
        // Thread A is held inside the decode while thread B loads the same name to completion. The cache is
        // then written twice; every caller must still end up with the one bitmap the cache holds, or the
        // renderables built from them no longer share a pixel buffer.
        string path = WriteGridPng(GridSize, seed: 0);
        byte[] bytes = File.ReadAllBytes(path);
        string name = "overlapping_" + Guid.NewGuid().ToString("N");
        using ManualResetEventSlim aInsideDecode = new();
        using ManualResetEventSlim bFinished = new();

        Task<SKBitmap> a = Task.Run(() =>
            SkiaResourceManager.GetSKBitmap(name, new GatedStream(bytes, aInsideDecode, bFinished)));
        aInsideDecode.Wait(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        SKBitmap fromB = SkiaResourceManager.GetSKBitmap(name, new MemoryStream(bytes));
        bFinished.Set();
        SKBitmap fromA = WaitFor(a);

        fromA.ShouldBeSameAs(fromB);
        SkiaResourceManager.GetSKBitmap(name).ShouldBeSameAs(fromB);
        fromA.IsImmutable.ShouldBeTrue();
        IsDisposed(fromA).ShouldBeFalse();
        IsDisposed(fromB).ShouldBeFalse();
    }

    [Fact]
    public void GetSKBitmap_TwoLoadsOfOneFileOverlappingInTheDecode_ReturnOneBitmap()
    {
        // The same race through the file-loading path (the one a SourceFile takes), with the first read of
        // the file held open by the stream hook.
        string path = WriteGridPng(GridSize, seed: 0);
        byte[] bytes = File.ReadAllBytes(path);
        using ManualResetEventSlim aInsideDecode = new();
        using ManualResetEventSlim bFinished = new();
        int hookCalls = 0;
        FileManager.CustomGetStreamFromFile = _ => Interlocked.Increment(ref hookCalls) == 1
            ? new GatedStream(bytes, aInsideDecode, bFinished)
            : new MemoryStream(bytes);

        Task<SKBitmap> a = Task.Run(() => SkiaResourceManager.GetSKBitmap(path));
        aInsideDecode.Wait(TimeSpan.FromSeconds(30)).ShouldBeTrue();
        SKBitmap fromB = SkiaResourceManager.GetSKBitmap(path);
        bFinished.Set();
        SKBitmap fromA = WaitFor(a);

        fromA.ShouldBeSameAs(fromB);
        IsDisposed(fromA).ShouldBeFalse();
    }
}
