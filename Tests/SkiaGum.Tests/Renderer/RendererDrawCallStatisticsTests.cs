using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// Pins <see cref="RenderingLibrary.Graphics.Renderer.RenderStateChangeStatistics"/> on Skia: the same
/// per-frame <c>DrawCallCount</c> MonoGame and raylib expose, counting the SKCanvas draw calls a frame issues.
/// </summary>
public class RendererDrawCallStatisticsTests : IDisposable
{
    readonly SKSurface _surface;

    public RendererDrawCallStatisticsTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(128, 128));
        GumService.Default.Initialize(_surface.Canvas, 128, 128);
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
    }

    [Fact]
    public void Draw_RectangleAndText_CountsOneDrawCallEach()
    {
        GumService.Default.Root.Children.Add(new RectangleRuntime
        {
            Width = 10,
            Height = 10,
            IsFilled = true,
            FillColor = SKColors.Red,
            StrokeWidth = 0,
        });
        GumService.Default.Root.Children.Add(new TextRuntime { Y = 20, Text = "Hi" });

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(2);
    }

    [Fact]
    public void Draw_EveryFrame_ResetsTheCountInsteadOfAccumulating()
    {
        GumService.Default.Root.Children.Add(new RectangleRuntime
        {
            Width = 10,
            Height = 10,
            IsFilled = true,
            FillColor = SKColors.Red,
            StrokeWidth = 0,
        });

        GumService.Default.Update(1.0 / 60.0);
        GumService.Default.Draw();
        GumService.Default.Update(2.0 / 60.0);
        GumService.Default.Draw();
        // A host that never calls Update still gets a fresh count per full-scene Draw.
        GumService.Default.Draw();

        SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(1);
    }

    static SpriteRuntime CreateSprite(SKBitmap texture, float x) => new()
    {
        Texture = texture,
        X = x,
        Width = 8,
        Height = 8,
        WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
    };

    [Fact]
    public void Draw_ConsecutiveSameTextureSprites_DrawInOneCall()
    {
        using SKBitmap texture = new(4, 4);
        texture.Erase(SKColors.Red);
        for (int i = 0; i < 5; i++)
        {
            // Each in its own container, as in a real screen; containers draw nothing, so they do not break the batch.
            ContainerRuntime container = new();
            container.Children.Add(CreateSprite(texture, i * 10));
            GumService.Default.Root.Children.Add(container);
        }

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(1);
    }

    [Fact]
    public void Draw_TextBetweenSameTextureSprites_BreaksTheBatch()
    {
        using SKBitmap texture = new(4, 4);
        texture.Erase(SKColors.Red);
        GumService.Default.Root.Children.Add(CreateSprite(texture, 0));
        GumService.Default.Root.Children.Add(CreateSprite(texture, 10));
        GumService.Default.Root.Children.Add(new TextRuntime { Y = 20, Text = "Hi" });
        GumService.Default.Root.Children.Add(CreateSprite(texture, 20));
        GumService.Default.Root.Children.Add(CreateSprite(texture, 30));

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(3);
    }

    [Fact]
    public void Draw_FlippedSpriteBetweenSameTextureSprites_DrawsAlone()
    {
        using SKBitmap texture = new(4, 4);
        texture.Erase(SKColors.Red);
        GumService.Default.Root.Children.Add(CreateSprite(texture, 0));
        SpriteRuntime flipped = CreateSprite(texture, 10);
        flipped.FlipHorizontal = true;
        GumService.Default.Root.Children.Add(flipped);
        GumService.Default.Root.Children.Add(CreateSprite(texture, 20));

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(3);
    }

    [Fact]
    public void Draw_RenderTargetContainer_CountsTheBakedChildAndTheComposite()
    {
        ContainerRuntime renderTarget = new()
        {
            Width = 20,
            Height = 20,
            IsRenderTarget = true,
        };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = 10,
            Height = 10,
            IsFilled = true,
            FillColor = SKColors.Red,
            StrokeWidth = 0,
        });
        GumService.Default.Root.Children.Add(renderTarget);

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(2);
    }
}
