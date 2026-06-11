using Gum.Renderables;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Covers the Raylib NineSlice renderable's tiling / border-scale / custom-frame-width
/// feature (issue #3105). The visual result is verified by the raylib sample, but the
/// section-layout math is pure and exercised directly here.
/// </summary>
public class NineSliceTests
{
    #region Property defaults

    [Fact]
    public void BorderScale_ShouldDefaultToOne()
    {
        NineSlice sut = new();
        sut.BorderScale.ShouldBe(1f);
    }

    [Fact]
    public void CustomFrameTextureCoordinateWidth_ShouldDefaultToNull()
    {
        NineSlice sut = new();
        sut.CustomFrameTextureCoordinateWidth.ShouldBeNull();
    }

    [Fact]
    public void IsTilingMiddleSections_ShouldDefaultToFalse()
    {
        NineSlice sut = new();
        sut.IsTilingMiddleSections.ShouldBeFalse();
    }

    #endregion

    #region ComputeDrawSections — non-tiling

    [Fact]
    public void ComputeDrawSections_NonTiling_ShouldProduceNineSections()
    {
        // 30x30 source, default 1/3 split → 10px bands. 100x100 destination.
        List<NineSliceDrawSection> sections = NineSlice.ComputeDrawSections(
            srcLeft: 0, srcTop: 0, srcRight: 30, srcBottom: 30,
            destWidth: 100, destHeight: 100,
            borderScale: 1f, customFrameTextureCoordinateWidth: null,
            isTilingMiddleSections: false);

        sections.Count.ShouldBe(9);
    }

    [Fact]
    public void ComputeDrawSections_NonTiling_TopEdge_ShouldStretchToInsideWidth()
    {
        List<NineSliceDrawSection> sections = NineSlice.ComputeDrawSections(
            srcLeft: 0, srcTop: 0, srcRight: 30, srcBottom: 30,
            destWidth: 100, destHeight: 100,
            borderScale: 1f, customFrameTextureCoordinateWidth: null,
            isTilingMiddleSections: false);

        // The top-middle band sits at local x=10 (after the left corner), y=0.
        NineSliceDrawSection topEdge = sections.Single(s =>
            s.Destination.X == 10 && s.Destination.Y == 0);

        // Stretched: one section spanning the full inside width (100 - 10 - 10).
        topEdge.Destination.Width.ShouldBe(80f);
        topEdge.Source.Width.ShouldBe(10f);
    }

    #endregion

    #region ComputeDrawSections — tiling

    [Fact]
    public void ComputeDrawSections_Tiling_ShouldProduceMoreSectionsThanStretching()
    {
        List<NineSliceDrawSection> stretched = NineSlice.ComputeDrawSections(
            srcLeft: 0, srcTop: 0, srcRight: 30, srcBottom: 30,
            destWidth: 100, destHeight: 100,
            borderScale: 1f, customFrameTextureCoordinateWidth: null,
            isTilingMiddleSections: false);

        List<NineSliceDrawSection> tiled = NineSlice.ComputeDrawSections(
            srcLeft: 0, srcTop: 0, srcRight: 30, srcBottom: 30,
            destWidth: 100, destHeight: 100,
            borderScale: 1f, customFrameTextureCoordinateWidth: null,
            isTilingMiddleSections: true);

        tiled.Count.ShouldBeGreaterThan(stretched.Count);
    }

    [Fact]
    public void ComputeDrawSections_Tiling_TopEdge_ShouldRepeatAtNaturalWidthInsteadOfStretching()
    {
        List<NineSliceDrawSection> sections = NineSlice.ComputeDrawSections(
            srcLeft: 0, srcTop: 0, srcRight: 30, srcBottom: 30,
            destWidth: 100, destHeight: 100,
            borderScale: 1f, customFrameTextureCoordinateWidth: null,
            isTilingMiddleSections: true);

        // Every quad in the top band (y=0, beyond the left corner) draws at the natural
        // 10px tile width rather than the stretched 80px inside width.
        List<NineSliceDrawSection> topBand = sections
            .Where(s => s.Destination.Y == 0 && s.Destination.X >= 10 && s.Destination.X < 90)
            .ToList();

        topBand.Count.ShouldBe(8);
        topBand.ShouldAllBe(s => s.Destination.Width == 10f);
    }

    #endregion

    #region ComputeDrawSections — BorderScale and CustomFrameTextureCoordinateWidth

    [Fact]
    public void ComputeDrawSections_BorderScale_ShouldGrowDestinationCornerWithoutGrowingSource()
    {
        List<NineSliceDrawSection> sections = NineSlice.ComputeDrawSections(
            srcLeft: 0, srcTop: 0, srcRight: 30, srcBottom: 30,
            destWidth: 200, destHeight: 200,
            borderScale: 2f, customFrameTextureCoordinateWidth: null,
            isTilingMiddleSections: false);

        // Top-left corner sits at local (0,0).
        NineSliceDrawSection topLeft = sections.Single(s =>
            s.Destination.X == 0 && s.Destination.Y == 0);

        topLeft.Destination.Width.ShouldBe(20f); // 10px source * BorderScale 2
        topLeft.Source.Width.ShouldBe(10f);      // source unchanged
    }

    [Fact]
    public void ComputeDrawSections_CustomFrameTextureCoordinateWidth_ShouldOverrideThirdSplit()
    {
        List<NineSliceDrawSection> sections = NineSlice.ComputeDrawSections(
            srcLeft: 0, srcTop: 0, srcRight: 30, srcBottom: 30,
            destWidth: 100, destHeight: 100,
            borderScale: 1f, customFrameTextureCoordinateWidth: 5f,
            isTilingMiddleSections: false);

        NineSliceDrawSection topLeft = sections.Single(s =>
            s.Destination.X == 0 && s.Destination.Y == 0);

        // Explicit 5px frame instead of the default 30/3 = 10px.
        topLeft.Source.Width.ShouldBe(5f);
        topLeft.Destination.Width.ShouldBe(5f);
    }

    [Fact]
    public void ComputeDrawSections_DestinationSmallerThanTwoCorners_ShouldClampCorners()
    {
        // 10px corners would overlap on an 8px destination; each corner clamps to 4px.
        List<NineSliceDrawSection> sections = NineSlice.ComputeDrawSections(
            srcLeft: 0, srcTop: 0, srcRight: 30, srcBottom: 30,
            destWidth: 8, destHeight: 8,
            borderScale: 1f, customFrameTextureCoordinateWidth: null,
            isTilingMiddleSections: false);

        NineSliceDrawSection topLeft = sections.Single(s =>
            s.Destination.X == 0 && s.Destination.Y == 0);

        topLeft.Destination.Width.ShouldBe(4f);
    }

    #endregion
}
