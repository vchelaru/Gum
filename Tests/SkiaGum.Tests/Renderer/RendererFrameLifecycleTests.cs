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

    private static SKColor PixelAt(SKSurface surface, int x, int y)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }
}
