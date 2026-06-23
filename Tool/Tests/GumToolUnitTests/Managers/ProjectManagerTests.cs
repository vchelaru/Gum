using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.CommandLine;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Moq;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Managers;

public class ProjectManagerTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IMessenger> _messenger;
    private readonly Mock<IFileWatchManager> _fileWatchManager;
    private readonly Mock<IStandardElementsManagerGumTool> _standardElementsManagerGumTool;
    private readonly Mock<IRetryService> _retryService;
    private readonly Mock<ICommandLineManager> _commandLineManager;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly ProjectManager _projectManager;

    public ProjectManagerTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _elementCommands = new Mock<IElementCommands>();
        _dialogService = new Mock<IDialogService>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _messenger = new Mock<IMessenger>();
        _fileWatchManager = new Mock<IFileWatchManager>();
        _standardElementsManagerGumTool = new Mock<IStandardElementsManagerGumTool>();
        _retryService = new Mock<IRetryService>();
        _commandLineManager = new Mock<ICommandLineManager>();
        _pluginManager = new Mock<IPluginManager>();

        _projectManager = new ProjectManager(
            _selectedState.Object,
            new Lazy<IElementCommands>(() => _elementCommands.Object),
            _dialogService.Object,
            _guiCommands.Object,
            new Lazy<IFileCommands>(() => _fileCommands.Object),
            _messenger.Object,
            new Lazy<IFileWatchManager>(() => _fileWatchManager.Object),
            _standardElementsManagerGumTool.Object,
            _retryService.Object,
            new Lazy<ICommandLineManager>(() => _commandLineManager.Object),
            _pluginManager.Object);
    }

    [Fact]
    public void AskUserForProjectNameIfNecessary_ReturnsFalse_WhenFolderNotEmptyAndUserDeclines()
    {
        SetCurrentProject(new GumProjectSave());

        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "GumProjectManagerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(Path.Combine(tempDirectory, "existing.txt"), "not empty");

        try
        {
            _dialogService
                .Setup(d => d.SaveFile(It.IsAny<SaveFileDialogOptions?>()))
                .Returns(Path.Combine(tempDirectory, "NewProject.gumx"));
            _dialogService
                .Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
                .Returns(MessageDialogResult.Negative);

            bool shouldSave = _projectManager.AskUserForProjectNameIfNecessary(out bool isProjectNew);

            shouldSave.ShouldBeFalse();
            isProjectNew.ShouldBeFalse();
            _dialogService.Verify(
                d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void AskUserForProjectNameIfNecessary_ReturnsFalse_WhenSaveCancelled()
    {
        SetCurrentProject(new GumProjectSave());

        _dialogService
            .Setup(d => d.SaveFile(It.IsAny<SaveFileDialogOptions?>()))
            .Returns((string?)null);

        bool shouldSave = _projectManager.AskUserForProjectNameIfNecessary(out bool isProjectNew);

        shouldSave.ShouldBeFalse();
        isProjectNew.ShouldBeFalse();
    }

    [Fact]
    public void AskUserForProjectNameIfNecessary_ReturnsTrueWithoutPrompting_WhenProjectAlreadyNamed()
    {
        SetCurrentProject(new GumProjectSave { FullFileName = "c:/existing/Project.gumx" });

        bool shouldSave = _projectManager.AskUserForProjectNameIfNecessary(out bool isProjectNew);

        shouldSave.ShouldBeTrue();
        isProjectNew.ShouldBeFalse();
        _dialogService.Verify(d => d.SaveFile(It.IsAny<SaveFileDialogOptions?>()), Times.Never);
    }

    [Fact]
    public void LoadProject_LoadsAndReturnsTrue_WhenFileChosen()
    {
        string chosenFile = "c:/projects/MyGame.gumx";
        _dialogService
            .Setup(d => d.OpenFile(It.IsAny<OpenFileDialogOptions?>()))
            .Returns(new List<string> { chosenFile });

        bool result = _projectManager.LoadProject();

        result.ShouldBeTrue();
        _fileCommands.Verify(f => f.LoadProject(chosenFile), Times.Once);
    }

    [Fact]
    public void LoadProject_ReturnsFalse_WhenCancelled()
    {
        _dialogService
            .Setup(d => d.OpenFile(It.IsAny<OpenFileDialogOptions?>()))
            .Returns((List<string>?)null);

        bool result = _projectManager.LoadProject();

        result.ShouldBeFalse();
        _fileCommands.Verify(f => f.LoadProject(It.IsAny<string>()), Times.Never);
    }

    // ProjectManager exposes no setter for its current project (it is assigned only by
    // CreateNewProject / LoadProject, which do heavy I/O and plugin work unsuitable for a
    // unit test), so the private field is set directly to drive the save-name flow.
    private void SetCurrentProject(GumProjectSave? project)
    {
        typeof(ProjectManager)
            .GetField("_gumProjectSave", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_projectManager, project);
    }
}
