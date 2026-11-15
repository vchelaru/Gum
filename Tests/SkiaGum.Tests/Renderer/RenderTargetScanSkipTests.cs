using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// A scene where nothing is a render target skips both render-target tree scans (the referenced-owner
/// collect and the any-target check). Each case where something can be a render target still finds it
/// on the frame it appears.
/// </summary>
public class RenderTargetScanSkipTests
{
    public RenderTargetScanSkipTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static RenderingLibrary.Graphics.Renderer SkiaRenderer => SystemManagers.Default.Renderer;

    private static SKSurface InitializeSurface()
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);
        GumService.Default.Root.Children.Clear();

        // The live count is process-wide; start each test from "nothing live" whatever ran before.
        RenderTargetUsageHandle.ResetForTests();

        return surface;
    }

    private static ContainerRuntime CreateRenderTarget()
    {
        ContainerRuntime renderTarget = new() { X = 2, Y = 2, Width = 20, Height = 20, IsRenderTarget = true };
        renderTarget.Children.Add(new RectangleRuntime { Width = 10, Height = 10, IsFilled = true, FillColor = SKColors.Red });
        return renderTarget;
    }

    private static ContainerRuntime Nest(int depth, GraphicalUiElement leaf)
    {
        ContainerRuntime top = new();
        ContainerRuntime current = top;
        for (int i = 1; i < depth; i++)
        {
            ContainerRuntime next = new();
            current.Children.Add(next);
            current = next;
        }
        current.Children.Add(leaf);
        return top;
    }

    [Fact]
    public void Draw_NoRenderTargets_RunsNoScans()
    {
        using SKSurface surface = InitializeSurface();
        GumService.Default.Root.Children.Add(Nest(5, new RectangleRuntime { Width = 4, Height = 4, IsFilled = true }));
        int before = SkiaRenderer.RenderTargetScanCount;

        GumService.Default.Draw();
        GumService.Default.Draw();

        SkiaRenderer.RenderTargetScanCount.ShouldBe(before);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Draw_RenderTargetAddedAtDepth_BakesOnThatFrame(int depth)
    {
        using SKSurface surface = InitializeSurface();
        GumService.Default.Root.Children.Add(Nest(depth, new RectangleRuntime { Width = 4, Height = 4, IsFilled = true }));
        GumService.Default.Draw();

        ContainerRuntime renderTarget = CreateRenderTarget();
        GumService.Default.Root.Children.Add(Nest(depth, renderTarget));
        GumService.Default.Draw();

        SkiaRenderer.HasBakedRenderTargetFor(renderTarget).ShouldBeTrue();
    }

    [Fact]
    public void Draw_RenderTargetAddedBetweenFrames_BakesOnTheNextFrame()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime holder = new();
        GumService.Default.Root.Children.Add(holder);
        GumService.Default.Draw();

        ContainerRuntime renderTarget = CreateRenderTarget();
        holder.Children.Add(renderTarget);
        GumService.Default.Draw();

        SkiaRenderer.HasBakedRenderTargetFor(renderTarget).ShouldBeTrue();
    }

    [Fact]
    public void Draw_RenderTargetNestedInsideRenderTarget_BakesBoth()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime outer = CreateRenderTarget();
        ContainerRuntime inner = CreateRenderTarget();
        outer.Children.Add(inner);
        GumService.Default.Root.Children.Add(outer);

        GumService.Default.Draw();

        SkiaRenderer.HasBakedRenderTargetFor(outer).ShouldBeTrue();
        SkiaRenderer.HasBakedRenderTargetFor(inner).ShouldBeTrue();
    }

    [Fact]
    public void Draw_FlagFlippedOnAtRuntime_BakesThenStopsWhenFlippedOff()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime container = new() { X = 2, Y = 2, Width = 20, Height = 20 };
        container.Children.Add(new RectangleRuntime { Width = 10, Height = 10, IsFilled = true });
        GumService.Default.Root.Children.Add(container);
        GumService.Default.Draw();
        SkiaRenderer.HasBakedRenderTargetFor(container).ShouldBeFalse();

        container.IsRenderTarget = true;
        GumService.Default.Draw();
        SkiaRenderer.HasBakedRenderTargetFor(container).ShouldBeTrue();

        container.IsRenderTarget = false;
        int scansBefore = SkiaRenderer.RenderTargetScanCount;
        GumService.Default.Draw();
        SkiaRenderer.HasBakedRenderTargetFor(container).ShouldBeFalse();
        SkiaRenderer.RenderTargetScanCount.ShouldBe(scansBefore);
    }

    [Fact]
    public void Draw_RenderTargetRemovedFromTree_NoLongerHasBakedTexture()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime renderTarget = CreateRenderTarget();
        GumService.Default.Root.Children.Add(renderTarget);
        GumService.Default.Draw();
        SkiaRenderer.HasBakedRenderTargetFor(renderTarget).ShouldBeTrue();

        GumService.Default.Root.Children.Remove(renderTarget);
        GumService.Default.Draw();

        SkiaRenderer.HasBakedRenderTargetFor(renderTarget).ShouldBeFalse();
    }

    [Fact]
    public void Draw_InvisibleRenderTarget_DoesNotBake()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime renderTarget = CreateRenderTarget();
        renderTarget.Visible = false;
        GumService.Default.Root.Children.Add(renderTarget);

        GumService.Default.Draw();

        SkiaRenderer.HasBakedRenderTargetFor(renderTarget).ShouldBeFalse();
    }

    [Fact]
    public void Draw_InvisibleRenderTargetReferencedBySprite_Bakes()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime renderTarget = CreateRenderTarget();
        renderTarget.Visible = false;
        GumService.Default.Root.Children.Add(renderTarget);
        GumService.Default.Root.Children.Add(new SpriteRuntime { RenderTargetTextureSource = renderTarget, Width = 20, Height = 20 });

        GumService.Default.Draw();

        SkiaRenderer.HasBakedRenderTargetFor(renderTarget).ShouldBeTrue();
    }

    [Fact]
    public void Draw_RenderTargetInsideClippedContainer_Bakes()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime renderTarget = CreateRenderTarget();
        ContainerRuntime clipper = new() { Width = 30, Height = 30, ClipsChildren = true };
        clipper.Children.Add(renderTarget);
        GumService.Default.Root.Children.Add(clipper);

        GumService.Default.Draw();

        SkiaRenderer.HasBakedRenderTargetFor(renderTarget).ShouldBeTrue();
    }

    [Fact]
    public void RenderLayer_RenderTargetInLayer_Bakes()
    {
        using SKSurface surface = InitializeSurface();
        Layer layer = new();
        ContainerRuntime renderTarget = CreateRenderTarget();
        layer.Add(renderTarget);

        SkiaRenderer.RenderLayer(SystemManagers.Default, layer);

        SkiaRenderer.HasBakedRenderTargetFor(renderTarget).ShouldBeTrue();
    }

    [Fact]
    public void Draw_ClonedRenderTargetAfterSourceTurnedOff_StillBakes()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime original = CreateRenderTarget();
        ContainerRuntime clone = (ContainerRuntime)original.Clone();
        original.IsRenderTarget = false;
        GumService.Default.Root.Children.Add(clone);

        GumService.Default.Draw();

        SkiaRenderer.HasBakedRenderTargetFor(clone).ShouldBeTrue();
    }

    [Fact]
    public void Draw_ClonedSpriteAfterSourceCleared_StillBakesItsReferencedTarget()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime renderTarget = CreateRenderTarget();
        renderTarget.Visible = false;
        GumService.Default.Root.Children.Add(renderTarget);
        SpriteRuntime original = new() { RenderTargetTextureSource = renderTarget, Width = 20, Height = 20 };
        SpriteRuntime clone = (SpriteRuntime)original.Clone();
        original.RenderTargetTextureSource = null;
        GumService.Default.Root.Children.Add(clone);

        GumService.Default.Draw();

        SkiaRenderer.HasBakedRenderTargetFor(renderTarget).ShouldBeTrue();
    }

    [Fact]
    public void Draw_AfterEveryRenderTargetIsGone_StopsScanning()
    {
        using SKSurface surface = InitializeSurface();
        ContainerRuntime renderTarget = CreateRenderTarget();
        GumService.Default.Root.Children.Add(renderTarget);
        GumService.Default.Draw();
        int scansWithTarget = SkiaRenderer.RenderTargetScanCount;
        scansWithTarget.ShouldBeGreaterThan(0);

        renderTarget.IsRenderTarget = false;
        GumService.Default.Draw();

        SkiaRenderer.RenderTargetScanCount.ShouldBe(scansWithTarget);
    }
}
