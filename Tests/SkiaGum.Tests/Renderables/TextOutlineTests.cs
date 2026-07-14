using Shouldly;
using SkiaGum;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies <see cref="Text.OutlineThickness"/> / <see cref="Text.OutlineColor"/> behavior. The
/// outline was originally drawn through RichTextKit's halo (issue #3675), but that halo is a centered
/// stroke with no join control -- thin at 1x and miter-spiking/embossing at wider widths. It is now
/// drawn at render time as a recolor + dilate pass in <see cref="Text.Render"/>, so
/// <see cref="Text.GetStyle"/> no longer emits any halo. These tests guard the property round-trips
/// and that the halo migration stays in place (a regression to setting HaloWidth would bring the
/// spikes back).
/// </summary>
public class TextOutlineTests
{
    [Fact]
    public void GetStyle_EmitsNoHalo_WhenOutlineThicknessIsZero()
    {
        Text sut = new();
        sut.OutlineThickness = 0;

        Topten.RichTextKit.Style style = sut.GetStyle();

        style.HaloWidth.ShouldBe(0f);
    }

    [Fact]
    public void GetStyle_EmitsNoHalo_EvenWhenOutlineThicknessIsSet()
    {
        // The outline is drawn in Render (recolor + dilate), not through RichTextKit's halo, so
        // GetStyle must leave HaloWidth at zero regardless of OutlineThickness.
        Text sut = new();
        sut.OutlineThickness = 4;
        sut.OutlineColor = SKColors.Red;

        Topten.RichTextKit.Style style = sut.GetStyle();

        style.HaloWidth.ShouldBe(0f);
    }

    [Fact]
    public void OutlineColor_DefaultsToBlack()
    {
        Text sut = new();

        sut.OutlineColor.ShouldBe(SKColors.Black);
    }

    [Fact]
    public void OutlineColor_RoundTrips()
    {
        Text sut = new();
        sut.OutlineColor = SKColors.Red;

        sut.OutlineColor.ShouldBe(SKColors.Red);
    }

    [Fact]
    public void OutlineThickness_RoundTrips()
    {
        Text sut = new();
        sut.OutlineThickness = 4;

        sut.OutlineThickness.ShouldBe(4);
    }
}
