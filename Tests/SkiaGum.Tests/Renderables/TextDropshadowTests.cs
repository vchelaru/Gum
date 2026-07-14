using Shouldly;
using SkiaGum;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies the standalone (non-BBCode) drop-shadow property set on <see cref="Text"/> (issue #3674).
/// The shadow is a canvas/ImageFilter effect applied in <see cref="Text.Render"/>, mirroring the
/// drop-shadow vocabulary <c>RenderableShapeBase</c> exposes for Skia shapes.
/// </summary>
public class TextDropshadowTests
{
    [Fact]
    public void DropshadowColor_ComposesFromChannelSetters()
    {
        Text sut = new();
        sut.DropshadowRed = 10;
        sut.DropshadowGreen = 20;
        sut.DropshadowBlue = 30;
        sut.DropshadowAlpha = 40;

        sut.DropshadowColor.ShouldBe(new SKColor(10, 20, 30, 40));
    }

    [Fact]
    public void GetDropshadowPaint_ReturnsNull_WhenHasDropshadowFalse()
    {
        Text sut = new();
        sut.HasDropshadow = false;

        sut.GetDropshadowPaint().ShouldBeNull();
    }

    [Fact]
    public void GetDropshadowPaint_ReturnsPaintWithImageFilter_WhenHasDropshadowTrue()
    {
        Text sut = new();
        sut.HasDropshadow = true;
        sut.DropshadowOffsetX = 2;
        sut.DropshadowOffsetY = 3;
        sut.DropshadowBlurX = 6;
        sut.DropshadowBlurY = 6;
        sut.DropshadowColor = SKColors.Black;

        using SKPaint? paint = sut.GetDropshadowPaint();

        paint.ShouldNotBeNull();
        paint.ImageFilter.ShouldNotBeNull();
    }

    [Fact]
    public void HasDropshadow_DefaultsToFalse()
    {
        Text sut = new();

        sut.HasDropshadow.ShouldBeFalse();
    }
}
