using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// The theme copy/import previously lived in AddFormsViewModel.OnAffirmative. It moved here so
/// new-project creation can import the default theme without showing the Add Forms dialog.
/// </summary>
public class FormsThemeImporterTests
{
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IImportLogic> _importLogic = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IFileWatchManager> _fileWatchManager = new();
    private readonly Mock<ISkiaShapeStandardsLogic> _skiaShapeStandards = new();
    private readonly FormsThemeImporter _importer;

    public FormsThemeImporterTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _projectState.Setup(x => x.GumProjectSave)
            .Returns(new GumProjectSave { FullFileName = "C:/project/Test.gumx" });

        _importer = new FormsThemeImporter(
            _formsFileService.Object,
            _dialogService.Object,
            _fileCommands.Object,
            _importLogic.Object,
            _projectState.Object,
            _fileWatchManager.Object,
            _skiaShapeStandards.Object);
    }

    [Fact]
    public void ImportTheme_SavesProjectAndReloadsIt_WhenNothingBlocksCopying()
    {
        _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new Dictionary<string, FilePath>());
        _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);

        bool result = _importer.ImportTheme("Standard", isIncludeDemoScreenGum: false);

        result.ShouldBeTrue();
        _fileCommands.Verify(x => x.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
        _fileCommands.Verify(x => x.LoadProject("C:/project/Test.gumx"), Times.Once);
        _dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Never);
    }

    [Fact]
    public void ImportTheme_TellsUserToSaveManually_WhenAutoSaveFails()
    {
        _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new Dictionary<string, FilePath>());
        _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(false);

        _importer.ImportTheme("Standard", isIncludeDemoScreenGum: false);

        _fileCommands.Verify(x => x.LoadProject(It.IsAny<string>()), Times.Never);
        _dialogService.Verify(
            x => x.ShowMessage("You must Save, then close/reopen the project.", null, null),
            Times.Once);
    }

    [Fact]
    public void ImportTheme_ReturnsFalseWithoutCopying_WhenNonStandardFilesWouldBeOverwritten()
    {
        // An existing non-gutx/non-gumx destination file blocks the whole import.
        string existingFile = System.IO.Path.GetTempFileName();
        try
        {
            _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new Dictionary<string, FilePath> { ["source"] = existingFile });

            bool result = _importer.ImportTheme("Standard", isIncludeDemoScreenGum: false);

            result.ShouldBeFalse();
            _fileCommands.Verify(x => x.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
        }
        finally
        {
            System.IO.File.Delete(existingFile);
        }
    }
}
