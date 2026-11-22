using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using GumRuntime;
using Shouldly;
using WpfDataUi.DataTypes;
using Xunit;

namespace Gum.Presentation.Tests.VariableGrid;

// NineSlice CustomFrameTextureCoordinateWidth + the four per-side source cuts (#8354) collapse into one
// linked/unlinked composite row, the same way BorderScale and its per-side overrides do. Unlike
// BorderScale, the shared value is nullable: blank cuts a third of the texture.
public class FrameTextureCoordinateCompositeTests : BaseTestClass
{
    private static readonly string[] ChannelNames =
    {
        "CustomFrameTextureCoordinateWidth", "CustomFrameTextureCoordinateLeft", "CustomFrameTextureCoordinateTop",
        "CustomFrameTextureCoordinateRight", "CustomFrameTextureCoordinateBottom"
    };

    private static CompositeMemberDescriptor Descriptor() =>
        new CompositeMemberRegistry().Descriptors.Single(d => d.ChannelRootNames.SequenceEqual(ChannelNames));

    private static Dictionary<string, InstanceMember> MembersFor(params string[] rootNames) =>
        rootNames.ToDictionary(name => name, name => new InstanceMember(name, null!));

    private static CompositeMemberLogic CreateLogic() =>
        new CompositeMemberLogic(null!, null!, null!, null!, ObjectFinder.Self, new CompositeMemberRegistry(), null!, null!, null!);

    [Fact]
    public void Descriptor_ShouldUseFrameTextureCoordinateDisplayAndTheNineSliceSidesComposite()
    {
        CompositeMemberDescriptor descriptor = Descriptor();

        descriptor.Displayer.ShouldBe(typeof(GumDisplayers.FrameTextureCoordinate));
        descriptor.CompositeType.ShouldBe(typeof(NineSliceSidesComposite));
        descriptor.CompositeNameFormat.ShouldBe("{prefix}CustomFrameTextureCoordinateWidth{suffix}");
    }

    [Fact]
    public void Descriptor_ComposeAndDecompose_KeepANullSharedValue()
    {
        NineSliceSidesComposite composite = (NineSliceSidesComposite)Descriptor().Compose(
            new object?[] { null, 4f, null, 0f, null });

        composite.ShouldBe(new NineSliceSidesComposite(null, 4f, null, 0f, null));
        composite.IsLinked.ShouldBeFalse();
        Descriptor().Decompose(new NineSliceSidesComposite(null, -2f, null, 6f, null))
            .ShouldBe(new object?[] { null, 0f, null, 6f, null });
        Descriptor().Decompose(new NineSliceSidesComposite(-1f, null, null, null, null))
            .ShouldBe(new object?[] { 0f, null, null, null, null });
    }

    [Fact]
    public void GroupTriples_ShouldGroupAllFiveChannels_AndNotWhenASideIsMissing()
    {
        CreateLogic().GroupTriples(Descriptor(), MembersFor(ChannelNames)).Count.ShouldBe(1);

        // A project below the version gate never gets the sides, so the width stays a plain row.
        CreateLogic().GroupTriples(Descriptor(), MembersFor(ChannelNames.Take(4).ToArray())).ShouldBeEmpty();
    }

    [Fact]
    public void DisplayLogic_BlankSharedValue_IsNull_AndUnparseableKeepsCurrent()
    {
        NineSliceSidesDisplayLogic logic = new NineSliceSidesDisplayLogic(isUniformNullable: true);
        NineSliceSidesComposite current = new NineSliceSidesComposite(5, null, null, null, null);

        logic.Compose(isLinked: true, "", new string?[] { "1", "2", "3", "4" }, current)
            .ShouldBe(new NineSliceSidesComposite(null, null, null, null, null));
        logic.Compose(isLinked: true, "x", new string?[] { "", "", "", "" }, current).Uniform.ShouldBe(5f);
        logic.Compose(isLinked: false, "", new string?[] { "6", "", "0", "-2" }, current)
            .ShouldBe(new NineSliceSidesComposite(null, 6, null, 0, 0));
    }

    [Fact]
    public void BorderScaleDisplayLogic_BlankSharedValue_KeepsCurrent()
    {
        // BorderScale has no "unset": a blank field keeps the scale it had.
        NineSliceSidesComposite current = new NineSliceSidesComposite(2, null, null, null, null);

        new NineSliceSidesDisplayLogic(isUniformNullable: false).Compose(isLinked: true, "", new string?[] { "", "", "", "" }, current)
            .Uniform.ShouldBe(2f);
    }

    [Fact]
    public void DefaultStates_NineSlice_ShouldIncludeTheSideCutsGatedAtV9()
    {
        StandardElementsManager.Self.RefreshDefaults();

        List<VariableSave> variables = StandardElementsManager.Self.DefaultStates["NineSlice"].Variables;

        foreach (string side in new[] { "Left", "Top", "Right", "Bottom" })
        {
            variables.ShouldContain(v => v.Name == "CustomFrameTextureCoordinate" + side
                && v.Type == "float?" && v.Value == null && v.Category == "Source"
                && v.MinimumGumxVersion == (int)GumProjectSave.GumxVersions.BbCodeTextEffects);
        }
    }

    [Theory]
    [InlineData("CustomFrameTextureCoordinateLeft")]
    [InlineData("CustomFrameTextureCoordinateBottom")]
    public void VersionGate_HidesSideCuts_OnProjectsOlderThanV9(string variableName)
    {
        StandardElementsManager.Self.RefreshDefaults();
        ShapeVariableVersionGate gate = new ShapeVariableVersionGate(StandardElementsManager.Self);
        int v9 = (int)GumProjectSave.GumxVersions.BbCodeTextEffects;

        gate.GetIfHiddenForProjectVersion(variableName, "NineSlice", v9 - 1).ShouldBeTrue();
        gate.GetIfHiddenForProjectVersion(variableName, "NineSlice", v9).ShouldBeFalse();
        gate.GetIfHiddenForProjectVersion("CustomFrameTextureCoordinateWidth", "NineSlice", 1).ShouldBeFalse();
    }

    [Fact]
    public void ExpandCompositeReferenceLine_SharedValue_ExpandsToEverySide()
    {
        StandardElementSave owner = new StandardElementSave { Name = "NineSlice" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = owner };
        foreach (string name in ChannelNames)
        {
            state.Variables.Add(new VariableSave { Name = name, Type = "float?", SetsValue = true });
        }
        owner.States.Add(state);

        List<string> lines = ElementSaveExtensions.ExpandCompositeReferenceLine(
            "CustomFrameTextureCoordinateWidth = Other.CustomFrameTextureCoordinateWidth", owner).ToList();

        lines.ShouldBe(ChannelNames.Select(name => $"{name} = Other.{name}").ToList());
    }
}
