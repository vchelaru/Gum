using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #7988: components that contain each other through instances build until the loop when a game starts
/// (#7468). A reload stops at the same place, reports the loop and leaves the tree a restart would show.
/// Before the fix the reload built one more level per level it walked, until the stack overflowed; every
/// component here counts its builds and throws past <see cref="MaxBuilds"/>, so a regression fails the
/// test instead of killing the test host.
/// </summary>
public class HotReloadInstanceLoopTests : BaseTestClass
{
    private const int MaxBuilds = 40;

    private readonly List<string> _errors = new List<string>();
    private readonly Action<string>? _previousReporter = ElementSaveExtensions.ReportPropertyAssignmentError;
    private int _builds;

    public HotReloadInstanceLoopTests()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _errors.Add;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.ReportPropertyAssignmentError = _previousReporter;
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static StateSave NewDefaultState(ElementSave element)
    {
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        return state;
    }

    private ComponentSave AddComponent(GumProjectSave project, string name, string baseType = "Container")
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        NewDefaultState(component);
        project.Components.Add(component);
        ElementSaveExtensions.RegisterGueInstantiation(name, () =>
        {
            _builds++;
            if (_builds > MaxBuilds)
            {
                throw new InvalidOperationException($"More than {MaxBuilds} components were built: the walk did not stop at the loop.");
            }
            return new GraphicalUiElement();
        });
        return component;
    }

    private static ScreenSave AddScreen(GumProjectSave project, string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        NewDefaultState(screen);
        project.Screens.Add(screen);
        return screen;
    }

    private static InstanceSave AddInstance(ElementSave owner, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = owner };
        owner.Instances.Add(instance);
        return instance;
    }

    private static GumProjectSave NewProject()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        NewDefaultState(container);
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    private static string Describe(GraphicalUiElement element, int depth = 0)
    {
        StringBuilder text = new StringBuilder();
        text.Append(' ', depth * 2).Append(element.Name).Append(" <").Append(element.ElementSave?.Name).AppendLine(">");
        foreach (GraphicalUiElement child in element.ContainedElements.Concat(element.Children).Distinct()
                     .Where(child => child.Tag is InstanceSave))
        {
            text.Append(Describe(child, depth + 1));
        }
        return text.ToString();
    }

    private string DescribeAFreshLoad(ElementSave element)
    {
        _builds = 0;
        return Describe(element.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false));
    }

    private GraphicalUiElement Build(ElementSave element)
    {
        GraphicalUiElement built = element.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        _errors.Clear();
        _builds = 0;
        return built;
    }

    private static HotReloadResult Reload(GraphicalUiElement root, GumProjectSave project) =>
        GumHotReloadManager.ApplyDiff(new[] { root }, project, SystemManagers.Default);

    // The reported project: LoopA holds LoopB, LoopB holds LoopA, the screen holds LoopA.
    private (GumProjectSave project, ScreenSave screen, ComponentSave loopA, ComponentSave loopB) ReportedProject()
    {
        GumProjectSave project = NewProject();
        ComponentSave loopA = AddComponent(project, "LoopA");
        ComponentSave loopB = AddComponent(project, "LoopB");
        AddInstance(loopA, "Inner", "LoopB");
        AddInstance(loopB, "Back", "LoopA");
        ScreenSave screen = AddScreen(project, "Looped");
        AddInstance(screen, "First", "LoopA");
        return (project, screen, loopA, loopB);
    }

    [Fact]
    public void TwoComponentLoop_ReloadWithNoChange_StopsAtTheLoopLikeARestart()
    {
        (GumProjectSave project, ScreenSave screen, _, _) = ReportedProject();
        GraphicalUiElement screenGue = Build(screen);

        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        Describe(screenGue).ShouldBe("Looped <Looped>\n  First <LoopA>\n    Inner <LoopB>\n".Replace("\n", Environment.NewLine));
        HotReloadIssue issue = result.Issues.ShouldHaveSingleItem();
        issue.Kind.ShouldBe(HotReloadIssueKind.InstanceLoop);
        issue.ElementName.ShouldBe("LoopB");
        issue.InstanceName.ShouldBe("Back");
        issue.MatchesRestart.ShouldBeTrue();
        result.MatchesRestart.ShouldBeTrue();
    }

    [Fact]
    public void TwoComponentLoop_ReloadWithNoChange_PrintsTheLoopOnceWithTheStartupText()
    {
        (GumProjectSave project, ScreenSave screen, _, _) = ReportedProject();
        GraphicalUiElement screenGue = Build(screen);

        HotReloadResult result = Reload(screenGue, project);

        string printed = _errors.ShouldHaveSingleItem();
        printed.ShouldContain("Circular instance");
        printed.ShouldContain("LoopB.Back");
        result.Issues.ShouldHaveSingleItem().Message.ShouldBe(printed);
    }

    [Fact]
    public void TwoComponentLoop_ReloadBuildsNoComponent()
    {
        (GumProjectSave project, ScreenSave screen, _, _) = ReportedProject();
        GraphicalUiElement screenGue = Build(screen);

        Reload(screenGue, project);

        _builds.ShouldBe(0);
    }

    [Fact]
    public void TwoComponentLoop_ManyReloads_TreeStaysTheSameAndEachResultReportsTheLoop()
    {
        (GumProjectSave project, ScreenSave screen, _, _) = ReportedProject();
        GraphicalUiElement screenGue = Build(screen);
        string before = Describe(screenGue);

        List<HotReloadResult> results = new List<HotReloadResult>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(Reload(screenGue, project));
        }

        Describe(screenGue).ShouldBe(before);
        results.ShouldAllBe(result => result.Issues.Count == 1 && result.Issues[0].Kind == HotReloadIssueKind.InstanceLoop);
    }

    [Fact]
    public void ComponentContainingItself_ReloadWithNoChange_StopsAtTheLoop()
    {
        GumProjectSave project = NewProject();
        ComponentSave self = AddComponent(project, "Self");
        AddInstance(self, "Again", "Self");
        ScreenSave screen = AddScreen(project, "Main");
        AddInstance(screen, "Outer", "Self");
        GraphicalUiElement screenGue = Build(screen);

        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        HotReloadIssue issue = result.Issues.ShouldHaveSingleItem();
        issue.Kind.ShouldBe(HotReloadIssueKind.InstanceLoop);
        issue.ElementName.ShouldBe("Self");
        issue.InstanceName.ShouldBe("Again");
    }

    [Fact]
    public void ThreeComponentLoop_ReloadWithNoChange_StopsAtTheLoop()
    {
        GumProjectSave project = NewProject();
        ComponentSave a = AddComponent(project, "A");
        ComponentSave b = AddComponent(project, "B");
        ComponentSave c = AddComponent(project, "C");
        AddInstance(a, "ToB", "B");
        AddInstance(b, "ToC", "C");
        AddInstance(c, "ToA", "A");
        ScreenSave screen = AddScreen(project, "Main");
        AddInstance(screen, "First", "A");
        GraphicalUiElement screenGue = Build(screen);

        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        result.Issues.ShouldHaveSingleItem().InstanceName.ShouldBe("ToA");
    }

    [Fact]
    public void LoopBelowTheScreensFirstComponent_ReloadWithNoChange_StopsAtTheLoop()
    {
        GumProjectSave project = NewProject();
        ComponentSave top = AddComponent(project, "Top");
        ComponentSave a = AddComponent(project, "A");
        ComponentSave b = AddComponent(project, "B");
        AddInstance(top, "Holder", "A");
        AddInstance(a, "ToB", "B");
        AddInstance(b, "ToA", "A");
        ScreenSave screen = AddScreen(project, "Main");
        AddInstance(screen, "TopInstance", "Top");
        GraphicalUiElement screenGue = Build(screen);

        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        result.Issues.ShouldHaveSingleItem().Kind.ShouldBe(HotReloadIssueKind.InstanceLoop);
    }

    [Fact]
    public void TwoInstancesOfTheLoopOnOneScreen_LoopIsReportedOncePerReload()
    {
        (GumProjectSave project, ScreenSave screen, _, _) = ReportedProject();
        AddInstance(screen, "Second", "LoopA");
        GraphicalUiElement screenGue = Build(screen);

        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        result.Issues.ShouldHaveSingleItem().Kind.ShouldBe(HotReloadIssueKind.InstanceLoop);
    }

    [Fact]
    public void ShownRootIsTheLoopComponentItself_ReloadWithNoChange_StopsAtTheLoop()
    {
        (GumProjectSave project, _, ComponentSave loopA, _) = ReportedProject();
        GraphicalUiElement root = Build(loopA);

        HotReloadResult result = Reload(root, project);

        Describe(root).ShouldBe(DescribeAFreshLoad(loopA));
        result.Issues.ShouldHaveSingleItem().Kind.ShouldBe(HotReloadIssueKind.InstanceLoop);
    }

    [Fact]
    public void LoopIntroducedByTheReload_StopsAtTheLoopAndReportsIt()
    {
        GumProjectSave project = NewProject();
        ComponentSave loopA = AddComponent(project, "LoopA");
        ComponentSave loopB = AddComponent(project, "LoopB");
        AddInstance(loopA, "Inner", "LoopB");
        ScreenSave screen = AddScreen(project, "Looped");
        AddInstance(screen, "First", "LoopA");
        GraphicalUiElement screenGue = Build(screen);

        AddInstance(loopB, "Back", "LoopA");
        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        HotReloadIssue issue = result.Issues.ShouldHaveSingleItem();
        issue.Kind.ShouldBe(HotReloadIssueKind.InstanceLoop);
        issue.InstanceName.ShouldBe("Back");
    }

    [Fact]
    public void LoopIntroducedByAddingTheOuterInstance_StopsAtTheLoopInTheNewInstance()
    {
        GumProjectSave project = NewProject();
        ComponentSave loopA = AddComponent(project, "LoopA");
        ComponentSave loopB = AddComponent(project, "LoopB");
        AddInstance(loopB, "Back", "LoopA");
        ScreenSave screen = AddScreen(project, "Looped");
        AddInstance(screen, "First", "LoopA");
        GraphicalUiElement screenGue = Build(screen);

        AddInstance(loopA, "Inner", "LoopB");
        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        result.Issues.ShouldHaveSingleItem().Kind.ShouldBe(HotReloadIssueKind.InstanceLoop);
    }

    [Fact]
    public void LoopIntroducedByARetype_RemovesTheOldInstanceAndStopsAtTheLoop()
    {
        GumProjectSave project = NewProject();
        AddComponent(project, "Leaf");
        ComponentSave loopA = AddComponent(project, "LoopA");
        ComponentSave loopB = AddComponent(project, "LoopB");
        AddInstance(loopA, "Inner", "LoopB");
        InstanceSave back = AddInstance(loopB, "Back", "Leaf");
        ScreenSave screen = AddScreen(project, "Looped");
        AddInstance(screen, "First", "LoopA");
        GraphicalUiElement screenGue = Build(screen);

        back.BaseType = "LoopA";
        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        screenGue.GetGraphicalUiElementByName("First")!.GetGraphicalUiElementByName("Inner")!
            .GetGraphicalUiElementByName("Back").ShouldBeNull();
        result.Issues.ShouldHaveSingleItem().Kind.ShouldBe(HotReloadIssueKind.InstanceLoop);
    }

    [Fact]
    public void LoopRemovedByTheReload_BuildsTheRestAndReportsNothing()
    {
        (GumProjectSave project, ScreenSave screen, _, ComponentSave loopB) = ReportedProject();
        GraphicalUiElement screenGue = Build(screen);

        loopB.Instances.Clear();
        AddComponent(project, "Leaf");
        AddInstance(loopB, "Tail", "Leaf");
        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        screenGue.GetGraphicalUiElementByName("First")!.GetGraphicalUiElementByName("Inner")!
            .GetGraphicalUiElementByName("Tail").ShouldNotBeNull();
        result.IsClean.ShouldBeTrue();
    }

    [Fact]
    public void LoopBrokenByARetype_BuildsTheRetypedInstance()
    {
        (GumProjectSave project, ScreenSave screen, _, ComponentSave loopB) = ReportedProject();
        GraphicalUiElement screenGue = Build(screen);

        AddComponent(project, "Leaf");
        loopB.Instances.Single().BaseType = "Leaf";
        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        screenGue.GetGraphicalUiElementByName("First")!.GetGraphicalUiElementByName("Inner")!
            .GetGraphicalUiElementByName("Back")!.ElementSave!.Name.ShouldBe("Leaf");
        result.IsClean.ShouldBeTrue();
    }

    [Fact]
    public void LoopInAComponentTheScreenDoesNotShow_ReloadIsClean()
    {
        (GumProjectSave project, _, _, _) = ReportedProject();
        AddComponent(project, "Leaf");
        ScreenSave other = AddScreen(project, "Other");
        AddInstance(other, "Thing", "Leaf");
        GraphicalUiElement screenGue = Build(other);

        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(other));
        result.IsClean.ShouldBeTrue();
        _errors.ShouldBeEmpty();
    }

    [Fact]
    public void ComponentHoldingAnInstanceOfItsDerivedComponent_ReloadKeepsTheTreeARestartBuilds()
    {
        // The reload diffs only the instances an element declares itself, so the inherited loop
        // instance is never met and nothing is reported; the tree still matches a restart.
        GumProjectSave project = NewProject();
        ComponentSave baseComponent = AddComponent(project, "BaseCard");
        AddComponent(project, "DerivedCard", baseType: "BaseCard");
        AddInstance(baseComponent, "Nested", "DerivedCard");
        ScreenSave screen = AddScreen(project, "Main");
        AddInstance(screen, "Card", "DerivedCard");
        GraphicalUiElement screenGue = Build(screen);

        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        result.Issues.ShouldAllBe(issue => issue.Kind == HotReloadIssueKind.InstanceLoop);
    }

    [Fact]
    public void BaseTypeLoopBesideAnInstanceLoop_ReloadFinishesAndKeepsTheTree()
    {
        (GumProjectSave project, ScreenSave screen, _, _) = ReportedProject();
        AddComponent(project, "CycleOne", baseType: "CycleTwo");
        AddComponent(project, "CycleTwo", baseType: "CycleOne");
        AddInstance(screen, "Cycled", "CycleOne");
        GraphicalUiElement screenGue = Build(screen);
        string before = Describe(screenGue);

        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(before);
        result.Issues.ShouldContain(issue => issue.Kind == HotReloadIssueKind.InstanceLoop);
    }

    [Fact]
    public void SameComponentSideBySide_ReloadAddingAnotherIsNotALoop()
    {
        GumProjectSave project = NewProject();
        AddComponent(project, "Leaf");
        ComponentSave holder = AddComponent(project, "Holder");
        AddInstance(holder, "A", "Leaf");
        ScreenSave screen = AddScreen(project, "Main");
        AddInstance(screen, "H", "Holder");
        GraphicalUiElement screenGue = Build(screen);

        AddInstance(holder, "B", "Leaf");
        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        screenGue.GetGraphicalUiElementByName("H")!.GetGraphicalUiElementByName("B").ShouldNotBeNull();
        result.IsClean.ShouldBeTrue();
    }

    [Fact]
    public void ComponentNestedInsideAnotherInstanceOfItsHolder_ReloadAddingIsNotALoop()
    {
        // Holder > Middle > Leaf, and the reload adds a Leaf beside Middle: Leaf is already built further
        // down, but not above the new instance, so it is not a loop.
        GumProjectSave project = NewProject();
        AddComponent(project, "Leaf");
        ComponentSave middle = AddComponent(project, "Middle");
        ComponentSave holder = AddComponent(project, "Holder");
        AddInstance(middle, "L", "Leaf");
        AddInstance(holder, "M", "Middle");
        ScreenSave screen = AddScreen(project, "Main");
        AddInstance(screen, "H", "Holder");
        GraphicalUiElement screenGue = Build(screen);

        AddInstance(holder, "L2", "Leaf");
        AddInstance(middle, "M2Leaf", "Leaf");
        HotReloadResult result = Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(screen));
        result.IsClean.ShouldBeTrue();
    }

    [Fact]
    public void AfterAReloadThatHitALoop_BuildingTheLoopComponentInCodeStillBuildsUntilTheLoop()
    {
        (GumProjectSave project, ScreenSave screen, ComponentSave loopA, _) = ReportedProject();
        GraphicalUiElement screenGue = Build(screen);
        Reload(screenGue, project);
        _errors.Clear();

        GraphicalUiElement built = loopA.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        Describe(built).ShouldBe("LoopA <LoopA>\n  Inner <LoopB>\n".Replace("\n", Environment.NewLine));
        _errors.ShouldHaveSingleItem().ShouldContain("Circular instance");
    }

    [Fact]
    public void AfterAReloadThatHitALoop_BuildingAComponentInCodeReportsThroughTheReporterAgain()
    {
        (GumProjectSave project, ScreenSave screen, _, _) = ReportedProject();
        GraphicalUiElement screenGue = Build(screen);
        Reload(screenGue, project);
        _errors.Clear();

        ComponentSave self = AddComponent(project, "Self");
        AddInstance(self, "Again", "Self");
        self.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        _errors.ShouldHaveSingleItem().ShouldContain("Self.Again");
    }

    [Fact]
    public void AfterAReload_TheLoopComponentsCanStillBeBuiltSideBySide()
    {
        (GumProjectSave project, _, _, _) = ReportedProject();
        ScreenSave other = AddScreen(project, "Other");
        AddInstance(other, "One", "LoopB");
        AddInstance(other, "Two", "LoopB");
        GraphicalUiElement screenGue = Build(other);

        Reload(screenGue, project);

        Describe(screenGue).ShouldBe(DescribeAFreshLoad(other));
        Describe(screenGue).ShouldBe(
            "Other <Other>\n  One <LoopB>\n    Back <LoopA>\n  Two <LoopB>\n    Back <LoopA>\n".Replace("\n", Environment.NewLine));
    }
}
