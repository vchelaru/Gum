using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;
using Xunit;

#pragma warning disable CS0618

namespace SkiaGum.GpuTests;

/// <summary>
/// Render-target containers draw on the GPU when the host canvas has a GRContext, and fall back to CPU raster
/// surfaces on a raster canvas (#8202). A raster canvas is the old CPU behavior, so it is the pixel reference.
/// </summary>
[Collection(GpuCollection.Name)]
public class RenderTargetGpuTests
{
    const int Width = 200;
    const int Height = 160;

    // CPU and GPU antialias text and gradient edges differently; the same scene drawn live (no render target) differs by 25 between them.
    const int CpuGpuTolerance = 32;

    // The same GPU canvas with and without a render target; measured 10 (a one-pixel sprite edge).
    const int SameBackendTolerance = 12;

    readonly WglGpuContext _gpu;

    public RenderTargetGpuTests(WglGpuContext gpu)
    {
        _gpu = gpu;
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    static ContainerRuntime BuildScene()
    {
        ContainerRuntime container = new()
        {
            X = 10,
            Y = 10,
            Width = 160,
            Height = 120,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            IsRenderTarget = true,
        };
        container.Children.Add(new RoundedRectangleRuntime
        {
            X = 5,
            Y = 5,
            Width = 140,
            Height = 50,
            CornerRadius = 12,
            UseGradient = true,
            GradientType = GradientType.Linear,
            GradientX1 = 0,
            GradientY1 = 0,
            GradientX2 = 140,
            GradientY2 = 50,
            Red1 = 255, Green1 = 80, Blue1 = 0, Alpha1 = 255,
            Red2 = 0, Green2 = 80, Blue2 = 255, Alpha2 = 255,
        });
        container.Children.Add(new TextRuntime
        {
            Text = "Hello Gum gyp",
            X = 8,
            Y = 62,
            Width = 140,
            Height = 20,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Color = SKColors.White,
            FontSize = 16,
        });
        SKBitmap texture = new(24, 24);
        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                texture.SetPixel(x, y, new SKColor((byte)(x * 10), (byte)(y * 10), 128, 255));
            }
        }
        container.Children.Add(new SpriteRuntime
        {
            Texture = texture,
            X = 100,
            Y = 85,
            Width = 48,
            Height = 30,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
        });
        return container;
    }

    static SystemManagers CreateManagers(SKCanvas canvas)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        SystemManagers managers = new();
        managers.Initialize();
        SystemManagers.Default = managers;
        managers.Canvas = canvas;
        managers.Renderer.ClearsCanvas = false;
        return managers;
    }

    static SKBitmap ReadBack(SKSurface surface)
    {
        SKBitmap bitmap = new(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0).ShouldBeTrue();
        return bitmap;
    }

    // Draws the scene for a number of frames on a raster canvas (the CPU path) and returns the last frame.
    static SKBitmap RenderOnCpu(ContainerRuntime container, int frames)
    {
        SKBitmap bitmap = new(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        SystemManagers managers = CreateManagers(canvas);
        container.AddToManagers(managers, null);
        for (int i = 0; i < frames; i++)
        {
            canvas.Clear(new SKColor(20, 20, 20));
            managers.Draw();
        }
        container.RemoveFromManagers();
        return bitmap;
    }

    // Draws the scene on the GPU surface; the callback sees the managers and surface after the last frame.
    void OnGpu(ContainerRuntime container, int frames, Action<SystemManagers, SKSurface, GRContext> check)
    {
        _gpu.WithSurface(Width, Height, (surface, context) =>
        {
            SystemManagers managers = CreateManagers(surface.Canvas);
            container.AddToManagers(managers, null);
            for (int i = 0; i < frames; i++)
            {
                surface.Canvas.Clear(new SKColor(20, 20, 20));
                managers.Draw();
            }
            check(managers, surface, context);
            container.RemoveFromManagers();
        });
    }

    static int MaxChannelDifference(SKBitmap a, SKBitmap b)
    {
        byte[] p = a.Bytes;
        byte[] q = b.Bytes;
        int max = 0;
        for (int i = 0; i < p.Length; i++)
        {
            max = Math.Max(max, Math.Abs(p[i] - q[i]));
        }
        return max;
    }

    [Fact]
    public void Draw_GpuCanvas_RenderTargetSurfaceIsGpuBacked()
    {
        ContainerRuntime container = BuildScene();

        OnGpu(container, 1, (managers, _, context) =>
        {
            SKSurface? target = managers.Renderer.TryGetRenderTargetSurfaceFor((IRenderableIpso)container.RenderableComponent!);
            target.ShouldNotBeNull();
            target.Canvas.Context.ShouldNotBeNull();
        });
    }

    [Fact]
    public void Draw_RasterCanvas_RenderTargetSurfaceFallsBackToCpu()
    {
        ContainerRuntime container = BuildScene();
        SKBitmap bitmap = new(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        SystemManagers managers = CreateManagers(canvas);
        container.AddToManagers(managers, null);
        managers.Draw();

        SKSurface? target = managers.Renderer.TryGetRenderTargetSurfaceFor((IRenderableIpso)container.RenderableComponent!);

        target.ShouldNotBeNull();
        target.Canvas.Context.ShouldBeNull();
        container.RemoveFromManagers();
        bitmap.Dispose();
    }

    [Fact]
    public void Draw_GpuRenderTarget_MatchesCpuRenderTarget()
    {
        // The first text laid out in a process measures differently; warm that up so both renders see the same state.
        RenderOnCpu(BuildScene(), 1).Dispose();
        using SKBitmap expected = RenderOnCpu(BuildScene(), 1);

        OnGpu(BuildScene(), 1, (_, surface, _) =>
        {
            using SKBitmap actual = ReadBack(surface);
            int d = MaxChannelDifference(actual, expected);
            d.ShouldBeLessThanOrEqualTo(CpuGpuTolerance, "max channel difference " + d);
        });
    }

    [Fact]
    public void Draw_GpuRenderTarget_MatchesGpuLiveDraw()
    {
        ContainerRuntime live = BuildScene();
        live.IsRenderTarget = false;
        SKBitmap? livePixels = null;
        OnGpu(live, 1, (_, surface, _) => livePixels = ReadBack(surface));

        OnGpu(BuildScene(), 1, (_, surface, _) =>
        {
            using SKBitmap actual = ReadBack(surface);
            int d = MaxChannelDifference(actual, livePixels!);
            d.ShouldBeLessThanOrEqualTo(SameBackendTolerance, "max channel difference " + d);
        });
        livePixels!.Dispose();
    }

    [Fact]
    public void Draw_ManualMode_BakesOnceAndMatchesEveryFrame()
    {
        ContainerRuntime everyFrame = BuildScene();
        ContainerRuntime manual = BuildScene();
        manual.RenderTargetRefreshMode = RenderTargetRefreshMode.Manual;
        SKBitmap? everyFramePixels = null;

        OnGpu(everyFrame, 3, (_, surface, _) => everyFramePixels = ReadBack(surface));
        OnGpu(manual, 3, (managers, surface, _) =>
        {
            managers.Renderer.RenderTargetBakeCount.ShouldBe(1);
            using SKBitmap actual = ReadBack(surface);
            MaxChannelDifference(actual, everyFramePixels!).ShouldBeLessThanOrEqualTo(SameBackendTolerance);
        });
        everyFramePixels!.Dispose();
    }

    [Fact]
    public void Draw_ContainerResized_RebuildsGpuSurfaceAtNewSize()
    {
        ContainerRuntime container = BuildScene();

        OnGpu(container, 1, (managers, surface, _) =>
        {
            IRenderableIpso renderable = (IRenderableIpso)container.RenderableComponent!;
            SKSurface first = managers.Renderer.TryGetRenderTargetSurfaceFor(renderable)!;
            first.Canvas.DeviceClipBounds.Width.ShouldBe(160);

            container.Width = 100;
            surface.Canvas.Clear(new SKColor(20, 20, 20));
            managers.Draw();

            SKSurface second = managers.Renderer.TryGetRenderTargetSurfaceFor(renderable)!;
            second.Canvas.DeviceClipBounds.Width.ShouldBe(100);
            second.Canvas.Context.ShouldNotBeNull();
        });
    }

    [Fact]
    public void UseContext_AlternatingCanvases_KeepsBothCaches()
    {
        SkiaRenderTargetService service = new();
        _gpu.WithSurface(8, 8, (surface, context) =>
        {
            InvisibleRenderable owner = new();
            service.UseContext(surface.Canvas.Context);
            SkiaRenderTarget gpuTarget = service.GetFor(owner, 16, 16)!;
            gpuTarget.Surface.Canvas.Context.ShouldNotBeNull();

            service.UseContext(null);
            SkiaRenderTarget cpuTarget = service.GetFor(owner, 16, 16)!;
            cpuTarget.Surface.Canvas.Context.ShouldBeNull();
            service.CachedCount.ShouldBe(2);

            service.UseContext(surface.Canvas.Context);
            service.GetFor(owner, 16, 16).ShouldBeSameAs(gpuTarget);
            service.UseContext(null);
            service.GetFor(owner, 16, 16).ShouldBeSameAs(cpuTarget);
            service.DisposeAll();
        });
    }

    [Fact]
    public void DisposeAll_DropsSurfacesAndContextReference()
    {
        SkiaRenderTargetService service = new();
        _gpu.WithSurface(8, 8, (surface, context) =>
        {
            service.UseContext(surface.Canvas.Context);
            service.GetFor(new InvisibleRenderable(), 16, 16);

            service.DisposeAll();

            service.CachedCount.ShouldBe(0);
            service.Context.ShouldBeNull();
        });
    }

    [Fact]
    public void Draw_AlternatingGpuAndRasterCanvas_ManualContainerKeepsBothCachesAndStaysCorrect()
    {
        ContainerRuntime container = BuildScene();
        container.RenderTargetRefreshMode = RenderTargetRefreshMode.Manual;
        RenderOnCpu(BuildScene(), 1).Dispose();
        SKBitmap? reference = null;
        OnGpu(BuildScene(), 1, (_, surface, _) => reference = ReadBack(surface));

        _gpu.WithSurface(Width, Height, (surface, _) =>
        {
            using SKBitmap bitmap = new(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
            using SKCanvas rasterCanvas = new(bitmap);
            SystemManagers managers = CreateManagers(surface.Canvas);
            container.AddToManagers(managers, null);

            // Two frames on each canvas, alternating every frame.
            for (int frame = 0; frame < 4; frame++)
            {
                managers.Canvas = frame % 2 == 0 ? surface.Canvas : rasterCanvas;
                managers.Canvas.Clear(new SKColor(20, 20, 20));
                managers.Draw();
            }
            managers.Canvas = surface.Canvas;
            surface.Canvas.Clear(new SKColor(20, 20, 20));
            managers.Draw();

            // One bake per canvas, never one per switch.
            managers.Renderer.RenderTargetBakeCount.ShouldBe(2);
            using SKBitmap gpuPixels = ReadBack(surface);
            MaxChannelDifference(gpuPixels, reference!).ShouldBeLessThanOrEqualTo(SameBackendTolerance);
            MaxChannelDifference(bitmap, reference!).ShouldBeLessThanOrEqualTo(CpuGpuTolerance);

            managers.ReleaseRenderTargets();
            managers.Renderer.TryGetRenderTargetSurfaceFor((IRenderableIpso)container.RenderableComponent!).ShouldBeNull();
            container.RemoveFromManagers();
        });
        reference!.Dispose();
    }

    [Fact]
    public void Draw_GpuRenderTarget_ServiceWalksAllocateNothingPerFrame()
    {
        // The per-frame cache bookkeeping (context selection, sweep, owner lookups) must not allocate. Measured
        // around the service only, since a full GPU draw allocates in Skia and the text path.
        SkiaRenderTargetService service = new();
        _gpu.WithSurface(8, 8, (surface, _) =>
        {
            InvisibleRenderable owner = new();
            GRRecordingContext? context = surface.Canvas.Context;
            service.UseContext(context);
            service.GetFor(owner, 16, 16);
            service.ClearUnusedRenderTargetsLastFrame();

            for (int pass = 0; pass < 2; pass++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++)
                {
                    service.ClearUnusedRenderTargetsLastFrame();
                    service.UseContext(context);
                    service.GetFor(owner, 16, 16);
                    service.MarkUsed(owner);
                    service.HasImage(owner);
                    service.TryGetExisting(owner);
                }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                if (pass == 1)
                {
                    allocated.ShouldBe(0);
                }
            }
            service.DisposeAll();
        });
    }
}
