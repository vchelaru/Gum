using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using SkiaSharp;

// SkiaResourceManager is what the default ContentLoader loads bitmaps through.
#pragma warning disable CS0618

namespace SkiaGum.GpuTests;

/// <summary>
/// #7876: NineSlices and Sprites over one loaded sprite sheet must share one GPU texture. A mutable bitmap is copied
/// by Skia for every renderable, so 200 of them filled Skia's 256 MiB GPU resource cache, thrashed it every frame and
/// cost ~18 ms per Draw. Asserts how the GPU resource cache scales with the renderable count (relative, no timings),
/// so it holds on software GL as well as a real GPU.
/// </summary>
[Collection(SharedGpuCollection.Name)]
public class SharedTextureGpuCacheTests : IDisposable
{
    const int SheetSize = 1024;
    const long SheetBytes = (long)SheetSize * SheetSize * 4;
    const int Frames = 5;

    readonly SharedGpuContext _gpu;
    readonly string _directory;

    public SharedTextureGpuCacheTests(SharedGpuContext gpu)
    {
        _gpu = gpu;
        _directory = Path.Combine(Path.GetTempPath(), "GumSharedTextureGpu_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    public static TheoryData<string> RenderableNames => new() { "NineSlice", "Sprite" };

    // Loaded through Gum's ContentLoader, because only a texture Gum loads is shared (#7876).
    SKBitmap LoadSheet()
    {
        string path = Path.Combine(_directory, "sheet.png");
        using (SKBitmap source = new(SheetSize, SheetSize))
        {
            for (int y = 0; y < SheetSize; y++)
            {
                for (int x = 0; x < SheetSize; x++)
                {
                    source.SetPixel(x, y, new SKColor((byte)(x / 4), (byte)(y / 4), (byte)((x ^ y) & 255)));
                }
            }
            using SKImage image = SKImage.FromBitmap(source);
            using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, encoded.ToArray());
        }
        return new ContentLoader().LoadContent<SKBitmap>(path)!;
    }

    static GraphicalUiElement Create(string name, SKBitmap sheet, int index)
    {
        GraphicalUiElement element = name == "Sprite"
            ? new SpriteRuntime { Texture = sheet }
            : new NineSliceRuntime { Texture = sheet };
        const int columns = 20;
        element.X = index % columns * 48 + 2;
        element.Y = index / columns % 15 * 40 + 2;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = 44;
        element.Height = 36;
        return element;
    }

    // Draws `count` renderables sharing `sheet` for a few frames and returns the GPU resource cache afterwards.
    (int Resources, long Bytes) MeasureCache(string name, SKBitmap sheet, int count)
    {
        int resources = 0;
        long bytes = 0;
        _gpu.WithSurface(1024, 640, (surface, context) =>
        {
            context.PurgeResources();
            GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            var managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = surface.Canvas;
            var elements = new List<GraphicalUiElement>();
            for (int i = 0; i < count; i++)
            {
                GraphicalUiElement element = Create(name, sheet, i);
                element.AddToManagers(managers, null);
                elements.Add(element);
            }
            for (int frame = 0; frame < Frames; frame++)
            {
                managers.Draw();
                context.Flush(true, true);
            }
            context.GetResourceCacheUsage(out resources, out bytes);
            foreach (GraphicalUiElement element in elements)
            {
                element.RemoveFromManagers();
            }
        });
        return (resources, bytes);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Draw_ManyRenderablesFromOneLoadedTexture_ShouldNotScaleGpuCacheWithCount(string name)
    {
        SKBitmap sheet = LoadSheet();
        (int Resources, long Bytes) few = MeasureCache(name, sheet, 20);
        (int Resources, long Bytes) many = MeasureCache(name, sheet, 200);
        // Shared: 20 and 200 renderables use the same few GPU resources (the one sheet texture and Skia's own).
        // Copied per renderable: the count and bytes grow with the renderable count until they hit the cache limit.
        many.Resources.ShouldBeLessThanOrEqualTo(few.Resources + 2,
            $"{name}: 200 renderables hold {many.Resources} GPU resources, 20 hold {few.Resources}; each renderable owns a texture copy");
        many.Bytes.ShouldBeLessThanOrEqualTo(few.Bytes + SheetBytes / 4,
            $"{name}: 200 renderables hold {many.Bytes} GPU bytes, 20 hold {few.Bytes}; each renderable owns a texture copy");
        many.Bytes.ShouldBeLessThan(3 * SheetBytes,
            $"{name}: {many.Bytes} GPU bytes for one {SheetBytes} byte sheet; it is uploaded more than once");
    }
}
