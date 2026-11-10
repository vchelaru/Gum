using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using SkiaSharp;

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// Pins Sprite pixels exactly (tolerance 0), so changing how sprites reach the canvas (DrawImage to a batched
/// DrawAtlas, #7826) cannot change what they draw.
/// </summary>
public class SpriteGoldenImageTests : IDisposable
{
    const int SurfaceSize = 96;

    readonly SKSurface _surface;
    readonly SKBitmap _texture;
    readonly SKFilterMode _previousFilter;

    public SpriteGoldenImageTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
        _surface = SKSurface.Create(new SKImageInfo(SurfaceSize, SurfaceSize));
        GumService.Default.Initialize(_surface.Canvas, SurfaceSize, SurfaceSize);
        _texture = CreateTexture();
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        RenderingLibrary.Graphics.Renderer.TextureFilter = _previousFilter;
        _surface.Dispose();
        _texture.Dispose();
    }

    // Distinct neighbors so a one-texel sampling shift shows, and partial alpha on the left columns.
    static SKBitmap CreateTexture()
    {
        SKBitmap bitmap = new(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 8), (byte)(y * 8), (byte)((x + y) % 2 * 255), (byte)(x < 8 ? 160 : 255)));
            }
        }
        return bitmap;
    }

    SpriteRuntime AddSprite(float x, float y, float width, float height, int sourceLeft = 4, int sourceTop = 4, int sourceSize = 16)
    {
        SpriteRuntime sprite = new()
        {
            Texture = _texture,
            TextureAddress = TextureAddress.Custom,
            TextureLeft = sourceLeft,
            TextureTop = sourceTop,
            TextureWidth = sourceSize,
            TextureHeight = sourceSize,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            X = x,
            Y = y,
            Width = width,
            Height = height,
        };
        GumService.Default.Root.Children.Add(sprite);
        return sprite;
    }

    void DrawAndMatch(string name, SKFilterMode filter = SKFilterMode.Nearest)
    {
        RenderingLibrary.Graphics.Renderer.TextureFilter = filter;
        GumService.Default.Draw();
        GoldenImageAssert.Matches(_surface, name, tolerance: 0);
    }

    [Theory]
    [InlineData(SKFilterMode.Nearest)]
    [InlineData(SKFilterMode.Linear)]
    public void Scale3(SKFilterMode filter)
    {
        AddSprite(10, 10, 48, 48);
        DrawAndMatch($"Sprite_Scale3_{filter}", filter);
    }

    [Fact]
    public void FractionalScale()
    {
        AddSprite(10.25f, 10.75f, 36.8f, 36.8f);
        DrawAndMatch("Sprite_FractionalScale");
    }

    [Fact]
    public void NonUniformScale()
    {
        AddSprite(10, 10, 48, 24);
        DrawAndMatch("Sprite_NonUniformScale");
    }

    [Theory]
    [InlineData(SKFilterMode.Nearest)]
    [InlineData(SKFilterMode.Linear)]
    public void Rotation30(SKFilterMode filter)
    {
        SpriteRuntime sprite = AddSprite(40, 30, 40, 40);
        sprite.Rotation = 30;
        DrawAndMatch($"Sprite_Rotation30_{filter}", filter);
    }

    [Fact]
    public void FlipHorizontalAndVertical()
    {
        AddSprite(4, 4, 40, 40).FlipHorizontal = true;
        AddSprite(50, 50, 40, 40).FlipVertical = true;
        DrawAndMatch("Sprite_Flip");
    }

    [Fact]
    public void FullImageSource()
    {
        AddSprite(10, 10, 64, 64, sourceLeft: 0, sourceTop: 0, sourceSize: 32);
        DrawAndMatch("Sprite_FullImageSource");
    }

    // #7030: sprites drawn end to end at a half pixel must not show a seam.
    [Fact]
    public void HalfPixelRow_NoSeam()
    {
        for (int i = 0; i < 4; i++)
        {
            AddSprite(10.5f + i * 16, 10.5f, 16, 16, sourceLeft: 8, sourceTop: 8, sourceSize: 8);
        }
        DrawAndMatch("Sprite_HalfPixelRow");
    }

    [Theory]
    [InlineData(SKFilterMode.Nearest)]
    [InlineData(SKFilterMode.Linear)]
    public void TintWithAlpha(SKFilterMode filter)
    {
        SpriteRuntime opaque = AddSprite(4, 4, 40, 40);
        opaque.Color = new SKColor(255, 128, 64, 255);
        SpriteRuntime translucent = AddSprite(50, 50, 40, 40);
        translucent.Color = new SKColor(255, 128, 64, 128);
        DrawAndMatch($"Sprite_TintAlpha_{filter}", filter);
    }

    // Same-texture sprites in separate containers, overlapping and drawn in order, with one rotated: the
    // order of the overlap must stay as drawn.
    [Fact]
    public void ManySameTextureSprites_KeepDrawOrder()
    {
        for (int i = 0; i < 6; i++)
        {
            ContainerRuntime container = new() { X = i * 12, Y = i * 10 };
            SpriteRuntime sprite = new()
            {
                Texture = _texture,
                TextureAddress = TextureAddress.Custom,
                TextureLeft = i * 4,
                TextureTop = 0,
                TextureWidth = 8,
                TextureHeight = 8,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
                Width = 24,
                Height = 24,
                Rotation = i == 3 ? 15 : 0,
            };
            container.Children.Add(sprite);
            GumService.Default.Root.Children.Add(container);
        }
        RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        GumService.Default.Draw();
        GoldenImageAssert.Matches(_surface, "Sprite_ManySameTexture", tolerance: 2);
    }
}
