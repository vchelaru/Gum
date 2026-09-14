using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
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

    [Fact]
    public void ImportTheme_ImportsComponentsAndBehaviorsBeforeScreens_RegardlessOfSourceOrder()
    {
        // A screen's own instances are typically Components. Each import triggers a synchronous
        // post-import error check that resolves those instances' BaseType via ObjectFinder, so a
        // screen imported before the component(s) it places throws (the component isn't registered
        // yet). Deliberately insert the screen entry FIRST in the dictionary to prove the importer
        // reorders by dependency instead of relying on source/enumeration order.
        string tempRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FormsThemeImporterTests_" + System.Guid.NewGuid());
        string sourceDir = System.IO.Path.Combine(tempRoot, "source");
        System.IO.Directory.CreateDirectory(sourceDir);
        string sourceScreen = System.IO.Path.Combine(sourceDir, "Demo.gusx");
        string sourceComponent = System.IO.Path.Combine(sourceDir, "Button.gucx");
        string sourceBehavior = System.IO.Path.Combine(sourceDir, "ButtonBehavior.behx");
        System.IO.File.WriteAllText(sourceScreen, "");
        System.IO.File.WriteAllText(sourceComponent, "");
        System.IO.File.WriteAllText(sourceBehavior, "");

        try
        {
            List<string> importOrder = new();
            _importLogic.Setup(x => x.ImportScreen(It.IsAny<FilePath>(), null, false))
                .Callback(() => importOrder.Add("screen"))
                .Returns((ScreenSave?)null);
            _importLogic.Setup(x => x.ImportComponent(It.IsAny<FilePath>(), null, false))
                .Callback(() => importOrder.Add("component"))
                .Returns((ComponentSave?)null);
            _importLogic.Setup(x => x.ImportBehavior(It.IsAny<FilePath>(), null, false))
                .Callback(() => importOrder.Add("behavior"))
                .Returns((BehaviorSave)null!);

            string destDir = System.IO.Path.Combine(tempRoot, "dest");
            // Deliberately insert the screen entry FIRST, so a pass proves the importer reorders
            // by dependency instead of relying on source/enumeration order.
            _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new Dictionary<string, FilePath>
                {
                    [sourceScreen] = System.IO.Path.Combine(destDir, "Demo.gusx"),
                    [sourceComponent] = System.IO.Path.Combine(destDir, "Button.gucx"),
                    [sourceBehavior] = System.IO.Path.Combine(destDir, "ButtonBehavior.behx"),
                });
            _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);

            _importer.ImportTheme("Standard", isIncludeDemoScreenGum: true);

            importOrder.ShouldBe(new[] { "behavior", "component", "screen" });
        }
        finally
        {
            System.IO.Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ImportTheme_ProceedsWithoutBlocking_WhenExistingFileIsByteIdenticalToSource()
    {
        // #4674/#4675 regression: the plain "File > New Project" font bundler and a theme's own
        // bundled Fonts/*.ttf can both write the identical file at the identical destination path
        // (e.g. Fonts/LiberationSans-Regular.ttf). Overwriting an already-identical file is a
        // no-op, not a real conflict, so it must not block the import the way a genuine content
        // difference does.
        string sourceFile = System.IO.Path.GetTempFileName();
        string destinationFile = System.IO.Path.GetTempFileName();
        try
        {
            byte[] bytes = { 1, 2, 3, 4, 5 };
            System.IO.File.WriteAllBytes(sourceFile, bytes);
            System.IO.File.WriteAllBytes(destinationFile, bytes);

            _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new Dictionary<string, FilePath> { [sourceFile] = destinationFile });
            _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);

            bool result = _importer.ImportTheme("Standard", isIncludeDemoScreenGum: false);

            result.ShouldBeTrue();
            _dialogService.Verify(
                x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
                Times.Never);
            _fileCommands.Verify(x => x.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
        }
        finally
        {
            System.IO.File.Delete(sourceFile);
            System.IO.File.Delete(destinationFile);
        }
    }
}
