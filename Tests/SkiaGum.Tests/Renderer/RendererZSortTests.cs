using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// The full-scene Draw sorts each layer's renderables by Z every frame, the way MonoGame's
/// RenderLayer calls Layer.SortRenderables, asserting on pixels of a raster surface.
/// </summary>
public class RendererZSortTests : IDisposable
{
    readonly List<IRenderableIpso> _added = new();

    public RendererZSortTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void Draw_LayerRenderablesAddedAgainstZ_DrawsByZ()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        RectangleRuntime red = CreateRectangle(x: 0, y: 0, SKColors.Red);
        red.Z = 1;
        RectangleRuntime lime = CreateRectangle(x: 8, y: 8, SKColors.Lime);
        lime.Z = 0;
        Layer mainLayer = SystemManagers.Default.Renderer.MainLayer;
        AddToMainLayer(red);
        AddToMainLayer(lime);

        SystemManagers.Default.Draw();

        PixelAt(surface, 12, 12).ShouldBe(SKColors.Red, "because the higher-Z red draws last even though it was added first");
        mainLayer.Renderables.IndexOf(lime).ShouldBeLessThan(mainLayer.Renderables.IndexOf(red), "because the sort reorders the layer's own list, as MonoGame's does");
    }

    [Fact]
    public void Draw_ZChangedAfterADraw_ResortsOnTheNextDraw()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        RectangleRuntime red = CreateRectangle(x: 0, y: 0, SKColors.Red);
        RectangleRuntime lime = CreateRectangle(x: 8, y: 8, SKColors.Lime);
        AddToMainLayer(red);
        AddToMainLayer(lime);
        SystemManagers.Default.Draw();
        PixelAt(surface, 12, 12).ShouldBe(SKColors.Lime, "because equal Z keeps add order");

        red.Z = 1;
        SystemManagers.Default.Draw();

        PixelAt(surface, 12, 12).ShouldBe(SKColors.Red, "because the layer is sorted on every Draw, not only when added");
    }

    [Fact]
    public void Draw_EqualZWithSecondarySortOnY_DrawsByY()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        RectangleRuntime red = CreateRectangle(x: 0, y: 8, SKColors.Red);
        RectangleRuntime lime = CreateRectangle(x: 0, y: 0, SKColors.Lime);
        Layer mainLayer = SystemManagers.Default.Renderer.MainLayer;
        mainLayer.SecondarySortOnY = true;
        AddToMainLayer(red);
        AddToMainLayer(lime);

        SystemManagers.Default.Draw();

        PixelAt(surface, 4, 12).ShouldBe(SKColors.Red, "because at equal Z the lower (greater Y) red draws last");
    }

    [Fact]
    public void Draw_PreRenderMovesY_SortsOnTheMovedY()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        MovesOnPreRenderRectangle red = new() { Y = 0, Width = 16, Height = 16, IsFilled = true, FillColor = SKColors.Red, PreRenderY = 8 };
        RectangleRuntime lime = CreateRectangle(x: 0, y: 4, SKColors.Lime);
        SystemManagers.Default.Renderer.MainLayer.SecondarySortOnY = true;
        AddToMainLayer(red);
        AddToMainLayer(lime);

        SystemManagers.Default.Draw();

        PixelAt(surface, 4, 12).ShouldBe(SKColors.Red, "because the layer sorts after PreRender, as MonoGame's RenderLayer does, so red's moved Y of 8 puts it below lime");
    }

    [Fact]
    public void Draw_ChildrenWithZAgainstOrder_DrawInChildOrder()
    {
        using SKSurface surface = CreateInitializedSurface(32);
        ContainerRuntime parent = new() { Width = 32, Height = 32 };
        RectangleRuntime red = CreateRectangle(x: 0, y: 0, SKColors.Red);
        red.Z = 1;
        RectangleRuntime lime = CreateRectangle(x: 8, y: 8, SKColors.Lime);
        parent.Children.Add(red);
        parent.Children.Add(lime);
        AddToMainLayer(parent);

        SystemManagers.Default.Draw();

        PixelAt(surface, 12, 12).ShouldBe(SKColors.Lime, "because MonoGame sorts only a layer's top-level renderables, never a parent's children");
    }

    public void Dispose()
    {
        Layer mainLayer = SystemManagers.Default.Renderer.MainLayer;
        foreach (IRenderableIpso renderable in _added)
        {
            mainLayer.Remove(renderable);
        }
        mainLayer.SecondarySortOnY = false;
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
        return surface;
    }

    private static RectangleRuntime CreateRectangle(float x, float y, SKColor color) => new()
    {
        X = x,
        Y = y,
        Width = 16,
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
