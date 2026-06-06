using System.Collections.Generic;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.VariableGrid;

public class CompositeMemberDescriptorTests
{
    private readonly CompositeMemberDescriptor _colorDescriptor = new CompositeMemberRegistry().Descriptors[0];

    [Fact]
    public void TryGetChannelNames_ShouldMapAffixedColor_WithPrefix()
    {
        bool matched = _colorDescriptor.TryGetChannelNames("StrokeColor", out IReadOnlyList<string> channelNames);

        matched.ShouldBeTrue();
        channelNames.ShouldBe(new[] { "StrokeRed", "StrokeGreen", "StrokeBlue" });
    }

    [Fact]
    public void TryGetChannelNames_ShouldMapAffixedColor_WithSuffix()
    {
        bool matched = _colorDescriptor.TryGetChannelNames("Color2", out IReadOnlyList<string> channelNames);

        matched.ShouldBeTrue();
        channelNames.ShouldBe(new[] { "Red2", "Green2", "Blue2" });
    }

    [Fact]
    public void TryGetChannelNames_ShouldMapFillColor()
    {
        bool matched = _colorDescriptor.TryGetChannelNames("FillColor", out IReadOnlyList<string> channelNames);

        matched.ShouldBeTrue();
        channelNames.ShouldBe(new[] { "FillRed", "FillGreen", "FillBlue" });
    }

    [Fact]
    public void TryGetChannelNames_ShouldMapPlainColor()
    {
        bool matched = _colorDescriptor.TryGetChannelNames("Color", out IReadOnlyList<string> channelNames);

        matched.ShouldBeTrue();
        channelNames.ShouldBe(new[] { "Red", "Green", "Blue" });
    }

    [Fact]
    public void TryGetChannelNames_ShouldNotMatch_NonColorName()
    {
        bool matched = _colorDescriptor.TryGetChannelNames("Width", out IReadOnlyList<string> channelNames);

        matched.ShouldBeFalse();
        channelNames.ShouldBeEmpty();
    }
}
