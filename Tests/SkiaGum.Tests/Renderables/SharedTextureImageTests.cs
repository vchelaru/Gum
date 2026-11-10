using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Diagnostics;

// SkiaResourceManager is what the default ContentLoader loads bitmaps through.
#pragma warning disable CS0618

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Many NineSlices and Sprites draw out of one loaded sprite sheet. Skia copies a mutable bitmap when
/// it builds an image from it, so each renderable used to own a private copy of the whole sheet; on a
/// GPU surface those copies overflow Skia's texture cache every frame (#7876). A texture loaded by
/// Gum is immutable, so every renderable's image shares the loaded bitmap's one pixel buffer. A bitmap
/// the caller creates stays mutable and keeps a snapshot per renderable.
/// </summary>
public class SharedTextureImageTests : BaseTestClass
{
    private const int GridSize = 12;
    private readonly string _directory;

    public SharedTextureImageTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GumSkiaSharedTexture_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public override void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    public static TheoryData<string> RenderableNames => new() { nameof(Sprite), nameof(NineSlice) };

    // A grid of distinct colors, so a wrongly shared or wrongly sourced texture changes the pixels.
    private static SKColor TexelColor(int x, int y, int seed) =>
        new((byte)(30 + x * 18), (byte)(30 + y * 18), (byte)(40 + seed * 90));

