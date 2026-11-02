using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary;
using Shouldly;
using System.Diagnostics;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Issue #7303: the blurred dropshadow is cached per element, so a static shadowed element pays the
/// silhouette + two Gaussian passes once instead of every frame, with the same pixels as the uncached path.
/// </summary>
public class ShadowBlurCacheTests : BaseTestClass
{
    private const int CanvasSize = 300;

    private static ShadowBlurRenderer Blur => global::RenderingLibrary.Graphics.Renderer.Self.ShadowBlur;

    private readonly ITestOutputHelper _output;

    public ShadowBlurCacheTests(ITestOutputHelper output)
    {
        _output = output;
        Blur.CacheEnabled = true;
        // Drop entries left by earlier tests' elements (a sweep frees what was not drawn since the last one).
        Blur.ClearUnusedRenderTargetsLastFrame();
        Blur.ClearUnusedRenderTargetsLastFrame();
    }

    private static RectangleRuntime AddRectangle(float blur = 16f)
    {
        RectangleRuntime rectangle = new();
        rectangle.X = 100;
        rectangle.Y = 100;
        rectangle.Width = 80;
        rectangle.Height = 60;
        rectangle.IsFilled = true;
        rectangle.FillColor = Color.White;
        rectangle.HasDropshadow = true;
        rectangle.DropshadowColor = new Color((byte)255, (byte)64, (byte)32, (byte)200);
        rectangle.DropshadowOffsetX = 5;
        rectangle.DropshadowOffsetY = 7;
        rectangle.DropshadowBlur = blur;
        GumService.Default.Root.Children.Add(rectangle);
        GumService.Default.Root.UpdateLayout();
        return rectangle;
    }

    private static Color[] DrawFrame()
    {
        GumService.Default.Root.UpdateLayout();
        RenderTexture2D host = LoadRenderTexture(CanvasSize, CanvasSize);
        try
        {
            BeginDrawing();
            BeginTextureMode(host);
            ClearBackground(Color.Black);
            GumService.Default.Draw();
            EndTextureMode();
            EndDrawing();

            Image image = LoadImageFromTexture(host.Texture);
            try
            {
                Color[] pixels = new Color[CanvasSize * CanvasSize];
                for (int y = 0; y < CanvasSize; y++)
                {
                    for (int x = 0; x < CanvasSize; x++)
                    {
                        pixels[y * CanvasSize + x] = GetImageColor(image, x, y);
                    }
                }
                return pixels;
            }
            finally
            {
                UnloadImage(image);
            }
        }
        finally
        {
            UnloadRenderTexture(host);
        }
    }

    private static Color[] DrawUncachedFrame()
    {
        Blur.CacheEnabled = false;
        try
        {
            return DrawFrame();
        }
        finally
        {
            Blur.CacheEnabled = true;
        }
    }

    private static int PassesDuring(Action action)
    {
        int before = Blur.BlurPassCount;
        action();
        return Blur.BlurPassCount - before;
    }

