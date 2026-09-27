using Gum;
using Gum.DataTypes;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.IO;

namespace SkiaGum.Tests.Renderables;

// Skia always sampled Sprite textures with nearest filtering, so a project whose
// TextureFilter is "Linear" rendered point-filtered (issue #5231).
public class TextureFilterTests : IDisposable
{
    private readonly SKFilterMode _savedFilter = global::RenderingLibrary.Graphics.Renderer.TextureFilter;

    public void Dispose() => global::RenderingLibrary.Graphics.Renderer.TextureFilter = _savedFilter;

    // A 2x1 black|white texture stretched to 100x1. Nearest sampling keeps x=40 pure black; linear
    // sampling blends it toward the white texel.
    private static byte RenderRedAt40(Action<SKBitmap, SKCanvas> draw)
    {
        using SKBitmap texture = new(2, 1);
        texture.SetPixel(0, 0, SKColors.Black);
        texture.SetPixel(1, 0, SKColors.White);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 1, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        draw(texture, surface.Canvas);

        using SKImage image = surface.Snapshot();
        using SKBitmap readback = SKBitmap.FromImage(image);
        return readback.GetPixel(40, 0).Red;
    }

    [Fact]
    public void Sprite_DefaultFilter_SamplesNearest()
    {
        byte red = RenderRedAt40((texture, canvas) =>
        {
            using Sprite sprite = new() { Texture = texture };
            sprite.DrawBound(new SKRect(0, 0, 100, 1), canvas, 0);
        });

        red.ShouldBe((byte)0);
    }

    [Fact]
    public void Sprite_LinearFilter_BlendsBetweenTexels()
    {
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Linear;

        byte red = RenderRedAt40((texture, canvas) =>
        {
            using Sprite sprite = new() { Texture = texture };
            sprite.DrawBound(new SKRect(0, 0, 100, 1), canvas, 0);
        });

        red.ShouldBeInRange((byte)30, (byte)220);
    }

    private static SKBitmap CreateColumnTexture(params SKColor[] columns)
    {
        SKBitmap texture = new(columns.Length, 6);
        for (int x = 0; x < columns.Length; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                texture.SetPixel(x, y, columns[x]);
            }
        }
        return texture;
    }

    // A 6x6 nine-slice source whose every row reads red,red | black,white | red,red, drawn 100 wide at
    // 1:1 height. The 2px corners draw 1:1 and the 2-texel center stretches across x=2..97. The same
    // NineSlice first draws a solid green texture, then the real one twice, so stale or wrong
    // per-section images from an earlier texture or draw would show.
    private static SKBitmap RenderNineSliceRow()
    {
        using SKBitmap decoy = CreateColumnTexture(
            SKColors.Lime, SKColors.Lime, SKColors.Lime, SKColors.Lime, SKColors.Lime, SKColors.Lime);
        using SKBitmap texture = CreateColumnTexture(
            SKColors.Red, SKColors.Red, SKColors.Black, SKColors.White, SKColors.Red, SKColors.Red);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 6, SKColorType.Rgba8888, SKAlphaType.Premul));
        SKRect bounds = new SKRect(0, 0, 100, 6);
        using NineSlice nineSlice = new() { Texture = decoy };
        nineSlice.DrawBound(bounds, surface.Canvas, 0);

        nineSlice.Texture = texture;
        surface.Canvas.Clear(SKColors.Transparent);
        nineSlice.DrawBound(bounds, surface.Canvas, 0);
        nineSlice.DrawBound(bounds, surface.Canvas, 0);

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    [Fact]
    public void NineSlice_DefaultFilter_SamplesNearest()
    {
        using SKBitmap readback = RenderNineSliceRow();

        SKColor interior = readback.GetPixel(40, 3);
        (interior.Red == 0 || interior.Red == 255).ShouldBeTrue($"x=40 should be a single texel, was {interior}");
    }

    // #5278: Linear blends inside a section, and must not blend in the neighboring section's texels.
    [Fact]
    public void NineSlice_LinearFilter_BlendsInsideSectionsWithoutBleedingAcrossThem()
    {
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Linear;

        using SKBitmap readback = RenderNineSliceRow();

        SKColor interior = readback.GetPixel(50, 3);
        interior.Red.ShouldBeInRange((byte)30, (byte)220, $"interior {interior}");
        interior.Green.ShouldBeInRange((byte)30, (byte)220, $"interior {interior}");

        // The center's first pixel sits next to the red left corner: any red means it bled in.
        SKColor centerLeftEdge = readback.GetPixel(2, 3);
        centerLeftEdge.Red.ShouldBeLessThanOrEqualTo((byte)8, $"center left edge {centerLeftEdge}");

        // The center's last pixel sits next to the red right corner: lost green means red bled in.
        SKColor centerRightEdge = readback.GetPixel(97, 3);
        centerRightEdge.Green.ShouldBeGreaterThanOrEqualTo((byte)247, $"center right edge {centerRightEdge}");

        // The corners draw 1:1 and stay pure red next to the black/white center.
        readback.GetPixel(1, 3).ShouldBe(SKColors.Red);
        readback.GetPixel(98, 3).ShouldBe(SKColors.Red);
    }

    // A 1x1 source makes the center section the whole image. Skia can hand back the image itself as
    // that "subset", and clearing the section images must not dispose a caller-assigned Image.
    [Fact]
    public void NineSlice_LinearFilter_WithWholeImageSection_DoesNotDisposeAssignedImage()
    {
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Linear;
        using SKBitmap texture = new(1, 1);
        using SKImage assignedImage = SKImage.FromBitmap(texture);
        using SKSurface surface = SKSurface.Create(new SKImageInfo(10, 10));
        using NineSlice nineSlice = new() { Image = assignedImage };

        nineSlice.DrawBound(new SKRect(0, 0, 10, 10), surface.Canvas, 0);
        nineSlice.Image = null;

        assignedImage.Handle.ShouldNotBe(IntPtr.Zero);
    }

    [Fact]
    public void Initialize_WithLinearProject_SetsRendererTextureFilter()
    {
        string directory = Path.Combine(Path.GetTempPath(), "SkiaTextureFilterTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            string gumxPath = Path.Combine(directory, "Proj.gumx");
            new GumProjectSave { TextureFilter = "Linear" }.Save(gumxPath, saveElements: false);
            using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));

            GumService.Default.Initialize(surface.Canvas, 200, 100, gumxPath);

            global::RenderingLibrary.Graphics.Renderer.TextureFilter.ShouldBe(SKFilterMode.Linear);
        }
        finally
        {
            Gum.Managers.ObjectFinder.Self.GumProjectSave = null;
            try { Directory.Delete(directory, recursive: true); } catch { /* best-effort */ }
        }
    }
}
