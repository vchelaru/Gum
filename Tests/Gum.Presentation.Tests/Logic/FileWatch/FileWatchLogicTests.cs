using System;
using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests.Logic.FileWatch;

public class FileWatchLogicTests
{
    private readonly Mock<IFileWatchManager> _fileWatchManager;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IProjectState> _projectState;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IDispatcher> _dispatcher;
    private Action? _postedAction;
    private readonly FileWatchLogic _fileWatchLogic;

    public FileWatchLogicTests()
    {
        _fileWatchManager = new Mock<IFileWatchManager>();
        _guiCommands = new Mock<IGuiCommands>();
        _projectState = new Mock<IProjectState>();
        _projectManager = new Mock<IProjectManager>();
        _dispatcher = new Mock<IDispatcher>();
        _dispatcher.Setup(d => d.Post(It.IsAny<Action>()))
            .Callback<Action>(action => _postedAction = action);

        _fileWatchLogic = new FileWatchLogic(
            _fileWatchManager.Object,
            _guiCommands.Object,
            _projectState.Object,
            _projectManager.Object,
            _dispatcher.Object);
    }

    /// <summary>Runs the scan the same way the real dispatcher would once it processes the post.</summary>
    private void RunPostedAction() => _postedAction!.Invoke();

    [Fact]
    public void HandleProjectUnloaded_DisablesWatcher()
    {
        _fileWatchLogic.HandleProjectUnloaded();

        _fileWatchManager.Verify(m => m.Disable(), Times.Once);
    }

