using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #7920: with Nearest sampling a NineSlice draws all nine sections with one DrawImageLattice call. Every
/// case where the lattice cannot match the per-section draws pixel for pixel keeps the per-section path.
/// </summary>
public class NineSliceLatticeTests : IDisposable
{
    const int SurfaceWidth = 220;
    const int SurfaceHeight = 340;

    readonly SKSurface _surface;
    readonly SKFilterMode _previousFilter;

    public NineSliceLatticeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(SurfaceWidth, SurfaceHeight));
        GumService.Default.Initialize(_surface.Canvas, SurfaceWidth, SurfaceHeight);
        _previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
    }

    public void Dispose()
    {
        RenderingLibrary.Graphics.Renderer.TextureFilter = _previousFilter;
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
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
                byte r = (byte)(random.Next(256) * alpha / 255);
                byte g = (byte)(random.Next(256) * alpha / 255);
                byte b = (byte)(random.Next(256) * alpha / 255);
                bitmap.SetPixel(x, y, new SKColor(r, g, b, alpha));
            }
        }
        return bitmap;
    }

    record struct Result(SKBitmap Pixels, int DrawCalls);

    Result Draw(NineSlice nineSlice, Action<SKCanvas>? transform = null)
    {
        SKCanvas canvas = _surface.Canvas;
        using SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(SurfaceWidth, SurfaceHeight),
            new[] { SKColors.DarkRed, SKColors.CornflowerBlue }, SKShaderTileMode.Clamp);
        using SKPaint backdrop = new() { Shader = shader };
        canvas.DrawRect(0, 0, SurfaceWidth, SurfaceHeight, backdrop);

        var managers = SystemManagers.Default;
        managers.Renderer.RenderStateChangeStatistics.Reset();
        canvas.Save();
        transform?.Invoke(canvas);
        nineSlice.Render(managers);
        canvas.Restore();
        int drawCalls = managers.Renderer.RenderStateChangeStatistics.DrawCallCount;

        SKBitmap pixels = new(SurfaceWidth, SurfaceHeight);
        _surface.ReadPixels(pixels.Info, pixels.GetPixels(), pixels.RowBytes, 0, 0);
        return new Result(pixels, drawCalls);
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
                int d = Math.Max(
                    Math.Max(Math.Abs(p.Red - q.Red), Math.Abs(p.Green - q.Green)),
                    Math.Max(Math.Abs(p.Blue - q.Blue), Math.Abs(p.Alpha - q.Alpha)));
                max = Math.Max(max, d);
            }
        }
        return max;
    }

    NineSlice Create(SKBitmap texture, float x, float y, float width, float height, Action<NineSlice>? configure = null)
    {
        NineSlice nineSlice = new()
        {
            Texture = texture,
            X = x,
            Y = y,
            Width = width,
            Height = height,
        };
        configure?.Invoke(nineSlice);
        return nineSlice;
    }

    // The new path and the per-section path drawn from identical settings.
    (Result Fast, Result Old) DrawBoth(SKBitmap texture, float x, float y, float width, float height, Action<NineSlice>? configure = null, Action<SKCanvas>? transform = null)
    {
        using NineSlice fast = Create(texture, x, y, width, height, configure);
        Result fastResult = Draw(fast, transform);
        using NineSlice old = Create(texture, x, y, width, height, configure);
        old.DisableLatticeFastPath = true;
        Result oldResult = Draw(old, transform);
        return (fastResult, oldResult);
    }

    [Fact]
    public void Render_NearestDefaults_DrawsOneOperation()
    {
        using SKBitmap texture = Texture(false);

        (Result fast, Result old) = DrawBoth(texture, 10, 10, 64, 40);

        fast.DrawCalls.ShouldBe(1);
        old.DrawCalls.ShouldBe(9);
    }

    [Theory]
    [InlineData(10f, 10f, 64f, 40f, false, false, false, 1f)]
    [InlineData(10f, 12f, 64f, 40f, false, false, false, 1f)]
    [InlineData(11f, 7f, 63f, 42f, false, false, false, 1f)]
    [InlineData(10f, 10f, 64f, 40f, true, false, false, 1f)]
    [InlineData(10f, 10f, 64f, 40f, false, true, false, 1f)]
    [InlineData(10f, 10f, 64f, 40f, true, true, true, 1f)]
    [InlineData(10f, 10f, 64f, 40f, false, false, true, 1f)]
    [InlineData(10f, 10f, 80f, 60f, false, false, false, 2f)]
    [InlineData(10f, 11f, 64f, 48f, true, false, true, 2f)]
    [InlineData(10f, 10f, 96f, 80f, false, false, true, 3f)]
    public void Render_Nearest_MatchesPerSectionPixels(
        float x, float y, float width, float height, bool flipH, bool semi, bool customFrame, float borderScale)
    {
        using SKBitmap texture = Texture(semi);

        (Result fast, Result old) = DrawBoth(texture, x, y, width, height, ns =>
        {
            ns.FlipHorizontal = flipH;
            ns.BorderScale = borderScale;
            if (customFrame)
            {
                ns.CustomFrameTextureCoordinateWidth = 8;
            }
        });

        fast.DrawCalls.ShouldBe(1);
        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Fact]
    public void Render_FlipVerticalAndTinted_MatchesPerSectionPixels()
    {
        using SKBitmap texture = Texture(true);

        (Result fast, Result old) = DrawBoth(texture, 10, 11, 60, 44, ns =>
        {
            ns.FlipVertical = true;
            ns.Color = new SKColor(255, 128, 64, 200);
        });

        fast.DrawCalls.ShouldBe(1);
        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Fact]
    public void Render_EdgeOnHalfPixel_KeepsPerSectionDraws()
    {
        // GPU surfaces round a lattice edge on a half pixel differently from a rectangle's edge.
        using SKBitmap texture = Texture(false);

        (Result fast, Result old) = DrawBoth(texture, 10.5f, 10, 64, 40);

        fast.DrawCalls.ShouldBe(old.DrawCalls);
        old.DrawCalls.ShouldBeGreaterThan(1);
        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Fact]
    public void Render_LinearSampling_KeepsPerSectionDraws()
    {
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Linear;
        using SKBitmap texture = Texture(false);

        (Result fast, Result old) = DrawBoth(texture, 10.5f, 10.25f, 64, 40);

        fast.DrawCalls.ShouldBe(old.DrawCalls);
        old.DrawCalls.ShouldBeGreaterThan(1);
        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Fact]
    public void Render_TilingMiddleSections_KeepsPerSectionDraws()
    {
        using SKBitmap texture = Texture(false);

        (Result fast, Result old) = DrawBoth(texture, 10, 10, 64, 40, ns => ns.IsTilingMiddleSections = true);

        fast.DrawCalls.ShouldBeGreaterThan(1);
        fast.DrawCalls.ShouldBe(old.DrawCalls);
        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Fact]
    public void Render_DifferentBorderScalePerSide_KeepsPerSectionDraws()
    {
        using SKBitmap texture = Texture(false);

        (Result fast, Result old) = DrawBoth(texture, 10, 10, 100, 80, ns =>
        {
            ns.BorderScale = 2;
            ns.CustomBorderScaleLeft = 1;
        });

        fast.DrawCalls.ShouldBe(old.DrawCalls);
        old.DrawCalls.ShouldBeGreaterThan(1);
        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Theory]
    [InlineData(4, 4, 24, 24, 10f, 10f, 64f, 40f)]
    [InlineData(2, 6, 20, 22, 11f, 12f, 70f, 50f)]
    public void Render_PartialSourceRectangle_MatchesPerSectionPixelsInOneOperation(
        int left, int top, int width, int height, float x, float y, float w, float h)
    {
        // The texels around the rectangle are random, so any sampling outside the rectangle shows.
        using SKBitmap texture = Texture(true);

        (Result fast, Result old) = DrawBoth(texture, x, y, w, h,
            ns => ns.SourceRectangle = new System.Drawing.Rectangle(left, top, width, height));

        fast.DrawCalls.ShouldBe(1);
        old.DrawCalls.ShouldBe(9);
        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Fact]
    public void Render_ZoomedWithFractionalParentOffset_MatchesPerSectionPixels()
    {
        // Found by fuzzing: an edge at device y 111.7956 differed by up to 214 on a raster surface.
        using SKBitmap texture = Texture(true, 24);

        (Result fast, Result old) = DrawBoth(texture, -5, 23.3978f, 77, 102, ns =>
        {
            ns.BorderScale = 3;
            ns.FlipVertical = true;
        }, canvas =>
        {
            canvas.Scale(2);
            canvas.Translate(0, 32.5f);
        });

        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Fact]
    public void Render_ShrunkWithFractionalParentOffset_MatchesPerSectionPixels()
    {
        using SKBitmap texture = Texture(true, 15);

        (Result fast, Result old) = DrawBoth(texture, 34.52f, 32.52f, 22.889f, 23.663f, ns =>
        {
            ns.BorderScale = 2;
            ns.FlipHorizontal = true;
            ns.FlipVertical = true;
        }, canvas =>
        {
            canvas.Scale(0.5f);
            canvas.Translate(0, 7.915f);
        });

        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Fact]
    public void Render_SeededFractionalFuzz_EngagedCasesMatchPerSectionPixels()
    {
        Random random = new(20260711);
        float[] canvasScales = { 0.5f, 1f, 1.25f, 1.5f, 2f, 3f };
        float[] fractions = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.02f, 0.05f, 0.1f, 0.25f, 0.5f, 0.75f };
        int engaged = 0;
        for (int i = 0; i < 300; i++)
        {
            int size = random.Next(12, 33);
            using SKBitmap texture = Texture(random.Next(2) == 0, size);
            float x = random.Next(0, 40) + fractions[random.Next(fractions.Length)];
            float y = random.Next(0, 40) + fractions[random.Next(fractions.Length)];
            float w = random.Next(30, 90) + fractions[random.Next(fractions.Length)];
            float h = random.Next(30, 90) + fractions[random.Next(fractions.Length)];
            float scale = new[] { 1f, 1f, 2f, 3f }[random.Next(4)];
            float zoom = canvasScales[random.Next(canvasScales.Length)];
            float offsetY = random.Next(0, 20) + fractions[random.Next(fractions.Length)];
            bool flipH = random.Next(2) == 0;
            bool flipV = random.Next(2) == 0;
            bool partial = random.Next(3) == 0 && size >= 16;

            (Result fast, Result old) = DrawBoth(texture, x, y, w, h, ns =>
            {
                ns.BorderScale = scale;
                ns.FlipHorizontal = flipH;
                ns.FlipVertical = flipV;
                if (partial)
                {
                    ns.SourceRectangle = new System.Drawing.Rectangle(1, 2, size - 3, size - 4);
                }
            }, canvas =>
            {
                canvas.Scale(zoom);
                canvas.Translate(0, offsetY);
            });

            if (fast.DrawCalls == 1)
            {
                engaged++;
            }
            MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0,
                $"case {i}: size {size} x {x} y {y} w {w} h {h} scale {scale} zoom {zoom} offsetY {offsetY} flip {flipH}/{flipV} partial {partial}");
        }
        engaged.ShouldBeGreaterThan(8);
    }

    [Fact]
    public void Render_SmallerThanBorders_KeepsPerSectionDraws()
    {
        using SKBitmap texture = Texture(false);

        (Result fast, Result old) = DrawBoth(texture, 10, 10, 12, 9);

        fast.DrawCalls.ShouldBe(old.DrawCalls);
        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }

    [Fact]
    public void Render_ProjectLoadedNineSlice_UsesOneOperation()
    {
        // Set the way a loaded project assigns it: through SetProperty, Blend and TextureAddress explicit,
        // the texture from SourceFile, and the frame NineSlice's BorderScale of 2.
        LoaderManager.Self.ContentLoader = new SkiaGum.Tests.GueDeriving.MockContentLoader(32, 32);
        NineSliceRuntime runtime = new();
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.SetProperty("Blend", Gum.RenderingLibrary.Blend.Normal);
        runtime.SetProperty("SourceFile", "9slice_border.png");
        runtime.SetProperty("TextureAddress", Gum.Managers.TextureAddress.EntireTexture);
        runtime.SetProperty("BorderScale", 2f);
        runtime.SetProperty("IsTilingMiddleSections", false);
        runtime.X = 10;
        runtime.Y = 10;
        runtime.Width = 80;
        runtime.Height = 60;

        Result result = Draw((NineSlice)runtime.RenderableComponent);

        result.DrawCalls.ShouldBe(1);
    }

    [Fact]
    public void Render_ScaledBordersWithIntegerParentOffset_MatchesPerSectionPixels()
    {
        // Found by fuzzing: border scale 3 under a parent translate of 7 differed by up to 237 on a raster surface, all in
        // one device column whose pixel centers sit exactly on a texel boundary (8 texels over 61 pixels).
        using SKBitmap texture = Texture(true, 24);

        (Result fast, Result old) = DrawBoth(texture, 1, 29, 109, 74, ns => ns.BorderScale = 3, canvas => canvas.Translate(7, 7));

        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
    }
}
