using Gum;
using Gum.Wireframe;
using RenderingLibrary;using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Skipping the opaque white Modulate filter must not change a pixel. Each case draws a real Sprite or NineSlice
/// through <see cref="GumService.Draw"/> on a raster surface, once as shipped and once as a subclass that attaches the
/// white filter the way the code did before the skip.
/// </summary>
public class WhiteTintSkipDrawTests
{
    const int Size = 32;

    private sealed class FilteredSprite : Sprite
    {
        protected override SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
        {
            SKPaint paint = base.GetPaint(boundingRect, absoluteRotation);
            paint.ColorFilter ??= SKColorFilter.CreateBlendMode(SKColors.White, SKBlendMode.Modulate);
            return paint;
        }
    }

    private sealed class FilteredNineSlice : NineSlice
    {
        protected override SKPaint GetPaint(SKRect boundingRect, float absoluteRotation)
        {
            SKPaint paint = base.GetPaint(boundingRect, absoluteRotation);
            paint.ColorFilter ??= SKColorFilter.CreateBlendMode(SKColors.White, SKBlendMode.Modulate);
            return paint;
        }
    }

    public static IEnumerable<object[]> Colors()
    {
        yield return new object[] { new SKColor(255, 255, 255, 255) };
        yield return new object[] { new SKColor(255, 255, 255, 200) };
        yield return new object[] { new SKColor(255, 255, 255, 128) };
        yield return new object[] { new SKColor(255, 255, 255, 1) };
        yield return new object[] { new SKColor(255, 255, 255, 0) };
        yield return new object[] { new SKColor(200, 120, 40, 255) };
    }

    [Theory]
    [MemberData(nameof(Colors))]
    public void Sprite_DrawsSamePixelsAsBeforeTheSkip(SKColor color)
    {
        byte[] shipped = Draw(new Sprite { Texture = CreateTexture(), Color = color });
        byte[] before = Draw(new FilteredSprite { Texture = CreateTexture(), Color = color });

        shipped.ShouldBe(before);
        shipped.Any(value => value != 0).ShouldBe(color.Alpha != 0);
    }

    [Theory]
    [MemberData(nameof(Colors))]
    public void NineSlice_DrawsSamePixelsAsBeforeTheSkip(SKColor color)
    {
        byte[] shipped = Draw(new NineSlice { Texture = CreateTexture(), Color = color });
        byte[] before = Draw(new FilteredNineSlice { Texture = CreateTexture(), Color = color });

        shipped.ShouldBe(before);
        shipped.Any(value => value != 0).ShouldBe(color.Alpha != 0);
    }

    static SKBitmap CreateTexture()
    {
        SKBitmap bitmap = new(Size, Size);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 7), (byte)(y * 7), (byte)(255 - x * 5), (byte)255));
            }
        }
        return bitmap;
    }

    static byte[] Draw(IRenderableIpso renderable)
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        using SKSurface surface = SKSurface.Create(new SKImageInfo(Size, Size, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Initialize(surface.Canvas, Size, Size);
        GraphicalUiElement element = new(renderable) { Width = Size, Height = Size, WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute, HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute };
        GumService.Default.Root.Children.Add(element);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap readback = SKBitmap.FromImage(image);
        return readback.Bytes;
    }
}
