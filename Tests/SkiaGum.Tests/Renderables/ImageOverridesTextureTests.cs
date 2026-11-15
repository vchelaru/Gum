using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// An <see cref="Sprite.Image"/> assigned to a Sprite after a <c>Texture</c> is what it draws and measures
/// (the sprite draws Image in preference to Texture).
/// </summary>
public class ImageOverridesTextureTests : IDisposable
{
    private readonly SKSurface _surface;
    private readonly SKFilterMode _savedFilter = global::RenderingLibrary.Graphics.Renderer.TextureFilter;

    public ImageOverridesTextureTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(16, 8));
        GumService.Default.Initialize(_surface.Canvas, 16, 8);
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = _savedFilter;
    }

    private static SKBitmap Solid(SKColor color, int width = 4, int height = 4)
    {
        SKBitmap bitmap = new(width, height);
        bitmap.Erase(color);
        return bitmap;
    }

    [Fact]
    public void Draw_SpriteWithImageAssignedOverItsTexture_ShowsTheImage()
    {
        using SKBitmap red = Solid(SKColors.Red);
        using SKBitmap blue = Solid(SKColors.Blue);
        using SKImage blueImage = SKImage.FromBitmap(blue);
        SpriteRuntime sprite = new()
        {
            Width = 4,
            Height = 4,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
        };
        sprite.Texture = red;
        sprite.Image = blueImage;
        GumService.Default.Root.Children.Add(sprite);

        GumService.Default.Draw();

        using SKImage snapshot = _surface.Snapshot();
        using SKBitmap drawn = SKBitmap.FromImage(snapshot);
        drawn.GetPixel(1, 1).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void TextureSize_SpriteWithImageAssignedOverItsTexture_IsTheImageSize()
    {
        using SKBitmap small = Solid(SKColors.Red, width: 8, height: 8);
        using SKBitmap large = Solid(SKColors.Blue, width: 24, height: 12);
        using SKImage largeImage = SKImage.FromBitmap(large);
        Sprite sprite = new() { Texture = small };

        sprite.Image = largeImage;

        sprite.TextureWidth.ShouldBe(24);
        sprite.TextureHeight.ShouldBe(12);
        sprite.AspectRatio.ShouldBe(2f);
    }
}
