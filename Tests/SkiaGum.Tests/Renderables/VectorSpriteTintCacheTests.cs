using RenderingLibrary;
using Shouldly;
using SkiaGum;
using SkiaSharp;
using Svg.Skia;
using System.IO;
using System.Text;

namespace SkiaGum.Tests.Renderables;

public class VectorSpriteTintCacheTests
{
    private const string SvgText =
        "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'>" +
        "<rect width='16' height='16' fill='#ffffff'/></svg>";

    private static SKSvg LoadSvg()
    {
        SKSvg svg = new SKSvg();
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(SvgText));
        svg.Load(stream);
        return svg;
    }

    private static SKColor Draw(VectorSprite sprite)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(16, 16, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        SystemManagers managers = new SystemManagers();
        managers.Canvas = surface.Canvas;
        sprite.Render(managers);
        using SKBitmap bitmap = new SKBitmap(16, 16);
        surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
        return bitmap.GetPixel(8, 8);
    }

    private static SKColor DrawFresh(SKSvg svg, SKColor color)
    {
        VectorSprite fresh = new VectorSprite { Width = 16, Height = 16, Texture = svg, Color = color };
        return Draw(fresh);
    }

    [Fact]
    public void Render_TintSequence_MatchesFreshInstanceEachDraw()
    {
        using SKSvg svg = LoadSvg();
        VectorSprite sprite = new VectorSprite { Width = 16, Height = 16, Texture = svg };
        SKColor[] sequence =
        {
            new SKColor(255, 0, 0),
            new SKColor(0, 0, 255),
            SKColors.White,
            new SKColor(0, 255, 0),
            new SKColor(0, 255, 0, 128),
            new SKColor(255, 0, 0),
        };

        foreach (SKColor color in sequence)
        {
            sprite.Color = color;
            Draw(sprite).ShouldBe(DrawFresh(svg, color), $"after setting {color}");
        }
    }

    [Fact]
    public void Render_AlphaChangedBetweenDraws_MatchesFreshInstance()
    {
        using SKSvg svg = LoadSvg();
        VectorSprite sprite = new VectorSprite { Width = 16, Height = 16, Texture = svg, Color = new SKColor(255, 0, 0) };
        Draw(sprite);

        sprite.Alpha = 100;

        Draw(sprite).ShouldBe(DrawFresh(svg, new SKColor(255, 0, 0, 100)));
        Draw(sprite).Alpha.ShouldBeLessThan((byte)255);
    }

    [Fact]
    public void Render_TwoSpritesDifferentTints_EachDrawsOwnTint()
    {
        using SKSvg svg = LoadSvg();
        VectorSprite red = new VectorSprite { Width = 16, Height = 16, Texture = svg, Color = new SKColor(255, 0, 0) };
        VectorSprite blue = (VectorSprite)red.Clone();
        blue.Color = new SKColor(0, 0, 255);

        Draw(red).ShouldBe(new SKColor(255, 0, 0, 255));
        Draw(blue).ShouldBe(new SKColor(0, 0, 255, 255));
        Draw(red).ShouldBe(new SKColor(255, 0, 0, 255));
    }
}
