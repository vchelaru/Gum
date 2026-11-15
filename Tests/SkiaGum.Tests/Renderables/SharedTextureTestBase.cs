using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

// SkiaResourceManager is what the default ContentLoader loads bitmaps through.
#pragma warning disable CS0618

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Shared arrangement for the tests of how a Skia texture that Gum loads is shared by many Sprites and
/// NineSlices (#7876): a temp folder of generated sprite sheets, a Sprite or NineSlice over a bitmap, and a
/// CPU raster render. Each test declares the values it asserts against.
/// </summary>
public abstract class SharedTextureTestBase : BaseTestClass
{
    protected const int GridSize = 12;
    protected const int SurfaceWidth = 160;
    protected const int SurfaceHeight = 40;

    protected readonly string Directory;

    protected SharedTextureTestBase()
    {
        Directory = Path.Combine(Path.GetTempPath(), "GumSkiaSharedTexture_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
    }

    public override void Dispose()
    {
        try { System.IO.Directory.Delete(Directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    public static TheoryData<string> RenderableNames => new() { nameof(Sprite), nameof(NineSlice) };

    // A grid of distinct colors, so a wrongly shared or wrongly sourced texture changes the pixels.
    protected static SKColor TexelColor(int x, int y, int seed) =>
        new((byte)(30 + x * 18), (byte)(30 + y * 18), (byte)(40 + seed * 90));

    protected static SKBitmap CreateGrid(int size, int seed)
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

    /// <summary>Writes a PNG of the grid into the temp folder and returns its absolute path.</summary>
    protected string WriteGridPng(int size, int seed)
    {
        string path = Path.Combine(Directory, $"sheet_{size}_{seed}_{Guid.NewGuid():N}.png");
        using SKBitmap source = CreateGrid(size, seed);
        using SKImage image = SKImage.FromBitmap(source);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream stream = File.OpenWrite(path);
        encoded.SaveTo(stream);
        return path;
    }

    protected static SKBitmap LoadFile(string path) => new ContentLoader().LoadContent<SKBitmap>(path)!;

    protected SKBitmap Load(int size, int seed) => LoadFile(WriteGridPng(size, seed));

    protected static GraphicalUiElement CreateRuntime(string name, SKBitmap? texture, float x)
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

    /// <summary>A bare renderable, with no GraphicalUiElement or global state, for drawing off the test thread.</summary>
    protected static RenderableShapeBase CreateRenderable(string name, SKBitmap texture)
    {
        if (name == nameof(Sprite))
        {
            return new Sprite { Texture = texture };
        }
        return new NineSlice { Texture = texture, BorderScale = 3 };
    }

    protected static SKImage GetImage(GraphicalUiElement runtime) => GetImage(runtime.RenderableComponent!);

    protected static SKImage GetImage(object renderable) => renderable switch
    {
        Sprite sprite => sprite.Image!,
        NineSlice nineSlice => nineSlice.Image!,
        _ => throw new ArgumentException(renderable.GetType().Name),
    };

    protected static void SetTexture(GraphicalUiElement runtime, SKBitmap? texture)
    {
        if (runtime is SpriteRuntime sprite) sprite.Texture = texture;
        else ((NineSliceRuntime)runtime).Texture = texture;
    }

    protected static void SetTexture(RenderableShapeBase renderable, SKBitmap? texture)
    {
        if (renderable is Sprite sprite) sprite.Texture = texture;
        else ((NineSlice)renderable).Texture = texture;
    }

    protected static bool IsDisposed(SKObject skObject) => skObject.Handle == IntPtr.Zero;

    protected static IntPtr PixelAddress(SKImage image)
    {
        using SKPixmap? pixmap = image.PeekPixels();
        return pixmap?.GetPixels() ?? IntPtr.Zero;
    }

    /// <summary>Renders the runtimes through the full Gum draw on a CPU raster surface.</summary>
    protected static byte[] Render(params GraphicalUiElement[] elements)
    {
        using SKBitmap bitmap = RenderBitmap(elements);
        return bitmap.Bytes;
    }

    /// <summary>Draws one renderable straight into a 30x30 area of its own CPU surface.</summary>
    protected static byte[] DrawDirect(RenderableShapeBase renderable)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(30, 30));
        renderable.DrawBound(new SKRect(0, 0, 30, 30), surface.Canvas, 0);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    /// <summary>Renders the runtimes and returns the color at (x, y).</summary>
    protected static SKColor ColorAt(int x, int y, params GraphicalUiElement[] elements)
    {
        using SKBitmap bitmap = RenderBitmap(elements);
        return bitmap.GetPixel(x, y);
    }

    protected static SKBitmap RenderBitmap(params GraphicalUiElement[] elements)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(SurfaceWidth, SurfaceHeight));
        GumService.Default.Initialize(surface.Canvas, SurfaceWidth, SurfaceHeight);
        foreach (GraphicalUiElement element in elements)
        {
            element.Parent = null;
            GumService.Default.Root.Children.Add(element);
        }

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }
}
