using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// What the tool does with a running Preview when the loaded project changes (#7541). Preview is
/// launched against one project file and cannot switch, so a different project closes it; a reload
/// of the same project keeps it, since Preview hot-reloads that file itself.
/// </summary>
public class PreviewLauncherProjectChangeTests : IDisposable
{
    private const string ProjectA = "/MyGame/A/GumProject.gumj";
    private const string ProjectB = "/MyGame/B/GumProject.gumj";

    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IOutputManager> _outputManager = new();
    private readonly Mock<IPreviewGumxProjectionService> _projection = new();
    private readonly RecordingPreviewStarter _starter = new RecordingPreviewStarter("/Preview/GumPreview");
    private readonly List<string> _selectionFiles = new List<string>();

    public void Dispose()
    {
        foreach (string file in _selectionFiles)
        {
            File.Delete(file);
        }
    }

    private PreviewLauncher CreateLauncher() =>
        new PreviewLauncher(_selectedState.Object, _projectManager.Object, _outputManager.Object, _projection.Object, _starter, () => false);

    private GumProjectSave Load(string fileName, params string[] screenNames)
    {
        GumProjectSave project = new GumProjectSave { FullFileName = fileName };
        foreach (string name in screenNames)
        {
            project.Screens.Add(new ScreenSave { Name = name });
        }
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(project);
        return project;
    }

    private PreviewLauncher LaunchOn(GumProjectSave project, ElementSave shown)
    {
        _selectedState.SetupGet(s => s.SelectedElement).Returns(shown);
        PreviewLauncher launcher = CreateLauncher();
        launcher.Launch();
        TrackSelectionFile(_starter.Started.Count - 1);
        return launcher;
    }

    private void TrackSelectionFile(int startIndex)
    {
        List<string> arguments = _starter.Started[startIndex].ArgumentList.ToList();
        _selectionFiles.Add(arguments[arguments.IndexOf("--selection-file") + 1]);
    }

    private string SelectionFileOf(int startIndex)
    {
        List<string> arguments = _starter.Started[startIndex].ArgumentList.ToList();
        return arguments[arguments.IndexOf("--selection-file") + 1];
    }

    private string ArgumentOf(int startIndex, string name)
    {
        List<string> arguments = _starter.Started[startIndex].ArgumentList.ToList();
        return arguments[arguments.IndexOf(name) + 1];
    }

