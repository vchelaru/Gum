#pragma warning disable CS0618
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
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #7162: every reload attempt raises <see cref="IGumHotReloadManager.ReloadFinished"/> with the issues
/// present in that reload and whether the tree matches a restart. The console output stays as it was.
/// </summary>
public class HotReloadResultTests : BaseTestClass
{
    private static readonly SweepSpec Reader = new SweepSpec { BoxReadsStyle = true };

    private readonly SweepWorld _world = new SweepWorld();
    private readonly List<HotReloadResult> _results = new List<HotReloadResult>();
    private readonly List<string> _printed = new List<string>();
    private string? _originalRelativeDirectory;

    public HotReloadResultTests()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Collect;
    }

    // The event is static, so noise from other tests can arrive too.
    private void Collect(string message)
    {
        if (message.Contains("Hot reload") || message.Contains("Could not copy font"))
        {
            _printed.Add(message);
        }
    }

    public override void Dispose()
    {
        if (_originalRelativeDirectory != null)
        {
            global::ToolsUtilities.FileManager.RelativeDirectory = _originalRelativeDirectory;
        }
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= Collect;
        _world.Dispose();
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private void Boot(SweepSpec spec)
    {
        _world.Boot(spec);
        _world.Manager.ReloadFinished += _results.Add;
    }

    private void SaveWithoutTheScreen()
    {
        GumProjectSave project = GumProjectSave.Load(_world.Gumx);
        project.ScreenReferences.Clear();
        project.Save(_world.Gumx, saveElements: false);
    }

    private void SettleThroughRetry(string path)
    {
        _world.NotifyAndSettle(path);
        _world.Advance(300);
        _world.Update();
    }

    private static HotReloadIssue Single(HotReloadResult result, HotReloadIssueKind kind) =>
        result.Issues.Single(issue => issue.Kind == kind);

    [Fact]
    public void CleanReload_IsCleanMatchesARestartAndRaisesOnce()
    {
        Boot(Reader);

        _world.SaveAll(Reader with { BoxX = 9 });
        _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile);

        _world.Reloads.ShouldBe(1);
        _results.Count.ShouldBe(1);
        _results[0].IsClean.ShouldBeTrue();
        _results[0].MatchesRestart.ShouldBeTrue();
        _results[0].Issues.ShouldBeEmpty();
        _printed.ShouldBeEmpty();
    }

    [Fact]
    public void ShownScreenDeleted_ReportsShownElementGone_ThatDoesNotMatchARestart()
    {
        Boot(Reader);

        SaveWithoutTheScreen();
        _world.NotifyAndSettle(_world.Gumx);

        _results.Count.ShouldBe(1);
        HotReloadIssue issue = Single(_results[0], HotReloadIssueKind.ShownElementGone);
        issue.ElementName.ShouldBe("MainScreen");
        issue.InstanceName.ShouldBeNull();
        issue.MatchesRestart.ShouldBeFalse();
        issue.Message.ShouldBe(_printed.Single());
        _results[0].IsClean.ShouldBeFalse();
        _results[0].MatchesRestart.ShouldBeFalse();
    }

    [Fact]
    public void TheSameProblemOnConsecutiveReloads_IsInBothResults_AndPrintedOnce()
    {
        Boot(Reader);
        SaveWithoutTheScreen();

        _world.NotifyAndSettle(_world.Gumx);
        _world.NotifyAndSettle(_world.Gumx);
        _world.NotifyAndSettle(_world.Gumx);

        _results.Count.ShouldBe(3);
        _results.ShouldAllBe(result => result.Issues.Count == 1 && result.Issues[0].Kind == HotReloadIssueKind.ShownElementGone);
        _results.ShouldAllBe(result => !result.MatchesRestart);
        _printed.Count.ShouldBe(1);
    }

    [Fact]
    public void ProblemGoneThenBack_ResultGoesCleanAgain()
    {
        Boot(Reader);
        SaveWithoutTheScreen();
        _world.NotifyAndSettle(_world.Gumx);

        _world.SaveAll(Reader with { BoxX = 77 });
        _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile);

        _results.Count.ShouldBe(2);
        _results[1].IsClean.ShouldBeTrue();
    }

    [Fact]
    public void MissingFile_ReportsMissingFile_ThatDoesNotMatchARestart()
    {
        Boot(Reader);
        _world.SaveAll(Reader with { WithGadget = true });
        File.Delete(_world.GadgetFile);

        _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile);

        HotReloadResult result = _results.Single();
        HotReloadIssue issue = Single(result, HotReloadIssueKind.MissingFile);
        issue.Message.ShouldContain("Gadget");
        issue.MatchesRestart.ShouldBeFalse();
        result.MatchesRestart.ShouldBeFalse();
        _world.Reloads.ShouldBe(1);
        _printed.ShouldContain(issue.Message);
    }

    [Fact]
    public void HalfWrittenFile_RaisesNothingForTheRetriedAttempt_AndReloadFailedForTheFinalOne()
    {
        Boot(Reader);
        _world.EditInPlace(_world.ScreenFile, "<GumScreen");

        _world.NotifyAndSettle(_world.ScreenFile);

        _results.ShouldBeEmpty("the first failed read is tried again, so it is not an outcome yet");
        _printed.ShouldBeEmpty();

        _world.Advance(300);
        _world.Update();

        _world.Reloads.ShouldBe(0, "ReloadCompleted is only for a reload that applied");
        HotReloadResult result = _results.Single();
        HotReloadIssue issue = result.Issues.Single();
        issue.Kind.ShouldBe(HotReloadIssueKind.ReloadFailed);
        result.MatchesRestart.ShouldBeFalse();
        issue.Message.ShouldBe(_printed.Single());
        issue.Message.ShouldContain("MainScreen");
    }

    [Fact]
    public void HalfWrittenFileCompletedBeforeTheRetry_RaisesOnlyTheCleanResult()
    {
        Boot(Reader);
        _world.EditInPlace(_world.ScreenFile, "<GumScreen");
        _world.NotifyAndSettle(_world.ScreenFile);

        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 70 }, "MainScreen"));
        _world.Advance(300);
        _world.Update();

        _results.Single().IsClean.ShouldBeTrue();
        _world.Reloads.ShouldBe(1);
    }

    [Fact]
    public void ThrowingSubscribers_DoNotBreakUpdateOrTheOtherSubscribers()
    {
        Boot(Reader);
        List<string> heard = new List<string>();
        _world.Manager.ReloadCompleted += () => throw new InvalidOperationException("completed boom");
        _world.Manager.ReloadCompleted += () => heard.Add("completed");
        _world.Manager.ReloadFinished += _ => throw new InvalidOperationException("finished boom");
        _world.Manager.ReloadFinished += _ => heard.Add("finished");
        _world.SaveAll(Reader with { BoxX = 9 });

        Should.NotThrow(() => _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile));

        heard.ShouldBe(new[] { "completed", "finished" });
        _world.Find("Box")!.X.ShouldBe(9f);
        _printed.Count(message => message.Contains("completed boom")).ShouldBe(1);
        _printed.Count(message => message.Contains("finished boom")).ShouldBe(1);
        _results.ShouldHaveSingleItem("the printed exceptions are not issues");
        _results[0].IsClean.ShouldBeTrue();
    }

    [Fact]
    public void FailureWhileApplying_RaisesReloadFailed()
    {
        Boot(Reader);
        GumHotReloadManager manager = new GumHotReloadManager(
            _ => { }, (_, _) => throw new InvalidOperationException("animation boom"), path => { }, () => _world.Now);
        manager.ReloadFinished += _results.Add;
        manager.Start(_world.Gumx);
        manager.Stop();
        _world.Manager = manager;
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxX = 50 }, "MainScreen"));

        SettleThroughRetry(_world.ScreenFile);

        HotReloadResult last = _results.Last();
        HotReloadIssue issue = Single(last, HotReloadIssueKind.ReloadFailed);
        issue.Message.ShouldContain("animation boom");
        issue.MatchesRestart.ShouldBeFalse();
        _printed.Single().ShouldContain("animation boom");
    }

    [Fact]
    public void FontLockedByTheEditor_ReportsFontFailure()
    {
        _originalRelativeDirectory = global::ToolsUtilities.FileManager.RelativeDirectory;
        string bin = Path.Combine(_world.Dir, "bin") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(bin);
        global::ToolsUtilities.FileManager.RelativeDirectory = bin;
        Boot(Reader);
        string fontCache = Path.Combine(_world.Dir, "FontCache");
        Directory.CreateDirectory(fontCache);
        string fnt = Path.Combine(fontCache, "Font18Arial.fnt");
        File.WriteAllText(fnt, "info");
        _world.Notify(fnt);

        using (new FileStream(fnt, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            _world.NotifyAndSettle(_world.ScreenFile);
        }

        HotReloadIssue issue = Single(_results.Single(), HotReloadIssueKind.FontFailure);
        issue.Message.ShouldContain("Font18Arial.fnt");
        issue.MatchesRestart.ShouldBeFalse();
        _world.Reloads.ShouldBe(1);
    }

    [Fact]
    public void BoxRetyped_ReportsInstanceRebuilt_ThatMatchesARestart()
    {
        Boot(Reader);

        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxType = "Container" }, "MainScreen"));
        _world.NotifyAndSettle(_world.ScreenFile);

        HotReloadResult result = _results.Single();
        HotReloadIssue issue = Single(result, HotReloadIssueKind.InstanceRebuilt);
        issue.ElementName.ShouldBe("MainScreen");
        issue.InstanceName.ShouldBe("Box");
        issue.MatchesRestart.ShouldBeTrue();
        result.MatchesRestart.ShouldBeTrue();
        result.IsClean.ShouldBeFalse();
        _printed.Single().ShouldBe(issue.Message);
    }

    [Fact]
    public void SeveralKindsInOneReload_AreAllListed_AndOneBadKindMakesItNotMatchARestart()
    {
        Boot(Reader);
        _world.SaveAll(Reader with { WithGadget = true });
        File.Delete(_world.GadgetFile);
        _world.EditInPlace(_world.ScreenFile, _world.ElementText(Reader with { BoxType = "Container", WithGadget = true }, "MainScreen"));

        _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile);

        HotReloadResult result = _results.Single();
        result.Issues.Select(issue => issue.Kind).ShouldContain(HotReloadIssueKind.MissingFile);
        result.Issues.Select(issue => issue.Kind).ShouldContain(HotReloadIssueKind.InstanceRebuilt);
        result.MatchesRestart.ShouldBeFalse();
    }

    [Fact]
    public void NoSubscriber_PrintsTheSameAndStillRaisesReloadCompleted()
    {
        _world.Boot(Reader);
        SaveWithoutTheScreen();

        _world.NotifyAndSettle(_world.Gumx);
        _world.NotifyAndSettle(_world.Gumx);

        _world.Reloads.ShouldBe(2);
        _printed.Count.ShouldBe(1);
        _printed[0].ShouldContain("'MainScreen'");
    }

    [Fact]
    public void GumService_ForwardsEachResultThroughHotReloadFinished()
    {
        _world.Boot(Reader);
        Gum.GumService service = Gum.GumService.Default;
        List<HotReloadResult> received = new List<HotReloadResult>();
        service.HotReloadFinished += received.Add;
        service.EnableHotReload(_world.Gumx);
        try
        {
            _world.SaveAll(Reader with { BoxX = 9 });
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (received.Count == 0 && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                service.Update(new Microsoft.Xna.Framework.GameTime());
                System.Threading.Thread.Sleep(10);
            }
        }
        finally
        {
            service.HotReloadFinished -= received.Add;
            service._hotReloadManager?.Stop();
        }

        received.Count.ShouldBeGreaterThan(0);
        received.ShouldAllBe(result => result.IsClean);
    }

    // ---- Tree-level cases that need components the sweep world does not have ----

    private static StateSave NewDefaultState(ElementSave element)
    {
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        return state;
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        NewDefaultState(component);
        project.Components.Add(component);
        return component;
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

    [Fact]
    public void ComponentRenamed_InstancesOfTheSamePairGiveOneIssueAndOneMessage()
    {
        GumProjectSave project = NewProject();
        ComponentSave card = AddComponent(project, "Card");
        ComponentSave badge = AddComponent(project, "Badge");
        ScreenSave main = new ScreenSave { Name = "Main" };
        NewDefaultState(main);
        project.Screens.Add(main);
        InstanceSave card1 = AddInstance(main, "Card1", "Card");
        InstanceSave card2 = AddInstance(main, "Card2", "Card");
        InstanceSave badge1 = AddInstance(main, "Badge1", "Badge");
        GraphicalUiElement screenGue = main.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        project.Components.Remove(card);
        project.Components.Remove(badge);
        AddComponent(project, "Card9");
        AddComponent(project, "Badge9");
        card1.BaseType = "Card9";
        card2.BaseType = "Card9";
        badge1.BaseType = "Badge9";

        HotReloadResult result = GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);

        result.Issues.Count.ShouldBe(2);
        result.Issues.ShouldAllBe(issue => issue.Kind == HotReloadIssueKind.InstanceRebuilt && issue.MatchesRestart);
        HotReloadIssue cardIssue = result.Issues.Single(issue => issue.InstanceName == "Card1");
        cardIssue.ElementName.ShouldBe("Main");
        cardIssue.Message.ShouldContain("'Card1' in 'Main' is now a 'Card9' (was 'Card')");
        cardIssue.Message.ShouldContain("stale");
        _printed.Count.ShouldBe(2);
        _printed.ShouldContain(cardIssue.Message);
    }

    [Fact]
    public void InstanceOfDeletedComponent_ReportsInstanceRemoved_ThatMatchesARestart_AndAgainOnlyWhileThereIsSomethingToRemove()
    {
        GumProjectSave project = NewProject();
        ComponentSave card = AddComponent(project, "Card");
        ScreenSave main = new ScreenSave { Name = "Main" };
        NewDefaultState(main);
        project.Screens.Add(main);
        AddInstance(main, "Card1", "Card");
        GraphicalUiElement screenGue = main.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        project.Components.Remove(card);

        HotReloadResult first = GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);
        HotReloadResult second = GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);

        HotReloadIssue issue = first.Issues.Single();
        issue.Kind.ShouldBe(HotReloadIssueKind.InstanceRemoved);
        issue.ElementName.ShouldBe("Main");
        issue.InstanceName.ShouldBe("Card1");
        issue.MatchesRestart.ShouldBeTrue();
        first.MatchesRestart.ShouldBeTrue();
        second.IsClean.ShouldBeTrue("the instance is already gone");
    }

    [Fact]
    public void ApplyDiffWithoutAMemory_ReportsAGoneElementEveryTime()
    {
        GumProjectSave project = NewProject();
        ScreenSave main = new ScreenSave { Name = "Main" };
        NewDefaultState(main);
        project.Screens.Add(main);
        GraphicalUiElement screenGue = main.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        project.Screens.Clear();

        HotReloadResult first = GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);
        HotReloadResult second = GumHotReloadManager.ApplyDiff(new[] { screenGue }, project, SystemManagers.Default);

        first.Issues.Single().Kind.ShouldBe(HotReloadIssueKind.ShownElementGone);
        second.Issues.Single().Kind.ShouldBe(HotReloadIssueKind.ShownElementGone);
        _printed.Count.ShouldBe(2);
    }

    // D derives from B1, which exposes Fill.X as "Tint", or from B2, which exposes it as "Tint2".
    // The screen sets W.Tint, so a restart ignores it under B2 while the running tree keeps it.
    private static GumProjectSave ExposureProject(string derivedBase)
    {
        GumProjectSave project = NewProject();
        foreach ((string name, string exposedAs) in new[] { ("B1", "Tint"), ("B2", "Tint2") })
        {
            ComponentSave component = AddComponent(project, name);
            AddInstance(component, "Fill", "Container");
            component.DefaultState!.Variables.Add(new VariableSave
            {
                Name = "Fill.X", Type = "float", Value = 0f, SetsValue = true, ExposedAsName = exposedAs
            });
        }
        ComponentSave derived = AddComponent(project, "D");
        derived.BaseType = derivedBase;
        ScreenSave main = new ScreenSave { Name = "Main" };
        StateSave mainDefault = NewDefaultState(main);
        project.Screens.Add(main);
        AddInstance(main, "W", "D");
        mainDefault.Variables.Add(new VariableSave { Name = "W.Tint", Type = "float", Value = 150f, SetsValue = true });
        project.Initialize();
        return project;
    }

    [Fact]
    public void BaseSwitchedToOneThatExposesTheVariableUnderAnotherName_IsListed()
    {
        GumProjectSave oldProject = ExposureProject("B1");
        GraphicalUiElement screenGue = oldProject.Screens.Single().ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        GumProjectSave newProject = ExposureProject("B2");

        HotReloadResult result = GumHotReloadManager.ApplyDiff(new[] { screenGue }, newProject, SystemManagers.Default, oldProject);

        static float FillX(GraphicalUiElement screen) =>
            screen.GetGraphicalUiElementByName("W")!.ContainedElements.Single(child => child.Name == "Fill").X;
        float hot = FillX(screenGue);
        float fresh = FillX(newProject.Screens.Single().ToGraphicalUiElement(SystemManagers.Default, addToManagers: false));
        hot.ShouldNotBe(fresh, "sanity: the oracle can tell");
        result.Issues.Single().Kind.ShouldBe(HotReloadIssueKind.ComponentBaseTypeChanged);
        result.MatchesRestart.ShouldBeFalse();
    }
}
