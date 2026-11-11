using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A NineSlice reports the size of the image it draws as its texture size, as a Sprite does: an
/// <c>Image</c> assigned to it (with or without an earlier <c>Texture</c>) is the texture.
/// </summary>
public class NineSliceImageTextureSizeTests
{
    private static SKBitmap Solid(SKColor color, int width, int height)
    {
        SKBitmap bitmap = new(width, height);
        bitmap.Erase(color);
        return bitmap;
    }

    [Fact]
    public void TextureSize_NineSliceWithOnlyAnImage_IsTheImageSize()
    {
        using SKBitmap bitmap = Solid(SKColors.Red, width: 24, height: 12);
        using SKImage image = SKImage.FromBitmap(bitmap);
        NineSlice nineSlice = new() { Image = image };

        nineSlice.TextureWidth.ShouldBe(24);
        nineSlice.TextureHeight.ShouldBe(12);
    }

    [Fact]
    public void TextureSize_NineSliceWithImageAssignedOverItsTexture_IsTheImageSize()
    {
        using SKBitmap small = Solid(SKColors.Red, width: 8, height: 8);
        using SKBitmap large = Solid(SKColors.Blue, width: 24, height: 12);
        using SKImage largeImage = SKImage.FromBitmap(large);
        NineSlice nineSlice = new() { Texture = small };

        nineSlice.Image = largeImage;

        nineSlice.TextureWidth.ShouldBe(24);
        nineSlice.TextureHeight.ShouldBe(12);
    }
}
