using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class BmfcSaveTests
{
    [Fact]
    public void ParseCharRanges_ShouldExpandRange()
    {
        var result = BmfcSave.ParseCharRanges("66-70");
        result.ShouldBe([66, 67, 68, 69, 70]);
    }

    [Fact]
    public void ParseCharRanges_ShouldHandleSingleValue()
    {
        var result = BmfcSave.ParseCharRanges("65");
        result.ShouldBe([65]);
    }

    [Fact]
    public void ParseCharRanges_ShouldHandleMultipleRanges()
    {
        var result = BmfcSave.ParseCharRanges("32-34,40-42");
        result.ShouldBe([32, 33, 34, 40, 41, 42]);
    }

    [Fact]
    public void ParseCharRanges_ShouldHandleMixedRangesAndSingleValues()
    {
        var result = BmfcSave.ParseCharRanges("65,67-69");
        result.ShouldBe([65, 67, 68, 69]);
    }
}
