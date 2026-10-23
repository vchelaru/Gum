using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Renderer.RenderLayer and a SortableLayer drawn as a renderable inside another layer, matching
/// MonoGame: the layer is PreRendered, sorted by Z and drawn under its own camera.
/// </summary>
public class RendererRenderLayerTests : IDisposable
{
    readonly List<IRenderableIpso> _added = new();

    public RendererRenderLayerTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void RenderLayer_RenderablesAddedAgainstZ_DrawsByZ()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        Layer layer = new();
        RectangleRuntime red = CreateRectangle(x: 0, y: 0, width: 16, SKColors.Red);
        red.Z = 1;
        RectangleRuntime lime = CreateRectangle(x: 8, y: 8, width: 16, SKColors.Lime);
        layer.Add(red);
        layer.Add(lime);

        SystemManagers.Default.Renderer.RenderLayer(SystemManagers.Default, layer);

        PixelAt(surface, 12, 12).ShouldBe(SKColors.Red, "because RenderLayer sorts by Z, so the higher-Z red draws last");
    }

    [Fact]
    public void RenderLayer_DefaultPrerender_PreRendersBeforeSorting()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        Layer layer = new() { SecondarySortOnY = true };
        MovesOnPreRenderRectangle red = new() { Y = 0, Width = 16, Height = 16, IsFilled = true, FillColor = SKColors.Red, PreRenderY = 8 };
        RectangleRuntime lime = CreateRectangle(x: 0, y: 4, width: 16, SKColors.Lime);
        layer.Add(red);
        layer.Add(lime);

        SystemManagers.Default.Renderer.RenderLayer(SystemManagers.Default, layer);

        PixelAt(surface, 4, 12).ShouldBe(SKColors.Red, "because RenderLayer PreRenders first, moving red to Y 8, below lime");
    }

    [Fact]
    public void SortableLayer_InMainLayer_DrawsSortedWithItsSiblings()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        SortableLayer sortableLayer = new();
        sortableLayer.Add(CreateRectangle(x: 0, y: 0, width: 32, SKColors.Lime));
        SortableLayerHost host = new(sortableLayer) { Z = 1 };
        RectangleRuntime red = CreateRectangle(x: 0, y: 0, width: 32, SKColors.Red);
        red.Z = 0;
        RectangleRuntime blue = CreateRectangle(x: 16, y: 0, width: 16, SKColors.Blue);
        blue.Z = 2;
        AddToMainLayer(blue);
        AddToMainLayer(host);
        AddToMainLayer(red);

        SystemManagers.Default.Draw();

        PixelAt(surface, 4, 4).ShouldBe(SKColors.Lime, "because the sortable layer (Z 1) draws over red (Z 0)");
        PixelAt(surface, 20, 4).ShouldBe(SKColors.Blue, "because blue (Z 2) draws over the sortable layer");
    }

    [Fact]
    public void SortableLayer_InMainLayerWithCameraOffset_AppliesTheCameraOnce()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        SystemManagers.Default.Renderer.Camera.X = -8;
        SortableLayer sortableLayer = new();
        sortableLayer.Add(CreateRectangle(x: 0, y: 0, width: 8, SKColors.Red));
        AddToMainLayer(new SortableLayerHost(sortableLayer));

        SystemManagers.Default.Draw();

        PixelAt(surface, 4, 4).Alpha.ShouldBe((byte)0, "because the camera moves the rectangle right by 8");
        PixelAt(surface, 12, 4).ShouldBe(SKColors.Red, "because the camera is applied once, not once per layer");
        PixelAt(surface, 20, 4).Alpha.ShouldBe((byte)0, "because a second camera offset would put the rectangle here");
    }

    [Fact]
    public void SortableLayer_InScreenSpaceInsideAWorldLayer_UsesItsOwnCamera()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        SystemManagers.Default.Renderer.Camera.X = -8;
        SortableLayer sortableLayer = new() { LayerCameraSettings = new LayerCameraSettings { IsInScreenSpace = true } };
        sortableLayer.Add(CreateRectangle(x: 0, y: 0, width: 8, SKColors.Red));
        AddToMainLayer(new SortableLayerHost(sortableLayer));

        SystemManagers.Default.Draw();

        PixelAt(surface, 4, 4).ShouldBe(SKColors.Red, "because, as in MonoGame, the sortable layer draws under its own camera, which ignores the main camera in screen space");
        PixelAt(surface, 12, 4).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void SortableLayer_DrawnThroughGumBatchWithAMatrix_AppliesTheMatrixOnce()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        SortableLayer sortableLayer = new();
        sortableLayer.Add(CreateRectangle(x: 0, y: 0, width: 8, SKColors.Red));
        RenderingLibrary.Graphics.Renderer renderer = SystemManagers.Default.Renderer;

        renderer.Begin(SKMatrix.CreateTranslation(8, 0));
        renderer.Draw(new SortableLayerHost(sortableLayer));
        renderer.End();

        PixelAt(surface, 4, 4).Alpha.ShouldBe((byte)0, "because the GumBatch matrix moves the rectangle right by 8");
        PixelAt(surface, 12, 4).ShouldBe(SKColors.Red, "because the sortable layer keeps the GumBatch matrix, as MonoGame's ForcedMatrix applies to every layer");
        PixelAt(surface, 20, 4).Alpha.ShouldBe((byte)0, "because the matrix is applied once");
    }

    [Fact]
    public void SortableLayer_ContainingARenderTarget_BakesAndDrawsIt()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        ContainerRuntime renderTarget = new() { Width = 16, Height = 16, IsRenderTarget = true };
        renderTarget.Children.Add(CreateRectangle(x: 0, y: 0, width: 16, SKColors.Red));
        SortableLayer sortableLayer = new();
        sortableLayer.Add(renderTarget);
        AddToMainLayer(new SortableLayerHost(sortableLayer));

        SystemManagers.Default.Draw();

        PixelAt(surface, 4, 4).ShouldBe(SKColors.Red, "because RenderLayer's PreRender bakes render targets, as MonoGame's does");
    }

    [Fact]
    public void SortableLayer_InsideARenderTargetWithCameraOffsetAndZoom_BakesLikeThePlainChildren()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        SystemManagers.Default.Renderer.Camera.X = -8;
        SystemManagers.Default.Renderer.Camera.Zoom = 2;
        SortableLayer sortableLayer = new();
        sortableLayer.Add(CreateRectangle(x: 0, y: 0, width: 8, SKColors.Red));
        ContainerRuntime renderTarget = new() { Width = 16, Height = 16, IsRenderTarget = true };
        renderTarget.Children.Add(new GraphicalUiElement(new SortableLayerHost(sortableLayer)));
        AddToMainLayer(renderTarget);

        SystemManagers.Default.Draw();

        PixelAt(surface, 20, 4).ShouldBe(SKColors.Red, "because, as in MonoGame, the bake draws the sortable layer in render-target space like any other child, and the composite applies the camera once (world 0 maps to screen 16)");
    }

    [Fact]
    public void SortableLayer_InsideARenderTargetInsideASortableLayer_BakesInRenderTargetSpace()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        SystemManagers.Default.Renderer.Camera.X = -8;
        SortableLayer innerLayer = new();
        innerLayer.Add(CreateRectangle(x: 0, y: 0, width: 8, SKColors.Red));
        ContainerRuntime renderTarget = new() { Width = 16, Height = 16, IsRenderTarget = true };
        renderTarget.Children.Add(new GraphicalUiElement(new SortableLayerHost(innerLayer)));
        SortableLayer outerLayer = new();
        outerLayer.Add(renderTarget);
        AddToMainLayer(new SortableLayerHost(outerLayer));

        SystemManagers.Default.Draw();

        PixelAt(surface, 4, 4).Alpha.ShouldBe((byte)0, "because the camera moves the render target right by 8");
        PixelAt(surface, 12, 4).ShouldBe(SKColors.Red, "because the inner layer bakes in render-target space even while the outer layer's camera is active");
    }

    [Fact]
    public void Draw_RenderThrows_RestoresTheCanvasAndCameraState()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        int saveCountBefore = surface.Canvas.SaveCount;
        ThrowingRenderable throwing = new();
        AddToMainLayer(throwing);

        Should.Throw<InvalidOperationException>(() => SystemManagers.Default.Draw());

        surface.Canvas.SaveCount.ShouldBe(saveCountBefore, "because the camera's Save is restored even when a Render throws");
        SystemManagers.Default.Renderer.MainLayer.Remove(throwing);
        Layer layer = new();
        layer.Add(CreateRectangle(x: 0, y: 0, width: 8, SKColors.Red));
        surface.Canvas.Translate(8, 0);
        SystemManagers.Default.Renderer.RenderLayer(SystemManagers.Default, layer);
        PixelAt(surface, 12, 4).ShouldBe(SKColors.Red, "because a standalone RenderLayer keeps the host's transform, not one left over from the failed Draw");
    }

    [Fact]
    public void Draw_RenderThrowsWhileBaking_RestoresTheScreenCanvas()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        SKCanvas screenCanvas = SystemManagers.Default.Canvas;
        ContainerRuntime renderTarget = new() { Width = 16, Height = 16, IsRenderTarget = true };
        renderTarget.Children.Add(new GraphicalUiElement(new ThrowingRenderable()));
        AddToMainLayer(renderTarget);

        Should.Throw<InvalidOperationException>(() => SystemManagers.Default.Draw());

        SystemManagers.Default.Canvas.ShouldBeSameAs(screenCanvas, "because the bake puts the screen canvas back even when a Render throws");
    }

    public void Dispose()
    {
        Layer mainLayer = SystemManagers.Default.Renderer.MainLayer;
        foreach (IRenderableIpso renderable in _added)
        {
            mainLayer.Remove(renderable);
        }
        SystemManagers.Default.Renderer.Camera.X = 0;
        SystemManagers.Default.Renderer.Camera.Zoom = 1;
    }

    private void AddToMainLayer(IRenderableIpso renderable)
    {
        SystemManagers.Default.Renderer.MainLayer.Add(renderable);
        _added.Add(renderable);
    }

    private static SKSurface CreateInitializedSurface(int size)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(size, size));
        GumService.Default.Initialize(surface.Canvas, size, size);
        surface.Canvas.Clear(SKColors.Transparent);
        return surface;
    }

    private static RectangleRuntime CreateRectangle(float x, float y, float width, SKColor color) => new()
    {
        X = x,
        Y = y,
        Width = width,
        Height = 16,
        IsFilled = true,
        FillColor = color,
    };

    private static SKColor PixelAt(SKSurface surface, int x, int y)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }

    // A SortableLayer is an IRenderable, not an IRenderableIpso, so it reaches a layer through a
    // renderable that draws it.
    private class SortableLayerHost : InvisibleRenderable
    {
        readonly SortableLayer _sortableLayer;

        public SortableLayerHost(SortableLayer sortableLayer)
        {
            _sortableLayer = sortableLayer;
        }

        public override void Render(ISystemManagers managers) => _sortableLayer.Render(managers);
    }

    private class ThrowingRenderable : InvisibleRenderable
    {
        public override void Render(ISystemManagers managers) => throw new InvalidOperationException("Render failed");
    }

    private class MovesOnPreRenderRectangle : RectangleRuntime
    {
        public float PreRenderY { get; set; }

        public override void PreRender()
        {
            Y = PreRenderY;
            base.PreRender();
        }
    }
}
