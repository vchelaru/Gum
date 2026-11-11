using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// #7920: the one-call lattice draw of a NineSlice matches the nine per-section draws on a GPU surface, where the
/// CPU raster tests cannot show a sampling difference.
/// </summary>
[Collection(GpuCollection.Name)]
public class NineSliceLatticeGpuTests
{
    readonly WglGpuContext _gpu;

    public NineSliceLatticeGpuTests(WglGpuContext gpu)
    {
        _gpu = gpu;
    }

    static SKBitmap Texture(bool semiTransparentBorders, int size = 32)
    {
        SKBitmap bitmap = new(size, size);
        Random random = new(5);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < size / 4 || y < size / 4 || x >= size - size / 4 || y >= size - size / 4;
                byte alpha = (byte)(semiTransparentBorders && border ? 140 : 255);
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)(random.Next(256) * alpha / 255),
                    (byte)(random.Next(256) * alpha / 255),
                    (byte)(random.Next(256) * alpha / 255),
                    alpha));
            }
        }
        return bitmap;
    }

    SKBitmap Render(SKBitmap texture, float x, float y, float width, float height, float borderScale, bool flip, bool perSection, out int drawCalls, bool flipVertical = false, float zoom = 1f, float offsetY = 0f, System.Drawing.Rectangle? source = null)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        int calls = 0;
        SKBitmap pixels = _gpu.Render(220, 200, 0, canvas =>
        {
            var managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = canvas;
            managers.Renderer.ClearsCanvas = false;
            using SKShader shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(220, 200),
                new[] { SKColors.DarkRed, SKColors.CornflowerBlue }, SKShaderTileMode.Clamp);
            using SKPaint backdrop = new() { Shader = shader };
            canvas.DrawRect(0, 0, 220, 200, backdrop);
            using NineSlice nineSlice = new()
            {
                Texture = texture,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                BorderScale = borderScale,
                FlipHorizontal = flip,
                FlipVertical = flipVertical,
                SourceRectangle = source,
                DisableLatticeFastPath = perSection,
            };
            managers.Renderer.RenderStateChangeStatistics.Reset();
            canvas.Save();
            canvas.Scale(zoom);
            canvas.Translate(0, offsetY);
            nineSlice.Render(managers);
            canvas.Restore();
            calls = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;
        });
        drawCalls = calls;
        return pixels;
    }

    static int MaxDifference(SKBitmap a, SKBitmap b)
    {
        int max = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                SKColor p = a.GetPixel(x, y);
                SKColor q = b.GetPixel(x, y);
                max = Math.Max(max, Math.Max(
                    Math.Max(Math.Abs(p.Red - q.Red), Math.Abs(p.Green - q.Green)),
                    Math.Max(Math.Abs(p.Blue - q.Blue), Math.Abs(p.Alpha - q.Alpha))));
            }
        }
        return max;
    }

    [Theory]
    [InlineData(10f, 10f, 64f, 40f, 1f, false, false)]
    [InlineData(10f, 12f, 64f, 40f, 1f, false, false)]
    [InlineData(10f, 10f, 80f, 60f, 2f, true, true)]
    [InlineData(9f, 11f, 64f, 40f, 1f, false, false)]
    [InlineData(11f, 7f, 63f, 42f, 1f, false, true)]
    [InlineData(11f, 7f, 63f, 42f, 1f, true, true)]
    [InlineData(10f, 10f, 80f, 60f, 2f, false, false)]
    [InlineData(10f, 11f, 80f, 60f, 2f, true, true)]
    public void Render_Nearest_MatchesPerSectionPixelsOnGpu(
        float x, float y, float width, float height, float borderScale, bool flip, bool semi)
    {
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        using SKBitmap texture = Texture(semi);

        using SKBitmap fast = Render(texture, x, y, width, height, borderScale, flip, perSection: false, out int fastCalls);
        using SKBitmap old = Render(texture, x, y, width, height, borderScale, flip, perSection: true, out int oldCalls);

        fastCalls.ShouldBe(1);
        oldCalls.ShouldBeGreaterThan(1);
        MaxDifference(fast, old).ShouldBe(0);
    }

    [Fact]
    public void Render_ShrunkWithFractionalParentOffset_MatchesPerSectionPixelsOnGpu()
    {
        // Found by fuzzing: an edge at device x 17.26 differed by up to 63 on a GPU surface.
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        using SKBitmap texture = Texture(true, 15);

        using SKBitmap fast = Render(texture, 34.52f, 32.52f, 22.889f, 23.663f, 2f, true, false, out _, flipVertical: true, zoom: 0.5f, offsetY: 7.915f);
        using SKBitmap old = Render(texture, 34.52f, 32.52f, 22.889f, 23.663f, 2f, true, true, out _, flipVertical: true, zoom: 0.5f, offsetY: 7.915f);

        MaxDifference(fast, old).ShouldBe(0);
    }

    [Fact]
    public void Render_PartialSourceRectangle_MatchesPerSectionPixelsOnGpu()
    {
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        using SKBitmap texture = Texture(true);
        var source = new System.Drawing.Rectangle(4, 4, 24, 24);

        using SKBitmap fast = Render(texture, 10, 10, 64, 40, 1f, false, false, out int fastCalls, source: source);
        using SKBitmap old = Render(texture, 10, 10, 64, 40, 1f, false, true, out _, source: source);

        fastCalls.ShouldBe(1);
        MaxDifference(fast, old).ShouldBe(0);
    }

    [Fact]
    public void Render_SeededFractionalFuzz_EngagedCasesMatchPerSectionPixelsOnGpu()
    {
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        Random random = new(20260712);
        float[] zooms = { 0.5f, 1f, 1.25f, 1.5f, 2f, 3f };
        float[] fractions = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.02f, 0.05f, 0.1f, 0.25f, 0.5f, 0.75f };
        int engaged = 0;
        for (int i = 0; i < 150; i++)
        {
            int size = random.Next(12, 33);
            using SKBitmap texture = Texture(random.Next(2) == 0, size);
            float x = random.Next(0, 20) + fractions[random.Next(fractions.Length)];
            float y = random.Next(0, 20) + fractions[random.Next(fractions.Length)];
            float w = random.Next(30, 60) + fractions[random.Next(fractions.Length)];
            float h = random.Next(30, 60) + fractions[random.Next(fractions.Length)];
            float scale = new[] { 1f, 1f, 2f }[random.Next(3)];
            float zoom = zooms[random.Next(zooms.Length)];
            float offsetY = random.Next(0, 10) + fractions[random.Next(fractions.Length)];
            bool flipH = random.Next(2) == 0;
            bool flipV = random.Next(2) == 0;
            System.Drawing.Rectangle? source = random.Next(3) == 0 && size >= 16
                ? new System.Drawing.Rectangle(1, 2, size - 3, size - 4)
                : null;

            using SKBitmap fast = Render(texture, x, y, w, h, scale, flipH, false, out int calls, flipV, zoom, offsetY, source);
            using SKBitmap old = Render(texture, x, y, w, h, scale, flipH, true, out _, flipV, zoom, offsetY, source);

            if (calls == 1)
            {
                engaged++;
            }
            MaxDifference(fast, old).ShouldBe(0,
                $"case {i}: size {size} x {x} y {y} w {w} h {h} scale {scale} zoom {zoom} offsetY {offsetY} flip {flipH}/{flipV} source {source}");
        }
        engaged.ShouldBeGreaterThan(4);
    }
}
