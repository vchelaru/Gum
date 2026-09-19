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
using System.Threading.Tasks;
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
    public async Task ImportThemeAsync_SavesProjectAndReloadsIt_WhenNothingBlocksCopying()
    {
        _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new Dictionary<string, FilePath>());
        _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);

        bool result = await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

        result.ShouldBeTrue();
        _fileCommands.Verify(x => x.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
        _fileCommands.Verify(x => x.LoadProjectAsync("C:/project/Test.gumx"), Times.Once);
        _dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportThemeAsync_TellsUserToSaveManually_WhenAutoSaveFails()
    {
        _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new Dictionary<string, FilePath>());
        _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(false);

        await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

        _fileCommands.Verify(x => x.LoadProjectAsync(It.IsAny<string>()), Times.Never);
        _dialogService.Verify(
            x => x.ShowMessage("You must Save, then close/reopen the project.", null, null),
            Times.Once);
    }

    [Fact]
    public async Task ImportThemeAsync_ReturnsFalseWithoutCopying_WhenNonStandardFilesWouldBeOverwritten()
    {
        // An existing non-gutx/non-gumx destination file blocks the whole import.
        string existingFile = System.IO.Path.GetTempFileName();
        try
        {
            _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new Dictionary<string, FilePath> { ["source"] = existingFile });

            bool result = await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

            result.ShouldBeFalse();
            _fileCommands.Verify(x => x.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
        }
        finally
        {
            System.IO.File.Delete(existingFile);
        }
    }

    [Fact]
    public async Task ImportThemeAsync_ImportsComponentsAndBehaviorsBeforeScreens_RegardlessOfSourceOrder()
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

            await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: true);

            importOrder.ShouldBe(new[] { "behavior", "component", "screen" });
        }
        finally
        {
            System.IO.Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ImportThemeAsync_ProceedsWithoutBlocking_WhenExistingFileIsByteIdenticalToSource()
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

            bool result = await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

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

    [Fact]
    public async Task ImportThemeAsync_ConvertsStandardXmlToProjectFormat_WhenDestinationExtensionDiffersFromSource()
    {
        // #4710: for a .gumx project the raw byte copy of a theme's Standards IS the entire
        // mechanism that applies them (the final reload just re-reads the file from disk), but a
        // .gumj project's reload only ever resolves .gutj - FormsFileService now computes a .gutj
        // destination for a JSON project, so this pins that FormsThemeImporter actually converts the
        // content there instead of leaving XML bytes under a .gutj name.
        string tempRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FormsThemeImporterTests_" + System.Guid.NewGuid());
        string sourceDir = System.IO.Path.Combine(tempRoot, "source");
        string destDir = System.IO.Path.Combine(tempRoot, "dest");
        System.IO.Directory.CreateDirectory(sourceDir);
        try
        {
            _projectState.Setup(x => x.GumProjectSave)
                .Returns(new GumProjectSave { FullFileName = "C:/project/Test.gumj" });

            string sourceGutx = System.IO.Path.Combine(sourceDir, "Text.gutx");
            new StandardElementSave { Name = "Text" }.Save(sourceGutx, useCompactFormat: true);

            string destGutj = System.IO.Path.Combine(destDir, "Text.gutj");

            _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new Dictionary<string, FilePath> { [sourceGutx] = destGutj });
            _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);

            await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

            System.IO.File.Exists(destGutj).ShouldBeTrue();
            StandardElementSave loaded = ElementReference.DeserializeElement<StandardElementSave>(
                destGutj, GumProjectSave.NativeVersion);
            loaded.Name.ShouldBe("Text");
        }
        finally
        {
            System.IO.Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ImportThemeAsync_ConvertsComponentXmlAndStillRoutesThroughImportLogic_WhenDestinationExtensionDiffersFromSource()
    {
        string tempRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FormsThemeImporterTests_" + System.Guid.NewGuid());
        string sourceDir = System.IO.Path.Combine(tempRoot, "source");
        string destDir = System.IO.Path.Combine(tempRoot, "dest");
        System.IO.Directory.CreateDirectory(sourceDir);
        try
        {
            _projectState.Setup(x => x.GumProjectSave)
                .Returns(new GumProjectSave { FullFileName = "C:/project/Test.gumj" });

            string sourceComponent = System.IO.Path.Combine(sourceDir, "Button.gucx");
            new ComponentSave { Name = "Button" }.Save(sourceComponent, useCompactFormat: true);

            string destGucj = System.IO.Path.Combine(destDir, "Button.gucj");

            _importLogic.Setup(x => x.ImportComponent(It.IsAny<FilePath>(), null, false))
                .Returns((ComponentSave?)null);
            _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new Dictionary<string, FilePath> { [sourceComponent] = destGucj });
            _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);

            await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

            System.IO.File.Exists(destGucj).ShouldBeTrue();
            ComponentSave loaded = ElementReference.DeserializeElement<ComponentSave>(
                destGucj, GumProjectSave.NativeVersion);
            loaded.Name.ShouldBe("Button");

            // AddAllElementsToProject filters by the SOURCE file's (always XML) extension now that
            // the destination extension varies by project format - this proves that still resolves
            // to the converted .gucj FilePath, not the un-mapped .gucx one.
            _importLogic.Verify(
                x => x.ImportComponent(It.Is<FilePath>(fp => fp.FullPath == destGucj), null, false),
                Times.Once);
        }
        finally
        {
            System.IO.Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ImportThemeAsync_DoesNotBlockOnExistingStandard_WhenItsExtensionIsTheProjectsJsonFormat()
    {
        // GetIfShouldSave used to only recognize ".gutx" as a Standard file - for a .gumj project
        // the destination is now ".gutj", and treating it as an ordinary (blocking) file instead of
        // a Standard would make Add Forms refuse to run against any project that already has Forms
        // controls, since every Standard element reference already exists on disk (#4710).
        string existingGutj = System.IO.Path.GetTempFileName();
        string differentGutj = existingGutj + ".gutj";
        try
        {
            System.IO.File.WriteAllText(existingGutj, "existing content");
            System.IO.File.Move(existingGutj, differentGutj);

            _projectState.Setup(x => x.GumProjectSave)
                .Returns(new GumProjectSave { FullFileName = "C:/project/Test.gumj" });
            // RelativeTo needs a real, existing directory on this OS - differentGutj already sits
            // directly under the OS temp directory, so use that rather than a fake Windows-style
            // path (which broke FilePath's relative-path math on Linux CI - #4712).
            _projectState.Setup(x => x.ProjectDirectory).Returns(System.IO.Path.GetTempPath());
            _formsFileService.Setup(x => x.GetSourceDestinations(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new Dictionary<string, FilePath> { ["source"] = differentGutj });
            // ShowYesNoMessage is an extension over ShowMessage(...); an affirmative result is "Yes".
            _dialogService
                .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
                .Returns(MessageDialogResult.Affirmative);
            _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);

            bool result = await _importer.ImportThemeAsync("Standard", isIncludeDemoScreenGum: false);

            result.ShouldBeTrue();
            _dialogService.Verify(
                x => x.ShowMessage(
                    It.Is<string>(m => m.StartsWith("Cannot add Forms controls")),
                    It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
                Times.Never);
        }
        finally
        {
            System.IO.File.Delete(differentGutj);
        }
    }
}
