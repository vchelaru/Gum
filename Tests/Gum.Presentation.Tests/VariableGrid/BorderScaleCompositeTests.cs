using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using WpfDataUi.DataTypes;
using Xunit;

namespace Gum.Presentation.Tests.VariableGrid;

// NineSlice BorderScale + the four per-side overrides (#6983) collapse into one linked/unlinked
// composite row, the same way Rectangle's CornerRadius does.
public class BorderScaleCompositeTests : BaseTestClass
{
    private static readonly string[] ChannelNames =
    {
        "BorderScale", "CustomBorderScaleLeft", "CustomBorderScaleTop",
        "CustomBorderScaleRight", "CustomBorderScaleBottom"
    };

    private static CompositeMemberDescriptor BorderScaleDescriptor() =>
        new CompositeMemberRegistry().Descriptors.Single(d => d.ChannelRootNames.SequenceEqual(ChannelNames));

    private static Dictionary<string, InstanceMember> MembersFor(params string[] rootNames) =>
        rootNames.ToDictionary(name => name, name => new InstanceMember(name, null!));

    private static CompositeMemberLogic CreateLogic() =>
        new CompositeMemberLogic(null!, null!, null!, null!, ObjectFinder.Self, new CompositeMemberRegistry(), null!, null!, null!);

    #region Descriptor

    [Fact]
    public void Descriptor_ShouldUseBorderScaleDisplayAndCompositeType()
    {
        CompositeMemberDescriptor descriptor = BorderScaleDescriptor();

        descriptor.Displayer.ShouldBe(typeof(GumDisplayers.BorderScale));
        descriptor.CompositeType.ShouldBe(typeof(BorderScaleComposite));
        descriptor.CompositeNameFormat.ShouldBe("{prefix}BorderScale{suffix}");
    }

    [Fact]
    public void Descriptor_Compose_ShouldBuildCompositeFromChannelsInOrder()
    {
        BorderScaleComposite composite = (BorderScaleComposite)BorderScaleDescriptor().Compose(
            new object?[] { 2f, 1f, null, 3f, null });

        composite.Uniform.ShouldBe(2f);
        composite.Left.ShouldBe(1f);
        composite.Top.ShouldBeNull();
        composite.Right.ShouldBe(3f);
        composite.Bottom.ShouldBeNull();
    }

    [Fact]
    public void Descriptor_Compose_ShouldKeepAnExplicitZeroOverride()
    {
        BorderScaleComposite composite = (BorderScaleComposite)BorderScaleDescriptor().Compose(
            new object?[] { 2f, 0f, null, null, null });

        composite.Left.ShouldBe(0f);
        composite.IsLinked.ShouldBeFalse();
    }

    [Fact]
    public void Descriptor_Decompose_ShouldReturnFiveChannelsInOrder()
    {
        object?[] channels = BorderScaleDescriptor().Decompose(new BorderScaleComposite(2f, 1f, null, 3f, null));

        channels.ShouldBe(new object?[] { 2f, 1f, null, 3f, null });
    }

    [Fact]
    public void Descriptor_Decompose_ShouldClampNegativesToZero()
    {
        object?[] channels = BorderScaleDescriptor().Decompose(new BorderScaleComposite(-2f, -1f, null, 3f, -0.5f));

        channels.ShouldBe(new object?[] { 0f, 0f, null, 3f, 0f });
    }

    [Fact]
    public void Descriptor_TryGetChannelNames_ShouldMapBorderScaleToAllChannels()
    {
        BorderScaleDescriptor().TryGetChannelNames("BorderScale", out IReadOnlyList<string> channels).ShouldBeTrue();

        channels.ShouldBe(ChannelNames);
    }

    [Fact]
    public void Composite_IsLinked_ShouldBeTrueOnlyWhenNoSideHasAnOverride()
    {
        new BorderScaleComposite(1f, null, null, null, null).IsLinked.ShouldBeTrue();
        new BorderScaleComposite(1f, null, null, null, 0f).IsLinked.ShouldBeFalse();
    }

    #endregion

    #region Grouping

    [Fact]
    public void GroupTriples_ShouldProduceSingleGroup_ForAllFiveChannels()
    {
        List<CompositeMemberLogic.CompositeTriple> triples =
            CreateLogic().GroupTriples(BorderScaleDescriptor(), MembersFor(ChannelNames));

        triples.Count.ShouldBe(1);
        triples[0].ChannelRootNames.ShouldBe(ChannelNames.ToList());
    }

    [Fact]
    public void GroupTriples_ShouldDropBorderScale_WhenAnOverrideChannelIsMissing()
    {
        // A project below the version gate never gets the overrides, so BorderScale stays a plain row.
        List<CompositeMemberLogic.CompositeTriple> triples = CreateLogic().GroupTriples(
            BorderScaleDescriptor(),
            MembersFor("BorderScale", "CustomBorderScaleLeft", "CustomBorderScaleTop", "CustomBorderScaleRight"));

        triples.ShouldBeEmpty();
    }

