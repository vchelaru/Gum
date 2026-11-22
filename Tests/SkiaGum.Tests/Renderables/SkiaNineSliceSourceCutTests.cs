using Gum;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A different source cut on each side (#8354) on the Skia nine-slice: corners and edges keep their
/// texels and size, only the middle stretches. The MonoGame and raylib nine-slices pin the same case.
/// </summary>
public class SkiaNineSliceSourceCutTests : BaseTestClass
{
    const int SurfaceSize = 64;
    const int Left = 10;
    const int Top = 10;

    readonly SKSurface _surface;

    public SkiaNineSliceSourceCutTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(SurfaceSize, SurfaceSize));
        GumService.Default.Initialize(_surface.Canvas, SurfaceSize, SurfaceSize);
    }

    public override void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
        base.Dispose();
    }

    static SKColor TexelColor(int x, int y) => new((byte)(20 + x * 18), (byte)(20 + y * 14), 128);

    static SKBitmap Texture()
    {
        SKBitmap bitmap = new(12, 16);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                bitmap.SetPixel(x, y, TexelColor(x, y));
            }
        }
        return bitmap;
    }

    (SKBitmap Pixels, int DrawCalls) Draw(SKBitmap texture, bool disableLattice)
    {
        // 12x16 texture cut at left 6, top 8, right 2, bottom 3 leaves a 4x5 middle; drawn at 40x26.
        using NineSlice nineSlice = new()
        {
            Texture = texture,
            X = Left,
            Y = Top,
            Width = 40,
            Height = 26,
            CustomFrameTextureCoordinateLeft = 6,
            CustomFrameTextureCoordinateTop = 8,
            CustomFrameTextureCoordinateRight = 2,
            CustomFrameTextureCoordinateBottom = 3,
            DisableLatticeFastPath = disableLattice,
        };

        _surface.Canvas.Clear(SKColors.Black);
        SystemManagers managers = SystemManagers.Default;
        managers.Renderer.RenderStateChangeStatistics.Reset();
        nineSlice.Render(managers);
        int drawCalls = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;

        SKBitmap pixels = new(SurfaceSize, SurfaceSize);
        _surface.ReadPixels(pixels.Info, pixels.GetPixels(), pixels.RowBytes, 0, 0);
        return (pixels, drawCalls);
    }

    [Fact]
    public void Render_PerSideCut_KeepsCornersAndEdgesAtTheirOwnSize()
    {
        using SKBitmap texture = Texture();
        using SKBitmap pixels = Draw(texture, disableLattice: true).Pixels;

        // Corners draw 1:1. The 32 wide middle draws each of its 4 texels 8 pixels wide; the 15 tall
        // middle draws each of its 5 texels 3 pixels tall.
        (int X, int Y, int TexelX, int TexelY)[] samples =
        {
            (0, 0, 0, 0), (5, 7, 5, 7),
            (38, 0, 10, 0), (39, 7, 11, 7),
            (0, 23, 0, 13), (5, 25, 5, 15),
            (39, 25, 11, 15),
            (6 + 4, 3, 6, 3), (6 + 28, 3, 9, 3),
            (2, 8 + 1, 2, 8), (2, 8 + 13, 2, 12),
            (6 + 12, 8 + 7, 7, 10),
            (38, 8 + 4, 10, 9),
            (6 + 20, 24, 8, 14),
        };
        foreach ((int x, int y, int texelX, int texelY) in samples)
        {
            pixels.GetPixel(Left + x, Top + y).ShouldBe(TexelColor(texelX, texelY), $"local pixel ({x}, {y})");
        }

        // Nothing past the 40x26 box.
        pixels.GetPixel(Left + 40, Top + 10).ShouldBe(SKColors.Black);
        pixels.GetPixel(Left + 10, Top + 26).ShouldBe(SKColors.Black);
    }

    [Fact]
    public void Render_PerSideCut_LatticeMatchesPerSectionPixels()
    {
        using SKBitmap texture = Texture();

        (SKBitmap fastPixels, int fastDrawCalls) = Draw(texture, disableLattice: false);
        (SKBitmap oldPixels, int oldDrawCalls) = Draw(texture, disableLattice: true);
        using SKBitmap fast = fastPixels;
        using SKBitmap old = oldPixels;

        fastDrawCalls.ShouldBe(1);
        oldDrawCalls.ShouldBe(9);
        for (int y = 0; y < SurfaceSize; y++)
        {
            for (int x = 0; x < SurfaceSize; x++)
            {
                fast.GetPixel(x, y).ShouldBe(old.GetPixel(x, y), $"pixel ({x}, {y})");
            }
        }
    }

    [Fact]
    public void SetProperty_CustomFrameTextureCoordinateSides_ShouldForwardToRuntime()
    {
        Gum.GueDeriving.NineSliceRuntime sut = new();
        System.Collections.Generic.List<string?> changed = new();
        sut.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        sut.SetProperty("CustomFrameTextureCoordinateLeft", 6f);
        sut.SetProperty("CustomFrameTextureCoordinateTop", 0f);
        sut.SetProperty("CustomFrameTextureCoordinateRight", 2.5f);
        sut.SetProperty("CustomFrameTextureCoordinateBottom", 3f);

        sut.CustomFrameTextureCoordinateLeft.ShouldBe(6f);
        sut.CustomFrameTextureCoordinateTop.ShouldBe(0f);
        sut.CustomFrameTextureCoordinateRight.ShouldBe(2.5f);
        sut.CustomFrameTextureCoordinateBottom.ShouldBe(3f);
        // Through the runtime, not reflection on the renderable, so bindings hear the change.
        changed.ShouldContain("CustomFrameTextureCoordinateLeft");
        changed.ShouldContain("CustomFrameTextureCoordinateBottom");

        sut.SetProperty("CustomFrameTextureCoordinateLeft", null!);

        sut.CustomFrameTextureCoordinateLeft.ShouldBeNull();
    }
}
