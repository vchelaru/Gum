using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Manual refresh mode for SkiaGum render-target containers (#7946), matching the MonoGame semantics:
/// bake once, draw the cached image until RefreshRenderTarget(), re-bake on size or zoom change, release
/// the cache when the container is not drawn for a frame. Behavior is proven with bake counts and pixels.
/// </summary>
public class RenderTargetManualRefreshTests
{
    // Halves every channel, so applying it twice (once into the cache, again at composite) reads as 25%.
    private const string HalveSksl = @"
uniform shader SpriteTexture;
half4 main(float2 coord) {
    return SpriteTexture.eval(coord) * 0.5;
}
";

    public RenderTargetManualRefreshTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static RenderingLibrary.Graphics.Renderer SkiaRenderer => SystemManagers.Default.Renderer;

    private static ContainerRuntime CreateContainer(SKColor color, float x = 4, float y = 4,
        RenderTargetRefreshMode mode = RenderTargetRefreshMode.Manual)
    {
        ContainerRuntime container = new()
        {
            X = x,
            Y = y,
            Width = 40,
            Height = 40,
            IsRenderTarget = true,
            RenderTargetRefreshMode = mode,
        };
        container.Children.Add(new RectangleRuntime
        {
            Width = 30,
            Height = 30,
            IsFilled = true,
            FillColor = color,
        });
        return container;
    }

    private static void SetChildColor(ContainerRuntime container, SKColor color) =>
        ((RectangleRuntime)container.Children[0]).FillColor = color;

    // GumService.Initialize turns the renderer's own clear off, so each frame starts from a cleared surface.
    private static void DrawFrame(SKSurface surface)
    {
        surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Draw();
    }

    private static SKColor PixelAt(SKSurface surface, int x, int y)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }

    private static byte[] Bytes(SKSurface surface)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    [Fact]
    public void Draw_ManualContainerTwice_BakesOnce()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        GumService.Default.Root.Children.Add(container);

        DrawFrame(surface);
        DrawFrame(surface);
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(1);
        PixelAt(surface, 10, 10).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void Draw_EveryFrameContainerTwice_BakesEachFrame()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red, mode: RenderTargetRefreshMode.EveryFrame);
        GumService.Default.Root.Children.Add(container);

        DrawFrame(surface);
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(2);
    }

    [Fact]
    public void Draw_ManualChildChanged_KeepsOldPixelsUntilRefresh()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);

        SetChildColor(container, SKColors.Blue);
        DrawFrame(surface);
        PixelAt(surface, 10, 10).ShouldBe(SKColors.Red);
        SkiaRenderer.RenderTargetBakeCount.ShouldBe(1);

        container.RefreshRenderTarget();
        DrawFrame(surface);
        PixelAt(surface, 10, 10).ShouldBe(SKColors.Blue);
        SkiaRenderer.RenderTargetBakeCount.ShouldBe(2);
    }

    [Fact]
    public void Draw_ManualRefreshedTwiceBetweenDraws_BakesOncePerDraw()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);

        container.RefreshRenderTarget();
        container.RefreshRenderTarget();
        DrawFrame(surface);
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(2);
    }

    [Fact]
    public void Draw_ManualResized_Rebakes()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);

        container.Width = 50;
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(2);
    }

    [Fact]
    public void Draw_ManualResizedByLessThanAPixel_Rebakes()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        container.Width = 50;
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);

        container.Width = 50.4f;
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(2);
    }

    [Fact]
    public void Draw_ManualZoomChanged_Rebakes()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);

        SkiaRenderer.Camera.Zoom = 2;
        DrawFrame(surface);
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(2);
    }

    [Fact]
    public void Draw_ManualMoved_DrawsCachedImageAtNewPositionWithoutRebake()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red, x: 0, y: 0);
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);

        container.X = 20;
        container.Y = 20;
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(1);
        PixelAt(surface, 30, 30).ShouldBe(SKColors.Red);
        PixelAt(surface, 5, 5).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Draw_ManualHiddenForAFrame_ReleasesCacheAndRebakesWhenShownAgain()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);

        container.Visible = false;
        DrawFrame(surface);
        DrawFrame(surface);
        SkiaRenderer.TryGetRenderTargetSurfaceFor(container).ShouldBeNull();

        container.Visible = true;
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(2);
        PixelAt(surface, 10, 10).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void Draw_ManualWithEffect_AppliesEffectOnceIntoTheCache()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(HalveSksl, out string errors);
        string.IsNullOrEmpty(errors).ShouldBeTrue(errors);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        container.RenderTargetEffect = effect;
        GumService.Default.Root.Children.Add(container);

        DrawFrame(surface);
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(1);
        SKColor pixel = PixelAt(surface, 10, 10);
        // Read back unpremultiplied: halved once is half alpha, halved at composite as well would be a quarter.
        pixel.Alpha.ShouldBeInRange((byte)120, (byte)135);
    }

    [Fact]
    public void Draw_ManualWithEffect_MatchesEveryFramePixels()
    {
        byte[] everyFrame = RenderEffectScene(RenderTargetRefreshMode.EveryFrame);
        byte[] manual = RenderEffectScene(RenderTargetRefreshMode.Manual);

        manual.ShouldBe(everyFrame);
    }

    private static byte[] RenderEffectScene(RenderTargetRefreshMode mode)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(HalveSksl, out string errors);
        string.IsNullOrEmpty(errors).ShouldBeTrue(errors);
        ContainerRuntime container = CreateContainer(SKColors.Red, mode: mode);
        container.RenderTargetEffect = effect;
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);
        DrawFrame(surface);
        return Bytes(surface);
    }

    [Fact]
    public void Draw_SpriteReadingManualContainer_ShowsCachedImageUntilRefresh()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red, x: 0, y: 0);
        container.Visible = false;
        GumService.Default.Root.Children.Add(container);
        SpriteRuntime sprite = new()
        {
            X = 30,
            Y = 30,
            Width = 30,
            Height = 30,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            RenderTargetTextureSource = container,
        };
        GumService.Default.Root.Children.Add(sprite);
        DrawFrame(surface);

        SetChildColor(container, SKColors.Blue);
        DrawFrame(surface);

        PixelAt(surface, 40, 40).ShouldBe(SKColors.Red);
        SkiaRenderer.RenderTargetBakeCount.ShouldBe(1);

        container.RefreshRenderTarget();
        DrawFrame(surface);
        PixelAt(surface, 40, 40).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void Draw_ManualSwitchedToEveryFrame_DropsCacheAndBakesEveryFrame()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);

        container.RenderTargetRefreshMode = RenderTargetRefreshMode.EveryFrame;
        SetChildColor(container, SKColors.Blue);
        DrawFrame(surface);
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(3);
        PixelAt(surface, 10, 10).ShouldBe(SKColors.Blue);
        SkiaRenderer.HasManualRenderTargetImage(container).ShouldBeFalse();
    }

    [Fact]
    public void Draw_ManualContainerAboveSizeCap_FallsBackToEveryFrame()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime container = CreateContainer(SKColors.Red);
        container.Width = 5000;
        container.Height = 8;
        GumService.Default.Root.Children.Add(container);

        DrawFrame(surface);
        DrawFrame(surface);

        SkiaRenderer.RenderTargetBakeCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(1f, 4f, 4f)]
    [InlineData(1f, 4.5f, 6.25f)]
    [InlineData(2f, 4.5f, 6.25f)]
    public void Draw_StaticScene_ManualAndEveryFrameGiveIdenticalPixels(float zoom, float x, float y)
    {
        byte[] everyFrame = RenderStaticScene(RenderTargetRefreshMode.EveryFrame, zoom, x, y);
        byte[] manual = RenderStaticScene(RenderTargetRefreshMode.Manual, zoom, x, y);

        manual.ShouldBe(everyFrame);
        manual.Any(b => b != 0).ShouldBeTrue();
    }

    private static byte[] RenderStaticScene(RenderTargetRefreshMode mode, float zoom, float x, float y)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(128, 128));
        GumService.Default.Initialize(surface.Canvas, 128, 128);
        SkiaRenderer.Camera.Zoom = zoom;
        ContainerRuntime container = CreateContainer(SKColors.Red, x, y, mode);
        container.Alpha = 200;
        container.Children.Add(new RectangleRuntime
        {
            X = 15,
            Y = 5,
            Width = 20,
            Height = 20,
            IsFilled = true,
            FillColor = SKColors.Lime,
        });
        GumService.Default.Root.Children.Add(container);
        DrawFrame(surface);
        DrawFrame(surface);
        return Bytes(surface);
    }
}