    private static void ShouldBeIdentical(Color[] actual, Color[] expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            if (!actual[i].Equals(expected[i]))
            {
                throw new ShouldAssertException(
                    $"Pixel {i % CanvasSize},{i / CanvasSize} is {actual[i].R},{actual[i].G},{actual[i].B},{actual[i].A} but expected {expected[i].R},{expected[i].G},{expected[i].B},{expected[i].A}");
            }
        }
    }

    [Fact]
    public void Draw_UnchangedElementSecondFrame_RunsNoBlurPasses()
    {
        AddRectangle();

        PassesDuring(() => DrawFrame()).ShouldBe(2);
        PassesDuring(() => DrawFrame()).ShouldBe(0);
        PassesDuring(() => DrawFrame()).ShouldBe(0);
    }

    [Fact]
    public void Draw_BlurChanged_RunsBlurPassesAgain()
    {
        RectangleRuntime rectangle = AddRectangle();
        DrawFrame();
        DrawFrame();

        rectangle.DropshadowBlur = 24f;

        PassesDuring(() => DrawFrame()).ShouldBe(2);
    }

    [Fact]
    public void Draw_SizeChanged_RunsBlurPassesAgain()
    {
        RectangleRuntime rectangle = AddRectangle();
        DrawFrame();
        DrawFrame();

        rectangle.Width = 90;

        PassesDuring(() => DrawFrame()).ShouldBe(2);
    }

    [Fact]
    public void Draw_CornerRadiusChanged_RunsBlurPassesAgain()
    {
        RectangleRuntime rectangle = AddRectangle();
        rectangle.CornerRadius = 4;
        DrawFrame();
        DrawFrame();

        rectangle.CornerRadius = 12;

        PassesDuring(() => DrawFrame()).ShouldBe(2);
    }

    [Fact]
    public void Draw_FillChangedToStroke_RunsBlurPassesAgain()
    {
        RectangleRuntime rectangle = AddRectangle();
        rectangle.IsFilled = true;
        rectangle.Color = Color.White;
        rectangle.StrokeWidth = 4;
        DrawFrame();
        DrawFrame();

        rectangle.IsFilled = false;

        PassesDuring(() => DrawFrame()).ShouldBe(2);
    }

    [Fact]
    public void Draw_OffsetChanged_ReusesBlurAndMatchesUncached()
    {
        RectangleRuntime rectangle = AddRectangle();
        DrawFrame();
        DrawFrame();

        rectangle.DropshadowOffsetX = 12;
        Color[] cached = default!;
        PassesDuring(() => cached = DrawFrame()).ShouldBe(0);

        ShouldBeIdentical(cached, DrawUncachedFrame());
    }

    [Fact]
    public void Draw_TintChanged_ReusesBlurAndMatchesUncached()
    {
        RectangleRuntime rectangle = AddRectangle();
        DrawFrame();
        DrawFrame();

        rectangle.DropshadowColor = new Color((byte)10, (byte)200, (byte)30, (byte)90);
        Color[] cached = default!;
        PassesDuring(() => cached = DrawFrame()).ShouldBe(0);

        ShouldBeIdentical(cached, DrawUncachedFrame());
    }

    [Fact]
    public void Draw_CircleAndArcUnchanged_RunNoBlurPassesOnSecondFrame()
    {
        CircleRuntime circle = new();
        circle.X = 20; circle.Y = 20; circle.Width = 80; circle.Height = 80;
        circle.IsFilled = true; circle.FillColor = Color.White;
        circle.HasDropshadow = true; circle.DropshadowBlur = 12; circle.DropshadowColor = Color.Black;
        GumService.Default.Root.Children.Add(circle);
        ArcRuntime arc = new();
        arc.X = 150; arc.Y = 150; arc.Width = 100; arc.Height = 100;
        arc.StartAngle = 0; arc.SweepAngle = 200; arc.Thickness = 10;
        arc.HasDropshadow = true; arc.DropshadowBlur = 12; arc.DropshadowColor = Color.Black;
        GumService.Default.Root.Children.Add(arc);

        PassesDuring(() => DrawFrame()).ShouldBe(4);
        PassesDuring(() => DrawFrame()).ShouldBe(0);

        arc.SweepAngle = 120;

        PassesDuring(() => DrawFrame()).ShouldBe(2);
    }

    [Fact]
    public void Draw_CachedFramesOneAndTwo_MatchUncachedOutput()
    {
        RectangleRuntime rectangle = AddRectangle();
        rectangle.CornerRadius = 10;
        Color[] cachedFrame1 = DrawFrame();
        Color[] cachedFrame2 = DrawFrame();

        Blur.CachedBlurCount.ShouldBe(1);
        Color[] uncachedFrame1 = DrawUncachedFrame();
        Color[] uncachedFrame2 = DrawUncachedFrame();

        ShouldBeIdentical(cachedFrame1, uncachedFrame1);
        ShouldBeIdentical(cachedFrame2, uncachedFrame2);
    }

    [Fact]
    public void Draw_AnimatedBlur_RunsBlurPassesEveryFrameAndKeepsOneEntry()
    {
        RectangleRuntime rectangle = AddRectangle();
        for (int frame = 0; frame < 5; frame++)
        {
            rectangle.DropshadowBlur = 10f + frame;
            PassesDuring(() => DrawFrame()).ShouldBe(2);
            Blur.CachedBlurCount.ShouldBe(1);
        }
    }

    [Fact]
    public void Draw_ElementRemoved_FreesItsCacheEntry()
    {
        RectangleRuntime rectangle = AddRectangle();
        DrawFrame();
        DrawFrame();
        Blur.CachedBlurCount.ShouldBe(1);

        GumService.Default.Root.Children.Remove(rectangle);
        DrawFrame();
        DrawFrame();

        Blur.CachedBlurCount.ShouldBe(0);
    }

    [Fact]
    public void Draw_ElementRecreatedEveryFrame_DoesNotAccumulateEntries()
    {
        for (int frame = 0; frame < 6; frame++)
        {
            GumService.Default.Root.Children.Clear();
            AddRectangle();
            DrawFrame();
        }
        DrawFrame();
        DrawFrame();

        Blur.CachedBlurCount.ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public void Draw_LargeShadowedElements_CacheCutsBlurPassesAndReportsTiming()
    {
        const int elementCount = 4;
        const int frames = 120;
        for (int i = 0; i < elementCount; i++)
        {
            RectangleRuntime rectangle = new();
            rectangle.X = 100 + i * 10;
            rectangle.Y = 100 + i * 10;
            rectangle.Width = 800;
            rectangle.Height = 600;
            rectangle.IsFilled = true;
            rectangle.FillColor = Color.White;
            rectangle.HasDropshadow = true;
            rectangle.DropshadowColor = new Color((byte)0, (byte)0, (byte)0, (byte)128);
            rectangle.DropshadowBlur = 56;
            GumService.Default.Root.Children.Add(rectangle);
        }
        GumService.Default.Root.UpdateLayout();

        RenderTexture2D host = LoadRenderTexture(1200, 1000);
        try
        {
            (int Passes, double Ms) Run(bool cache)
            {
                Blur.CacheEnabled = cache;
                Blur.ClearUnusedRenderTargetsLastFrame();
                Blur.ClearUnusedRenderTargetsLastFrame();
                int passesBefore = Blur.BlurPassCount;
                double drawMs = 0;
                for (int frame = 0; frame < frames; frame++)
                {
                    BeginDrawing();
                    BeginTextureMode(host);
                    ClearBackground(Color.Black);
                    Stopwatch watch = Stopwatch.StartNew();
                    GumService.Default.Draw();
                    EndTextureMode();
                    // Reading one pixel back waits for the GPU to finish the frame.
                    Image sync = LoadImageFromTexture(host.Texture);
                    UnloadImage(sync);
                    watch.Stop();
                    EndDrawing();
                    drawMs += watch.Elapsed.TotalMilliseconds;
                }
                return (Blur.BlurPassCount - passesBefore, drawMs);
            }

            var uncached = Run(false);
            var cached = Run(true);
            _output.WriteLine($"uncached: {uncached.Passes} blur passes, {uncached.Ms:F0} ms for {frames} frames");
            _output.WriteLine($"cached:   {cached.Passes} blur passes, {cached.Ms:F0} ms for {frames} frames");

            uncached.Passes.ShouldBe(elementCount * 2 * frames);
            cached.Passes.ShouldBe(elementCount * 2);
        }
        finally
        {
            Blur.CacheEnabled = true;
            UnloadRenderTexture(host);
        }
    }
}
