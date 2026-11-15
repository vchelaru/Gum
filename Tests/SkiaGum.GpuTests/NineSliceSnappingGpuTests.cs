using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// #8052: a NineSlice rounds each of its four edges to a whole device pixel (Nearest sampling). Real Forms theme
/// tiles on a GPU surface: nothing changes when the edges already sit on whole pixels, and at fractional edges only
/// the rows and columns along the edges may change.
/// </summary>
[Collection(GpuCollection.Name)]
public class NineSliceSnappingGpuTests
{
    readonly WglGpuContext _gpu;

    public NineSliceSnappingGpuTests(WglGpuContext gpu)
    {
        _gpu = gpu;
    }

    // The two Forms tiles a button or list item uses: the outlined one and the solid one, tinted like a theme color.
    static readonly (string Name, int Left, int Top, SKColor Tint)[] Tiles =
    {
        ("Bordered", 24, 48, SKColors.White),
        ("SolidTinted", 0, 48, new SKColor(0x2B, 0x6C, 0xB0)),
    };

    SKBitmap Render(int tileIndex, float zoom, float x, float y, float width, float height, int count, float spacing, bool snap)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        SKFilterMode previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        int pixelWidth = (int)MathF.Ceiling((x + width + 4) * zoom);
        int pixelHeight = (int)MathF.Ceiling((y + count * (height + spacing) + 4) * zoom);
        try
        {
            return _gpu.Render(pixelWidth, pixelHeight, 0, canvas =>
            {
                var managers = new SystemManagers();
                managers.Initialize();
                SystemManagers.Default = managers;
                managers.Canvas = canvas;
                managers.Renderer.ClearsCanvas = false;
                SKBitmap sheet = managers.GetOrLoadEmbeddedTexture2d("UISpriteSheet.png");
                canvas.Clear(new SKColor(255, 0, 255));
                canvas.Save();
                canvas.Scale(zoom);
                for (int i = 0; i < count; i++)
                {
                    using var nineSlice = new NineSlice
                    {
                        Texture = sheet,
                        SourceRectangle = new System.Drawing.Rectangle(Tiles[tileIndex].Left, Tiles[tileIndex].Top, 24, 24),
                        X = x,
                        Y = y + i * (height + spacing),
                        Width = width,
                        Height = height,
                        Color = Tiles[tileIndex].Tint,
                        DisableDeviceSnapping = !snap,
                    };
                    nineSlice.Render(managers);
                }
                canvas.Restore();
            });
        }
        finally
        {
            RenderingLibrary.Graphics.Renderer.TextureFilter = previousFilter;
        }
    }

    static int Difference(SKColor a, SKColor b) =>
        Math.Max(Math.Max(Math.Abs(a.Red - b.Red), Math.Abs(a.Green - b.Green)), Math.Max(Math.Abs(a.Blue - b.Blue), Math.Abs(a.Alpha - b.Alpha)));

    [Theory]
    [InlineData(0, 1f)]
    [InlineData(0, 2f)]
    [InlineData(1, 1f)]
    [InlineData(1, 2f)]
    public void Render_AtWholePixelEdges_IsUnchangedBySnapping(int tile, float zoom)
    {
        using SKBitmap snapped = Render(tile, zoom, 8f, 9f, 48f, 24f, 4, 0f, snap: true);
        using SKBitmap exact = Render(tile, zoom, 8f, 9f, 48f, 24f, 4, 0f, snap: false);

        for (int y = 0; y < snapped.Height; y++)
        {
            for (int x = 0; x < snapped.Width; x++)
            {
                Difference(snapped.GetPixel(x, y), exact.GetPixel(x, y)).ShouldBe(0, $"pixel ({x}, {y})");
            }
        }
    }

    [Theory]
    [InlineData(0, 1f, 0.3f, 30.698242f)]
    [InlineData(0, 1f, 0.7f, 20.5f)]
    [InlineData(0, 2f, 0.3f, 30.698242f)]
    [InlineData(0, 2f, 0.6f, 20.5f)]
    [InlineData(1, 1f, 0.3f, 30.698242f)]
    [InlineData(1, 1f, 0.7f, 20.5f)]
    [InlineData(1, 2f, 0.3f, 30.698242f)]
    [InlineData(1, 2f, 0.6f, 20.5f)]
    public void Render_AtFractionalEdges_ChangesOnlyRowsAndColumnsAlongTheEdges(int tile, float zoom, float fraction, float height)
    {
        const float width = 48.5f;
        const int count = 4;
        using SKBitmap snapped = Render(tile, zoom, 8f + fraction, 9f + fraction, width, height, count, 0f, snap: true);
        using SKBitmap exact = Render(tile, zoom, 8f + fraction, 9f + fraction, width, height, count, 0f, snap: false);
        SaveSheetIfRequested(tile, zoom, fraction, height, exact, snapped);

        // Each item's outer edges, in device pixels: a changed pixel must be within one pixel of one of them.
        var rows = new List<float>();
        for (int i = 0; i <= count; i++)
        {
            rows.Add((9f + fraction + i * height) * zoom);
        }
        float[] columns = { (8f + fraction) * zoom, (8f + fraction + width) * zoom };

        int changed = 0;
        for (int y = 0; y < snapped.Height; y++)
        {
            for (int x = 0; x < snapped.Width; x++)
            {
                if (Difference(snapped.GetPixel(x, y), exact.GetPixel(x, y)) == 0)
                {
                    continue;
                }
                changed++;
                bool nearRow = rows.Any(r => Math.Abs(y + 0.5f - r) <= 1.5f);
                bool nearColumn = columns.Any(c => Math.Abs(x + 0.5f - c) <= 1.5f);
                (nearRow || nearColumn).ShouldBeTrue($"pixel ({x}, {y}) changed away from every item edge");
            }
        }
    }

    // Before (exact edges), after (snapped) and the difference, side by side, magnified. For the pull request.
    static void SaveSheetIfRequested(int tile, float zoom, float fraction, float height, SKBitmap before, SKBitmap after)
    {
        string? folder = Environment.GetEnvironmentVariable("GUM_SEAM_DUMP");
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }
        Directory.CreateDirectory(folder);
        const int magnify = 6;
        int w = before.Width * magnify;
        int h = before.Height * magnify;
        using var sheet = new SKBitmap(w * 3 + 40, h);
        using (var canvas = new SKCanvas(sheet))
        {
            canvas.Clear(SKColors.DimGray);
            var sampling = new SKSamplingOptions(SKFilterMode.Nearest);
            using var beforeImage = SKImage.FromBitmap(before);
            using var afterImage = SKImage.FromBitmap(after);
            canvas.DrawImage(beforeImage, new SKRect(0, 0, w, h), sampling);
            canvas.DrawImage(afterImage, new SKRect(w + 20, 0, 2 * w + 20, h), sampling);
            using var diff = new SKBitmap(before.Width, before.Height);
            for (int y = 0; y < diff.Height; y++)
            {
                for (int x = 0; x < diff.Width; x++)
                {
                    diff.SetPixel(x, y, Difference(before.GetPixel(x, y), after.GetPixel(x, y)) == 0 ? SKColors.Black : SKColors.Yellow);
                }
            }
            using var diffImage = SKImage.FromBitmap(diff);
            canvas.DrawImage(diffImage, new SKRect(2 * w + 40, 0, 3 * w + 40, h), sampling);
        }
        using var image = SKImage.FromBitmap(sheet);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(folder, $"snap-{Tiles[tile].Name}-z{zoom}-f{fraction}-h{height}.png"), data.ToArray());
    }
}
