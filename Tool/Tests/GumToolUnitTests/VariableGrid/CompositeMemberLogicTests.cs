using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using WpfDataUi.DataTypes;
using Xunit;

namespace GumToolUnitTests.VariableGrid;

public class CompositeMemberLogicTests : BaseTestClass
{
    private readonly CompositeMemberLogic _logic;
    private readonly CompositeMemberDescriptor _colorDescriptor;

    public CompositeMemberLogicTests()
    {
        CompositeMemberRegistry registry = new();
        _colorDescriptor = registry.Descriptors[0];
        _logic = new CompositeMemberLogic(null!, null!, null!, null!, ObjectFinder.Self, registry, null!, null!, null!);
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

    /// <summary>
    /// Builds a single-category component whose default state declares the given channel variables, plus a
    /// matching grid member per channel - enough for <see cref="CompositeMemberLogic.Apply"/> to collapse them
    /// into a composite swatch.
    /// </summary>
    private static (MemberCategory category, ComponentSave element) BuildComponentWithChannels(
        params string[] channelRootNames)
    {
        ComponentSave element = new() { Name = "MyComp" };
        StateSave defaultState = new() { Name = "Default", ParentContainer = element };
        element.States.Add(defaultState);

        MemberCategory category = new();
        foreach (string channelRootName in channelRootNames)
        {
            defaultState.Variables.Add(new VariableSave { Name = channelRootName, Value = 0, Type = "int" });
            category.Members.Add(new InstanceMember(channelRootName, defaultState));
        }

        return (category, element);
    }

    private CompositeInstanceMember ApplyAndGetComposite(MemberCategory category, ComponentSave element)
    {
        _logic.Apply(new List<MemberCategory> { category }, element, instance: null);
        return category.Members.OfType<CompositeInstanceMember>().Single();
    }

    [Fact]
    public void Apply_ShouldAddCopyQualifiedVariableNameMenu_ForPlainColor()
    {
        (MemberCategory category, ComponentSave element) = BuildComponentWithChannels("Red", "Green", "Blue");

        CompositeInstanceMember composite = ApplyAndGetComposite(category, element);

        composite.ContextMenuEvents.Keys.ShouldContain("Copy Qualified Variable Name");
    }

    [Fact]
    public void Apply_ShouldAddCopyQualifiedVariableNameMenu_ForAffixedColor()
    {
        // VariableReferenceLogic now expands any composite color reference (StrokeColor -> Stroke channels),
        // so affixed colors offer the copy item too.
        (MemberCategory category, ComponentSave element) =
            BuildComponentWithChannels("StrokeRed", "StrokeGreen", "StrokeBlue");

        CompositeInstanceMember composite = ApplyAndGetComposite(category, element);

        composite.ContextMenuEvents.Keys.ShouldContain("Copy Qualified Variable Name");
    }

    [Fact]
    public void GetCompositeQualifiedName_ShouldIncludeInstanceName_WhenInstanceProvided()
    {
        ComponentSave element = new() { Name = "MyComp" };
        InstanceSave instance = new() { Name = "MyInstance" };

        _logic.GetCompositeQualifiedName(element, instance, "Color")
            .ShouldBe("Components/MyComp.MyInstance.Color");
    }

    [Fact]
    public void GetCompositeQualifiedName_ShouldQualifyWithElementPrefix_WhenNoInstance()
    {
        ComponentSave element = new() { Name = "MyComp" };

        _logic.GetCompositeQualifiedName(element, instance: null, "Color")
            .ShouldBe("Components/MyComp.Color");
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

    private static CompositeMemberDescriptor CornerRadiusDescriptor() =>
        new CompositeMemberRegistry().Descriptors.Single(d => d.ChannelRootNames.SequenceEqual(new[]
        {
            "CornerRadius", "CustomRadiusTopLeft", "CustomRadiusTopRight",
            "CustomRadiusBottomLeft", "CustomRadiusBottomRight"
        }));

    [Fact]
    public void GroupTriples_ShouldProduceSingleTriple_ForAllFiveCornerRadiusChannels()
    {
        Dictionary<string, InstanceMember> members = MembersFor(
            "CornerRadius", "CustomRadiusTopLeft", "CustomRadiusTopRight",
            "CustomRadiusBottomLeft", "CustomRadiusBottomRight");

        List<CompositeMemberLogic.CompositeTriple> triples = _logic.GroupTriples(CornerRadiusDescriptor(), members);

        triples.Count.ShouldBe(1);
        triples[0].Prefix.ShouldBe("");
        triples[0].Suffix.ShouldBe("");
        triples[0].ChannelRootNames.ShouldBe(new List<string>
        {
            "CornerRadius", "CustomRadiusTopLeft", "CustomRadiusTopRight",
            "CustomRadiusBottomLeft", "CustomRadiusBottomRight"
        });
    }

    [Fact]
    public void GroupTriples_ShouldDropCornerRadius_WhenACustomRadiusChannelIsMissing()
    {
        // Mirrors the version-gate landmine: a project below the gate never gets the
        // CustomRadius* variables at all, so the composite must not form - CornerRadius stays a
        // plain individual row.
        Dictionary<string, InstanceMember> members = MembersFor(
            "CornerRadius", "CustomRadiusTopLeft", "CustomRadiusTopRight", "CustomRadiusBottomLeft");

        List<CompositeMemberLogic.CompositeTriple> triples = _logic.GroupTriples(CornerRadiusDescriptor(), members);

        triples.ShouldBeEmpty();
    }
}
