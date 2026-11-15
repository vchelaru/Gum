using CrossRuntimeTests;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.CrossRuntime;

/// <summary>
/// A MayMatch cell accepts a platform-dependent difference, but only one as small as what was measured
/// (frame2-6x6/9x1 on macOS arm64: 1 pixel, max channel difference 53). Anything larger is a regression.
/// </summary>
public class NineSliceMayMatchCapTests
{
    private const string Sheet = "frame2-6x6";

    private static string[] Unexpected(string name, int pixels, int channel) =>
        NineSliceCrossRuntimeTests.FindUnexpected(
            Sheet, new[] { new CellDifference(0, name, pixels, channel) }, NineSliceKnownDifferences.All);

    [Fact]
    public void MayMatchCell_DifferenceLargerThanChannelCap_IsUnexpected()
    {
        string[] unexpected = Unexpected("9x1", pixels: 1, channel: 64);

        unexpected.Length.ShouldBe(1);
        unexpected[0].ShouldContain("53");
    }

    [Fact]
    public void MayMatchCell_MoreDifferingPixelsThanCap_IsUnexpected()
    {
        string[] unexpected = Unexpected("9x1", pixels: 2, channel: 53);

        unexpected.Length.ShouldBe(1);
        unexpected[0].ShouldContain("cap");
    }

    [Fact]
    public void MayMatchCell_DifferenceWithinCap_IsAccepted()
    {
        Unexpected("9x1", pixels: 1, channel: 53).ShouldBeEmpty();
        Unexpected("9x9", pixels: 17, channel: 121).ShouldBeEmpty();
    }

    [Fact]
    public void MayMatchCell_WithNoDifference_IsNotReported()
    {
        NineSliceCrossRuntimeTests.FindUnexpected(Sheet, Array.Empty<CellDifference>(), NineSliceKnownDifferences.All)
            .ShouldBeEmpty();
    }

    [Fact]
    public void CellRemovedFromTheList_ThatDiffers_IsUnexpected()
    {
        Dictionary<string, KnownDifference> withoutCell = NineSliceKnownDifferences.All
            .Where(pair => pair.Key != $"{Sheet}/9x1")
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        NineSliceCrossRuntimeTests.FindUnexpected(
            Sheet, new[] { new CellDifference(0, "9x1", 1, 53) }, withoutCell).Length.ShouldBe(1);
    }

    [Fact]
    public void EveryMayMatchEntry_HasACap()
    {
        NineSliceKnownDifferences.All.Where(pair => pair.Value.MayMatch)
            .Where(pair => pair.Value.MaxDifferingPixels is null || pair.Value.MaxChannelDifference is null)
            .Select(pair => pair.Key)
            .ShouldBeEmpty();
    }
}
