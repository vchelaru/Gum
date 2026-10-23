using Shouldly;
using SkiaGum;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies <see cref="Text.OutlineThickness"/> / <see cref="Text.OutlineColor"/> behavior. The
/// outline moved through a recolor + dilate render pass for a while (issue #3675 -> #3693) because
/// RichTextKit's halo is a centered stroke with a hardcoded miter join that spiked at acute glyph
/// corners. That join is now fixed upstream (vendored patch, issue #4068), so the outline is drawn
/// through <see cref="Style.HaloWidth"/>/<see cref="Style.HaloColor"/> again -- doubled so the full
/// <see cref="Text.OutlineThickness"/> width shows outward instead of half of it being hidden under
/// the fill -- which is also what makes a per-run <c>[OutlineThickness]</c> BBCode tag possible
/// (issue #4037), since RichTextKit paints per-run halo natively in one pass.
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
    public void GetStyle_EmitsDoubleWidthHalo_WhenOutlineThicknessIsSet()
    {
        Text sut = new();
        sut.OutlineThickness = 4;
        sut.OutlineColor = SKColors.Red;

        Topten.RichTextKit.Style style = sut.GetStyle();

        style.HaloWidth.ShouldBe(8f);
        style.HaloColor.ShouldBe(SKColors.Red);
    }

    [Fact]
    public void GetStyle_ScalesHaloWithFontScale()
    {
        Text sut = new();
        sut.OutlineThickness = 4;
        sut.FontScale = 1;
        float halfScale = sut.GetStyle().HaloWidth;

        sut.FontScale = 2;
        float doubleScale = sut.GetStyle().HaloWidth;

        halfScale.ShouldBe(8f);
        doubleScale.ShouldBe(16f);
    }

    [Fact]
    public void GetStyle_KeepsHaloTheSame_WhenOnlyFontSizeChanges()
    {
        Text sut = new();
        sut.OutlineThickness = 4;
        sut.FontScale = 2;
        sut.FontSize = 20;
        float atSmallSize = sut.GetStyle().HaloWidth;

        sut.FontSize = 60;
        float atLargeSize = sut.GetStyle().HaloWidth;

        atLargeSize.ShouldBe(atSmallSize);
    }

    [Fact]
    public void GetStyle_ScalesHaloWithGlobalTextScale()
    {
        decimal previous = Text.GlobalTextScale;
        try
        {
            Text sut = new();
            sut.OutlineThickness = 4;
            Text.GlobalTextScale = 2m;

            sut.GetStyle().HaloWidth.ShouldBe(16f);
        }
        finally
        {
            Text.GlobalTextScale = previous;
        }
    }

    [Fact]
    public void GetStyledRuns_ScalesHaloOfAFontScaleRunByTheRunsOwnScale()
    {
        Text sut = new();
        sut.OutlineThickness = 3;
        sut.FontScale = 1;
        sut.RawText = "plain [FontScale=3]big[/FontScale]";

        List<Text.StyledTextRun> runs = sut.GetStyledRuns();

        runs.Single(r => r.Text == "big").Style.HaloWidth.ShouldBe(18f);
        runs.First(r => r.Text.StartsWith("plain")).Style.HaloWidth.ShouldBe(6f);
    }

    [Fact]
    public void GetBlendLayerBounds_PadsByTheHaloOfTheLargestFontScaleRun()
    {
        Text sut = new();
        sut.OutlineThickness = 2;
        sut.FontScale = 1;
        sut.RawText = "plain [FontScale=3]big[/FontScale]";
        Topten.RichTextKit.TextBlock block = sut.GetTextBlock();

        SKRect bounds = sut.GetBlendLayerBounds(block);

        // The [FontScale=3] run's outline reaches 2 * 3 pixels past the block, plus the 2 pixel margin.
        (bounds.Left).ShouldBeLessThanOrEqualTo(-8f);
        (bounds.Right - block.MeasuredWidth).ShouldBeGreaterThanOrEqualTo(8f);
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
