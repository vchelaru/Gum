using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.VariableGrid;

public class CompositeMemberRegistryTests
{
    private readonly CompositeMemberRegistry _registry;

    public CompositeMemberRegistryTests()
    {
        _registry = new CompositeMemberRegistry();
    }

    private CompositeMemberDescriptor ColorDescriptor =>
        _registry.Descriptors.Single(d => d.ChannelRootNames.SequenceEqual(new[] { "Red", "Green", "Blue" }));

    [Fact]
    public void ColorDescriptor_Compose_ShouldBuildOpaqueColorFromChannels()
    {
        Color color = (Color)ColorDescriptor.Compose(new object?[] { 10, 20, 30 });

        color.R.ShouldBe((byte)10);
        color.G.ShouldBe((byte)20);
        color.B.ShouldBe((byte)30);
        color.A.ShouldBe((byte)255);
    }

    [Fact]
    public void ColorDescriptor_Compose_ShouldClampOutOfRangeAndTreatNullAsZero()
    {
        Color color = (Color)ColorDescriptor.Compose(new object?[] { 300, null, -5 });

        color.R.ShouldBe((byte)255);
        color.G.ShouldBe((byte)0);
        color.B.ShouldBe((byte)0);
    }

    [Fact]
    public void ColorDescriptor_Decompose_ShouldReturnOnlyRgbAndIgnoreAlpha()
    {
        object?[] channels = ColorDescriptor.Decompose(Color.FromArgb(50, 1, 2, 3));

        channels.Length.ShouldBe(3);
        channels[0].ShouldBe(1);
        channels[1].ShouldBe(2);
        channels[2].ShouldBe(3);
    }

    [Fact]
    public void ColorDescriptor_ShouldUseColorDisplayAndColorType()
    {
        ColorDescriptor.Displayer.ShouldBe(typeof(Gum.Controls.DataUi.ColorDisplay));
        ColorDescriptor.CompositeType.ShouldBe(typeof(Color));
        ColorDescriptor.CompositeNameFormat.ShouldBe("{prefix}Color{suffix}");
    }
}
