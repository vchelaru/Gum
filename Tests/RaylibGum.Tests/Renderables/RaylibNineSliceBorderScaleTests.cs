using Gum.GueDeriving;
using Gum.Renderables;
using RaylibGum.Helpers;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Per-side border scale overrides (#6983) on the Raylib nine-slice. The section layout is pure, so
/// it is pinned here with the same scenarios the MonoGame and Skia nine-slices draw pixel for pixel.
/// </summary>
public class RaylibNineSliceBorderScaleTests : BaseTestClass
{
    // A 3x3 texel source with a 1 texel frame.
    private static List<NineSliceDrawSection> Layout(float width, float height, float borderScale,
        float? left, float? top, float? right, float? bottom, bool isTiling = false) =>
        NineSlice.ComputeDrawSections(0, 0, 3, 3, width, height,
            borderScale,
            left ?? borderScale, top ?? borderScale, right ?? borderScale, bottom ?? borderScale,
            customFrameTextureCoordinateWidth: null, isTilingMiddleSections: isTiling);

    private static NineSliceDrawSection SectionWithSource(List<NineSliceDrawSection> sections, int srcX, int srcY) =>
        sections.Single(s => s.Source.X == srcX && s.Source.Y == srcY);

    private static void ShouldBeDest(NineSliceDrawSection section, float x, float y, float width, float height)
    {
        section.Destination.X.ShouldBe(x);
        section.Destination.Y.ShouldBe(y);
        section.Destination.Width.ShouldBe(width);
        section.Destination.Height.ShouldBe(height);
    }

    [Fact]
    public void CustomBorderScales_ShouldDefaultToNull()
    {
        NineSlice sut = new();

        sut.CustomBorderScaleLeft.ShouldBeNull();
        sut.CustomBorderScaleTop.ShouldBeNull();
        sut.CustomBorderScaleRight.ShouldBeNull();
        sut.CustomBorderScaleBottom.ShouldBeNull();
    }

    [Fact]
    public void ComputeDrawSections_NoOverrides_ShouldMatchTheUniformOverload()
    {
        List<NineSliceDrawSection> uniform = NineSlice.ComputeDrawSections(0, 0, 3, 3, 60, 60, 10f, null, false);
        List<NineSliceDrawSection> perSide = Layout(60, 60, 10, null, null, null, null);

        perSide.Count.ShouldBe(uniform.Count);
        for (int i = 0; i < uniform.Count; i++)
        {
            perSide[i].Source.ShouldBe(uniform[i].Source);
            perSide[i].Destination.ShouldBe(uniform[i].Destination);
        }
    }

    [Fact]
    public void ComputeDrawSections_PerSideOverrides_ShouldSizeEachBandByItsOwnScale()
    {
        // Left 20, top 5, right 30, bottom falls back to 10 (BorderScale).
        List<NineSliceDrawSection> sections = Layout(100, 100, 10, 20f, 5f, 30f, null);

        ShouldBeDest(SectionWithSource(sections, 0, 0), 0, 0, 20, 5);
        ShouldBeDest(SectionWithSource(sections, 1, 0), 20, 0, 50, 5);
        ShouldBeDest(SectionWithSource(sections, 2, 0), 70, 0, 30, 5);
        ShouldBeDest(SectionWithSource(sections, 0, 1), 0, 5, 20, 85);
        ShouldBeDest(SectionWithSource(sections, 1, 1), 20, 5, 50, 85);
        ShouldBeDest(SectionWithSource(sections, 2, 1), 70, 5, 30, 85);
        ShouldBeDest(SectionWithSource(sections, 0, 2), 0, 90, 20, 10);
        ShouldBeDest(SectionWithSource(sections, 1, 2), 20, 90, 50, 10);
        ShouldBeDest(SectionWithSource(sections, 2, 2), 70, 90, 30, 10);
    }

    [Fact]
    public void ComputeDrawSections_ZeroScaleSide_ShouldDropThatBandAndFillTheSpace()
    {
        // Left 20, top 0, right 5, bottom 10 in 60x60.
        List<NineSliceDrawSection> sections = Layout(60, 60, 10, 20f, 0f, 5f, null);

        sections.Count.ShouldBe(6);
        sections.Any(s => s.Source.Y == 0).ShouldBeFalse();
        ShouldBeDest(SectionWithSource(sections, 0, 1), 0, 0, 20, 50);
        ShouldBeDest(SectionWithSource(sections, 1, 1), 20, 0, 35, 50);
        ShouldBeDest(SectionWithSource(sections, 2, 1), 55, 0, 5, 50);
        ShouldBeDest(SectionWithSource(sections, 0, 2), 0, 50, 20, 10);
        ShouldBeDest(SectionWithSource(sections, 1, 2), 20, 50, 35, 10);
        ShouldBeDest(SectionWithSource(sections, 2, 2), 55, 50, 5, 10);
    }

    [Fact]
    public void ComputeDrawSections_ZeroScaleSide_ShouldLeaveNoGapOrOverlap()
    {
        List<NineSliceDrawSection> sections = Layout(60, 60, 10, 20f, 0f, 5f, null);

        float area = sections.Sum(s => s.Destination.Width * s.Destination.Height);
        area.ShouldBe(60f * 60f);
    }

    [Fact]
    public void ComputeDrawSections_ZeroLeftWhileTiling_ShouldStartTilesAtTheLeftEdge()
    {
        List<NineSliceDrawSection> sections = Layout(60, 60, 10, 0f, null, null, null, isTiling: true);

        sections.Min(s => s.Destination.X).ShouldBe(0f);
        sections.Where(s => s.Destination.X == 0).ShouldAllBe(s => s.Source.X >= 1f);
    }

    [Fact]
    public void ComputeDrawSections_TilingEdges_ShouldTileAtTheirOwnSidesScale()
    {
        // Top scale 20: one 1 texel tile is 20 wide, so a 60 wide inside needs 3 tiles; the
        // bottom stays at 10, so it needs 6.
        List<NineSliceDrawSection> sections = Layout(80, 80, 10, null, 20f, null, null, isTiling: true);

        sections.Count(s => s.Source.X == 1 && s.Source.Y == 0).ShouldBe(3);
        sections.Count(s => s.Source.X == 1 && s.Source.Y == 2).ShouldBe(6);
    }

    [Fact]
    public void SetProperty_CustomBorderScales_ShouldForwardToRuntime()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("CustomBorderScaleLeft", 1.5f);
        sut.SetProperty("CustomBorderScaleTop", 2.5f);
        sut.SetProperty("CustomBorderScaleRight", 0f);
        sut.SetProperty("CustomBorderScaleBottom", 4f);

        sut.CustomBorderScaleLeft.ShouldBe(1.5f);
        sut.CustomBorderScaleTop.ShouldBe(2.5f);
        sut.CustomBorderScaleRight.ShouldBe(0f);
        sut.CustomBorderScaleBottom.ShouldBe(4f);
    }

    [Fact]
    public void SetProperty_CustomBorderScaleNull_ShouldClearTheOverride()
    {
        NineSliceRuntime sut = new();
        sut.CustomBorderScaleLeft = 3f;

        sut.SetProperty("CustomBorderScaleLeft", null);

        sut.CustomBorderScaleLeft.ShouldBeNull();
    }

    [Fact]
    public void GetDrawSections_LoneLeftOverride_WithBorderScaleOne_WidensOnlyTheLeftBand()
    {
        NineSlice sut = new() { Width = 100, Height = 100, BorderScale = 1f, CustomBorderScaleLeft = 10f };

        List<NineSliceDrawSection> sections = sut.GetDrawSections(0, 0, 3, 3);

        SectionWithSource(sections, 0, 0).Destination.Width.ShouldBe(10f);
        SectionWithSource(sections, 2, 0).Destination.Width.ShouldBe(1f);
        SectionWithSource(sections, 0, 0).Destination.Height.ShouldBe(1f);
        SectionWithSource(sections, 0, 2).Destination.Height.ShouldBe(1f);
    }

    [Fact]
    public void GetDrawSections_EachSideOverride_MapsToItsOwnSide()
    {
        NineSlice sut = new() { Width = 100, Height = 100, BorderScale = 1f };
        sut.CustomBorderScaleLeft = 10f;
        sut.CustomBorderScaleTop = 20f;
        sut.CustomBorderScaleRight = 30f;
        sut.CustomBorderScaleBottom = 40f;

        List<NineSliceDrawSection> sections = sut.GetDrawSections(0, 0, 3, 3);

        SectionWithSource(sections, 0, 1).Destination.Width.ShouldBe(10f);
        SectionWithSource(sections, 1, 0).Destination.Height.ShouldBe(20f);
        SectionWithSource(sections, 2, 1).Destination.Width.ShouldBe(30f);
        SectionWithSource(sections, 1, 2).Destination.Height.ShouldBe(40f);
    }

    [Fact]
    public void CanUseNinePatch_ShouldBeTrueOnlyWhenNoScaleOverrideIsSet()
    {
        new NineSlice().CanUseNinePatch(isFlipped: false).ShouldBeTrue();
        new NineSlice { CustomBorderScaleLeft = 1f }.CanUseNinePatch(false).ShouldBeFalse();
        new NineSlice { CustomBorderScaleTop = 1f }.CanUseNinePatch(false).ShouldBeFalse();
        new NineSlice { CustomBorderScaleRight = 1f }.CanUseNinePatch(false).ShouldBeFalse();
        new NineSlice { CustomBorderScaleBottom = 1f }.CanUseNinePatch(false).ShouldBeFalse();
    }

    [Fact]
    public void ComputeDrawSections_SmallWidth_ShouldShrinkTheSampledTexels()
    {
        // 6 texel source with a 2 texel frame at scale 10 wants 40 wide, but only 20 is available.
        List<NineSliceDrawSection> sections = NineSlice.ComputeDrawSections(0, 0, 6, 6, 20, 100,
            10f, 10f, 10f, 10f, 10f, null, false);

        NineSliceDrawSection left = sections.Single(s => s.Destination.X == 0 && s.Destination.Y == 0);
        left.Destination.Width.ShouldBe(10f);
        left.Source.Width.ShouldBe(1f);
    }

    [Fact]
    public void ComputeDrawSections_NegativeOverride_ShouldBehaveAsZero()
    {
        List<NineSliceDrawSection> sections = Layout(60, 60, 10, -5f, null, null, null);

        sections.Any(s => s.Source.X == 0).ShouldBeFalse();
        SectionWithSource(sections, 1, 1).Destination.X.ShouldBe(0f);
    }
}
