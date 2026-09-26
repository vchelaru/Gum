using RenderingLibrary;
using Shouldly;
using SkiaGum;
using SkiaSharp;
using Svg.Skia;

namespace SkiaGum.Tests.Renderables;

public class VectorSpriteTests
{
    // An SKSvg that failed to load (or was never loaded) has a null Picture.
    [Fact]
    public void Render_TextureWithoutPicture_DoesNotThrow()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(16, 16));
        SystemManagers managers = new SystemManagers();
        managers.Canvas = surface.Canvas;
        using SKSvg svg = new SKSvg();
        VectorSprite sprite = new VectorSprite();
        sprite.Texture = svg;

        Should.NotThrow(() => sprite.Render(managers));
    }

    [Fact]
    public void TextureWidth_TextureWithoutPicture_IsNull()
    {
        using SKSvg svg = new SKSvg();
        VectorSprite sprite = new VectorSprite();
        sprite.Texture = svg;

        float? width = sprite.TextureWidth;

        width.ShouldBeNull();
    }
}
