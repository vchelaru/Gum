using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Consecutive sprites on one texture are batched into a single DrawAtlas call (#7826). A sprite whose
/// <c>Image</c> was assigned over its <c>Texture</c> draws that image (the sprite prefers Image over Texture),
/// so it must not be batched with, or recolored by, the sprites that draw the texture.
/// </summary>
public class SpriteAtlasBatchImageOverrideTests : IDisposable
{
    private readonly SKSurface _surface;
    private readonly SKFilterMode _savedFilter = global::RenderingLibrary.Graphics.Renderer.TextureFilter;

    public SpriteAtlasBatchImageOverrideTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(64, 16));
        GumService.Default.Initialize(_surface.Canvas, 64, 16);
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = _savedFilter;
    }

    private static SKBitmap Solid(SKColor color)
    {
        SKBitmap bitmap = new(4, 4);
        bitmap.Erase(color);
        return bitmap;
    }

    private static SpriteRuntime NewSprite(float x) => new()
    {
        X = x,
        Y = 0,
        Width = 4,
        Height = 4,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
    };

    private SKBitmap DrawAll(params GraphicalUiElement[] runtimes)
    {
        _surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Root.Children.Clear();
        foreach (GraphicalUiElement runtime in runtimes)
        {
            GumService.Default.Root.Children.Add(runtime);
        }
        GumService.Default.Draw();
        using SKImage snapshot = _surface.Snapshot();
        return SKBitmap.FromImage(snapshot);
    }

    [Fact]
    public void Draw_SpriteWithImageAssignedOverTheSharedTexture_ShowsItsImageNextToSpritesOnTheTexture()
    {
        using SKBitmap red = Solid(SKColors.Red);
        using SKBitmap blue = Solid(SKColors.Blue);
        using SKImage blueImage = SKImage.FromBitmap(blue);
        SpriteRuntime first = NewSprite(0);
        first.Texture = red;
        SpriteRuntime overridden = NewSprite(6);
        overridden.Texture = red;
        overridden.Image = blueImage;
        SpriteRuntime last = NewSprite(12);
        last.Texture = red;

        using SKBitmap drawn = DrawAll(first, overridden, last);

        drawn.GetPixel(1, 1).ShouldBe(SKColors.Red);
        drawn.GetPixel(7, 1).ShouldBe(SKColors.Blue);
        drawn.GetPixel(13, 1).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void Draw_SpriteWithImageAssignedOverTheSharedTexture_AsFirstInTheBatch_DoesNotRecolorTheOthers()
    {
        using SKBitmap red = Solid(SKColors.Red);
        using SKBitmap blue = Solid(SKColors.Blue);
        using SKImage blueImage = SKImage.FromBitmap(blue);
        SpriteRuntime overridden = NewSprite(0);
        overridden.Texture = red;
        overridden.Image = blueImage;
        SpriteRuntime second = NewSprite(6);
        second.Texture = red;

        using SKBitmap drawn = DrawAll(overridden, second);

        drawn.GetPixel(1, 1).ShouldBe(SKColors.Blue);
        drawn.GetPixel(7, 1).ShouldBe(SKColors.Red);
    }
}
