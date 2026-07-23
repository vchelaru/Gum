using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Coverage for SkiaGum render-target support (#3988): a <c>ContainerRuntime.IsRenderTarget</c>
/// container bakes its subtree into an offscreen <see cref="SKSurface"/> that the renderer caches
/// (keyed by the container), composites in place, and lets a Sprite sample via
/// <c>RenderTargetTextureSource</c>. Mirrors the raylib pull-model tests.
/// </summary>
public class RenderTargetTests
{
    public RenderTargetTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void Draw_ThenContainerNoLongerRenderTarget_ReclaimsBakedTexture()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = new()
        {
            X = 4,
            Y = 4,
            Width = 40,
            Height = 40,
            IsRenderTarget = true,
        };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 30,
            Height = 30,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        GumService.Default.Root.Children.Add(renderTarget);

        GumService.Default.Draw();
        SystemManagers.Default.Renderer.HasBakedRenderTargetFor(renderTarget).ShouldBeTrue();

        renderTarget.IsRenderTarget = false;
        GumService.Default.Draw();

        SystemManagers.Default.Renderer.HasBakedRenderTargetFor(renderTarget).ShouldBeFalse();
    }

    [Fact]
    public void Draw_WithInvisibleReferencedRenderTarget_StillBakes()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime invisibleRenderTarget = new()
        {
            X = 4,
            Y = 4,
            Width = 40,
            Height = 40,
            IsRenderTarget = true,
            Visible = false,
        };
        invisibleRenderTarget.Children.Add(new RectangleRuntime
        {
            Width = 30,
            Height = 30,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        GumService.Default.Root.Children.Add(invisibleRenderTarget);

        SpriteRuntime sprite = new()
        {
            X = 0,
            Y = 0,
            Width = 40,
            Height = 40,
            RenderTargetTextureSource = invisibleRenderTarget,
        };
        GumService.Default.Root.Children.Add(sprite);

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.HasBakedRenderTargetFor(invisibleRenderTarget).ShouldBeTrue();
    }

    [Fact]
    public void Draw_WithNonRenderTargetContainer_CachesNothing()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime plainContainer = new()
        {
            X = 4,
            Y = 4,
            Width = 40,
            Height = 40,
            IsRenderTarget = false,
        };
        plainContainer.Children.Add(new RectangleRuntime
        {
            Width = 30,
            Height = 30,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        GumService.Default.Root.Children.Add(plainContainer);

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.HasBakedRenderTargetFor(plainContainer).ShouldBeFalse();
    }

    [Fact]
    public void Draw_WithRenderTargetContainer_CachesBakedTexture()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = new()
        {
            X = 4,
            Y = 4,
            Width = 40,
            Height = 40,
            IsRenderTarget = true,
        };
        renderTarget.Children.Add(new RectangleRuntime
        {
            X = 0,
            Y = 0,
            Width = 30,
            Height = 30,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        GumService.Default.Root.Children.Add(renderTarget);

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.HasBakedRenderTargetFor(renderTarget).ShouldBeTrue();
    }
}
