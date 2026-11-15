using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

// A sprite sized by layout has scaleX and scaleY a float step apart; it must still batch (#7917) without the
// drawn size moving by a visible amount. The batch draws one uniform scale: the average of the two.
public class SpriteAtlasBatchLayoutSizeTests : IDisposable
{
    const int SurfaceSize = 300;
    readonly SKSurface _surface;

    public SpriteAtlasBatchLayoutSizeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(SurfaceSize, SurfaceSize));
        GumService.Default.Initialize(_surface.Canvas, SurfaceSize, SurfaceSize);
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
    }

    static SKBitmap Pattern(int width, int height)
    {
        SKBitmap bitmap = new(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Hard edges every few texels so a sub-pixel size error shows up as a pixel difference.
                byte r = (byte)(((x / 7) % 2) * 200 + 30);
                byte g = (byte)(((y / 5) % 2) * 200 + 30);
                bitmap.SetPixel(x, y, new SKColor(r, g, (byte)((x + y) % 256)));
            }
        }
        return bitmap;
    }

    static SpriteRuntime Sprite(SKBitmap texture, float width, float height, float x = 0, float y = 0) => new()
    {
        Texture = texture,
        X = x,
        Y = y,
        Width = width,
        Height = height,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
    };

    int DrawCalls => SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount;

    SKBitmap Snapshot()
    {
        using SKImage image = _surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    [Theory]
    [InlineData(413, 578, 102.32599f, 143.20685f)]
    [InlineData(413, 578, 206.6f, 289.13995f)]
    [InlineData(256, 256, 100.3f, 100.3f)]
    [InlineData(333, 211, 77.77f, 49.277688f)]
    [InlineData(1000, 700, 123.456f, 86.4192f)]
    public void Draw_LayoutSizedSprites_BatchIntoOneCall(int textureWidth, int textureHeight, float width, float height)
    {
        using SKBitmap texture = Pattern(textureWidth, textureHeight);
        GumService.Default.Root.Children.Add(Sprite(texture, width, height));
        GumService.Default.Root.Children.Add(Sprite(texture, width, height, x: 150));

        GumService.Default.Draw();

        DrawCalls.ShouldBe(1);
    }

    [Theory]
    [InlineData(413, 578, 102.32599f, 143.20685f)]
    [InlineData(413, 578, 206.6f, 289.13995f)]
    [InlineData(333, 211, 77.77f, 49.277688f)]
    [InlineData(1000, 700, 123.456f, 86.4192f)]
    public void Draw_BatchedSprite_PixelsMatchSingleDraw(int textureWidth, int textureHeight, float width, float height)
    {
        using SKBitmap texture = Pattern(textureWidth, textureHeight);
        _surface.Canvas.Clear(SKColors.Gray);
        GumService.Default.Root.Children.Add(Sprite(texture, width, height, x: 10.5f, y: 7.25f));
        GumService.Default.Draw();
        using SKBitmap batched = Snapshot();

        _surface.Canvas.Clear(SKColors.Gray);
        using SKImage image = SKImage.FromBitmap(texture);
        _surface.Canvas.DrawImage(image, new SKRect(10.5f, 7.25f, 10.5f + width, 7.25f + height),
            RenderingLibrary.Graphics.Renderer.TextureSampling);
        using SKBitmap single = Snapshot();


        int maxDifference = 0;
        int maxX = 0;
        int maxY = 0;
        for (int y = 0; y < SurfaceSize; y++)
        {
            for (int x = 0; x < SurfaceSize; x++)
            {
                SKColor a = batched.GetPixel(x, y);
                SKColor b = single.GetPixel(x, y);
                int difference = Math.Max(Math.Max(Math.Abs(a.Red - b.Red), Math.Abs(a.Green - b.Green)), Math.Abs(a.Blue - b.Blue));
                if (difference > maxDifference)
                {
                    maxDifference = difference;
                    maxX = x;
                    maxY = y;
                }
            }
        }
        maxDifference.ShouldBe(0, $"max channel difference {maxDifference} at ({maxX},{maxY})");
    }

    [Fact]
    public void Draw_StretchedSprite_DoesNotBatch()
    {
        using SKBitmap texture = Pattern(100, 100);
        GumService.Default.Root.Children.Add(Sprite(texture, 200, 100));
        GumService.Default.Root.Children.Add(Sprite(texture, 200, 100, y: 120));

        GumService.Default.Draw();

        DrawCalls.ShouldBe(2);
    }

    [Fact]
    public void Draw_SlightlyStretchedSprite_AtToleranceBoundary()
    {
        using SKBitmap texture = Pattern(400, 400);
        // Far-edge error is d / 2 pixels for a height d over: d = 0.001 batches (0.0005 px), d = 0.006 does not (0.003 px).
        GumService.Default.Root.Children.Add(Sprite(texture, 100.0f, 100.0f + 0.001f));
        GumService.Default.Root.Children.Add(Sprite(texture, 100.0f, 100.0f + 0.001f, x: 120));
        GumService.Default.Draw();
        DrawCalls.ShouldBe(1, "0.0005 px edge error batches");

        GumService.Default.Root.Children.Clear();
        GumService.Default.Root.Children.Add(Sprite(texture, 100.0f, 100.0f + 0.006f));
        GumService.Default.Root.Children.Add(Sprite(texture, 100.0f, 100.0f + 0.006f, x: 120));
        GumService.Default.Draw();
        DrawCalls.ShouldBe(2, "0.003 px edge error draws alone");
    }

    static SKBitmap Noise(int width, int height, int seed)
    {
        Random random = new(seed);
        SKBitmap bitmap = new(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
            }
        }
        return bitmap;
    }

    static int PixelsOverEightLevels(SKBitmap a, SKBitmap b)
    {
        int count = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                SKColor p = a.GetPixel(x, y);
                SKColor q = b.GetPixel(x, y);
                if (Math.Max(Math.Max(Math.Abs(p.Red - q.Red), Math.Abs(p.Green - q.Green)), Math.Abs(p.Blue - q.Blue)) > 8)
                {
                    count++;
                }
            }
        }
        return count;
    }

    [Fact]
    public void Draw_RandomLayoutDerivedSizes_AllBatch()
    {
        using SKBitmap texture = Pattern(413, 578);
        Random random = new(7917);
        for (int i = 0; i < 20; i++)
        {
            // Layout sizes: the width is arbitrary, the height keeps the aspect ratio through a float division.
            float width = 20 + (float)random.NextDouble() * 120;
            float height = width * 578f / 413f;
            GumService.Default.Root.Children.Add(Sprite(texture, width, height, x: i * 3f));
        }

        GumService.Default.Draw();

        DrawCalls.ShouldBe(1);
    }

    [Fact]
    public void Draw_NoiseTexture_BatchedMatchesSingleDraw()
    {
        using SKBitmap texture = Noise(413, 578, seed: 1);
        _surface.Canvas.Clear(SKColors.Gray);
        GumService.Default.Root.Children.Add(Sprite(texture, 102.32599f, 143.20685f, x: 10.5f, y: 7.25f));
        GumService.Default.Draw();
        using SKBitmap batched = Snapshot();

        _surface.Canvas.Clear(SKColors.Gray);
        using SKImage image = SKImage.FromBitmap(texture);
        _surface.Canvas.DrawImage(image, new SKRect(10.5f, 7.25f, 10.5f + 102.32599f, 7.25f + 143.20685f),
            RenderingLibrary.Graphics.Renderer.TextureSampling);
        using SKBitmap single = Snapshot();

        PixelsOverEightLevels(batched, single).ShouldBe(0);
    }

    // The reference gives every sprite its own copy of its texture, so the batch breaks between sprites and each
    // draws alone; the scene is otherwise identical.
    void BuildMixedScene(SKBitmap[] textures, bool sharedTextures)
    {
        GumService.Default.Root.Children.Clear();
        for (int i = 0; i < 24; i++)
        {
            float x = (i % 6) * 48f + 3.3f;
            float y = (i / 6) * 70f + 2.7f;
            if (i % 8 == 3)
            {
                GumService.Default.Root.Children.Add(new RectangleRuntime { X = x, Y = y, Width = 40, Height = 40, IsFilled = true, FillColor = SKColors.Blue, StrokeWidth = 0 });
            }
            else if (i % 8 == 6)
            {
                GumService.Default.Root.Children.Add(new TextRuntime { X = x, Y = y, Text = "Hi" });
            }
            else
            {
                SKBitmap texture = textures[i / 8];
                if (!sharedTextures)
                {
                    texture = texture.Copy();
                }
                GumService.Default.Root.Children.Add(Sprite(texture, 40.37f, 40.37f * 578f / 413f, x, y));
            }
        }
    }

    [Fact]
    public void Draw_MixedScene_BatchedMatchesSingleDraws()
    {
        SKBitmap[] textures = { Noise(413, 578, 1), Noise(413, 578, 2), Noise(413, 578, 3) };
        try
        {
            _surface.Canvas.Clear(SKColors.Gray);
            BuildMixedScene(textures, sharedTextures: true);
            GumService.Default.Draw();
            int batchedCalls = DrawCalls;
            using SKBitmap batched = Snapshot();

            _surface.Canvas.Clear(SKColors.Gray);
            BuildMixedScene(textures, sharedTextures: false);
            GumService.Default.Draw();
            int singleCalls = DrawCalls;
            using SKBitmap single = Snapshot();

            batchedCalls.ShouldBeLessThan(singleCalls);
            PixelsOverEightLevels(batched, single).ShouldBe(0);
        }
        finally
        {
            foreach (SKBitmap texture in textures)
            {
                texture.Dispose();
            }
        }
    }
}
