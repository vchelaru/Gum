using Gum.Renderables;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// A different source cut on each side (#8354) on the raylib nine-slice: corners and edges keep their
/// texels and size, only the middle stretches. The MonoGame nine-slice pins the same case.
/// </summary>
public class RaylibNineSliceSourceCutTests : BaseTestClass
{
    private static void ShouldHave(List<NineSliceDrawSection> sections,
        float srcX, float srcY, float srcWidth, float srcHeight, float x, float y, float width, float height)
    {
        NineSliceDrawSection section = sections.Single(s => s.Source.X == srcX && s.Source.Y == srcY);
        section.Source.Width.ShouldBe(srcWidth);
        section.Source.Height.ShouldBe(srcHeight);
        section.Destination.X.ShouldBe(x);
        section.Destination.Y.ShouldBe(y);
        section.Destination.Width.ShouldBe(width);
        section.Destination.Height.ShouldBe(height);
    }

    [Fact]
    public void GetDrawSections_PerSideCut_ShouldKeepCornersAndEdgesAtTheirOwnSize()
    {
        // 12x16 texture cut at left 6, top 8, right 2, bottom 3 leaves a 4x5 middle; drawn at 40x26.
        NineSlice sut = new()
        {
            Width = 40, Height = 26,
            CustomFrameTextureCoordinateLeft = 6, CustomFrameTextureCoordinateTop = 8,
            CustomFrameTextureCoordinateRight = 2, CustomFrameTextureCoordinateBottom = 3,
        };

        List<NineSliceDrawSection> sections = sut.GetDrawSections(0, 0, 12, 16);

        sections.Count.ShouldBe(9);
        ShouldHave(sections, 0, 0, 6, 8, 0, 0, 6, 8);
        ShouldHave(sections, 6, 0, 4, 8, 6, 0, 32, 8);
        ShouldHave(sections, 10, 0, 2, 8, 38, 0, 2, 8);
        ShouldHave(sections, 0, 8, 6, 5, 0, 8, 6, 15);
        ShouldHave(sections, 6, 8, 4, 5, 6, 8, 32, 15);
        ShouldHave(sections, 10, 8, 2, 5, 38, 8, 2, 15);
        ShouldHave(sections, 0, 13, 6, 3, 0, 23, 6, 3);
        ShouldHave(sections, 6, 13, 4, 3, 6, 23, 32, 3);
        ShouldHave(sections, 10, 13, 2, 3, 38, 23, 2, 3);
    }

    [Fact]
    public void GetDrawSections_PerSideCutWithBorderScale_ShouldScaleThicknessButNotTheCut()
    {
        NineSlice sut = new()
        {
            Width = 40, Height = 26,
            CustomFrameTextureCoordinateWidth = 3,
            CustomFrameTextureCoordinateLeft = 6, CustomFrameTextureCoordinateRight = 2,
            CustomBorderScaleLeft = 2,
        };

        List<NineSliceDrawSection> sections = sut.GetDrawSections(0, 0, 12, 16);

        ShouldHave(sections, 0, 0, 6, 3, 0, 0, 12, 3);
        ShouldHave(sections, 6, 0, 4, 3, 12, 0, 26, 3);
        ShouldHave(sections, 10, 0, 2, 3, 38, 0, 2, 3);
        ShouldHave(sections, 6, 3, 4, 10, 12, 3, 26, 20);
    }

    [Fact]
    public void GetDrawSections_NoCutSet_ShouldCutAThirdOfEachAxis()
    {
        NineSlice sut = new() { Width = 40, Height = 26 };

        List<NineSliceDrawSection> sections = sut.GetDrawSections(0, 0, 12, 16);

        ShouldHave(sections, 0, 0, 4, 5, 0, 0, 4, 5);
        ShouldHave(sections, 4, 5, 4, 6, 4, 5, 32, 16);
        ShouldHave(sections, 8, 11, 4, 5, 36, 21, 4, 5);
    }

    [Fact]
    public void CanUseNinePatch_ShouldBeFalseWhenAnySideCutIsSet()
    {
        new NineSlice { CustomFrameTextureCoordinateLeft = 1 }.CanUseNinePatch(isFlipped: false).ShouldBeFalse();
        new NineSlice { CustomFrameTextureCoordinateTop = 1 }.CanUseNinePatch(isFlipped: false).ShouldBeFalse();
        new NineSlice { CustomFrameTextureCoordinateRight = 1 }.CanUseNinePatch(isFlipped: false).ShouldBeFalse();
        new NineSlice { CustomFrameTextureCoordinateBottom = 1 }.CanUseNinePatch(isFlipped: false).ShouldBeFalse();
    }
}
