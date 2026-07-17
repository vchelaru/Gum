using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// AddFormsViewModel had no test coverage before this file. Added alongside extracting
/// IFormsFileService (which unblocked the VM's move into the headless Gum.Presentation assembly,
/// ADR-0005 Phase 3, #3754) so DefaultThemeName went from a static const on the concrete
/// FormsFileService to an instance member reachable through the interface.
/// </summary>
public class AddFormsViewModelTests
{
    private readonly Mock<IFormsFileService> _formsFileService;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IImportLogic> _importLogic;
    private readonly Mock<IProjectState> _projectState;
    private readonly Mock<IFileWatchManager> _fileWatchManager;
    private readonly Mock<ISkiaShapeStandardsLogic> _skiaShapeStandards;

    public AddFormsViewModelTests()
    {
        _formsFileService = new Mock<IFormsFileService>();
        _dialogService = new Mock<IDialogService>();
        _fileCommands = new Mock<IFileCommands>();
        _importLogic = new Mock<IImportLogic>();
        _projectState = new Mock<IProjectState>();
        _fileWatchManager = new Mock<IFileWatchManager>();
        _skiaShapeStandards = new Mock<ISkiaShapeStandardsLogic>();

        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
    }

    private AddFormsViewModel CreateSut() => new(
        _formsFileService.Object,
        _dialogService.Object,
        _fileCommands.Object,
        _importLogic.Object,
        _projectState.Object,
        _fileWatchManager.Object,
        _skiaShapeStandards.Object);

    [Fact]
    public void Constructor_SelectsDefaultTheme_WhenPresentAmongAvailableThemes()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Bubblegum", "Standard" });

        AddFormsViewModel sut = CreateSut();

        sut.SelectedTheme.ShouldBe("Standard");
    }

    [Fact]
    public void Constructor_FallsBackToFirstAvailableTheme_WhenDefaultThemeIsNotPresent()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Bubblegum" });

        AddFormsViewModel sut = CreateSut();

        sut.SelectedTheme.ShouldBe("Bubblegum");
    }

    [Fact]
    public void OnAffirmative_SavesProjectAndReloadsIt_WhenNothingBlocksCopying()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes())
            .Returns(new List<string> { "Standard" });
        _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new Dictionary<string, FilePath>());
        _projectState.Setup(x => x.GumProjectSave)
            .Returns(new GumProjectSave { FullFileName = "C:/project/Test.gumx" });
        _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);

        AddFormsViewModel sut = CreateSut();
        bool? affirmativeResult = null;
        sut.RequestClose += (_, e) => affirmativeResult = e;

        sut.OnAffirmative();

        _fileCommands.Verify(x => x.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
        _fileCommands.Verify(x => x.LoadProject("C:/project/Test.gumx"), Times.Once);
        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()), Times.Never);
        affirmativeResult.ShouldBe(true);
    }
}
