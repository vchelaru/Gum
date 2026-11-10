using MonoGameGum.Tests;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

// SkiaResourceManager is what the default ContentLoader loads bitmaps through.
#pragma warning disable CS0618

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Several threads loading one file, or drawing renderables that share one loaded bitmap, each on its own
/// CPU surface (#7876). Skia refcounts the shared pixels atomically, so sharing must not tear anything.
/// </summary>
public class SharedTextureConcurrencyTests : SharedTextureTestBase
{
    private static T WaitFor<T>(Task<T> task)
    {
        task.Wait(TimeSpan.FromSeconds(60)).ShouldBeTrue("a thread did not finish");
        return task.Result;
    }

    [Fact]
    public void LoadContent_ManyThreadsOnOneNewFile_AllGetUsableImmutableBitmaps()
    {
        for (int round = 0; round < SweepScale.Cases(40); round++)
        {
            string path = WriteGridPng(GridSize, seed: 0);
            using Barrier start = new(8);
            List<Task<SKBitmap>> loads = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                start.SignalAndWait();
                return LoadFile(path);
            })).ToList();

            foreach (Task<SKBitmap> load in loads)
            {
                SKBitmap bitmap = WaitFor(load);
                bitmap.IsImmutable.ShouldBeTrue();
                IsDisposed(bitmap).ShouldBeFalse();
                bitmap.GetPixel(3, 5).ShouldBe(TexelColor(3, 5, seed: 0));
            }
        }
    }

    [Fact]
    public void ApplyCached_TwoThreadsOnOneKey_BothSpritesGetAnImmutableBitmapOfThePixels()
    {
        byte[] pixels = new byte[4 * 4 * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i + 1] = 255;
            pixels[i + 3] = 255;
        }
        string cacheKey = "SharedTextureConcurrencyTests_" + Guid.NewGuid().ToString("N");
        Gum.GueDeriving.SpriteRuntime first = new();
        Gum.GueDeriving.SpriteRuntime second = new();
        using Barrier start = new(2);

        Task apply(Gum.GueDeriving.SpriteRuntime target) => Task.Run(() =>
        {
            start.SignalAndWait();
            PixelDataTextureApplier.ApplyCached(target, cacheKey, pixels, 4, 4);
        });
        Task[] tasks = { apply(first), apply(second) };
        Task.WaitAll(tasks, TimeSpan.FromSeconds(60)).ShouldBeTrue();

        foreach (Gum.GueDeriving.SpriteRuntime sprite in new[] { first, second })
        {
            SKBitmap texture = ((Sprite)sprite.RenderableComponent).Texture!;
            texture.IsImmutable.ShouldBeTrue();
            texture.GetPixel(1, 1).ShouldBe(SKColors.Lime);
        }
        LoaderManager.Self.Dispose(cacheKey);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Draw_TwoThreadsOnSeparateSurfaces_BothShowTheSharedTextureExactly(string name)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        RenderableShapeBase reference = CreateRenderable(name, sheet);
        byte[] expected = DrawDirect(reference);
        expected.Any(value => value != 0).ShouldBeTrue();
        int iterations = SweepScale.Cases(1000);
        using Barrier start = new(2);

        Task<int> Run() => Task.Run(() =>
        {
            int mismatches = 0;
            start.SignalAndWait();
            for (int i = 0; i < iterations; i++)
            {
                // A fresh renderable each time, so images are created, drawn and disposed from both threads at once.
                RenderableShapeBase renderable = CreateRenderable(name, sheet);
                if (!DrawDirect(renderable).AsSpan().SequenceEqual(expected))
                {
                    mismatches++;
                }
                ((IDisposable)renderable).Dispose();
            }
            return mismatches;
        });
        Task<int> first = Run();
        Task<int> second = Run();

        WaitFor(first).ShouldBe(0);
        WaitFor(second).ShouldBe(0);
        DrawDirect(reference).ShouldBe(expected);
        ((IDisposable)reference).Dispose();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Draw_TwoThreadsOnTheSameRenderableList_BothShowTheSharedTextureExactly(string name)
    {
        // The same long-lived renderables drawn from two threads at once (each into its own canvas).
        SKBitmap sheet = Load(GridSize, seed: 0);
        List<RenderableShapeBase> renderables = Enumerable.Range(0, 20).Select(_ => CreateRenderable(name, sheet)).ToList();
        byte[] expected = DrawDirect(renderables[0]);
        int iterations = SweepScale.Cases(500);
        using Barrier start = new(2);

        Task<int> Run(int offset) => Task.Run(() =>
        {
            int mismatches = 0;
            start.SignalAndWait();
            for (int i = 0; i < iterations; i++)
            {
                // Two renderables never draw on the same thread at the same instant as their twin on the
                // other thread because the offsets differ, which keeps each renderable's cached paint single-threaded.
                RenderableShapeBase renderable = renderables[(i * 2 + offset) % renderables.Count];
                if (!DrawDirect(renderable).AsSpan().SequenceEqual(expected))
                {
                    mismatches++;
                }
            }
            return mismatches;
        });
        Task<int> first = Run(0);
        Task<int> second = Run(1);

        WaitFor(first).ShouldBe(0);
        WaitFor(second).ShouldBe(0);
        renderables.ForEach(renderable => ((IDisposable)renderable).Dispose());
    }
}
