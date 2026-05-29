using System.Collections.Generic;
using System.Linq;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using WpfDataUi.DataTypes;
using Xunit;

namespace GumToolUnitTests.VariableGrid;

public class CompositeMemberLogicTests
{
    private readonly CompositeMemberLogic _logic;
    private readonly CompositeMemberDescriptor _colorDescriptor;

    public CompositeMemberLogicTests()
    {
        CompositeMemberRegistry registry = new();
        _colorDescriptor = registry.Descriptors[0];
        _logic = new CompositeMemberLogic(null!, null!, null!, null!, ObjectFinder.Self, registry);
    }

    private static Dictionary<string, InstanceMember> MembersFor(params string[] rootNames)
    {
        Dictionary<string, InstanceMember> result = new();
        foreach (string rootName in rootNames)
        {
            result[rootName] = new InstanceMember(rootName, null!);
        }
        return result;
    }

    [Fact]
    public void GroupTriples_ShouldDropIncompleteTriple_WhenAChannelIsMissing()
    {
        Dictionary<string, InstanceMember> members = MembersFor("StrokeRed", "StrokeGreen");

        List<CompositeMemberLogic.CompositeTriple> triples = _logic.GroupTriples(_colorDescriptor, members);

        triples.ShouldBeEmpty();
    }

    [Fact]
    public void GroupTriples_ShouldGroupGradientChannelsBySuffix()
    {
        Dictionary<string, InstanceMember> members =
            MembersFor("Red1", "Green1", "Blue1", "Red2", "Green2", "Blue2");

        List<CompositeMemberLogic.CompositeTriple> triples = _logic.GroupTriples(_colorDescriptor, members);

        triples.Count.ShouldBe(2);
        triples.ShouldContain(t => t.Suffix == "1" && t.Prefix == "");
        triples.ShouldContain(t => t.Suffix == "2" && t.Prefix == "");
    }

    [Fact]
    public void GroupTriples_ShouldGroupMultipleAffixedTriplesSeparately()
    {
        Dictionary<string, InstanceMember> members = MembersFor(
            "StrokeRed", "StrokeGreen", "StrokeBlue",
            "FillRed", "FillGreen", "FillBlue",
            "DropshadowRed", "DropshadowGreen", "DropshadowBlue");

        List<CompositeMemberLogic.CompositeTriple> triples = _logic.GroupTriples(_colorDescriptor, members);

        triples.Count.ShouldBe(3);
        triples.ShouldContain(t => t.Prefix == "Stroke");
        triples.ShouldContain(t => t.Prefix == "Fill");
        triples.ShouldContain(t => t.Prefix == "Dropshadow");
    }

    [Fact]
    public void GroupTriples_ShouldKeepCompleteTriple_WhenAnotherIsIncomplete()
    {
        Dictionary<string, InstanceMember> members =
            MembersFor("Red", "Green", "Blue", "FillRed", "FillGreen");

        List<CompositeMemberLogic.CompositeTriple> triples = _logic.GroupTriples(_colorDescriptor, members);

        triples.Count.ShouldBe(1);
        triples[0].Prefix.ShouldBe("");
        triples[0].Suffix.ShouldBe("");
    }

    [Fact]
    public void GroupTriples_ShouldOrderChannelRootNamesAsRedGreenBlue()
    {
        Dictionary<string, InstanceMember> members = MembersFor("StrokeBlue", "StrokeRed", "StrokeGreen");

        List<CompositeMemberLogic.CompositeTriple> triples = _logic.GroupTriples(_colorDescriptor, members);

        triples.Count.ShouldBe(1);
        triples[0].ChannelRootNames.ShouldBe(new List<string> { "StrokeRed", "StrokeGreen", "StrokeBlue" });
    }

    [Fact]
    public void GroupTriples_ShouldProduceSingleTriple_ForPlainColor()
    {
        Dictionary<string, InstanceMember> members = MembersFor("Red", "Green", "Blue");

        List<CompositeMemberLogic.CompositeTriple> triples = _logic.GroupTriples(_colorDescriptor, members);

        triples.Count.ShouldBe(1);
        triples[0].Prefix.ShouldBe("");
        triples[0].Suffix.ShouldBe("");
    }
}
