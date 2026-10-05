using Gum;
using Gum.DataTypes;
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

    // Pixel coverage for Sprite.RenderTargetTextureSource (#5658).
    [Fact]
    public void Draw_SpriteReferencingInvisibleRenderTarget_ShowsContentAtSpritePositionOnly()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime invisibleRenderTarget = CreateSolidRenderTarget(0, 0, 20, 20, SKColors.Red);
        invisibleRenderTarget.Visible = false;
        GumService.Default.Root.Children.Add(invisibleRenderTarget);

        GumService.Default.Root.Children.Add(CreateSprite(invisibleRenderTarget, 40, 40, 20, 20));

        GumService.Default.Draw();

        using SKBitmap bitmap = SKBitmap.FromImage(surface.Snapshot());
        bitmap.GetPixel(50, 50).ShouldBe(SKColors.Red);
        bitmap.GetPixel(10, 10).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Draw_TwoSpritesReferencingOneRenderTarget_BothShowContent()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = CreateSolidRenderTarget(0, 0, 16, 16, SKColors.Red);
        renderTarget.Visible = false;
        GumService.Default.Root.Children.Add(renderTarget);

        GumService.Default.Root.Children.Add(CreateSprite(renderTarget, 20, 0, 16, 16));
        GumService.Default.Root.Children.Add(CreateSprite(renderTarget, 40, 40, 16, 16));

        GumService.Default.Draw();

        using SKBitmap bitmap = SKBitmap.FromImage(surface.Snapshot());
        bitmap.GetPixel(28, 8).ShouldBe(SKColors.Red);
        bitmap.GetPixel(48, 48).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void Draw_SpriteReferencingVisibleRenderTarget_ShowsBothContainerAndSprite()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = CreateSolidRenderTarget(0, 0, 20, 20, SKColors.Red);
        GumService.Default.Root.Children.Add(renderTarget);

        GumService.Default.Root.Children.Add(CreateSprite(renderTarget, 40, 40, 20, 20));

        GumService.Default.Draw();

        using SKBitmap bitmap = SKBitmap.FromImage(surface.Snapshot());
        bitmap.GetPixel(10, 10).ShouldBe(SKColors.Red);
        bitmap.GetPixel(50, 50).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void Draw_SpriteLargerThanRenderTarget_ScalesBakedImage()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        // 20x20 container: left half red, right half blue.
        ContainerRuntime renderTarget = new()
        {
            Width = 20,
            Height = 20,
            IsRenderTarget = true,
            Visible = false,
        };
        renderTarget.Children.Add(new RectangleRuntime
        {
            X = 0,
            Width = 10,
            Height = 20,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        renderTarget.Children.Add(new RectangleRuntime
        {
            X = 10,
            Width = 10,
            Height = 20,
            IsFilled = true,
            FillColor = SKColors.Blue,
        });
        GumService.Default.Root.Children.Add(renderTarget);

        // 2x scale: red should cover x 10..30, blue x 30..50.
        GumService.Default.Root.Children.Add(CreateSprite(renderTarget, 10, 10, 40, 40));

        GumService.Default.Draw();

        using SKBitmap bitmap = SKBitmap.FromImage(surface.Snapshot());
        bitmap.GetPixel(25, 30).ShouldBe(SKColors.Red);
        bitmap.GetPixel(35, 30).ShouldBe(SKColors.Blue);
        bitmap.GetPixel(45, 30).ShouldBe(SKColors.Blue);
        bitmap.GetPixel(5, 30).Alpha.ShouldBe((byte)0);
        bitmap.GetPixel(55, 30).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Draw_ChildOfInvisibleNonRenderTargetParent_IsNotDrawn()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime invisibleParent = new() { Width = 20, Height = 20, Visible = false };
        invisibleParent.Children.Add(new RectangleRuntime
        {
            Width = 20,
            Height = 20,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        GumService.Default.Root.Children.Add(invisibleParent);

        GumService.Default.Draw();

        using SKBitmap bitmap = SKBitmap.FromImage(surface.Snapshot());
        bitmap.GetPixel(10, 10).Alpha.ShouldBe((byte)0);
    }

    // Resize/move between frames (#5657): the cached surface must follow the container's new size and
    // position, with no stale pixels from the previous frame.
    [Fact]
    public void Draw_AfterRenderTargetGrows_FillsNewSize()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = CreateFillingRenderTarget(0, 0, 20, 20, SKColors.Red);
        GumService.Default.Root.Children.Add(renderTarget);
        GumService.Default.Draw();

        renderTarget.Width = 40;
        renderTarget.Height = 40;
        surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Draw();

        using SKBitmap bitmap = SKBitmap.FromImage(surface.Snapshot());
        bitmap.GetPixel(30, 30).ShouldBe(SKColors.Red);
        bitmap.GetPixel(45, 45).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Draw_AfterRenderTargetShrinks_LeavesNoStalePixels()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = CreateFillingRenderTarget(0, 0, 40, 40, SKColors.Red);
        GumService.Default.Root.Children.Add(renderTarget);
        GumService.Default.Draw();

        renderTarget.Width = 20;
        renderTarget.Height = 20;
        surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Draw();

        using SKBitmap bitmap = SKBitmap.FromImage(surface.Snapshot());
        bitmap.GetPixel(10, 10).ShouldBe(SKColors.Red);
        bitmap.GetPixel(30, 30).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Draw_AfterRenderTargetMoves_DrawsAtNewPositionOnly()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime renderTarget = CreateFillingRenderTarget(0, 0, 20, 20, SKColors.Red);
        GumService.Default.Root.Children.Add(renderTarget);
        GumService.Default.Draw();

        renderTarget.X = 30;
        renderTarget.Y = 30;
        surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Draw();

        using SKBitmap bitmap = SKBitmap.FromImage(surface.Snapshot());
        bitmap.GetPixel(40, 40).ShouldBe(SKColors.Red);
        bitmap.GetPixel(10, 10).Alpha.ShouldBe((byte)0);
    }

    // Container whose single child tracks the container's size, so resizing the container resizes the content.
    private static ContainerRuntime CreateFillingRenderTarget(float x, float y, float width, float height, SKColor color)
    {
        ContainerRuntime renderTarget = new()
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            IsRenderTarget = true,
        };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 100,
            Height = 100,
            WidthUnits = DimensionUnitType.PercentageOfParent,
            HeightUnits = DimensionUnitType.PercentageOfParent,
            IsFilled = true,
            FillColor = color,
        });
        return renderTarget;
    }

    private static ContainerRuntime CreateSolidRenderTarget(float x, float y, float width, float height, SKColor color)
    {
        ContainerRuntime renderTarget = new()
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            IsRenderTarget = true,
        };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = width,
            Height = height,
            IsFilled = true,
            FillColor = color,
        });
        return renderTarget;
    }

    private static SpriteRuntime CreateSprite(ContainerRuntime source, float x, float y, float width, float height) =>
        new()
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            RenderTargetTextureSource = source,
        };

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
