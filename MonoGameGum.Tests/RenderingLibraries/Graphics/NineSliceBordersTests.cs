using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

// NineSliceBorders is the one source for how thick each border band is, shared by the MonoGame,
// Raylib and Skia nine-slices (#6983).
public class NineSliceBordersTests
{
    [Fact]
    public void Compute_UniformScale_ShouldScaleEverySideByIt()
    {
        NineSliceBorders sut = NineSliceBorders.Compute(4, 3, 2f, 2f, 2f, 2f, 100, 100);

        sut.DestLeft.ShouldBe(8f);
        sut.DestRight.ShouldBe(8f);
        sut.DestTop.ShouldBe(6f);
        sut.DestBottom.ShouldBe(6f);
        sut.SourceLeft.ShouldBe(4);
        sut.SourceRight.ShouldBe(4);
        sut.SourceTop.ShouldBe(3);
        sut.SourceBottom.ShouldBe(3);
    }

    [Fact]
    public void Compute_PerSideScales_ShouldScaleEachSideByItsOwn()
    {
        NineSliceBorders sut = NineSliceBorders.Compute(10, 10, 1f, 2f, 3f, 0.5f, 200, 200);

        sut.DestLeft.ShouldBe(10f);
        sut.DestTop.ShouldBe(20f);
        sut.DestRight.ShouldBe(30f);
        sut.DestBottom.ShouldBe(5f);
    }

    [Fact]
    public void Compute_ZeroScale_ShouldCollapseThatSideAndKeepFullSourceInset()
    {
        NineSliceBorders sut = NineSliceBorders.Compute(10, 10, 0f, 1f, 1f, 1f, 100, 100);

        sut.DestLeft.ShouldBe(0f);
        // The middle pieces still sample past the full frame, so the collapsed side's texels are skipped.
        sut.SourceLeft.ShouldBe(10);
        sut.DestRight.ShouldBe(10f);
    }

    [Fact]
    public void Compute_BothSidesZero_ShouldDrawNoBorderOnThatAxis()
    {
        NineSliceBorders sut = NineSliceBorders.Compute(10, 10, 0f, 1f, 0f, 1f, 100, 100);

        sut.DestLeft.ShouldBe(0f);
        sut.DestRight.ShouldBe(0f);
        sut.DestTop.ShouldBe(10f);
    }

    [Fact]
    public void Compute_NegativeScale_ShouldBehaveAsZero()
    {
        NineSliceBorders sut = NineSliceBorders.Compute(10, 10, -3f, 1f, 1f, 1f, 100, 100);

        sut.DestLeft.ShouldBe(0f);
    }

    [Fact]
    public void Compute_BandsWiderThanNineSlice_ShouldShrinkInProportionToTheirScales()
    {
        // 10 texel frame, scales 1 and 3 would need 40 but only 20 is available.
        NineSliceBorders sut = NineSliceBorders.Compute(10, 10, 1f, 1f, 3f, 1f, 20, 100);

        sut.DestLeft.ShouldBe(5f);
        sut.DestRight.ShouldBe(15f);
        (sut.DestLeft + sut.DestRight).ShouldBe(20f);
    }

    [Fact]
    public void Compute_BandsWiderThanNineSlice_ShouldSampleFewerTexelsOnBothSides()
    {
        NineSliceBorders sut = NineSliceBorders.Compute(10, 10, 1f, 1f, 3f, 1f, 20, 100);

        sut.SourceLeft.ShouldBe(5);
        sut.SourceRight.ShouldBe(5);
    }

    [Fact]
    public void Compute_UniformScaleWiderThanNineSlice_ShouldSplitTheWidthEvenly()
    {
        NineSliceBorders sut = NineSliceBorders.Compute(10, 10, 2f, 2f, 2f, 2f, 30, 30);

        sut.DestLeft.ShouldBe(15f);
        sut.DestRight.ShouldBe(15f);
        sut.SourceLeft.ShouldBe(8);
    }

    [Fact]
    public void Compute_AllScalesZero_ShouldDrawNoBorderAtAll()
    {
        NineSliceBorders sut = NineSliceBorders.Compute(10, 10, 0f, 0f, 0f, 0f, 50, 50);

        sut.DestLeft.ShouldBe(0f);
        sut.DestTop.ShouldBe(0f);
        sut.DestRight.ShouldBe(0f);
        sut.DestBottom.ShouldBe(0f);
    }

    [Fact]
    public void Compute_NegativeSize_ShouldNotProduceNegativeThickness()
    {
        NineSliceBorders sut = NineSliceBorders.Compute(10, 10, 1f, 1f, 1f, 1f, -20, -20);

        sut.DestLeft.ShouldBe(0f);
        sut.DestTop.ShouldBe(0f);
    }
}
