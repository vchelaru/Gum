using Gum;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A sprite with Wrap set and a custom source rectangle larger than its texture repeats the texture,
/// as it does on MonoGame. Scrolling UI such as the Hollow Knight mana wave depends on it.
/// </summary>
public class SpriteWrapTests
{
    private static SKBitmap CreateRedBlueTexture()
    {
        SKBitmap texture = new(2, 1);
        texture.SetPixel(0, 0, SKColors.Red);
        texture.SetPixel(1, 0, SKColors.Blue);
        return texture;
    }

    private static SKColor[] DrawSprite(SpriteRuntime sprite, int width)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(width, 1));
        GumService.Default.Initialize(surface.Canvas, width, 1);
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        GumService.Default.Root.Children.Add(sprite);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        SKColor[] pixels = new SKColor[width];
        for (int x = 0; x < width; x++)
        {
            pixels[x] = bitmap.GetPixel(x, 0);
        }
        return pixels;
    }

    private static SpriteRuntime CreateSprite(SKBitmap texture, int sourceWidth, int sourceLeft = 0)
    {
        SpriteRuntime sprite = new()
        {
            Texture = texture,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            Width = sourceWidth,
            Height = 1,
            TextureAddress = TextureAddress.Custom,
            TextureLeft = sourceLeft,
            TextureTop = 0,
            TextureWidth = sourceWidth,
            TextureHeight = 1,
        };
        return sprite;
    }

    [Fact]
    public void Draw_WithWrapAndSourceWiderThanTexture_RepeatsTheTexture()
    {
        using SKBitmap texture = CreateRedBlueTexture();
        SpriteRuntime sprite = CreateSprite(texture, sourceWidth: 4);
        sprite.SetProperty("Wrap", true);

        SKColor[] pixels = DrawSprite(sprite, width: 4);

        pixels.ShouldBe([SKColors.Red, SKColors.Blue, SKColors.Red, SKColors.Blue]);
    }

    [Fact]
    public void Draw_WithWrapAndSourceStartingPastTheTexture_ContinuesTheRepeat()
    {
        // Scrolling moves the source window, so it starts inside the second repeat.
        using SKBitmap texture = CreateRedBlueTexture();
        SpriteRuntime sprite = CreateSprite(texture, sourceWidth: 2, sourceLeft: 3);
        sprite.SetProperty("Wrap", true);

        SKColor[] pixels = DrawSprite(sprite, width: 2);

        // Source x 3 is the blue texel of the second repeat, source x 4 is the red of the third.
        pixels.ShouldBe([SKColors.Blue, SKColors.Red]);
    }

    [Fact]
    public void Draw_WithoutWrap_DoesNotRepeatTheTexture()
    {
        using SKBitmap texture = CreateRedBlueTexture();
        SpriteRuntime sprite = CreateSprite(texture, sourceWidth: 4);

        SKColor[] pixels = DrawSprite(sprite, width: 4);

        pixels.ShouldNotBe([SKColors.Red, SKColors.Blue, SKColors.Red, SKColors.Blue]);
    }

    [Fact]
    public void Draw_WithWrapAndSourceInsideTheTexture_DrawsTheSourceRectangle()
    {
        using SKBitmap texture = CreateRedBlueTexture();
        SpriteRuntime sprite = CreateSprite(texture, sourceWidth: 1, sourceLeft: 1);
        sprite.SetProperty("Wrap", true);

        SKColor[] pixels = DrawSprite(sprite, width: 1);

        pixels.ShouldBe([SKColors.Blue]);
    }
}