    [Fact]
    public void RefreshRootDirectory_WatchesProjectDirectoryAndOutOfProjectFileDirectories()
    {
        // Font-cache files always live under "FontCache/" inside the project directory, which is
        // watched with IncludeSubdirectories, so they never contribute a directory of their own.
        // Files referenced from outside the project do, and are the only thing that can actually be
        // lost by narrowing what the dependency walk enumerates. A custom font is the subtle case:
        // it is a font, but it is classified as an external file rather than a font-cache file, so
        // it must survive a walk that excludes font-cache enumeration.
        // Rooted on this OS, so the paths stay absolute on Linux as well as Windows.
        string root = OperatingSystem.IsWindows() ? "C:/" : "/";
        string outOfProjectTexture = root + "Elsewhere/Art/bg.png";
        string outOfProjectFont = root + "OutsideFonts/Fancy.ttf";

        GumProjectSave project = new GumProjectSave { FullFileName = root + "FakeGumProject/MyProject.gumx" };
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave sprite = new InstanceSave { Name = "Sprite1", BaseType = "Sprite", ParentContainer = screen };
        screen.Instances.Add(sprite);
        screen.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Sprite1.SourceFile",
            Type = "string",
            Value = outOfProjectTexture,
            IsFile = true,
            SetsValue = true,
        });
        InstanceSave text = new InstanceSave { Name = "Text1", BaseType = "Text", ParentContainer = screen };
        screen.Instances.Add(text);
        screen.DefaultState.Variables.Add(new VariableSave { Name = "Text1.UseCustomFont", Type = "bool", Value = false, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "Text1.Font", Type = "string", Value = "Arial", IsFont = true, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "Text1.FontSize", Type = "int", Value = 18, SetsValue = true });
        InstanceSave customFontText = new InstanceSave { Name = "Text2", BaseType = "Text", ParentContainer = screen };
        screen.Instances.Add(customFontText);
        screen.DefaultState.Variables.Add(new VariableSave { Name = "Text2.UseCustomFont", Type = "bool", Value = true, SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Text2.CustomFontFile",
            Type = "string",
            Value = outOfProjectFont,
            IsFile = true,
            SetsValue = true,
        });
        project.Screens.Add(screen);

        ObjectFinder.Self.GumProjectSave = project;
        _projectManager.SetupGet(m => m.GumProjectSave).Returns(project);
        _projectState.SetupGet(m => m.GumProjectSave).Returns(project);

        HashSet<FilePath>? watched = null;
        _fileWatchManager
            .Setup(m => m.EnableWithDirectories(It.IsAny<HashSet<FilePath>>()))
            .Callback<HashSet<FilePath>>(directories => watched = directories);

        _fileWatchLogic.RefreshRootDirectory();
        RunPostedAction();

        watched.ShouldNotBeNull();
        watched.ShouldContain(new FilePath(root + "FakeGumProject/"));
        watched.ShouldContain(new FilePath(root + "Elsewhere/Art/"));
        watched.ShouldContain(new FilePath(root + "OutsideFonts/"));
    }

    [Fact]
    public void RefreshRootDirectory_ClearsIgnoredFilesAndDisables_WhenNoProjectLoaded()
    {
        // GumProjectSave defaults to null on the mock, so GumProjectSave?.FullFileName
        // is null and RefreshRootDirectory takes the "no project" branch. This avoids
        // GetFileWatchRootDirectories, which would require heavy ObjectFinder.Self setup.
        _fileWatchLogic.RefreshRootDirectory();
        RunPostedAction();

        _fileWatchManager.Verify(m => m.ClearIgnoredFiles(), Times.Once);
        _fileWatchManager.Verify(m => m.Disable(), Times.Once);
    }

    [Fact]
    public void RefreshRootDirectory_DoesNotScanSynchronously()
    {
        // #4873: the scan is dominated by File.Exists checks and must not run inline on whatever
        // thread called RefreshRootDirectory (the UI thread, during project load) - it's posted
        // via the dispatcher instead.
        _projectManager.Setup(m => m.GumProjectSave).Returns((GumProjectSave)null);

        _fileWatchLogic.RefreshRootDirectory();

        _dispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
        _fileWatchManager.Verify(m => m.Disable(), Times.Never);
        _fileWatchManager.Verify(m => m.ClearIgnoredFiles(), Times.Never);
    }

    [Fact]
    public void RefreshRootDirectory_CalledRepeatedlyBeforeScanRuns_CoalescesIntoOneScan()
    {
        // A ProjectLoad refresh followed by several IsFile VariableSet-triggered refreshes (e.g. a
        // multi-select edit) before the dispatcher gets around to running the first one should not
        // each re-scan the whole project.
        _projectManager.Setup(m => m.GumProjectSave).Returns((GumProjectSave)null);

        _fileWatchLogic.RefreshRootDirectory();
        _fileWatchLogic.RefreshRootDirectory();
        _fileWatchLogic.RefreshRootDirectory();

        _dispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);

        RunPostedAction();

        _fileWatchManager.Verify(m => m.Disable(), Times.Once);
    }

    [Fact]
    public void RefreshRootDirectory_WhenProjectUnloadedBeforeScanRuns_TakesNoProjectBranch()
    {
        // The project can be unloaded (or replaced by a newer one) in the gap between the request
        // and the dispatcher running it. Since GetFileWatchRootDirectories() only runs once the
        // posted action executes, it reads whatever project is current at that point rather than
        // acting on stale state captured when the refresh was first requested - this is what keeps
        // the deferred scan safe without any explicit staleness check (#4873).
        var project = new GumProjectSave { FullFileName = @"C:\Project\Project.gumx" };
        _projectManager.Setup(m => m.GumProjectSave).Returns(project);

        _fileWatchLogic.RefreshRootDirectory();

        // Project unloaded (or a different one loaded) before the dispatcher ran the posted scan.
        _projectManager.Setup(m => m.GumProjectSave).Returns((GumProjectSave)null);

        RunPostedAction();

        _fileWatchManager.Verify(m => m.Disable(), Times.Once);
        _fileWatchManager.Verify(m => m.EnableWithDirectories(It.IsAny<HashSet<FilePath>>()), Times.Never);
    }
}
