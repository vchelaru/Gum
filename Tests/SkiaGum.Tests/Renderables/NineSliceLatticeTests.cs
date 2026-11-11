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
    const int SurfaceWidth = 140;
    const int SurfaceHeight = 100;

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
                byte r = (byte)(random.Next(256) * alpha / 255);
                byte g = (byte)(random.Next(256) * alpha / 255);
                byte b = (byte)(random.Next(256) * alpha / 255);
                bitmap.SetPixel(x, y, new SKColor(r, g, b, alpha));
            }
        }
        return bitmap;
    }

    record struct Result(SKBitmap Pixels, int DrawCalls);

    Result Draw(NineSlice nineSlice)
    {
        SKCanvas canvas = _surface.Canvas;
        using SKShader shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(SurfaceWidth, SurfaceHeight),
            new[] { SKColors.DarkRed, SKColors.CornflowerBlue }, SKShaderTileMode.Clamp);
        using SKPaint backdrop = new() { Shader = shader };
        canvas.DrawRect(0, 0, SurfaceWidth, SurfaceHeight, backdrop);

        var managers = SystemManagers.Default;
        managers.Renderer.RenderStateChangeStatistics.Reset();
        nineSlice.Render(managers);
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
    (Result Fast, Result Old) DrawBoth(SKBitmap texture, float x, float y, float width, float height, Action<NineSlice>? configure = null)
    {
        using NineSlice fast = Create(texture, x, y, width, height, configure);
        Result fastResult = Draw(fast);
        using NineSlice old = Create(texture, x, y, width, height, configure);
        old.DisableLatticeFastPath = true;
        Result oldResult = Draw(old);
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
    [InlineData(10.25f, 10.75f, 64f, 40f, false, false, false, 1f)]
    [InlineData(10.3f, 10.7f, 63.4f, 41.6f, false, false, false, 1f)]
    [InlineData(10f, 10f, 64f, 40f, true, false, false, 1f)]
    [InlineData(10f, 10f, 64f, 40f, false, true, false, 1f)]
    [InlineData(10f, 10f, 64f, 40f, true, true, true, 1f)]
    [InlineData(10f, 10f, 64f, 40f, false, false, true, 1f)]
    [InlineData(10f, 10f, 80f, 60f, false, false, false, 2f)]
    [InlineData(10.25f, 10.75f, 63.4f, 41.6f, true, false, true, 2f)]
    [InlineData(10.3f, 10.7f, 63.4f, 41.6f, false, false, true, 1.5f)]
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

        (Result fast, Result old) = DrawBoth(texture, 10.25f, 10.75f, 60, 44, ns =>
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

    [Fact]
    public void Render_PartialSourceRectangle_KeepsPerSectionDraws()
    {
        using SKBitmap texture = Texture(false);

        (Result fast, Result old) = DrawBoth(texture, 10, 10, 64, 40,
            ns => ns.SourceRectangle = new System.Drawing.Rectangle(4, 4, 24, 24));

        fast.DrawCalls.ShouldBe(old.DrawCalls);
        old.DrawCalls.ShouldBeGreaterThan(1);
        MaxDifference(fast.Pixels, old.Pixels).ShouldBe(0);
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
}
