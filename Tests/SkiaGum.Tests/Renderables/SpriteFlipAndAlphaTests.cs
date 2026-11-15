using Gum;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// FlipHorizontal and FlipVertical set on a SpriteRuntime reach the drawn Skia sprite, and a tint
/// alpha is applied once, as on MonoGame.
/// </summary>
public class SpriteFlipAndAlphaTests
{
    public SpriteFlipAndAlphaTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    // Four quadrants: top-left red, top-right green, bottom-left blue, bottom-right white.
    private static SKBitmap CreateQuadrantTexture()
    {
        SKBitmap texture = new(4, 4);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                SKColor color = (x < 2, y < 2) switch
                {
                    (true, true) => SKColors.Red,
                    (false, true) => SKColors.Lime,
                    (true, false) => SKColors.Blue,
                    _ => SKColors.White,
                };
                texture.SetPixel(x, y, color);
            }
        }
        return texture;
    }

    private static SKBitmap Render(SKBitmap texture, Action<SpriteRuntime> configure)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        SpriteRuntime sprite = new()
        {
            Texture = texture,
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        };
        configure(sprite);
        GumService.Default.Root.Children.Add(sprite);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_FlipHorizontal_MirrorsTheTexture(bool customSourceRectangle)
    {
        using SKBitmap texture = CreateQuadrantTexture();

        using SKBitmap pixels = Render(texture, sprite =>
        {
            if (customSourceRectangle)
            {
                sprite.TextureAddress = TextureAddress.Custom;
                sprite.TextureLeft = 0;
                sprite.TextureTop = 0;
                sprite.TextureWidth = 4;
                sprite.TextureHeight = 4;
            }
            sprite.FlipHorizontal = true;
        });

        pixels.GetPixel(15, 15).ShouldBe(SKColors.Lime);
        pixels.GetPixel(25, 15).ShouldBe(SKColors.Red);
        pixels.GetPixel(15, 25).ShouldBe(SKColors.White);
        pixels.GetPixel(25, 25).ShouldBe(SKColors.Blue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_FlipVertical_MirrorsTheTexture(bool customSourceRectangle)
    {
        using SKBitmap texture = CreateQuadrantTexture();

        using SKBitmap pixels = Render(texture, sprite =>
        {
            if (customSourceRectangle)
            {
                sprite.TextureAddress = TextureAddress.Custom;
                sprite.TextureLeft = 0;
                sprite.TextureTop = 0;
                sprite.TextureWidth = 4;
                sprite.TextureHeight = 4;
            }
            sprite.FlipVertical = true;
        });

        pixels.GetPixel(15, 15).ShouldBe(SKColors.Blue);
        pixels.GetPixel(25, 15).ShouldBe(SKColors.White);
        pixels.GetPixel(15, 25).ShouldBe(SKColors.Red);
        pixels.GetPixel(25, 25).ShouldBe(SKColors.Lime);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void Render_ParentFlipHorizontal_MirrorsTheChildSprite(bool parentFlip, bool childFlip, bool expectMirrored)
    {
        // MonoGame combines a sprite's FlipHorizontal with its parents' (GetAbsoluteFlipHorizontal).
        using SKBitmap texture = CreateQuadrantTexture();
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        ContainerRuntime container = new()
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        };
        container.FlipHorizontal = parentFlip;
        SpriteRuntime sprite = new()
        {
            Texture = texture,
            Width = 20,
            Height = 20,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        };
        sprite.FlipHorizontal = childFlip;
        container.Children.Add(sprite);
        GumService.Default.Root.Children.Add(container);
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap pixels = SKBitmap.FromImage(image);
        pixels.GetPixel(15, 15).ShouldBe(expectMirrored ? SKColors.Lime : SKColors.Red);
        pixels.GetPixel(25, 15).ShouldBe(expectMirrored ? SKColors.Red : SKColors.Lime);
    }

    [Fact]
    public void Render_FlipHorizontalWithWrap_MirrorsTheRepeatedTexture()
    {
        using SKBitmap texture = CreateQuadrantTexture();

        using SKBitmap pixels = Render(texture, sprite =>
        {
            sprite.TextureAddress = TextureAddress.Custom;
            sprite.TextureLeft = 0;
            sprite.TextureTop = 0;
            sprite.TextureWidth = 8;
            sprite.TextureHeight = 4;
            sprite.SetProperty("Wrap", true);
            sprite.FlipHorizontal = true;
        });

        // Unflipped, the two repeats read red, green, red, green across x 10..30 in 5 pixel columns.
        pixels.GetPixel(12, 12).ShouldBe(SKColors.Lime);
        pixels.GetPixel(17, 12).ShouldBe(SKColors.Red);
        pixels.GetPixel(22, 12).ShouldBe(SKColors.Lime);
        pixels.GetPixel(27, 12).ShouldBe(SKColors.Red);
    }

    [Theory]
    [InlineData(ColorOperation.Modulate)]
    [InlineData(ColorOperation.ColorTextureAlpha)]
    [InlineData(ColorOperation.Add)]
    public void Render_HalfAlpha_DrawsAtHalfOpacity(ColorOperation colorOperation)
    {
        using SKBitmap texture = new(4, 4);
        texture.Erase(SKColors.White);

        using SKBitmap pixels = Render(texture, sprite =>
        {
            sprite.ColorOperation = colorOperation;
            sprite.Alpha = 128;
        });

        // 128/255 of the texture's opaque alpha, within rounding, as on MonoGame
        // (MonoGameGum.IntegrationTests SpriteAlphaTests).
        ((int)pixels.GetPixel(20, 20).Alpha).ShouldBeInRange(126, 130);
    }

    [Fact]
    public void Render_NineSliceHalfAlpha_DrawsAtHalfOpacity()
    {
        using SKBitmap texture = new(4, 4);
        texture.Erase(SKColors.White);
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        NineSliceRuntime nineSlice = new()
        {
            Texture = texture,
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            Alpha = 128,
        };
        GumService.Default.Root.Children.Add(nineSlice);
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap pixels = SKBitmap.FromImage(image);
        ((int)pixels.GetPixel(20, 20).Alpha).ShouldBeInRange(126, 130);
    }
}
