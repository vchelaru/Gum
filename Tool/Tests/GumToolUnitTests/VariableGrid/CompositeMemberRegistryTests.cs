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

    private CompositeMemberDescriptor CornerRadiusDescriptor =>
        _registry.Descriptors.Single(d => d.ChannelRootNames.SequenceEqual(new[]
        {
            "CornerRadius", "CustomRadiusTopLeft", "CustomRadiusTopRight",
            "CustomRadiusBottomLeft", "CustomRadiusBottomRight"
        }));

    [Fact]
    public void CornerRadiusDescriptor_Compose_ShouldBuildCompositeFromChannelsInOrder()
    {
        CornerRadiusComposite composite = (CornerRadiusComposite)CornerRadiusDescriptor.Compose(
            new object?[] { 8f, 1f, null, 3f, null });

        composite.Uniform.ShouldBe(8f);
        composite.TopLeft.ShouldBe(1f);
        composite.TopRight.ShouldBeNull();
        composite.BottomLeft.ShouldBe(3f);
        composite.BottomRight.ShouldBeNull();
    }

    [Fact]
    public void CornerRadiusDescriptor_Decompose_ShouldReturnFiveChannelsInOrder()
    {
        CornerRadiusComposite composite = new(8f, 1f, null, 3f, null);

        object?[] channels = CornerRadiusDescriptor.Decompose(composite);

        channels.ShouldBe(new object?[] { 8f, 1f, null, 3f, null });
    }

    [Fact]
    public void CornerRadiusDescriptor_Decompose_ShouldClampNegativeUniformAndOverridesToZero()
    {
        CornerRadiusComposite composite = new(-8f, -1f, null, 3f, -0.5f);

        object?[] channels = CornerRadiusDescriptor.Decompose(composite);

        channels.ShouldBe(new object?[] { 0f, 0f, null, 3f, 0f });
    }

    [Fact]
    public void CornerRadiusDescriptor_ShouldUseCornerRadiusDisplayAndCompositeType()
    {
        CornerRadiusDescriptor.Displayer.ShouldBe(typeof(Gum.Controls.DataUi.CornerRadiusDisplay));
        CornerRadiusDescriptor.CompositeType.ShouldBe(typeof(CornerRadiusComposite));
        CornerRadiusDescriptor.CompositeNameFormat.ShouldBe("{prefix}CornerRadius{suffix}");
    }

    [Fact]
    public void CornerRadiusComposite_IsLinked_ShouldBeTrue_WhenAllOverridesAreNull()
    {
        CornerRadiusComposite composite = new(8f, null, null, null, null);

        composite.IsLinked.ShouldBeTrue();
    }

    [Fact]
    public void CornerRadiusComposite_IsLinked_ShouldBeFalse_WhenAnyOverrideIsSet()
    {
        CornerRadiusComposite composite = new(8f, null, 4f, null, null);

        composite.IsLinked.ShouldBeFalse();
    }
}
