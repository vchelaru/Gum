#pragma warning disable CS0618
using Gum;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// The result's promise: when the hot tree differs from what a fresh load of the same files shows,
/// <see cref="HotReloadResult.MatchesRestart"/> is false. Each case is one edit the tool can save.
/// </summary>
public class HotReloadMatchesRestartTests : BaseTestClass
{
    private readonly SweepWorld _world = new SweepWorld();
    private readonly List<HotReloadResult> _results = new List<HotReloadResult>();
    private readonly List<string> _printed = new List<string>();

    public HotReloadMatchesRestartTests()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Collect;
    }

    private void Collect(string message)
    {
        if (message.Contains("Hot reload"))
        {
            _printed.Add(message);
        }
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= Collect;
        _world.Dispose();
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    // The tool's save rewrites every file, and the watcher reports each one.
    private HotReloadResult Edit(SweepSpec from, SweepSpec to)
    {
        _world.Boot(from);
        _world.Manager.ReloadFinished += _results.Add;
        _world.SaveAll(to);
        _world.NotifyAndSettle(
            _world.Gumx, _world.ScreenFile, _world.StylesFile, _world.BaseFile, _world.WidgetFile, _world.GadgetFile);
        return _results.Last();
    }

    private static bool Differs(SweepWorld world) =>
        SweepWorld.Describe(world.Screen) != world.DescribeAFreshLoad();

    private void AssertHonest(SweepSpec from, SweepSpec to, bool expectDifference)
    {
        HotReloadResult result = Edit(from, to);

        bool differs = Differs(_world);
        string hot = SweepWorld.Describe(_world.Screen);
        string fresh = _world.DescribeAFreshLoad();
        if (!differs)
        {
            return;
        }
        expectDifference.ShouldBeTrue("the hot tree differs from a fresh load:\nhot:\n" + hot + "fresh:\n" + fresh);
        result.MatchesRestart.ShouldBeFalse("the tree differs from a restart, so the result must say so:\nhot:\n" + hot + "fresh:\n" + fresh);
    }

    [Fact]
    public void ComponentBaseTypeChanged_ToContainer() =>
        AssertHonest(new SweepSpec(), new SweepSpec { WidgetBase = "Container" }, expectDifference: true);

    [Fact]
    public void ComponentBaseTypeChanged_BackToABaseThatSetsX()
    {
        _results.Clear();
        AssertHonest(new SweepSpec { WidgetBase = "Container" }, new SweepSpec(), expectDifference: true);
    }

    [Fact]
    public void BaseComponentGainsAnInstance_WhileADerivedComponentIsShown() =>
        AssertHonest(new SweepSpec(), new SweepSpec { BaseInstance = true }, expectDifference: true);

    [Fact]
    public void BaseComponentLosesAnInstance_WhileADerivedComponentIsShown() =>
        AssertHonest(new SweepSpec { BaseInstance = true }, new SweepSpec(), expectDifference: true);

    [Fact]
    public void BaseComponentVariableEdited_WhileADerivedComponentIsShown() =>
        AssertHonest(new SweepSpec(), new SweepSpec { BaseX = 19 }, expectDifference: false);

    [Fact]
    public void VariableExposed() =>
        AssertHonest(new SweepSpec(), new SweepSpec { ExposedName = "FillRed", ExposedValue = 140 }, expectDifference: true);

    [Fact]
    public void ExposedVariableRenamed() =>
        AssertHonest(
            new SweepSpec { ExposedName = "FillRed", ExposedValue = 140 },
            new SweepSpec { ExposedName = "FillTint", ExposedValue = 140 },
            expectDifference: true);

    [Fact]
    public void ExposedVariableUnexposed() =>
        AssertHonest(new SweepSpec { ExposedName = "FillRed", ExposedValue = 140 }, new SweepSpec(), expectDifference: true);

    [Fact]
    public void ExposedValueEdited() =>
        AssertHonest(
            new SweepSpec { ExposedName = "FillRed", ExposedValue = 140 },
            new SweepSpec { ExposedName = "FillRed", ExposedValue = 90 },
            expectDifference: false);

    [Fact]
    public void ParentVariableAdded() =>
        AssertHonest(new SweepSpec(), new SweepSpec { LabelParent = "Widget1" }, expectDifference: true);

    [Fact]
    public void ParentVariableRemoved() =>
        AssertHonest(new SweepSpec { LabelParent = "Widget1" }, new SweepSpec(), expectDifference: true);

    [Fact]
    public void StateAddedToAShownComponent() =>
        AssertHonest(new SweepSpec { WidgetHasHighlighted = false }, new SweepSpec(), expectDifference: false);

    [Fact]
    public void StateRemovedFromAShownComponent() =>
        AssertHonest(new SweepSpec(), new SweepSpec { WidgetHasHighlighted = false }, expectDifference: false);

    [Fact]
    public void StandardElementDefaultChanged() =>
        AssertHonest(new SweepSpec(), new SweepSpec { RectWidth = 80 }, expectDifference: true);

    [Fact]
    public void ScreenValueEdited_IsTheBaseline() =>
        AssertHonest(new SweepSpec(), new SweepSpec { BoxX = 33, BoxRed = 40 }, expectDifference: false);

    private HotReloadIssue ShouldHaveOneIssue(HotReloadResult result, HotReloadIssueKind kind)
    {
        HotReloadIssue issue = result.Issues.Single(candidate => candidate.Kind == kind);
        issue.MatchesRestart.ShouldBeFalse();
        result.MatchesRestart.ShouldBeFalse();
        return issue;
    }

    private void SaveAndReload(SweepSpec spec)
    {
        _world.SaveAll(spec);
        _world.NotifyAndSettle(
            _world.Gumx, _world.ScreenFile, _world.StylesFile, _world.BaseFile, _world.WidgetFile, _world.GadgetFile);
    }

    [Fact]
    public void ComponentBaseTypeChanged_IsListedOncePerElementWithItsMessage_AndEveryReloadWhileItStaysStale()
    {
        HotReloadResult first = Edit(new SweepSpec(), new SweepSpec { WidgetBase = "Container" });

        HotReloadIssue issue = ShouldHaveOneIssue(first, HotReloadIssueKind.ComponentBaseTypeChanged);
        issue.ElementName.ShouldBe("Widgets/Widget");
        issue.Message.ShouldBe(
            "Hot reload: 'Widgets/Widget' changed its base type; edits to what it inherits do not reach the running UI until you restart.");
        _printed.ShouldBe(new[] { issue.Message });

        SaveAndReload(new SweepSpec { WidgetBase = "Container", BoxX = 9 });

        ShouldHaveOneIssue(_results.Last(), HotReloadIssueKind.ComponentBaseTypeChanged);
        _printed.Count.ShouldBe(1, "printed once, but the second result still lists it");
        _results.Count.ShouldBe(2);

        SaveAndReload(new SweepSpec { BoxX = 9 });

        _results.Last().IsClean.ShouldBeTrue("the base type is back to the one the visuals were built with");
        SweepWorld.Describe(_world.Screen).ShouldBe(_world.DescribeAFreshLoad());
    }

    [Fact]
    public void ExposedVariablesChanged_AreListed()
    {
        HotReloadResult result = Edit(new SweepSpec(), new SweepSpec { ExposedName = "FillRed", ExposedValue = 140 });

        HotReloadIssue issue = ShouldHaveOneIssue(result, HotReloadIssueKind.ExposedVariableChanged);
        issue.ElementName.ShouldBe("Widgets/Widget");
        _printed.ShouldBe(new[] { issue.Message });
    }

    [Fact]
    public void StandardElementDefaultChanged_IsListedOnceHoweverManyInstancesUseIt()
    {
        HotReloadResult result = Edit(new SweepSpec(), new SweepSpec { RectWidth = 80 });

        HotReloadIssue issue = ShouldHaveOneIssue(result, HotReloadIssueKind.StandardElementChanged);
        issue.ElementName.ShouldBe("ColoredRectangle");
        issue.Message.ShouldContain("'ColoredRectangle'");
        _printed.ShouldBe(new[] { issue.Message });
    }

    [Fact]
    public void AnEditThatChangesNoDefinition_IsClean()
    {
        HotReloadResult result = Edit(new SweepSpec(), new SweepSpec { BoxX = 33, WidgetRed = 31, BaseX = 8 });

        result.IsClean.ShouldBeTrue();
    }

    private static SweepSpec Screens(string? screenBase, bool baseBLikeA = false) =>
        new SweepSpec { WithBaseScreens = true, ScreenBase = screenBase, BaseBLikeA = baseBLikeA };

    [Fact]
    public void ScreenBaseSwitchedBetweenScreensWithDifferentContent_IsListed()
    {
        HotReloadResult result = Edit(Screens("BaseA"), Screens("BaseB"));

        ShouldHaveOneIssue(result, HotReloadIssueKind.ComponentBaseTypeChanged).ElementName.ShouldBe("MainScreen");
        SweepWorld.Describe(_world.Screen).ShouldNotBe(_world.DescribeAFreshLoad(), "sanity: the oracle can tell");
    }

    [Fact]
    public void ScreenBaseSwitchedBetweenScreensWithTheSameContent_IsStillListed() =>
        ShouldHaveOneIssue(
            Edit(Screens("BaseA", baseBLikeA: true), Screens("BaseB", baseBLikeA: true)),
            HotReloadIssueKind.ComponentBaseTypeChanged);

    [Fact]
    public void ScreenBaseAddedAndRemoved_AreListed()
    {
        ShouldHaveOneIssue(Edit(Screens(null), Screens("BaseA")), HotReloadIssueKind.ComponentBaseTypeChanged);
        SweepWorld.Describe(_world.Screen).ShouldNotBe(_world.DescribeAFreshLoad(), "sanity: the oracle can tell");
    }

    [Fact]
    public void BaseComponentDeleted_ListsTheDerivedComponentAndPrintsItOnce()
    {
        HotReloadResult first = Edit(new SweepSpec(), new SweepSpec { OmitBaseComponent = true });

        HotReloadIssue issue = ShouldHaveOneIssue(first, HotReloadIssueKind.BaseElementMissing);
        issue.ElementName.ShouldBe("Widgets/Widget");
        issue.Message.ShouldContain("'Widgets/Base'");
        _printed.ShouldBe(new[] { issue.Message });

        SaveAndReload(new SweepSpec { OmitBaseComponent = true, BoxX = 9 });

        ShouldHaveOneIssue(_results.Last(), HotReloadIssueKind.BaseElementMissing);
        _printed.Count.ShouldBe(1);

        SaveAndReload(new SweepSpec { BoxX = 9 });

        _results.Last().IsClean.ShouldBeTrue("the base is back");
        SweepWorld.Describe(_world.Screen).ShouldBe(_world.DescribeAFreshLoad());
    }

    [Fact]
    public void BaseOfABaseDeleted_AtDepthThree_ListsTheLayerThatLostItsBase()
    {
        HotReloadResult result = Edit(
            new SweepSpec { WithMid = true, WidgetBase = "Widgets/Mid" },
            new SweepSpec { WithMid = true, WidgetBase = "Widgets/Mid", OmitBaseComponent = true });

        ShouldHaveOneIssue(result, HotReloadIssueKind.BaseElementMissing).ElementName.ShouldBe("Widgets/Mid");
        SweepWorld.Describe(_world.Screen).ShouldNotBe(_world.DescribeAFreshLoad(), "sanity: the oracle can tell");
    }

    [Fact]
    public void BaseRenamedWithoutUpdatingTheDerivedComponent_IsListedAsMissing() =>
        ShouldHaveOneIssue(
            Edit(new SweepSpec(), new SweepSpec { BaseName = "Widgets/Base2" }),
            HotReloadIssueKind.BaseElementMissing);

    [Fact]
    public void BaseRenamedAndTheDerivedComponentUpdated_IsListedAsABaseTypeChange() =>
        ShouldHaveOneIssue(
            Edit(new SweepSpec(), new SweepSpec { BaseName = "Widgets/Base2", WidgetBase = "Widgets/Base2" }),
            HotReloadIssueKind.ComponentBaseTypeChanged);

    [Fact]
    public void BaseReplacedByAComponentThatDoesNotExistYet_IsListedUntilItIsAdded()
    {
        HotReloadResult first = Edit(new SweepSpec(), new SweepSpec { WidgetBase = "Widgets/NewBase" });

        ShouldHaveOneIssue(first, HotReloadIssueKind.ComponentBaseTypeChanged);

        SaveAndReload(new SweepSpec { WidgetBase = "Widgets/NewBase", WithNewBase = true });

        ShouldHaveOneIssue(_results.Last(), HotReloadIssueKind.ComponentBaseTypeChanged);
        SweepWorld.Describe(_world.Screen).ShouldNotBe(_world.DescribeAFreshLoad(), "sanity: the oracle can tell");
    }

    [Fact]
    public void ComponentWhoseFileIsMissing_ListsOnlyTheMissingFile()
    {
        _world.Boot(Reader());
        _world.Manager.ReloadFinished += _results.Add;
        File.Delete(_world.WidgetFile);
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader() with { BoxX = 50 }, "MainScreen"));

        _world.NotifyAndSettle(_world.ScreenFile);
        _world.NotifyAndSettle(_world.ScreenFile);

        _results.Count.ShouldBe(2);
        _results.ShouldAllBe(result => result.Issues.Count == 1 && result.Issues[0].Kind == HotReloadIssueKind.MissingFile);
        _printed.Count.ShouldBe(2, "the missing file is printed on every reload, nothing else");

        _world.SaveAll(Reader());
        _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile, _world.WidgetFile);

        _results.Last().IsClean.ShouldBeTrue("the file is back, and the base type is the one the visuals were built with");
    }

    private static SweepSpec Reader() => new SweepSpec { BoxReadsStyle = true };
}