    [Fact]
    public void ReactToProjectLoaded_WhenAnotherProjectIsLoaded_ClosesThePreview()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);

        GumProjectSave b = Load(ProjectB, "MainScreen");
        launcher.ReactToProjectLoaded(b);

        launcher.IsRunning.ShouldBeFalse();
        _starter.CloseCount.ShouldBe(1);
    }

    [Fact]
    public void ReactToProjectLoaded_WhenANewUnsavedProjectIsCreated_ClosesThePreview()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);

        // A new project has no file name until the new-project flow saves it, after the load event.
        GumProjectSave blank = Load("", "MainScreen");
        launcher.ReactToProjectLoaded(blank);

        launcher.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void ReactToProjectLoaded_WhenTheSameProjectIsReloaded_KeepsThePreviewRunning()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);

        GumProjectSave reloaded = Load(ProjectA, "MainScreen");
        launcher.ReactToProjectLoaded(reloaded);

        launcher.IsRunning.ShouldBeTrue();
        _starter.CloseCount.ShouldBe(0);
    }

    [Fact]
    public void ReactToProjectLoaded_WhenTheSameProjectIsReloadedWithDifferentCasing_KeepsThePreviewRunning()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);

        GumProjectSave reloaded = Load(ProjectA.ToUpperInvariant(), "MainScreen");
        launcher.ReactToProjectLoaded(reloaded);

        launcher.IsRunning.ShouldBeTrue();
    }

    [Fact]
    public void ReactToProjectLoaded_WhenAnotherProjectIsLoaded_DeletesTheSelectionFile()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        string selectionFile = SelectionFileOf(0);

        launcher.ReactToProjectLoaded(Load(ProjectB, "MainScreen"));

        File.Exists(selectionFile).ShouldBeFalse();
    }

    [Fact]
    public void ReactToProjectLoaded_WhenAnotherProjectIsLoaded_UnpinsAndRaisesPinnedChanged()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        launcher.Pin().ShouldBeTrue();
        int changes = 0;
        launcher.PinnedChanged += () => changes++;

        GumProjectSave b = Load(ProjectB, "MainScreen");
        launcher.ReactToProjectLoaded(b);

        launcher.PinnedElement.ShouldBeNull();
        changes.ShouldBe(1);
    }

    [Fact]
    public void ReactToProjectLoaded_WhenAnotherProjectIsLoaded_ReportsNoError()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);

        launcher.ReactToProjectLoaded(Load(ProjectB, "MainScreen"));

        _outputManager.Verify(o => o.AddError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void PushSelection_AfterAnotherProjectWasLoaded_WritesNothing()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        string selectionFile = SelectionFileOf(0);

        GumProjectSave b = Load(ProjectB, "MainScreen");
        launcher.ReactToProjectLoaded(b);
        launcher.PushSelection(b.Screens[0]);

        File.Exists(selectionFile).ShouldBeFalse();
    }

    [Fact]
    public void Launch_AfterAnotherProjectWasLoaded_StartsAPreviewOnTheNewProject()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        GumProjectSave b = Load(ProjectB, "MainScreen");
        launcher.ReactToProjectLoaded(b);

        _selectedState.SetupGet(s => s.SelectedElement).Returns(b.Screens[0]);
        launcher.Launch();
        TrackSelectionFile(1);

        _starter.Started.Count.ShouldBe(2);
        ArgumentOf(1, "--project").ShouldBe(ProjectB);
        SelectionFileOf(1).ShouldNotBe(SelectionFileOf(0));
    }

    [Fact]
    public void Launch_AfterOpeningAnotherProjectAndComingBack_StartsAFreshPreview()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);

        launcher.ReactToProjectLoaded(Load(ProjectB, "MainScreen"));
        GumProjectSave backToA = Load(ProjectA, "MainScreen");
        launcher.ReactToProjectLoaded(backToA);

        launcher.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void ReactToProjectLoaded_WhenThePreviewAlreadyExited_DoesNotCloseItAgain()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        _starter.HasExited = true;

        Should.NotThrow(() => launcher.ReactToProjectLoaded(Load(ProjectB, "MainScreen")));

        _starter.CloseCount.ShouldBe(0);
    }

    [Fact]
    public void ReactToProjectLoaded_WhenNoPreviewWasLaunched_DoesNothing()
    {
        GumProjectSave b = Load(ProjectB, "MainScreen");

        Should.NotThrow(() => CreateLauncher().ReactToProjectLoaded(b));

        _outputManager.VerifyNoOtherCalls();
    }

    [Fact]
    public void ReactToProjectLoaded_WhenAGumxPreviewRunsAndAnotherProjectLoads_ClosesItAndStopsRefreshing()
    {
        GumProjectSave a = Load("/MyGame/A/GumProject.gumx", "MainScreen");
        _selectedState.SetupGet(s => s.SelectedElement).Returns(a.Screens[0]);
        _projection.Setup(p => p.Project(a)).Returns(new PreviewGumxProjection("/tmp/preview/GumProject.gumj", "/tmp/preview"));
        PreviewLauncher launcher = CreateLauncher();
        launcher.ProcessStarter = new NativeAotStarter(_starter);
        launcher.Launch();
        TrackSelectionFile(0);
        _projection.Invocations.Clear();

        launcher.ReactToProjectLoaded(Load("/MyGame/B/GumProject.gumx", "MainScreen"));
        launcher.RefreshIfRunning();

        launcher.IsRunning.ShouldBeFalse();
        _projection.Verify(p => p.Project(It.IsAny<GumProjectSave>()), Times.Never);
    }

    [Fact]
    public void ReactToProjectLoaded_WhenTheSameProjectIsReloaded_FollowsTheNewObjectsOnRename()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        GumProjectSave reloaded = Load(ProjectA, "MainScreen");
        launcher.ReactToProjectLoaded(reloaded);

        ScreenSave newMain = reloaded.Screens[0];
        newMain.Name = "Renamed";
        launcher.ReactToElementRenamed(newMain);

        PreviewSelectionMessage.TryParse(File.ReadAllLines(SelectionFileOf(0)))!.ElementName.ShouldBe("Renamed");
    }

    [Fact]
    public void ReactToProjectLoaded_WhenTheSameProjectIsReloaded_AndThePinnedElementIsGone_KeepsRunningButUnpins()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        launcher.Pin().ShouldBeTrue();

        launcher.ReactToProjectLoaded(Load(ProjectA, "Other"));

        launcher.IsRunning.ShouldBeTrue();
        launcher.PinnedElement.ShouldBeNull();
    }

    [Fact]
    public void ReactToProjectLoaded_WhenTheSameProjectIsReloaded_AndAComponentTookTheScreensName_Unpins()
    {
        GumProjectSave a = Load(ProjectA, "Thing");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        launcher.Pin().ShouldBeTrue();
        GumProjectSave reloaded = Load(ProjectA);
        reloaded.Components.Add(new ComponentSave { Name = "Thing" });

        launcher.ReactToProjectLoaded(reloaded);

        launcher.PinnedElement.ShouldBeNull();
    }

    [Fact]
    public void ReactToProjectLoaded_WhenTheSameProjectIsReloaded_PushesNothingByItself()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        DateTime before = File.GetLastWriteTimeUtc(SelectionFileOf(0));

        launcher.ReactToProjectLoaded(Load(ProjectA, "MainScreen"));

        File.GetLastWriteTimeUtc(SelectionFileOf(0)).ShouldBe(before);
    }

    [Fact]
    public void Close_ThenClose_ClosesTheProcessOnce()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);

        launcher.Close();
        launcher.Close();

        _starter.CloseCount.ShouldBe(1);
    }

    [Fact]
    public void Launch_PassesTheLoadedProjectAndTheToolsProcessId()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        LaunchOn(a, a.Screens[0]);

        ArgumentOf(0, "--project").ShouldBe(ProjectA);
        ArgumentOf(0, "--parent-pid").ShouldBe(Environment.ProcessId.ToString());
        ArgumentOf(0, "--element").ShouldBe("MainScreen");
    }

    [Fact]
    public void Launch_AfterThePreviewExitedByItself_DeletesTheOldSelectionFileAndUsesANewOne()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        string first = SelectionFileOf(0);
        _starter.HasExited = true;

        launcher.Launch();
        _starter.HasExited = false;
        TrackSelectionFile(1);

        File.Exists(first).ShouldBeFalse();
        SelectionFileOf(1).ShouldNotBe(first);
    }

    [Fact]
    public void PushSelection_AfterThePreviewExitedByItself_WritesNothing()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        File.Delete(SelectionFileOf(0));
        _starter.HasExited = true;

        launcher.PushSelection(a.Screens[0]);

        File.Exists(SelectionFileOf(0)).ShouldBeFalse();
    }

    [Fact]
    public void Launch_WhileRunning_KeepsTheOriginalProjectArgument()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);

        launcher.Launch();

        _starter.Started.Count.ShouldBe(1);
    }

    [Fact]
    public void ReactToElementRenamed_AfterAnotherProjectWasLoaded_PushesNothing()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        string selectionFile = SelectionFileOf(0);
        GumProjectSave b = Load(ProjectB, "MainScreen");
        launcher.ReactToProjectLoaded(b);

        launcher.ReactToElementRenamed(b.Screens[0]);

        File.Exists(selectionFile).ShouldBeFalse();
    }

    [Fact]
    public void Unpin_AfterAnotherProjectWasLoaded_DoesNotRaisePinnedChanged()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        launcher.Pin().ShouldBeTrue();
        launcher.ReactToProjectLoaded(Load(ProjectB, "MainScreen"));
        int changes = 0;
        launcher.PinnedChanged += () => changes++;

        launcher.Unpin();

        changes.ShouldBe(0);
    }

    [Fact]
    public void Pin_AfterAnotherProjectWasLoaded_RefusesBecauseNothingIsRunning()
    {
        GumProjectSave a = Load(ProjectA, "MainScreen");
        PreviewLauncher launcher = LaunchOn(a, a.Screens[0]);
        launcher.ReactToProjectLoaded(Load(ProjectB, "MainScreen"));

        launcher.Pin().ShouldBeFalse();
    }

    private sealed class NativeAotStarter : IPreviewProcessStarter
    {
        private readonly RecordingPreviewStarter _inner;

        public NativeAotStarter(RecordingPreviewStarter inner)
        {
            _inner = inner;
        }

        public ResolvedPreviewExecutable? Resolve() => new ResolvedPreviewExecutable("/Preview/GumPreview", IsNativeAot: true);

        public IPreviewProcess? Start(System.Diagnostics.ProcessStartInfo startInfo) => _inner.Start(startInfo);
    }
}