    private static SKBitmap CreateGrid(int size, int seed)
    {
        SKBitmap bitmap = new(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bitmap.SetPixel(x, y, TexelColor(x, y, seed));
            }
        }
        return bitmap;
    }

    private SKBitmap Load(int size, int seed)
    {
        string path = Path.Combine(_directory, $"sheet_{size}_{seed}_{Guid.NewGuid():N}.png");
        using (SKBitmap source = CreateGrid(size, seed))
        using (SKImage image = SKImage.FromBitmap(source))
        using (SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100))
        using (FileStream stream = File.OpenWrite(path))
        {
            encoded.SaveTo(stream);
        }
        return new ContentLoader().LoadContent<SKBitmap>(path)!;
    }

    private static GraphicalUiElement CreateRuntime(string name, SKBitmap texture, float x)
    {
        GraphicalUiElement runtime;
        if (name == nameof(Sprite))
        {
            runtime = new SpriteRuntime { Texture = texture };
        }
        else
        {
            runtime = new NineSliceRuntime { Texture = texture, BorderScale = 3 };
        }
        runtime.X = x;
        runtime.Y = 4;
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.Width = 30;
        runtime.Height = 30;
        return runtime;
    }

    private static SKImage GetImage(GraphicalUiElement runtime) => runtime.RenderableComponent switch
    {
        Sprite sprite => sprite.Image!,
        NineSlice nineSlice => nineSlice.Image!,
        _ => throw new ArgumentException(runtime.RenderableComponent.GetType().Name),
    };

    private static void SetTexture(GraphicalUiElement runtime, SKBitmap? texture)
    {
        if (runtime is SpriteRuntime sprite) sprite.Texture = texture;
        else ((NineSliceRuntime)runtime).Texture = texture;
    }

    private static bool IsDisposed(SKObject skObject) => skObject.Handle == IntPtr.Zero;

    private static IntPtr PixelAddress(SKImage image)
    {
        using SKPixmap? pixmap = image.PeekPixels();
        return pixmap?.GetPixels() ?? IntPtr.Zero;
    }

    private static byte[] Render(params GraphicalUiElement[] elements)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(160, 40));
        GumService.Default.Initialize(surface.Canvas, 160, 40);
        foreach (GraphicalUiElement element in elements)
        {
            element.Parent = null;
            GumService.Default.Root.Children.Add(element);
        }

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    // Guard: fails when each renderable copies the texture.

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Renderables_FromOneLoadedTexture_ShouldShareOnePixelBuffer(string name)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        List<GraphicalUiElement> runtimes = new();
        for (int i = 0; i < 100; i++)
        {
            runtimes.Add(CreateRuntime(name, sheet, x: 4));
        }

        HashSet<IntPtr> buffers = new(runtimes.Select(runtime => PixelAddress(GetImage(runtime))));

        buffers.Count.ShouldBe(1);
        buffers.Single().ShouldBe(sheet.GetPixels());
    }

    [Fact]
    public void NineSlicesAndSprites_FromOneLoadedTexture_ShouldShareOnePixelBuffer()
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement sprite = CreateRuntime(nameof(Sprite), sheet, x: 4);
        GraphicalUiElement nineSlice = CreateRuntime(nameof(NineSlice), sheet, x: 40);

        PixelAddress(GetImage(sprite)).ShouldBe(PixelAddress(GetImage(nineSlice)));
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Clone_OfARenderableWithALoadedTexture_ShouldShareThePixelBuffer(string name)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(name, sheet, x: 4);

        RenderableShapeBase clone = (RenderableShapeBase)((ICloneable)runtime.RenderableComponent).Clone();

        IntPtr cloneBuffer = PixelAddress(clone is Sprite sprite ? sprite.Image! : ((NineSlice)clone).Image!);
        cloneBuffer.ShouldBe(PixelAddress(GetImage(runtime)));
    }

    [Fact]
    public void LoadedTextures_ShouldBeImmutable_WhateverTheLoadPath()
    {
        SKBitmap fromFile = Load(GridSize, seed: 0);
        SystemManagers systemManagers = new();
        systemManagers.Initialize();
        SKBitmap embedded = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png")!;
        SpriteRuntime target = new();
        string cacheKey = "SharedTextureImageTests_" + Guid.NewGuid().ToString("N");

        PixelDataTextureApplier.ApplyCached(target, cacheKey, new byte[4 * 4 * 4], 4, 4);
        SKBitmap cachedPixelData = ((Sprite)target.RenderableComponent).Texture!;

        fromFile.IsImmutable.ShouldBeTrue();
        embedded.IsImmutable.ShouldBeTrue();
        cachedPixelData.IsImmutable.ShouldBeTrue();
    }

    [Fact]
    public void RenderablesFromOneLoadedSheet_ShouldNotRetainACopyOfTheSheetEach()
    {
        const int sheetSize = 1024;
        const int renderableCount = 100;
        long sheetBytes = (long)sheetSize * sheetSize * 4;
        SKBitmap sheet = Load(sheetSize, seed: 0);
        GC.Collect();
        Process process = Process.GetCurrentProcess();
        process.Refresh();
        long before = process.WorkingSet64;

        List<GraphicalUiElement> runtimes = new();
        for (int i = 0; i < renderableCount; i++)
        {
            runtimes.Add(CreateRuntime(i % 2 == 0 ? nameof(Sprite) : nameof(NineSlice), sheet, x: 4));
        }
        process.Refresh();
        long retained = process.WorkingSet64 - before;

        // A private copy each would retain renderableCount * sheetBytes (400 MiB); sharing retains none.
        retained.ShouldBeLessThan(sheetBytes * 10);
        GC.KeepAlive(runtimes);
    }

    // Characterization: behavior that must not change.

    [Fact]
    public void LoadedTexture_ShouldDrawIdenticallyToAMutableCopyOfIt()
    {
        SKBitmap loaded = Load(GridSize, seed: 0);
        using SKBitmap mutableCopy = loaded.Copy();
        mutableCopy.IsImmutable.ShouldBeFalse();

        byte[] fromLoaded = Render(
            CreateRuntime(nameof(NineSlice), loaded, x: 4),
            CreateRuntime(nameof(Sprite), loaded, x: 40),
            CreateRuntime(nameof(NineSlice), loaded, x: 76));
        byte[] fromCopy = Render(
            CreateRuntime(nameof(NineSlice), mutableCopy, x: 4),
            CreateRuntime(nameof(Sprite), mutableCopy, x: 40),
            CreateRuntime(nameof(NineSlice), mutableCopy, x: 76));

        fromLoaded.ShouldBe(fromCopy);
        fromLoaded.Any(value => value != 0).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Dispose_OfOneRenderable_ShouldNotBreakAnotherSharingTheTexture(string name)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement kept = CreateRuntime(name, sheet, x: 4);
        GraphicalUiElement disposed = CreateRuntime(name, sheet, x: 80);
        byte[] before = Render(kept);
        SKImage disposedImage = GetImage(disposed);

        ((IDisposable)disposed.RenderableComponent).Dispose();
        byte[] after = Render(kept);

        IsDisposed(disposedImage).ShouldBeTrue();
        IsDisposed(GetImage(kept)).ShouldBeFalse();
        IsDisposed(sheet).ShouldBeFalse();
        after.ShouldBe(before);
        before.Any(value => value != 0).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Texture_ChangedToAnotherAndBack_ShouldDrawTheOriginalPixels(string name)
    {
        SKBitmap first = Load(GridSize, seed: 0);
        SKBitmap second = Load(GridSize, seed: 1);
        GraphicalUiElement runtime = CreateRuntime(name, first, x: 4);
        byte[] original = Render(runtime);

        SetTexture(runtime, second);
        byte[] other = Render(runtime);
        SetTexture(runtime, first);
        byte[] back = Render(runtime);

        other.ShouldNotBe(original);
        back.ShouldBe(original);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Clone_ShouldSurviveTheOriginalBeingDisposed(string name)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement original = CreateRuntime(name, sheet, x: 4);

        RenderableShapeBase clone = (RenderableShapeBase)((ICloneable)original.RenderableComponent).Clone();
        ((IDisposable)original.RenderableComponent).Dispose();
        SKImage cloneImage = clone is Sprite sprite ? sprite.Image! : ((NineSlice)clone).Image!;

        IsDisposed(cloneImage).ShouldBeFalse();
        using SKBitmap pixels = SKBitmap.FromImage(cloneImage);
        pixels.GetPixel(3, 5).ShouldBe(TexelColor(3, 5, seed: 0));
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void RenderablesAlreadyBuilt_ShouldKeepDrawing_WhenTheBitmapIsDisposed(string name)
    {
        // Content unload (hot reload, LoaderManager.DisposeAndClear) disposes the cached bitmap while
        // renderables built from it may still be on screen for a frame.
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(name, sheet, x: 4);
        byte[] before = Render(runtime);

        sheet.Dispose();
        byte[] after = Render(runtime);

        after.ShouldBe(before);
        before.Any(value => value != 0).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void CallerOwnedBitmap_EditedAndReassigned_ShouldShowTheEdit(string name)
    {
        using SKBitmap bitmap = CreateGrid(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(name, bitmap, x: 4);
        byte[] before = Render(runtime);

        bitmap.Erase(SKColors.Magenta);
        bitmap.NotifyPixelsChanged();
        SetTexture(runtime, bitmap);
        byte[] after = Render(runtime);

        bitmap.IsImmutable.ShouldBeFalse();
        after.ShouldNotBe(before);
    }

    [Fact]
    public void PooledPixelData_UploadedAgain_ShouldGiveTheSpriteAnImageOfTheNewPixels()
    {
        // The Forms color picker re-uploads one pooled bitmap as its hue changes, so the bitmap stays
        // mutable and each upload must reach the sprite's image.
        SpriteRuntime owner = new();
        SpriteRuntime target = new();
        byte[] red = new byte[4 * 4 * 4];
        byte[] blue = new byte[4 * 4 * 4];
        for (int i = 0; i < red.Length; i += 4)
        {
            red[i] = 255; red[i + 3] = 255;
            blue[i + 2] = 255; blue[i + 3] = 255;
        }

        PixelDataTextureApplier.ApplyPooled(target, owner, red, 4, 4);
        SKColor first = ReadFirstPixel(GetImage(target));
        PixelDataTextureApplier.ApplyPooled(target, owner, blue, 4, 4);
        SKColor second = ReadFirstPixel(GetImage(target));

        ((Sprite)target.RenderableComponent).Texture!.IsImmutable.ShouldBeFalse();
        first.ShouldBe(SKColors.Red);
        second.ShouldBe(SKColors.Blue);
    }

    private static SKColor ReadFirstPixel(SKImage image)
    {
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(0, 0);
    }
}
