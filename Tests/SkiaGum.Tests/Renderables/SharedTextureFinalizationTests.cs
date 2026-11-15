using MonoGameGum.Tests;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Renderables that share one loaded bitmap are often dropped without being disposed (a screen torn down
/// by clearing its children), so the garbage collector finalizes their images and, later or earlier, the
/// bitmap itself. Skia refcounts the shared pixels, so no order of those finalizers may free pixels that a
/// live image still draws, and the process must survive every order (#7876).
/// </summary>
public class SharedTextureFinalizationTests : SharedTextureTestBase
{
    public enum BitmapEnd
    {
        /// <summary>The bitmap stays alive and referenced while the renderables are collected.</summary>
        StaysAlive,
        /// <summary>The bitmap is disposed first, while images built from it are still referenced.</summary>
        DisposedFirst,
        /// <summary>The bitmap is disposed after the renderables were finalized.</summary>
        DisposedLast,
    }

    public static TheoryData<string, BitmapEnd> Cases
    {
        get
        {
            TheoryData<string, BitmapEnd> data = new();
            foreach (string name in new[] { nameof(Sprite), nameof(NineSlice) })
            {
                foreach (BitmapEnd end in Enum.GetValues<BitmapEnd>())
                {
                    data.Add(name, end);
                }
            }
            return data;
        }
    }

    private sealed class ReleaseFlag
    {
        public volatile bool Released;
    }

    // A bitmap over native memory that reports when Skia frees the pixels (see SharedTextureLifetimeTests).
    private static SKBitmap CreateTrackedBitmap(SKColor color, ReleaseFlag flag)
    {
        SKImageInfo info = new(GridSize, GridSize, SKColorType.Rgba8888, SKAlphaType.Premul);
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
        bitmap.InstallPixels(info, memory, info.RowBytes, (address, context) =>
        {
            Marshal.FreeHGlobal(address);
            flag.Released = true;
        }, null).ShouldBeTrue();
        bitmap.SetImmutable();
        return bitmap;
    }

    private static void CollectFinalizers()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    // Builds and drops garbage in its own frame so none of it is still reachable from the test's locals.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndDrop(string name, SKBitmap bitmap, int count)
    {
        for (int i = 0; i < count; i++)
        {
            RenderableShapeBase renderable = CreateRenderable(name, bitmap);
            if (i % 50 == 0)
            {
                DrawDirect(renderable);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<RenderableShapeBase> CreateSurvivors(string name, SKBitmap bitmap, int count) =>
        Enumerable.Range(0, count).Select(_ => CreateRenderable(name, bitmap)).ToList();

    [Theory]
    [MemberData(nameof(Cases))]
    public void Finalizers_OfDroppedRenderables_LeaveTheSurvivorsDrawingTheSamePixels(string name, BitmapEnd end)
    {
        SKBitmap bitmap = Load(GridSize, seed: 0);
        List<RenderableShapeBase> survivors = CreateSurvivors(name, bitmap, count: 3);
        byte[] expected = DrawDirect(survivors[0]);
        expected.Any(value => value != 0).ShouldBeTrue();

        for (int round = 0; round < SweepScale.Cases(30); round++)
        {
            // Once the bitmap is disposed no new renderable can be built from it, so the later rounds only
            // collect what the earlier rounds dropped.
            if (end == BitmapEnd.DisposedFirst && round == 1)
            {
                bitmap.Dispose();
            }
            if (bitmap.Handle != IntPtr.Zero)
            {
                CreateAndDrop(name, bitmap, count: 200);
            }
            CollectFinalizers();

            foreach (RenderableShapeBase survivor in survivors)
            {
                DrawDirect(survivor).ShouldBe(expected);
            }
        }

        if (end == BitmapEnd.DisposedLast)
        {
            bitmap.Dispose();
            CollectFinalizers();
            DrawDirect(survivors[1]).ShouldBe(expected);
        }
        survivors.ForEach(survivor => ((IDisposable)survivor).Dispose());
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Finalizers_OfEveryRenderableAndTheBitmap_FreeTheNativePixelsExactlyOnce(string name, BitmapEnd end)
    {
        ReleaseFlag released = new();
        SKBitmap bitmap = CreateTrackedBitmap(SKColors.Orange, released);
        CreateAndDrop(name, bitmap, count: 200);

        if (end == BitmapEnd.DisposedFirst)
        {
            bitmap.Dispose();
        }
        CollectFinalizers();
        if (end == BitmapEnd.DisposedLast)
        {
            released.Released.ShouldBeFalse("the bitmap object still references its pixels");
            bitmap.Dispose();
        }
        if (end == BitmapEnd.StaysAlive)
        {
            released.Released.ShouldBeFalse("the bitmap object still references its pixels");
            GC.KeepAlive(bitmap);
            bitmap.Dispose();
        }

        released.Released.ShouldBeTrue();
        CollectFinalizers();
    }

    [Fact]
    public void Finalizers_OfAnUndisposedBitmapAndItsRenderables_FreeTheNativePixels()
    {
        // Nothing is disposed: the whole object graph is garbage, as after a screen is torn down.
        ReleaseFlag released = new();
        CreateTrackedGarbage(released);

        for (int round = 0; round < 10 && !released.Released; round++)
        {
            CollectFinalizers();
        }

        released.Released.ShouldBeTrue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateTrackedGarbage(ReleaseFlag released)
    {
        SKBitmap bitmap = CreateTrackedBitmap(SKColors.Orange, released);
        CreateAndDrop(nameof(Sprite), bitmap, count: 50);
        CreateAndDrop(nameof(NineSlice), bitmap, count: 50);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Finalizers_WhileAnotherThreadDraws_DoNotCorruptItsPixels(string name)
    {
        SKBitmap bitmap = Load(GridSize, seed: 0);
        RenderableShapeBase drawn = CreateRenderable(name, bitmap);
        byte[] expected = DrawDirect(drawn);
        int drawCount = SweepScale.Cases(1000);
        int mismatches = 0;
        Exception? failure = null;
        using ManualResetEventSlim started = new();

        Thread drawer = new(() =>
        {
            try
            {
                started.Set();
                for (int i = 0; i < drawCount; i++)
                {
                    if (!DrawDirect(drawn).AsSpan().SequenceEqual(expected))
                    {
                        Interlocked.Increment(ref mismatches);
                    }
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        drawer.Start();
        started.Wait();
        for (int round = 0; round < SweepScale.Cases(30); round++)
        {
            CreateAndDrop(name, bitmap, count: 100);
            CollectFinalizers();
        }
        drawer.Join(TimeSpan.FromSeconds(60)).ShouldBeTrue();

        failure.ShouldBeNull();
        mismatches.ShouldBe(0);
        ((IDisposable)drawn).Dispose();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void ImagesOfADisposedBitmap_AfterFinalizersRun_StillDrawTheBitmapsPixels(string name)
    {
        // Images derived from the bitmap are still referenced when the bitmap is disposed and the
        // finalizers of everything else run.
        SKBitmap bitmap = Load(GridSize, seed: 0);
        RenderableShapeBase kept = CreateRenderable(name, bitmap);
        byte[] expected = DrawDirect(kept);
        CreateAndDrop(name, bitmap, count: 100);

        bitmap.Dispose();
        CollectFinalizers();

        DrawDirect(kept).ShouldBe(expected);
        expected.Any(value => value != 0).ShouldBeTrue();
        ((IDisposable)kept).Dispose();
    }
}
