using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Skia port of MonoGame's GumBatchRenderTargetBakeTests: a GumBatch draw does not bake render
/// targets, so <see cref="RenderingLibrary.Graphics.Renderer.PreRenderRoots"/> bakes
/// <c>IsRenderTarget</c> containers and the targets a <c>RenderTargetTextureSource</c> references.
/// </summary>
public class GumBatchRenderTargetBakeTests
{
    public GumBatchRenderTargetBakeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Fact]
    public void PreRenderRoots_WithRootsNotInAnyLayer_BakesContainerAndReferencingSprite()
    {
        using SKSurface surface = CreateInitializedSurface(128);
        ContainerRuntime source = CreateRedRenderTargetContainer();
        source.X = 64;
        SpriteRuntime sprite = CreateSpriteReferencing(source);

        DrawFramesThroughGumBatch(surface, new List<IRenderableIpso> { sprite, source });

        PixelAt(surface, 32, 32).ShouldBe(SKColors.Red, "because the sprite shows the baked container");
        PixelAt(surface, 96, 32).ShouldBe(SKColors.Red, "because the container composites its bake");
    }

    [Fact]
    public void PreRenderRoots_CalledOncePerRootAcrossSeveralBeginEndCycles_BakesEveryRoot()
    {
        using SKSurface surface = CreateInitializedSurface(128);
        ContainerRuntime left = CreateRedRenderTargetContainer();
        ContainerRuntime right = CreateRedRenderTargetContainer();
        right.X = 64;
        GumBatch batch = new();
        SystemManagers.Default.Renderer.ClearsCanvas = false;

        for (int frame = 0; frame < 2; frame++)
        {
            GumService.Default.Update(frame + 1.0);
            surface.Canvas.Clear(SKColors.Black);

            SystemManagers.Default.Renderer.PreRenderRoots(new List<IRenderableIpso> { left });
            SystemManagers.Default.Renderer.PreRenderRoots(new List<IRenderableIpso> { right });

            batch.Begin();
            batch.Draw(left);
            batch.End();

            batch.Begin();
            batch.Draw(right);
            batch.End();
        }

        PixelAt(surface, 32, 32).ShouldBe(SKColors.Red);
        PixelAt(surface, 96, 32).ShouldBe(SKColors.Red, "because the second PreRenderRoots must not drop the first one's bake");
    }

    [Fact]
    public void PreRenderRoots_WithInvisibleSourceContainer_BakesItForTheReferencingSprite()
    {
        using SKSurface surface = CreateInitializedSurface(128);
        ContainerRuntime source = CreateRedRenderTargetContainer();
        source.Visible = false;
        SpriteRuntime sprite = CreateSpriteReferencing(source);

        DrawFramesThroughGumBatch(surface, new List<IRenderableIpso> { sprite, source });

        PixelAt(surface, 32, 32).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void AlternatingGumBatchOnlyAndDrawFrames_KeepRenderTargetSurface()
    {
        using SKSurface surface = CreateInitializedSurface(128);
        ContainerRuntime renderTarget = CreateRedRenderTargetContainer();
        GumService.Default.Root.Children.Add(renderTarget);
        List<IRenderableIpso> roots = new() { renderTarget };
        GumBatch batch = new();
        List<SKSurface?> surfaces = new();

        for (int frame = 0; frame < 4; frame++)
        {
            GumService.Default.Update(frame + 1.0);
            if (frame % 2 == 1)
            {
                SystemManagers.Default.Renderer.PreRenderRoots(roots);
                batch.Begin();
                batch.Draw(renderTarget);
                batch.End();
            }
            else
            {
                GumService.Default.Draw();
            }
            surfaces.Add(SystemManagers.Default.Renderer.TryGetRenderTargetSurfaceFor(renderTarget));
        }

        surfaces[0].ShouldNotBeNull();
        for (int i = 1; i < surfaces.Count; i++)
        {
            surfaces[i].ShouldBeSameAs(surfaces[0], $"because frame {i} must not reallocate the surface");
        }
    }

    private static void DrawFramesThroughGumBatch(SKSurface surface, List<IRenderableIpso> roots)
    {
        GumBatch batch = new();
        for (int frame = 0; frame < 2; frame++)
        {
            GumService.Default.Update(frame + 1.0);
            surface.Canvas.Clear(SKColors.Black);

            SystemManagers.Default.Renderer.PreRenderRoots(roots);

            batch.Begin();
            foreach (IRenderableIpso root in roots)
            {
                batch.Draw(root);
            }
            batch.End();
        }
    }

    private static ContainerRuntime CreateRedRenderTargetContainer()
    {
        ContainerRuntime container = new() { Width = 64, Height = 64, IsRenderTarget = true };
        container.Children.Add(new RectangleRuntime
        {
            Width = 64,
            Height = 64,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        return container;
    }

    private static SpriteRuntime CreateSpriteReferencing(ContainerRuntime source) => new()
    {
        Width = 64,
        Height = 64,
        WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        RenderTargetTextureSource = source,
    };

    private static SKSurface CreateInitializedSurface(int size)
    {
        SKSurface surface = SKSurface.Create(new SKImageInfo(size, size));
        GumService.Default.Initialize(surface.Canvas, size, size);
        return surface;
    }

    private static SKColor PixelAt(SKSurface surface, int x, int y)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }
}
