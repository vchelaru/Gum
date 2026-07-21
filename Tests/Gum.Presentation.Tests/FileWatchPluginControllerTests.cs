using System.Collections.Generic;
using System.ComponentModel;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.FileWatchPlugin;
using Gum.ToolStates;
using Moq;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="FileWatchPluginController"/>'s behavior after its extraction from
/// <c>MainFileWatchPlugin</c> (issue #3931) — every method here used to read the plugin's own
/// private fields rather than take its dependencies as constructor parameters, which is what
/// blocked the extraction until now.
/// </summary>
public class FileWatchPluginControllerTests
{
    private static (FileWatchPluginController Controller, Mock<IFileWatchManager> FileWatchManager, FileWatchLogic FileWatchLogic, Mock<IProjectManager> ProjectManager)
        CreateSut()
    {
        var fileWatchManager = new Mock<IFileWatchManager>();
        var guiCommands = new Mock<IGuiCommands>();
        var projectState = new Mock<IProjectState>();
        var projectManager = new Mock<IProjectManager>();

        var fileWatchLogic = new FileWatchLogic(
            fileWatchManager.Object,
            guiCommands.Object,
            projectState.Object,
            projectManager.Object);

        var controller = new FileWatchPluginController(fileWatchManager.Object, fileWatchLogic);

        return (controller, fileWatchManager, fileWatchLogic, projectManager);
    }

    [Fact]
    public void HandleViewModelPropertyChanged_WhenPrintFileChangesToOutputChanges_SyncsValueToFileWatchManager()
    {
        var (controller, fileWatchManager, _, _) = CreateSut();
        var viewModel = new FileWatchViewModel { PrintFileChangesToOutput = true };

        controller.HandleViewModelPropertyChanged(
            viewModel, new PropertyChangedEventArgs(nameof(FileWatchViewModel.PrintFileChangesToOutput)));

        fileWatchManager.VerifySet(m => m.PrintFileChangesToOutput = true, Times.Once);
    }

    [Fact]
    public void HandleViewModelPropertyChanged_WhenUnrelatedPropertyChanges_DoesNotTouchFileWatchManager()
    {
        var (controller, fileWatchManager, _, _) = CreateSut();
        var viewModel = new FileWatchViewModel { PrintFileChangesToOutput = true };

        controller.HandleViewModelPropertyChanged(
            viewModel, new PropertyChangedEventArgs(nameof(FileWatchViewModel.WatchFolderInformation)));

        fileWatchManager.VerifySet(m => m.PrintFileChangesToOutput = It.IsAny<bool>(), Times.Never);
    }

    [Fact]
    public void HandleVariableSet_WhenChangedVariableIsFile_RefreshesRootDirectory()
    {
        var (controller, fileWatchManager, _, projectManager) = CreateSut();
        projectManager.Setup(m => m.GumProjectSave).Returns((GumProjectSave)null);
        var element = new ComponentSave();
        element.States.Add(new StateSave());
        element.DefaultState.Variables.Add(new VariableSave
        {
            Name = "SourceFile",
            IsCustomVariable = true,
            IsFile = true
        });

        controller.HandleVariableSet(element, instance: null, variableName: "SourceFile", oldValue: null);

        // RefreshRootDirectory -> no project loaded -> clears ignores and disables.
        fileWatchManager.Verify(m => m.Disable(), Times.Once);
    }

    [Fact]
    public void HandleVariableSet_WhenChangedVariableIsNotFile_DoesNotRefreshRootDirectory()
    {
        var (controller, fileWatchManager, _, _) = CreateSut();
        var element = new ComponentSave();
        element.States.Add(new StateSave());
        element.DefaultState.Variables.Add(new VariableSave
        {
            Name = "SomeVariable",
            IsCustomVariable = true,
            IsFile = false
        });

        controller.HandleVariableSet(element, instance: null, variableName: "SomeVariable", oldValue: null);

        fileWatchManager.Verify(m => m.Disable(), Times.Never);
        fileWatchManager.Verify(m => m.ClearIgnoredFiles(), Times.Never);
    }

    [Fact]
    public void HandleVariableSet_WhenElementIsNull_DoesNothing()
    {
        var (controller, fileWatchManager, _, _) = CreateSut();

        controller.HandleVariableSet(element: null, instance: null, variableName: "SourceFile", oldValue: null);

        fileWatchManager.Verify(m => m.Disable(), Times.Never);
        fileWatchManager.Verify(m => m.ClearIgnoredFiles(), Times.Never);
    }

