using RenderingLibrary;
using Shouldly;
using SkiaGum;
using SkiaSharp;
using Svg.Skia;
using System.IO;
using System.Text;

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

    [Fact]
    public void Render_LoadedSvg_DrawsItsShapes()
    {
        const string svgText =
            "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'>" +
            "<rect width='16' height='16' fill='#ff0000'/></svg>";

        SKColor center = RenderSvgAndReadCenter(svgText);

        center.ShouldBe(new SKColor(255, 0, 0, 255));
    }

    [Fact]
    public void Render_SvgWithFilterRegion_DrawsFilteredShapes()
    {
        const string svgText =
            "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'>" +
            "<filter id='f' x='0' y='0' width='1' height='1'><feOffset x='0' y='0' width='16' height='16' dx='0' dy='0'/></filter>" +
            "<rect width='16' height='16' fill='#0000ff' filter='url(#f)'/></svg>";

        SKColor center = RenderSvgAndReadCenter(svgText);

        center.ShouldBe(new SKColor(0, 0, 255, 255));
    }

    private static SKColor RenderSvgAndReadCenter(string svgText)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(16, 16, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        SystemManagers managers = new SystemManagers();
        managers.Canvas = surface.Canvas;
        using SKSvg svg = new SKSvg();
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(svgText));
        svg.Load(stream);
        VectorSprite sprite = new VectorSprite();
        sprite.Width = 16;
        sprite.Height = 16;
        sprite.Texture = svg;

        sprite.Render(managers);

        using SKBitmap bitmap = new SKBitmap(16, 16);
        surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
        return bitmap.GetPixel(8, 8);
    }
}