    [Fact]
    public void GroupTriples_ShouldNotGroupCornerRadiusChannels_AsBorderScale()
    {
        List<CompositeMemberLogic.CompositeTriple> triples = CreateLogic().GroupTriples(
            BorderScaleDescriptor(),
            MembersFor("CornerRadius", "CustomRadiusTopLeft", "CustomRadiusTopRight",
                "CustomRadiusBottomLeft", "CustomRadiusBottomRight"));

        triples.ShouldBeEmpty();
    }

    #endregion

    #region Field logic

    [Fact]
    public void DisplayLogic_ComposesLinkedAndUnlinkedValues()
    {
        BorderScaleDisplayLogic logic = new BorderScaleDisplayLogic();
        BorderScaleComposite current = new BorderScaleComposite(4, null, null, null, null);

        logic.Compose(isLinked: true, "6", "1", "2", "3", "4", current).ShouldBe(new BorderScaleComposite(6, null, null, null, null));
        logic.Compose(isLinked: false, "6", "1", "", "3", "x", current).ShouldBe(new BorderScaleComposite(6, 1, null, 3, null));
        logic.Compose(isLinked: true, "bad", "", "", "", "", current).Uniform.ShouldBe(4f);
    }

    [Fact]
    public void DisplayLogic_KeepsAZeroSideAsZero_NotInherited()
    {
        BorderScaleDisplayLogic logic = new BorderScaleDisplayLogic();
        BorderScaleComposite current = new BorderScaleComposite(4, null, null, null, null);

        logic.Compose(isLinked: false, "2", "0", "", "", "", current).Left.ShouldBe(0f);
    }

    [Fact]
    public void DisplayLogic_FloorsTypedNegativesAtZero()
    {
        BorderScaleDisplayLogic logic = new BorderScaleDisplayLogic();
        BorderScaleComposite current = new BorderScaleComposite(4, null, null, null, null);

        logic.Compose(isLinked: false, "-6", "-1", "", "3", "-0.5", current)
            .ShouldBe(new BorderScaleComposite(0, 0, null, 3, 0));
    }

    [Fact]
    public void DisplayLogic_FormatsDecimalsWithoutTrailingZeros()
    {
        BorderScaleDisplayLogic logic = new BorderScaleDisplayLogic();

        logic.FormatFloat(2.5f).ShouldBe("2.5");
        logic.FormatFloat(1f).ShouldBe("1");
        logic.FormatNullableFloat(null).ShouldBe("");
        logic.ParseFloat("0.25").ShouldBe(0.25f);
    }

    #endregion

    #region Variable definitions and version gating

    [Fact]
    public void DefaultStates_NineSlice_ShouldIncludeCustomBorderScaleOverrides()
    {
        StandardElementsManager.Self.RefreshDefaults();

        List<VariableSave> variables = StandardElementsManager.Self.DefaultStates["NineSlice"].Variables;

        foreach (string side in new[] { "Left", "Top", "Right", "Bottom" })
        {
            variables.ShouldContain(v => v.Name == "CustomBorderScale" + side
                && v.Type == "float?" && v.Value == null && v.Category == "Dimensions"
                && v.MinimumGumxVersion == (int)GumProjectSave.GumxVersions.ShaderParametersExpansion);
        }
    }

    [Fact]
    public void DefaultStates_NineSlice_ShouldKeepBorderScaleUngatedWithDefaultOne()
    {
        StandardElementsManager.Self.RefreshDefaults();

        VariableSave borderScale = StandardElementsManager.Self.DefaultStates["NineSlice"].Variables
            .Single(v => v.Name == "BorderScale");

        borderScale.Value.ShouldBe(1f);
        borderScale.MinimumGumxVersion.ShouldBe(0);
    }

    [Fact]
    public void DefaultStates_OtherStandards_ShouldNotIncludeCustomBorderScaleOverrides()
    {
        StandardElementsManager.Self.RefreshDefaults();

        foreach (string name in new[] { "Sprite", "Rectangle", "Container" })
        {
            StandardElementsManager.Self.DefaultStates[name].Variables
                .ShouldNotContain(v => v.Name.StartsWith("CustomBorderScale"));
        }
    }

    [Theory]
    [InlineData("CustomBorderScaleLeft")]
    [InlineData("CustomBorderScaleTop")]
    [InlineData("CustomBorderScaleRight")]
    [InlineData("CustomBorderScaleBottom")]
    public void VersionGate_HidesOverrides_OnProjectsOlderThanV7(string variableName)
    {
        ShapeVariableVersionGate gate = new ShapeVariableVersionGate();
        int v7 = (int)GumProjectSave.GumxVersions.ShaderParametersExpansion;

        gate.GetIfHiddenForProjectVersion(variableName, "NineSlice", v7 - 1).ShouldBeTrue();
        gate.GetIfHiddenForProjectVersion(variableName, "NineSlice", v7).ShouldBeFalse();
    }

    [Fact]
    public void VersionGate_NeverHidesBorderScaleItself()
    {
        ShapeVariableVersionGate gate = new ShapeVariableVersionGate();

        gate.GetIfHiddenForProjectVersion("BorderScale", "NineSlice", 1).ShouldBeFalse();
    }

    #endregion
}
