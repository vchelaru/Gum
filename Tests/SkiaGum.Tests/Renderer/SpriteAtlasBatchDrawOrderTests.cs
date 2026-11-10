using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

// Sprites batched through DrawAtlas (#7826) keep draw order, and no batch is left pending when control returns to the host.
public class SpriteAtlasBatchDrawOrderTests : IDisposable
{
    readonly SKSurface _surface;
    readonly SKBitmap _red;
    readonly SKBitmap _green;

    public SpriteAtlasBatchDrawOrderTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(_surface.Canvas, 64, 64);
        _red = Solid(SKColors.Red);
        _green = Solid(SKColors.Lime);
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
        _red.Dispose();
        _green.Dispose();
    }

    static SKBitmap Solid(SKColor color)
    {
        SKBitmap bitmap = new(4, 4);
        bitmap.Erase(color);
        return bitmap;
    }

    static SpriteRuntime Sprite(SKBitmap texture, float x, float y = 0, float size = 8) => new()
    {
        Texture = texture,
        X = x,
        Y = y,
        Width = size,
        Height = size,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
    };

    SKColor PixelAt(int x, int y)
    {
        using SKImage image = _surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }

    int DrawCalls => SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount;

    [Fact]
    public void RendererDraw_SpriteOutsideCycle_DrawsImmediately()
    {
        _surface.Canvas.Clear(SKColors.Black);

        SystemManagers.Default.Renderer.Draw(Sprite(_red, 0));

        PixelAt(4, 4).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void ImmediateCycle_HostDrawBetweenSprites_KeepsCallOrder()
    {
        _surface.Canvas.Clear(SKColors.Black);
        GumBatch gumBatch = new();
        using SKPaint blue = new() { Color = SKColors.Blue };

        gumBatch.Begin();
        gumBatch.Draw(Sprite(_red, 0));
        _surface.Canvas.DrawRect(0, 0, 8, 8, blue);
        gumBatch.Draw(Sprite(_red, 20));
        gumBatch.End();

        PixelAt(4, 4).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void Draw_RectangleBetweenSameTextureSprites_KeepsZOrder()
    {
        GumService.Default.Root.Children.Add(Sprite(_red, 0));
        GumService.Default.Root.Children.Add(new RectangleRuntime { X = 4, Width = 8, Height = 8, IsFilled = true, FillColor = SKColors.Blue, StrokeWidth = 0 });
        GumService.Default.Root.Children.Add(Sprite(_red, 8));

        GumService.Default.Draw();

        PixelAt(6, 4).ShouldBe(SKColors.Blue);
        PixelAt(10, 4).ShouldBe(SKColors.Red);
        DrawCalls.ShouldBe(3);
    }

    [Fact]
    public void Draw_OtherTextureBetweenSprites_KeepsZOrder()
    {
        GumService.Default.Root.Children.Add(Sprite(_red, 0));
        GumService.Default.Root.Children.Add(Sprite(_green, 4));
        GumService.Default.Root.Children.Add(Sprite(_red, 8));

        GumService.Default.Draw();

        PixelAt(6, 4).ShouldBe(SKColors.Lime);
        PixelAt(10, 4).ShouldBe(SKColors.Red);
        DrawCalls.ShouldBe(3);
    }

    [Fact]
    public void Draw_ClippedContainerBetweenSprites_ClipsOnlyItsChild()
    {
        GumService.Default.Root.Children.Add(Sprite(_red, 0, size: 20));
        ContainerRuntime clip = new() { X = 30, Width = 4, Height = 4, ClipsChildren = true, WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
        clip.Children.Add(Sprite(_red, 0, size: 20));
        GumService.Default.Root.Children.Add(clip);
        GumService.Default.Root.Children.Add(Sprite(_red, 0, y: 30, size: 20));

        GumService.Default.Draw();

        PixelAt(18, 18).ShouldBe(SKColors.Red, "the sprite before the clip is not clipped");
        PixelAt(32, 2).ShouldBe(SKColors.Red);
        PixelAt(40, 2).ShouldNotBe(SKColors.Red, "the clipped child stays clipped");
        PixelAt(18, 48).ShouldBe(SKColors.Red, "the sprite after the clip is not clipped");
    }

    [Fact]
    public void Draw_InvisibleAndOffscreenSpritesBetween_StillOneBatch()
    {
        GumService.Default.Root.Children.Add(Sprite(_red, 0));
        SpriteRuntime hidden = Sprite(_red, 10);
        hidden.Visible = false;
        GumService.Default.Root.Children.Add(hidden);
        GumService.Default.Root.Children.Add(Sprite(_red, 500));
        GumService.Default.Root.Children.Add(Sprite(_red, 20));

        GumService.Default.Draw();

        PixelAt(12, 4).ShouldBe(SKColors.Black.WithAlpha(0));
        PixelAt(22, 4).ShouldBe(SKColors.Red);
        DrawCalls.ShouldBe(1);
    }

    [Fact]
    public void Draw_TextureFilterChangedBetweenFrames_SecondFrameUsesNewFilter()
    {
        GumService.Default.Root.Children.Add(Sprite(_red, 0));
        GumService.Default.Root.Children.Add(Sprite(_red, 10));
        SKFilterMode before = RenderingLibrary.Graphics.Renderer.TextureFilter;
        try
        {
            GumService.Default.Draw();
            RenderingLibrary.Graphics.Renderer.TextureFilter = before == SKFilterMode.Nearest ? SKFilterMode.Linear : SKFilterMode.Nearest;
            GumService.Default.Draw();
            DrawCalls.ShouldBe(1);
        }
        finally
        {
            RenderingLibrary.Graphics.Renderer.TextureFilter = before;
        }
    }

    [Theory]
    [InlineData("FlipV", 3)]
    [InlineData("NonUniform", 3)]
    [InlineData("BlendAdditive", 3)]
    [InlineData("ColorAdd", 3)]
    [InlineData("ColorTextureAlpha", 3)]
    [InlineData("AlphaLinear", 3)]
    [InlineData("Alpha0Nearest", 3)]
    [InlineData("AlphaSameNearest", 1)]
    [InlineData("Rotation", 1)]
    [InlineData("Tint", 1)]
    [InlineData("SourceRect", 1)]
    public void Draw_MiddleSpriteVariant_DrawCallCount(string variant, int expected)
    {
        SpriteRuntime a = Sprite(_red, 0), b = Sprite(_red, 10), c = Sprite(_red, 20);
        SKFilterMode before = RenderingLibrary.Graphics.Renderer.TextureFilter;
        switch (variant)
        {
            case "FlipV": b.FlipVertical = true; break;
            case "NonUniform": b.Width = 16; break;
            case "BlendAdditive": b.Blend = Gum.RenderingLibrary.Blend.Additive; break;
            case "ColorAdd": b.ColorOperation = RenderingLibrary.Graphics.ColorOperation.Add; break;
            case "ColorTextureAlpha": b.ColorOperation = RenderingLibrary.Graphics.ColorOperation.ColorTextureAlpha; break;
            case "AlphaLinear": RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Linear; b.Alpha = 128; break;
            case "Alpha0Nearest": b.Alpha = 0; break;
            case "AlphaSameNearest": a.Alpha = b.Alpha = c.Alpha = 128; break;
            case "Rotation": b.Rotation = 30; break;
            case "Tint": b.Color = SKColors.Gray; break;
            case "SourceRect": b.TextureAddress = Gum.Managers.TextureAddress.Custom; b.TextureWidth = 2; b.TextureHeight = 2; break;
        }
        GumService.Default.Root.Children.Add(a);
        GumService.Default.Root.Children.Add(b);
        GumService.Default.Root.Children.Add(c);
        try
        {
            GumService.Default.Draw();
            DrawCalls.ShouldBe(expected);
        }
        finally
        {
            RenderingLibrary.Graphics.Renderer.TextureFilter = before;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(17)]
    [InlineData(64)]
    public void Draw_SteadyBatch_AllocatesNothing(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GumService.Default.Root.Children.Add(Sprite(_red, i % 8 * 8, i / 8 * 8));
        }
        for (int i = 0; i < 20; i++) GumService.Default.Draw();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) GumService.Default.Draw();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;

        DrawCalls.ShouldBe(1);
        bytes.ShouldBe(0);
    }

    // The pool keeps one exact-length array set per batch length, so random lengths stop allocating once
    // each length has been drawn.
    [Fact]
    public void Flush_RandomBatchLengths_AllocatesNothingOnceEachLengthSeen()
    {
        using SKImage image = SKImage.FromBitmap(_red);
        SpriteAtlasBatch batch = new(new RenderStateChangeStatistics());
        SKCanvas canvas = _surface.Canvas;
        SKRect source = new(0, 0, 4, 4);
        SKSamplingOptions sampling = new(SKFilterMode.Nearest);
        void DrawBatch(int length)
        {
            for (int i = 0; i < length; i++)
            {
                batch.TryAdd(canvas, _red, image, source, new SKRect(i, 0, i + 8, 8), SKColors.Gray, 255, sampling);
            }
            batch.Flush();
        }
        for (int length = 1; length <= 64; length++)
        {
            DrawBatch(length);
        }
        Random random = new(1234);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < 300; frame++)
        {
            DrawBatch(random.Next(1, 65));
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;

        bytes.ShouldBe(0);
    }

    [Fact]
    public void SpriteRender_CalledByHost_DrawsImmediately()
    {
        _surface.Canvas.Clear(SKColors.Black);
        SpriteRuntime sprite = Sprite(_red, 0);

        ((IRenderable)sprite.RenderableComponent).Render(SystemManagers.Default);

        PixelAt(4, 4).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void DeferredCycle_End_DrawsBeforeReturning()
    {
        _surface.Canvas.Clear(SKColors.Black);
        GumBatch gumBatch = new();

        gumBatch.Begin(mode: RenderingLibrary.Graphics.Renderer.GumBatchDrawMode.Deferred);
        gumBatch.Draw(Sprite(_red, 0));
        gumBatch.Draw(Sprite(_red, 20));
        gumBatch.End();

        PixelAt(4, 4).ShouldBe(SKColors.Red);
        PixelAt(24, 4).ShouldBe(SKColors.Red);
    }
}
