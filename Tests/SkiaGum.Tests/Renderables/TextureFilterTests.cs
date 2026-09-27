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
