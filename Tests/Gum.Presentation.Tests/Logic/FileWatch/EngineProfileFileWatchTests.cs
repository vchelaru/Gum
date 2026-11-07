using System;
using System.Collections.Generic;
using System.IO;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests.Logic.FileWatch;

/// <summary>
/// An edit, rename or delete of the project's .gumengine is picked up without restarting the tool (#7720).
/// </summary>
public class EngineProfileFileWatchTests : BaseTestClass
{
    private readonly string _folder;
    private readonly string _profileFolder;
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager = new Mock<IWireframeObjectManager>();
    private readonly Mock<IProjectRestrictions> _restrictions = new Mock<IProjectRestrictions>();
    private readonly FileChangeReactionLogic _reaction;

    public EngineProfileFileWatchTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "gum-engine-watch-" + Guid.NewGuid().ToString("N"));
        _profileFolder = Path.Combine(_folder, "engine");
        Directory.CreateDirectory(Path.Combine(_folder, "game"));
        Directory.CreateDirectory(_profileFolder);

        _wireframeObjectManager.Setup(w => w.AllIpsos).Returns(new List<GraphicalUiElement>());
        _restrictions.Setup(r => r.ProfileFilePath).Returns(new FilePath(Path.Combine(_profileFolder, "Engine.gumengine")));

        _reaction = new FileChangeReactionLogic(
            Mock.Of<ISelectedState>(),
            Mock.Of<IWireframeCommands>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<IOutputManager>(),
            _wireframeObjectManager.Object,
            Mock.Of<IProjectState>(),
            Mock.Of<IStandardElementsManagerGumTool>(),
            Mock.Of<IPluginManager>(),
            Mock.Of<Gum.Services.Dialogs.IDialogService>(),
            new UnsavedChangesTracker(),
            Mock.Of<IVariableReferenceLogic>(),
            Mock.Of<IUndoManager>(),
            projectRestrictions: _restrictions.Object);
    }

    public override void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        base.Dispose();
    }

    [Fact]
    public void ReactToFileChanged_ShouldReloadRestrictionsAndRefreshCanvas_WhenTheProfileChanged()
    {
        _reaction.ReactToFileChanged(new FilePath(Path.Combine(_profileFolder, "Engine.gumengine")));

        _restrictions.Verify(r => r.Reload(), Times.Once);
        _wireframeObjectManager.Verify(w => w.RefreshAll(true, true), Times.Once);
    }

    [Fact]
    public void ReactToFileDeleted_ShouldReloadRestrictions_WhenTheProfileWasDeleted()
    {
        _reaction.ReactToFileDeleted(new FilePath(Path.Combine(_profileFolder, "Engine.gumengine")));

        _restrictions.Verify(r => r.Reload(), Times.Once);
    }

    [Fact]
    public void IsEngineProfile_ShouldBeFalse_ForAnyOtherFile()
    {
        _reaction.IsEngineProfile(new FilePath(Path.Combine(_profileFolder, "Other.gumengine"))).ShouldBeFalse();
        _reaction.IsEngineProfile(new FilePath(Path.Combine(_profileFolder, "Engine.gumengine"))).ShouldBeTrue();
    }

    [Fact]
    public void RefreshRootDirectory_ShouldWatchAProfileFolderOutsideTheProjectFolder()
    {
        WatchedDirectories watched = Scan(profileFolderExists: true);

        watched.Directories.ShouldContain(new FilePath(_profileFolder + "/"));
        watched.Directories.ShouldContain(new FilePath(Path.Combine(_folder, "game") + "/"));
    }

    [Fact]
    public void RefreshRootDirectory_ShouldNotCreateAMissingProfileFolder()
    {
        Directory.Delete(_profileFolder);

        WatchedDirectories watched = Scan(profileFolderExists: false);

        watched.Directories.ShouldNotContain(new FilePath(_profileFolder + "/"));
    }

    private sealed class WatchedDirectories
    {
        public HashSet<FilePath> Directories { get; set; } = new HashSet<FilePath>();
    }

    private WatchedDirectories Scan(bool profileFolderExists)
    {
        GumProjectSave project = new GumProjectSave { FullFileName = Path.Combine(_folder, "game", "Test.gumx") };
        ObjectFinder.Self.GumProjectSave = project;
        Mock<IProjectManager> projectManager = new Mock<IProjectManager>();
        projectManager.SetupGet(m => m.GumProjectSave).Returns(project);
        Mock<IProjectState> projectState = new Mock<IProjectState>();
        projectState.SetupGet(m => m.GumProjectSave).Returns(project);
        Mock<IFileWatchManager> fileWatchManager = new Mock<IFileWatchManager>();
        WatchedDirectories result = new WatchedDirectories();
        fileWatchManager
            .Setup(m => m.EnableWithDirectories(It.IsAny<HashSet<FilePath>>()))
            .Callback<HashSet<FilePath>>(directories => result.Directories = directories);
        Action? posted = null;
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(d => d.Post(It.IsAny<Action>())).Callback<Action>(action => posted = action);

        FileWatchLogic logic = new FileWatchLogic(
            fileWatchManager.Object, Mock.Of<IGuiCommands>(), projectState.Object, projectManager.Object,
            dispatcher.Object, _restrictions.Object);
        logic.RefreshRootDirectory();
        posted!.Invoke();
        return result;
    }
}
