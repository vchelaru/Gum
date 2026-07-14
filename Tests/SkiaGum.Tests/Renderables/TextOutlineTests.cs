using Shouldly;
using SkiaGum;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies that <see cref="Text.OutlineThickness"/> is rendered through RichTextKit's halo
/// (issue #3675). Before this change the property was accepted by the dispatch layer but the
/// Skia renderer never drew an outline, so it had zero visual effect.
/// </summary>
public class TextOutlineTests
{
    [Fact]
    public void GetStyle_NoHalo_WhenOutlineThicknessIsZero()
    {
        Text sut = new();
        sut.OutlineThickness = 0;

        Topten.RichTextKit.Style style = sut.GetStyle();

        style.HaloWidth.ShouldBe(0f);
    }

    [Fact]
    public void GetStyle_SetsHaloColor_FromOutlineColor()
    {
        Text sut = new();
        sut.OutlineThickness = 3;
        sut.OutlineColor = SKColors.Red;

        Topten.RichTextKit.Style style = sut.GetStyle();

        style.HaloColor.ShouldBe(SKColors.Red);
    }

    [Fact]
    public void GetStyle_SetsHaloWidth_FromOutlineThickness()
    {
        Text sut = new();
        sut.OutlineThickness = 4;

        Topten.RichTextKit.Style style = sut.GetStyle();

        style.HaloWidth.ShouldBe(4f);
    }

    [Fact]
    public void OutlineColor_DefaultsToBlack()
    {
        Text sut = new();

        sut.OutlineColor.ShouldBe(SKColors.Black);
    }
}
