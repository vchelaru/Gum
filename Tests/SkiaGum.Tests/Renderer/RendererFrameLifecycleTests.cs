using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Pins what a full-scene <see cref="RenderingLibrary.Graphics.Renderer.Draw(SystemManagers?)"/> does once
/// per call (clear, render-target bake) versus once per layer (camera transform), so GumBatch's
/// Begin/End cycles can share the per-layer half without the per-frame half.
/// </summary>
public class RendererFrameLifecycleTests
{
    public RendererFrameLifecycleTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private sealed class PreRenderCounter : InvisibleRenderable
    {
        public int PreRenderCount;
        public override void PreRender() => PreRenderCount++;
    }

    // Adds a child the first time it is pre-rendered, as a renderable that builds content in PreRender would.
    private sealed class ChildAddingRenderable : InvisibleRenderable
    {
        public PreRenderCounter? Added;
        public override void PreRender()
        {
            if (Added == null)
            {
                Added = new PreRenderCounter();
                Children.Add(new GraphicalUiElement(Added));
            }
        }
    }

    private static SKSurface InitializeSurface()
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(32, 32));
        GumService.Default.Initialize(surface.Canvas, 32, 32);
        return surface;
    }

    [Fact]
    public void Draw_NestedChild_PreRendersOncePerFrame()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime outer = new();
        ContainerRuntime middle = new();
        PreRenderCounter counter = new();
        GraphicalUiElement leaf = new(counter);
        middle.Children.Add(leaf);
        outer.Children.Add(middle);
        GumService.Default.Root.Children.Add(outer);

        GumService.Default.Draw();

        counter.PreRenderCount.ShouldBe(1);
    }

    [Fact]
    public void Draw_DeeplyNestedChildren_PreRenderEachNodeOncePerFrame()
    {
        using SKSurface surface = InitializeSurface();
        List<PreRenderCounter> counters = new();
        GraphicalUiElement parent = GumService.Default.Root;
        for (int depth = 0; depth < 6; depth++)
        {
            PreRenderCounter counter = new();
            GraphicalUiElement node = new(counter);
            parent.Children.Add(node);
            counters.Add(counter);
            parent = node;
        }

        GumService.Default.Draw();

        counters.Select(item => item.PreRenderCount).ShouldAllBe(count => count == 1);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1000f)]
    public void Draw_ClippedContainerChild_PreRendersOncePerFrame(float containerX)
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime outer = new() { X = containerX, Width = 20, Height = 20, ClipsChildren = true };
        ContainerRuntime middle = new() { Width = 20, Height = 20, ClipsChildren = true };
        PreRenderCounter counter = new();
        middle.Children.Add(new GraphicalUiElement(counter));
        outer.Children.Add(middle);
        GumService.Default.Root.Children.Add(outer);

        GumService.Default.Draw();

        counter.PreRenderCount.ShouldBe(1);
    }

    [Fact]
    public void Draw_RenderTargetContainerChild_PreRendersOncePerBakeAndOncePerWalk()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime renderTarget = new() { Width = 20, Height = 20, IsRenderTarget = true };
        ContainerRuntime middle = new();
        PreRenderCounter counter = new();
        middle.Children.Add(new GraphicalUiElement(counter));
        renderTarget.Children.Add(middle);
        GumService.Default.Root.Children.Add(renderTarget);

        GumService.Default.Draw();

        // Once when the bake draws the container's children, once in the frame's own pre-render walk.
        counter.PreRenderCount.ShouldBe(2);
    }

    [Fact]
    public void Draw_ChildAddedDuringParentPreRender_PreRendersTheChildOnce()
    {
        using SKSurface surface = InitializeSurface();
        ChildAddingRenderable adder = new();
        ContainerRuntime container = new();
        container.Children.Add(new GraphicalUiElement(adder));
        GumService.Default.Root.Children.Add(container);

        GumService.Default.Draw();

        adder.Added.ShouldNotBeNull();
        adder.Added!.PreRenderCount.ShouldBe(1);
    }

    [Fact]
    public void Draw_HiddenChild_IsNotPreRendered()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime outer = new();
        PreRenderCounter counter = new();
        GraphicalUiElement leaf = new(counter) { Visible = false };
        outer.Children.Add(leaf);
        GumService.Default.Root.Children.Add(outer);

        GumService.Default.Draw();

        counter.PreRenderCount.ShouldBe(0);
    }

    [Fact]
    public void Draw_ClearsCanvas_ErasesPreviousFramePixels()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(32, 32));
        GumService.Default.Initialize(surface.Canvas, 32, 32);
        SystemManagers.Default.Renderer.ClearsCanvas = true;
        surface.Canvas.Clear(SKColors.Blue);

        GumService.Default.Draw();

        PixelAt(surface, 5, 5).ShouldNotBe(SKColors.Blue);
    }

    [Fact]
    public void Draw_ClearsCanvas_KeepsEarlierLayersOfTheSameFrame()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(32, 32));
        GumService.Default.Initialize(surface.Canvas, 32, 32);
        SystemManagers.Default.Renderer.ClearsCanvas = true;

        RectangleRuntime mainLayerRectangle = new()
        {
            Width = 10,
            Height = 10,
            IsFilled = true,
            FillColor = SKColors.Red,
        };
        GumService.Default.Root.Children.Add(mainLayerRectangle);

        Layer secondLayer = SystemManagers.Default.Renderer.AddLayer();
        RectangleRuntime secondLayerRectangle = new()
        {
            X = 20,
            Y = 20,
            Width = 10,
            Height = 10,
            IsFilled = true,
            FillColor = SKColors.Lime,
        };
        secondLayerRectangle.AddToManagers(SystemManagers.Default, secondLayer);

        GumService.Default.Draw();

        PixelAt(surface, 5, 5).ShouldBe(SKColors.Red, "because the canvas is cleared once per frame, not once per layer");
        PixelAt(surface, 25, 25).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Draw_EachFrame_RebakesRenderTargetAndCompositesIt()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = new()
        {
            X = 4,
            Y = 4,
            Width = 20,
            Height = 20,
            IsRenderTarget = true,
        };
        RectangleRuntime child = new()
        {
            Width = 20,
            Height = 20,
            IsFilled = true,
            FillColor = SKColors.Red,
        };
        renderTarget.Children.Add(child);
        GumService.Default.Root.Children.Add(renderTarget);

        GumService.Default.Draw();
        SKImage? firstFrameImage = SystemManagers.Default.Renderer.TryGetBakedRenderTargetFor(renderTarget);
        PixelAt(surface, 10, 10).ShouldBe(SKColors.Red);

        child.FillColor = SKColors.Lime;
        GumService.Default.Draw();

        SKImage? secondFrameImage = SystemManagers.Default.Renderer.TryGetBakedRenderTargetFor(renderTarget);
        secondFrameImage.ShouldNotBeNull();
        secondFrameImage.ShouldNotBeSameAs(firstFrameImage);
        PixelAt(surface, 10, 10).ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Draw_SecondLayerSpriteReferencingFirstLayerRenderTarget_ShowsBake()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        ContainerRuntime renderTarget = new() { Width = 20, Height = 20, IsRenderTarget = true };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 20,
            Height = 20,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        GumService.Default.Root.Children.Add(renderTarget);
        Layer secondLayer = SystemManagers.Default.Renderer.AddLayer();
        SpriteRuntime sprite = new()
        {
            X = 30,
            Y = 30,
            Width = 20,
            Height = 20,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            RenderTargetTextureSource = renderTarget,
        };
        sprite.AddToManagers(SystemManagers.Default, secondLayer);

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.TryGetBakedRenderTargetFor(renderTarget).ShouldNotBeNull(
            "because the second layer must not dispose the first layer's bake");
        PixelAt(surface, 40, 40).ShouldBe(SKColors.Red);
    }

    private static SKColor PixelAt(SKSurface surface, int x, int y)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }
}
