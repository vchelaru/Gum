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

    // ContainerRuntime.Blend/BlendState (#3989) additive composite. The baked texture is
    // premultiplied, so a correct additive composite adds the premultiplied color straight onto the
    // background (SKBlendMode.Plus): half-alpha red (200,0,0,128) premultiplies to ~(100,0,0), added
    // onto a (50,0,0) background reaches ~150 red. A plain alpha-over composite (the non-additive
    // path, see the next test) would only reach ~125 -- the >140 threshold discriminates the two.
    [Fact]
    public void Draw_AdditiveRenderTarget_AddsToBackgroundInsteadOfReplacing()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        RectangleRuntime background = new()
        {
            X = 0,
            Y = 0,
            Width = 64,
            Height = 64,
            IsFilled = true,
            FillColor = new SKColor(50, 0, 0, 255),
        };
        GumService.Default.Root.Children.Add(background);

        ContainerRuntime additive = new()
        {
            X = 0,
            Y = 0,
            Width = 64,
            Height = 64,
            IsRenderTarget = true,
            Blend = Gum.RenderingLibrary.Blend.Additive,
        };
        additive.Children.Add(new RectangleRuntime
        {
            X = 0,
            Y = 0,
            Width = 64,
            Height = 64,
            IsFilled = true,
            FillColor = new SKColor(200, 0, 0, 128),
        });
        GumService.Default.Root.Children.Add(additive);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        bitmap.GetPixel(32, 32).Red.ShouldBeGreaterThan((byte)140);
    }

    // Contrast for the additive test above: with no Blend set (the default), the render-target
    // composite stays a plain alpha-over blit, so the background is dimmed rather than added to.
    [Fact]
    public void Draw_NonAdditiveRenderTarget_CompositesOverBackgroundInsteadOfAdding()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        RectangleRuntime background = new()
        {
            X = 0,
            Y = 0,
            Width = 64,
            Height = 64,
            IsFilled = true,
            FillColor = new SKColor(50, 0, 0, 255),
        };
        GumService.Default.Root.Children.Add(background);

        ContainerRuntime normal = new()
        {
            X = 0,
            Y = 0,
            Width = 64,
            Height = 64,
            IsRenderTarget = true,
        };
        normal.Children.Add(new RectangleRuntime
        {
            X = 0,
            Y = 0,
            Width = 64,
            Height = 64,
            IsFilled = true,
            FillColor = new SKColor(200, 0, 0, 128),
        });
        GumService.Default.Root.Children.Add(normal);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        bitmap.GetPixel(32, 32).Red.ShouldBeLessThan((byte)140);
    }
}