    [Fact]
    public void HandleProjectLocationSet_RefreshesProjectLoadedState()
    {
        var (controller, fileWatchManager, _, projectManager) = CreateSut();
        projectManager.Setup(m => m.GumProjectSave).Returns((GumProjectSave)null);

        controller.HandleProjectLocationSet(new FilePath(@"C:\Project\Project.gumx"));

        // HandleProjectLoaded() clears ignores, then RefreshRootDirectory() (no project loaded)
        // clears them again before disabling.
        fileWatchManager.Verify(m => m.ClearIgnoredFiles(), Times.Exactly(2));
        fileWatchManager.Verify(m => m.Disable(), Times.Once);
    }

    [Fact]
    public void HandleProjectLoad_WhenSaveHasNoFullFileName_DisablesWatcher()
    {
        var (controller, fileWatchManager, _, _) = CreateSut();
        var save = new GumProjectSave();

        controller.HandleProjectLoad(save);

        fileWatchManager.Verify(m => m.Disable(), Times.Once);
    }

    [Fact]
    public void HandleProjectLoad_WhenSaveHasFullFileName_RefreshesRootDirectory()
    {
        var (controller, fileWatchManager, _, projectManager) = CreateSut();
        var save = new GumProjectSave { FullFileName = @"C:\Project\Project.gumx" };
        projectManager.Setup(m => m.GumProjectSave).Returns((GumProjectSave)null);

        controller.HandleProjectLoad(save);

        // save.FullFileName is set, so HandleProjectLoaded runs RefreshRootDirectory; the mocked
        // ProjectManager reports no loaded project, so it takes the "no project" branch (clears
        // ignores twice - once from HandleProjectLoaded, once from RefreshRootDirectory - then disables).
        fileWatchManager.Verify(m => m.ClearIgnoredFiles(), Times.Exactly(2));
        fileWatchManager.Verify(m => m.Disable(), Times.Once);
    }

    [Fact]
    public void RefreshDisplay_WhenNoFilesAreWatched_LeavesViewModelUntouched()
    {
        var (controller, fileWatchManager, _, _) = CreateSut();
        fileWatchManager.Setup(m => m.CurrentFilePathsWatching).Returns(new List<FilePath>());
        var viewModel = new FileWatchViewModel();

        controller.RefreshDisplay(viewModel);

        viewModel.WatchFolderInformation.ShouldBeNull();
    }

    [Fact]
    public void RefreshDisplay_WhenEnabled_ListsWatchedDirectoriesAndFlushCountdown()
    {
        var (controller, fileWatchManager, _, _) = CreateSut();
        fileWatchManager.Setup(m => m.CurrentFilePathsWatching)
            .Returns(new List<FilePath> { new FilePath(@"C:\Project\") });
        fileWatchManager.Setup(m => m.Enabled).Returns(true);
        fileWatchManager.Setup(m => m.ChangedFilesWaitingForFlush).Returns(new List<FilePath>());
        fileWatchManager.Setup(m => m.TimeToNextFlush).Returns(System.TimeSpan.FromSeconds(3.25));
        fileWatchManager.Setup(m => m.TimedChangesToIgnore)
            .Returns(new Dictionary<FilePath, System.DateTime>());
        var viewModel = new FileWatchViewModel();

        controller.RefreshDisplay(viewModel);

        viewModel.WatchFolderInformation.ShouldStartWith("File path(s) watching:");
        viewModel.TimeToNextFlush.ShouldBe("File flush in: 03:25");
        viewModel.NumberOfFilesToFlush.ShouldBe("0");
    }

    [Fact]
    public void RefreshDisplay_WhenDisabled_ReportsFileWatchingDisabled()
    {
        var (controller, fileWatchManager, _, _) = CreateSut();
        fileWatchManager.Setup(m => m.CurrentFilePathsWatching)
            .Returns(new List<FilePath> { new FilePath(@"C:\Project\") });
        fileWatchManager.Setup(m => m.Enabled).Returns(false);
        fileWatchManager.Setup(m => m.ChangedFilesWaitingForFlush).Returns(new List<FilePath>());
        fileWatchManager.Setup(m => m.TimeToNextFlush).Returns(System.TimeSpan.Zero);
        fileWatchManager.Setup(m => m.TimedChangesToIgnore)
            .Returns(new Dictionary<FilePath, System.DateTime>());
        var viewModel = new FileWatchViewModel();

        controller.RefreshDisplay(viewModel);

        viewModel.WatchFolderInformation.ShouldBe("File watching is disabled");
        viewModel.TimeToNextFlush.ShouldBe("Waiting for file change");
    }
}
