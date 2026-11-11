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

    static SKBitmap Texture(bool semiTransparentBorders)
    {
        SKBitmap bitmap = new(32, 32);
        Random random = new(5);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bool border = x < 8 || y < 8 || x >= 24 || y >= 24;
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

    SKBitmap Render(SKBitmap texture, float x, float y, float width, float height, float borderScale, bool flip, bool perSection, out int drawCalls)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        int calls = 0;
        SKBitmap pixels = _gpu.Render(140, 100, 0, canvas =>
        {
            var managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = canvas;
            managers.Renderer.ClearsCanvas = false;
            using SKShader shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(140, 100),
                new[] { SKColors.DarkRed, SKColors.CornflowerBlue }, SKShaderTileMode.Clamp);
            using SKPaint backdrop = new() { Shader = shader };
            canvas.DrawRect(0, 0, 140, 100, backdrop);
            using NineSlice nineSlice = new()
            {
                Texture = texture,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                BorderScale = borderScale,
                FlipHorizontal = flip,
                DisableLatticeFastPath = perSection,
            };
            managers.Renderer.RenderStateChangeStatistics.Reset();
            nineSlice.Render(managers);
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
    [InlineData(10.5f, 10.25f, 64f, 40f, 1f, false, false)]
    [InlineData(10.5f, 10f, 80f, 60f, 2f, true, true)]
    [InlineData(10.25f, 10.75f, 64f, 40f, 1f, false, false)]
    [InlineData(10.3f, 10.7f, 63.4f, 41.6f, 1f, false, true)]
    [InlineData(10.3f, 10.7f, 63.4f, 41.6f, 1f, true, true)]
    [InlineData(10f, 10f, 80f, 60f, 2f, false, false)]
    [InlineData(10.5f, 10.25f, 80.4f, 60.6f, 2f, true, true)]
    public void Render_Nearest_MatchesPerSectionPixelsOnGpu(
        float x, float y, float width, float height, float borderScale, bool flip, bool semi)
    {
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        using SKBitmap texture = Texture(semi);

        using SKBitmap fast = Render(texture, x, y, width, height, borderScale, flip, perSection: false, out int fastCalls);
        using SKBitmap old = Render(texture, x, y, width, height, borderScale, flip, perSection: true, out int oldCalls);

        if (x % 1f != 0.5f && y % 1f != 0.5f) { fastCalls.ShouldBe(1); }
        oldCalls.ShouldBeGreaterThan(1);
        MaxDifference(fast, old).ShouldBe(0);
    }
}
