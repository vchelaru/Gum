using Gum.RenderingLibrary;
using Shouldly;
using SkiaGum;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies that SkiaGum <see cref="Text"/> honors <see cref="Text.Blend"/> by carrying the
/// mapped <see cref="SKBlendMode"/> on the paint built in <see cref="Text.Render"/> (issue #3676),
/// and that a blend composes with a drop shadow in the same render paint.
/// </summary>
public class TextBlendTests
{
    [Fact]
    public void Blend_DefaultsToNull()
    {
        Text sut = new();

        sut.Blend.ShouldBeNull();
    }

    [Fact]
    public void GetRenderPaint_CarriesBlendMode_WhenBlendSet()
    {
        Text sut = new();
        sut.Blend = Blend.Additive;

        using SKPaint? paint = sut.GetRenderPaint();

        paint.ShouldNotBeNull();
        paint.BlendMode.ShouldBe(SKBlendMode.Plus);
    }

    [Fact]
    public void GetRenderPaint_ComposesBlendAndShadow_WhenBothSet()
    {
        Text sut = new();
        sut.Blend = Blend.Additive;
        sut.HasDropshadow = true;
        sut.DropshadowOffsetX = 2;
        sut.DropshadowOffsetY = 3;
        sut.DropshadowColor = SKColors.Black;

        using SKPaint? paint = sut.GetRenderPaint();

        paint.ShouldNotBeNull();
        paint.BlendMode.ShouldBe(SKBlendMode.Plus);
        paint.ImageFilter.ShouldNotBeNull();
    }

    [Fact]
    public void GetRenderPaint_ReturnsNull_WhenNoBlendOrShadow()
    {
        Text sut = new();

        sut.GetRenderPaint().ShouldBeNull();
    }
}
