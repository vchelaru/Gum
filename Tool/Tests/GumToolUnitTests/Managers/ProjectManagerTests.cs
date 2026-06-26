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

    [Fact]
    public void RecreateMissingStandardElements_DoesNotCrash_AndInforms_ForMissingPluginStandard()
    {
        // Repro of #3373: clicking "Yes" to recreate a missing Skia standard (Arc) crashed with
        // KeyNotFoundException because Arc is not in StandardElementsManager's built-in defaults. The
        // tool must not offer a Yes/No it cannot honor; it leaves the element flagged and shows a
        // single informational message instead.
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Arc", IsSourceFileMissing = true });
        SetCurrentProject(project);

        // Simulate the user clicking "Yes" on any recreate prompt -- the pre-fix crash path.
        _dialogService
            .Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.Is<MessageDialogStyle?>(s => s != null)))
            .Returns(MessageDialogResult.Affirmative);

        Should.NotThrow(() => _projectManager.RecreateMissingStandardElements());

        // No Yes/No prompt (the only call with a non-null style) is offered for a plugin standard.
        _dialogService.Verify(
            d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.Is<MessageDialogStyle?>(s => s != null)),
            Times.Never);
        // A single informational message (null style) names the missing standard.
        _dialogService.Verify(
            d => d.ShowMessage(
                It.Is<string>(m => m.Contains("Arc")),
                It.IsAny<string?>(),
                It.Is<MessageDialogStyle?>(s => s == null)),
            Times.Once);
        // The element is left in place (still flagged missing) rather than removed.
        project.StandardElements.ShouldContain(e => e.Name == "Arc");
    }

    [Fact]
    public void RecreateMissingStandardElements_PromptsToRecreate_ForMissingBuiltInStandard()
    {
        // A built-in standard (Sprite) lives in StandardElementsManager's defaults, so the tool can
        // rebuild it and still offers the Yes/No prompt. Declining leaves the project untouched and
        // shows no plugin-standard informational message.
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Sprite", IsSourceFileMissing = true });
        SetCurrentProject(project);

        _dialogService
            .Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.Is<MessageDialogStyle?>(s => s != null)))
            .Returns(MessageDialogResult.Negative);

        _projectManager.RecreateMissingStandardElements();

        // The Yes/No recreate prompt (non-null style) is offered for a built-in standard...
        _dialogService.Verify(
            d => d.ShowMessage(
                It.Is<string>(m => m.Contains("Sprite")),
                It.IsAny<string?>(),
                It.Is<MessageDialogStyle?>(s => s != null)),
            Times.Once);
        // ...and no informational (null style) message is shown for it.
        _dialogService.Verify(
            d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.Is<MessageDialogStyle?>(s => s == null)),
            Times.Never);
    }

    // IDialogService.Show has an out parameter, which Moq can only populate through a
    // callback delegate whose signature mirrors the method; this delegate provides that shape.
    private delegate void ShowChoiceDialogCallback(
        Action<ChoiceDialogViewModel>? initializer,
        out ChoiceDialogViewModel viewModel);

    [Fact]
    public void ShowReadOnlyDialog_ShowsChoiceDialog_ThroughInjectedDialogService()
    {
        string fileName = "c:/projects/ReadOnly.gumx";
        ChoiceDialogViewModel shownDialog = new ChoiceDialogViewModel();

        _dialogService
            .Setup(d => d.Show(It.IsAny<Action<ChoiceDialogViewModel>>(), out It.Ref<ChoiceDialogViewModel>.IsAny))
            .Callback(new ShowChoiceDialogCallback(
                (Action<ChoiceDialogViewModel>? initializer, out ChoiceDialogViewModel viewModel) =>
                {
                    viewModel = shownDialog;
                    initializer?.Invoke(shownDialog);
                }))
            .Returns(false);

        _projectManager.ShowReadOnlyDialog(fileName);

        // The dialog is shown through the injected IDialogService (not a static Locator),
        // and its message names the offending file and explains that it is read-only.
        _dialogService.Verify(
            d => d.Show(It.IsAny<Action<ChoiceDialogViewModel>>(), out It.Ref<ChoiceDialogViewModel>.IsAny),
            Times.Once);
        shownDialog.Message.ShouldContain(fileName);
        shownDialog.Message.ShouldContain("read-only");
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
