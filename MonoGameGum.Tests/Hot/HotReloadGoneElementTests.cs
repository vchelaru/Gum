#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #7142: a reload cannot map a visual to an element that is gone from the project (the shown screen
/// was renamed or deleted). The visual stays as it was, and the game is told once, by name.
/// </summary>
public class HotReloadGoneElementTests : BaseTestClass
{
    private readonly SweepWorld _world = new SweepWorld();
    private readonly List<string> _errors = new List<string>();

    public HotReloadGoneElementTests()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Collect;
    }

    // The event is static, so noise from other tests can arrive too.
    private void Collect(string message)
    {
        if (message.Contains("Hot reload"))
        {
            _errors.Add(message);
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

    // What the tool saves after the screen is deleted: the project file no longer lists it.
    private void SaveWithoutTheScreen()
    {
        GumProjectSave project = GumProjectSave.Load(_world.Gumx);
        project.ScreenReferences.Clear();
        project.Save(_world.Gumx, saveElements: false);
    }

    [Fact]
    public void ShownScreenDeleted_ReportsItByNameAndKeepsTheOldUi()
    {
        _world.Boot(new SweepSpec());

        SaveWithoutTheScreen();
        _world.NotifyAndSettle(_world.Gumx);

        _world.Reloads.ShouldBe(1);
        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain("'MainScreen'");
        _errors[0].ShouldContain("restart");
        _world.Find("Box").ShouldNotBeNull("the old UI is kept");
    }

    [Fact]
    public void ShownScreenDeleted_WhenTheHostOptsOut_PrintsNothingButStillReportsItInTheResult()
    {
        _world.Boot(new SweepSpec());
        HotReloadResult? result = null;
        _world.Manager.ReloadFinished += received => result = received;
        GumHotReloadManager.PrintsShownElementGone = false;
        try
        {
            SaveWithoutTheScreen();
            _world.NotifyAndSettle(_world.Gumx);
        }
        finally
        {
            GumHotReloadManager.PrintsShownElementGone = true;
        }

        _errors.ShouldBeEmpty();
        result.ShouldNotBeNull();
        result.Issues.Any(issue => issue.Kind == HotReloadIssueKind.ShownElementGone).ShouldBeTrue();
        result.MatchesRestart.ShouldBeFalse();
    }

    [Fact]
    public void ShownScreenStillGone_IsReportedOnceNotOnEveryLaterSave()
    {
        _world.Boot(new SweepSpec());
        SaveWithoutTheScreen();
        _world.NotifyAndSettle(_world.Gumx);

        _world.NotifyAndSettle(_world.Gumx);
        _world.NotifyAndSettle(_world.Gumx);

        _world.Reloads.ShouldBe(3);
        _errors.Count.ShouldBe(1);
    }

    [Fact]
    public void ShownScreenBack_ReloadsItAgainAndIsReportedAgainIfLostAgain()
    {
        _world.Boot(new SweepSpec());
        SaveWithoutTheScreen();
        _world.NotifyAndSettle(_world.Gumx);

        _world.SaveAll(new SweepSpec { BoxX = 77 });
        _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile);

        _world.Find("Box")!.X.ShouldBe(77f, "an undone rename maps to the screen again");
        _errors.Count.ShouldBe(1);

        SaveWithoutTheScreen();
        _world.NotifyAndSettle(_world.Gumx);

        _errors.Count.ShouldBe(2);
    }

    [Fact]
    public void GoneComponentAddedByCode_IsReportedAsAComponent()
    {
        _world.Boot(new SweepSpec());
        GraphicalUiElement byCode = ObjectFinder.Self.GumProjectSave!.Components
            .Single(c => c.Name == "Widgets/Widget")
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        byCode.Name = "FromCode";
        byCode.Parent = _world.Screen;

        _world.SaveAll(new SweepSpec { WithWidget = false });
        _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile);

        _errors.Count.ShouldBe(1);
        _errors[0].ShouldContain("component 'Widgets/Widget'");
        _errors[0].ShouldNotContain("MainScreen");
    }

    [Fact]
    public void ScreenStillInTheProject_ReportsNothing()
    {
        _world.Boot(new SweepSpec());

        _world.SaveAll(new SweepSpec { BoxX = 9 });
        _world.NotifyAndSettle(_world.Gumx, _world.ScreenFile);

        _errors.ShouldBeEmpty();
    }
}
