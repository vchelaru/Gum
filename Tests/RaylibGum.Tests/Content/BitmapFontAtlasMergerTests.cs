using RenderingLibrary.Content;
using Shouldly;
using System;
using Xunit;

namespace RaylibGum.Tests.Content;

// Pure pixel/rect math behind merging a multi-page .fnt into one raylib texture (#5316). No window
// or GL needed.
public class BitmapFontAtlasMergerTests
{
    [Fact]
    public void ComputeLayout_WithPagesOfDifferentSizes_ShouldStackAndUseWidestWidth()
    {
        BitmapFontAtlasMerger.Layout layout = BitmapFontAtlasMerger.ComputeLayout(
            new[] { 4, 6, 5 }, new[] { 2, 3, 7 });

        layout.Width.ShouldBe(6);
        layout.Height.ShouldBe(12);
        layout.PageYOffsets.ShouldBe(new[] { 0, 2, 5 });
    }

    [Fact]
    public void ComputeLayout_WithOnePage_ShouldKeepItsSizeAtOffsetZero()
    {
        BitmapFontAtlasMerger.Layout layout = BitmapFontAtlasMerger.ComputeLayout(new[] { 8 }, new[] { 4 });

        layout.Width.ShouldBe(8);
        layout.Height.ShouldBe(4);
        layout.PageYOffsets.ShouldBe(new[] { 0 });
    }

    [Fact]
    public void ComputeLayout_WithNoPages_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() =>
            BitmapFontAtlasMerger.ComputeLayout(Array.Empty<int>(), Array.Empty<int>()));
    }

    [Fact]
    public void ComputeLayout_WithZeroSizedPage_ShouldThrow()
    {
        Should.Throw<ArgumentException>(() =>
            BitmapFontAtlasMerger.ComputeLayout(new[] { 4, 0 }, new[] { 2, 2 }));
    }

    [Fact]
    public void MergeRgba_ShouldPlaceEachPageAtItsOffsetAndPadNarrowPagesTransparent()
    {
        // Page 0 is 2x1 (pixels 1,2), page 1 is 1x2 (pixels 3,4), each pixel's four bytes equal.
        int[] widths = { 2, 1 };
        int[] heights = { 1, 2 };
        byte[][] pages =
        {
            new byte[] { 1, 1, 1, 1, 2, 2, 2, 2 },
            new byte[] { 3, 3, 3, 3, 4, 4, 4, 4 },
        };
        BitmapFontAtlasMerger.Layout layout = BitmapFontAtlasMerger.ComputeLayout(widths, heights);

        byte[] atlas = BitmapFontAtlasMerger.MergeRgba(pages, widths, heights, layout);

        // Atlas is 2 wide x 3 tall.
        atlas.ShouldBe(new byte[]
        {
            1, 1, 1, 1, 2, 2, 2, 2,
            3, 3, 3, 3, 0, 0, 0, 0,
            4, 4, 4, 4, 0, 0, 0, 0,
        });
    }

    [Fact]
    public void MergeRgba_WithPageShorterThanItsSize_ShouldThrow()
    {
        int[] widths = { 2 };
        int[] heights = { 2 };
        BitmapFontAtlasMerger.Layout layout = BitmapFontAtlasMerger.ComputeLayout(widths, heights);

        Should.Throw<ArgumentException>(() =>
            BitmapFontAtlasMerger.MergeRgba(new[] { new byte[4] }, widths, heights, layout));
    }
}
