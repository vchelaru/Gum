using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

public class FileCommandsTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly FileCommands _fileCommands;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IProjectState> _projectState;
    private readonly Mock<IOutputManager> _outputManager;
    private readonly Mock<IRecycleBinService> _recycleBinService;
    private readonly LocalizationService _localizationService;
    private readonly UnsavedChangesTracker _unsavedChangesTracker;
    private readonly GumProjectSave _gumProject;
    private readonly List<string> _outputCalls;
    private readonly List<string> _errorCalls;
    private string? _tempDirectory;

    public FileCommandsTests()
    {
        _mocker = new AutoMocker();

        _localizationService = new LocalizationService();
        _mocker.Use<ILocalizationService>(_localizationService);

        _recycleBinService = _mocker.GetMock<IRecycleBinService>();

        _outputCalls = new List<string>();
        _errorCalls = new List<string>();
        _outputManager = _mocker.GetMock<IOutputManager>();
        _outputManager.Setup(o => o.AddOutput(It.IsAny<string>()))
            .Callback<string>(s => _outputCalls.Add(s));
        _outputManager.Setup(o => o.AddError(It.IsAny<string>()))
            .Callback<string>(s => _errorCalls.Add(s));

        _gumProject = new GumProjectSave();
        _projectManager = _mocker.GetMock<IProjectManager>();
        _projectManager.Setup(p => p.GumProjectSave).Returns(_gumProject);
        _projectState = _mocker.GetMock<IProjectState>();
        _projectState.Setup(p => p.GumProjectSave).Returns(_gumProject);
        _projectState.Setup(p => p.ProjectDirectory).Returns(() =>
            _tempDirectory == null ? null : _tempDirectory + Path.DirectorySeparatorChar);

        _mocker.Use<IPathCaseSensitivity>(new PathCaseSensitivity());
        _unsavedChangesTracker = new UnsavedChangesTracker();
        _mocker.Use<IUnsavedChangesTracker>(_unsavedChangesTracker);
        _fileCommands = _mocker.CreateInstance<FileCommands>();
    }

    public override void Dispose()
    {
        if (_tempDirectory != null && Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        base.Dispose();
    }

    [Fact]
    public void MoveToRecycleBin_ShouldDelegateToInjectedRecycleBinService()
    {
        FilePath filePath = @"C:\MyProject\Component.gucx";

        _fileCommands.MoveToRecycleBin(filePath);

        _recycleBinService.Verify(x => x.MoveToRecycleBin(filePath), Times.Once);
    }

    [Fact]
    public void MoveToRecycleBin_WithSeveralFiles_ShouldDelegateTheWholeBatchInOneCall()
    {
        List<FilePath> filePaths = new List<FilePath> { "/MyProject/Button.gucx", "/MyProject/Label.gucx" };

        _fileCommands.MoveToRecycleBin(filePaths);

        _recycleBinService.Verify(x => x.MoveToRecycleBin(filePaths), Times.Once);
    }

    [Fact]
    public void GetFullFileName_ShouldReturnPathUnderProjectDirectory()
    {
        // A real, OS-native temp directory is used here (rather than a hardcoded "C:\..." literal)
        // because FilePath's path-rooting logic (FileManager.IsRelative -> Path.IsPathRooted) is
        // platform-specific: a Windows-style drive-letter path is not rooted on macOS/Linux, so a
        // hardcoded literal computes a different (wrong) FullPath there.
        _tempDirectory = CreateTempDirectory();
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumx");
        ComponentSave component = new() { Name = "MyComponent" };

        FilePath result = _fileCommands.GetFullFileName(component);

        FilePath expectedDirectory = FileManager.GetDirectory(_gumProject.FullFileName);
        FilePath expected = expectedDirectory.Original + "Components\\MyComponent.gucx";
        result.FullPath.ShouldBe(expected.FullPath);
    }

    [Fact]
    public void GetFullPathXmlFile_ForBehavior_ShouldReturnBehaviorsSubfolderPath_WhenMatchingReferenceHasNoSourcePath()
    {
        _tempDirectory = CreateTempDirectory();
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumx");
        _gumProject.BehaviorReferences.Add(new BehaviorReference { Name = "ButtonBehavior" });
        BehaviorSave behavior = new() { Name = "ButtonBehavior" };

        FilePath result = _fileCommands.GetFullPathXmlFile(behavior);

        FilePath expectedDirectory = FileManager.GetDirectory(_gumProject.FullFileName);
        FilePath expected = expectedDirectory.Original + "Behaviors\\ButtonBehavior.behx";
        result.FullPath.ShouldBe(expected.FullPath);
    }

    [Fact]
    public void GetFullPathXmlFile_ForBehavior_ShouldResolveSourcePath_WhenMatchingReferenceHasSourcePathSet()
    {
        _tempDirectory = CreateTempDirectory();
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumx");
        _gumProject.BehaviorReferences.Add(new BehaviorReference
        {
            Name = "ButtonBehavior",
            SourcePath = "../SharedBehaviors/ButtonBehavior.behx"
        });
        BehaviorSave behavior = new() { Name = "ButtonBehavior" };

        FilePath result = _fileCommands.GetFullPathXmlFile(behavior);

        FilePath expectedDirectory = FileManager.GetDirectory(_gumProject.FullFileName);
        FilePath expected = expectedDirectory.Original + "../SharedBehaviors/ButtonBehavior.behx";
        result.FullPath.ShouldBe(expected.FullPath);
    }

    [Fact]
    public void GetFullPathXmlFile_ForBehavior_ShouldFallBackToBehaviorsSubfolderPath_WhenNoMatchingReferenceExists()
    {
        // A behavior can be saved before its BehaviorReference is added to the project
        // (e.g. right after creation) - must still fall back to the legacy convention.
        _tempDirectory = CreateTempDirectory();
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumx");
        BehaviorSave behavior = new() { Name = "NewlyCreatedBehavior" };

        FilePath result = _fileCommands.GetFullPathXmlFile(behavior);

        FilePath expectedDirectory = FileManager.GetDirectory(_gumProject.FullFileName);
        FilePath expected = expectedDirectory.Original + "Behaviors\\NewlyCreatedBehavior.behx";
        result.FullPath.ShouldBe(expected.FullPath);
    }

    [Fact]
    public void TryAutoSaveBehavior_ForLinkedBehavior_ShouldCaptureOverrideWithoutChangingSharedFile()
    {
        _tempDirectory = CreateTempDirectory();
        string projectDir = Path.Combine(_tempDirectory, "MyProject");
        Directory.CreateDirectory(projectDir);
        _gumProject.FullFileName = Path.Combine(projectDir, "MyProject.gumx");

        string sharedDir = Path.Combine(_tempDirectory, "SharedBehaviors");
        Directory.CreateDirectory(sharedDir);
        string sharedFilePath = Path.Combine(sharedDir, "ButtonBehavior.behx");
        new BehaviorSave { Name = "ButtonBehavior", DefaultImplementation = "Controls/ButtonStandard" }
            .Save(sharedFilePath, useCompactFormat: true);

        BehaviorReference reference = new()
        {
            Name = "ButtonBehavior",
            SourcePath = "../SharedBehaviors/ButtonBehavior.behx"
        };
        _gumProject.BehaviorReferences.Add(reference);

        _projectManager.Setup(p => p.AutoSave).Returns(true);
        bool isProjectNew = false;
        _projectManager.Setup(p => p.AskUserForProjectNameIfNecessary(out isProjectNew)).Returns(true);

        BehaviorSave behavior = new() { Name = "ButtonBehavior", DefaultImplementation = "Bubblegum/Controls/Button" };

        _fileCommands.TryAutoSaveBehavior(behavior);

        reference.DefaultImplementationOverride.ShouldBe("Bubblegum/Controls/Button");
        BehaviorSave sharedOnDisk = BehaviorReference.DeserializeBehavior(sharedFilePath, projectVersion: _gumProject.Version);
        sharedOnDisk.DefaultImplementation.ShouldBe("Controls/ButtonStandard");
        behavior.DefaultImplementation.ShouldBe("Bubblegum/Controls/Button");
        _projectManager.Verify(p => p.SaveProject(It.IsAny<bool>()), Times.AtLeastOnce);
    }

    [Fact]
    public void TryAutoSaveBehavior_ForNonLinkedBehavior_ShouldSaveDefaultImplementationDirectlyToItsOwnFile()
    {
        _tempDirectory = CreateTempDirectory();
        string projectDir = Path.Combine(_tempDirectory, "MyProject");
        Directory.CreateDirectory(projectDir);
        _gumProject.FullFileName = Path.Combine(projectDir, "MyProject.gumx");
        _gumProject.BehaviorReferences.Add(new BehaviorReference { Name = "ButtonBehavior" });

        _projectManager.Setup(p => p.AutoSave).Returns(true);
        bool isProjectNew = false;
        _projectManager.Setup(p => p.AskUserForProjectNameIfNecessary(out isProjectNew)).Returns(true);

        BehaviorSave behavior = new() { Name = "ButtonBehavior", DefaultImplementation = "Controls/ButtonStandard" };

        _fileCommands.TryAutoSaveBehavior(behavior);

        string ownFilePath = Path.Combine(projectDir, "Behaviors", "ButtonBehavior.behx");
        BehaviorSave onDisk = BehaviorReference.DeserializeBehavior(ownFilePath, projectVersion: _gumProject.Version);
        onDisk.DefaultImplementation.ShouldBe("Controls/ButtonStandard");
    }

    // Save All sorts every state's variables before writing; an auto-save of one element must write
    // the same bytes, or the next Save All reorders the file (a canvas drop appends its X and Y).
    [Fact]
    public void TryAutoSaveElement_WritesVariablesSortedByName_AsSaveAllDoes()
    {
        _tempDirectory = CreateTempDirectory();
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumx");
        _gumProject.ComponentReferences.Add(new ElementReference { Name = "Button", ElementType = ElementType.Component });
        _projectManager.Setup(p => p.AutoSave).Returns(true);
        bool isProjectNew = false;
        _projectManager.Setup(p => p.AskUserForProjectNameIfNecessary(out isProjectNew)).Returns(true);
        ComponentSave button = new() { Name = "Button", BaseType = "Container" };
        StateSave state = new() { Name = "Default", ParentContainer = button };
        button.States.Add(state);
        state.SetValue("Sprite.X", 60f, "float");
        state.SetValue("Background.X", 10f, "float");

        _fileCommands.TryAutoSaveElement(button);

        string saved = File.ReadAllText(Path.Combine(_tempDirectory, "Components", "Button.gucx"));
        saved.IndexOf("Background.X", StringComparison.Ordinal).ShouldBeLessThan(saved.IndexOf("Sprite.X", StringComparison.Ordinal));
    }

    [Fact]
    public void LoadLocalizationFile_ShouldClearPreviousDatabase_WhenSwitchingToAProjectWithNoLocalizationFiles()
    {
        _tempDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(_tempDirectory, "Strings.csv"), "StringId,English\nT_OK,OK\n");
        _gumProject.LocalizationFiles.Add("Strings.csv");
        _fileCommands.LoadLocalizationFile();
        _localizationService.HasDatabase.ShouldBeTrue();

        _gumProject.LocalizationFiles.Clear();
        _fileCommands.LoadLocalizationFile();

        _localizationService.HasDatabase.ShouldBeFalse();
        _localizationService.Translate("T_OK").ShouldBe("T_OK");
    }

    [Fact]
    public void LoadLocalizationFile_ShouldDoNothing_WhenLocalizationFilesIsEmpty()
    {
        bool raised = false;
        _fileCommands.LocalizationLoaded += () => raised = true;

        _fileCommands.LoadLocalizationFile();

        _outputCalls.Count.ShouldBe(0);
        _errorCalls.Count.ShouldBe(0);
        _localizationService.HasDatabase.ShouldBeFalse();
        raised.ShouldBeTrue();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldLoadResxSatellites_WhenPathUsesBackslashes()
    {
        _tempDirectory = CreateTempDirectory();
        Directory.CreateDirectory(Path.Combine(_tempDirectory, "Loc"));
        WriteResxFile(Path.Combine(_tempDirectory, "Loc", "Strings.resx"),
            new Dictionary<string, string> { { "T_OK", "OK" } });
        WriteResxFile(Path.Combine(_tempDirectory, "Loc", "Strings.es.resx"),
            new Dictionary<string, string> { { "T_OK", "Aceptar" } });
        _gumProject.LocalizationFiles.Add("Loc\\Strings.resx");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.ShouldBeEmpty();
        _localizationService.Languages.ShouldBe(new[] { "Default", "es" });
        _localizationService.CurrentLanguage = 2;
        _localizationService.Translate("T_OK").ShouldBe("Aceptar");
    }

    [Fact]
    public void LoadLocalizationFile_ShouldLoadSingleCsv_AndApplyTheProjectLanguage()
    {
        // The runtime's parser drops "//" comment rows and fills a short row's missing cells with the ID.
        _tempDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(_tempDirectory, "Strings.csv"),
            "String ID,English,Spanish\n// comment,x,y\nT_Hello,Hello\nT_Bye,Bye,Adios\n");
        _gumProject.LocalizationFiles.Add("Strings.csv");
        _gumProject.CurrentLanguageIndex = 2;

        _fileCommands.LoadLocalizationFile();

        _errorCalls.ShouldBeEmpty();
        _localizationService.CurrentLanguage.ShouldBe(2);
        _localizationService.Translate("T_Bye").ShouldBe("Adios");
        _localizationService.Translate("T_Hello").ShouldBe("T_Hello");
        _localizationService.Keys.ShouldNotContain("// comment");
    }

    [Fact]
    public void LoadLocalizationFile_ShouldRouteRepeatedCsvIdToOutputTab()
    {
        _tempDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(_tempDirectory, "Strings.csv"),
            "String ID,English\nT_Hello,First\nT_Hello,Second\n");
        _gumProject.LocalizationFiles.Add("Strings.csv");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.ShouldBeEmpty();
        _outputCalls.ShouldHaveSingleItem().ShouldContain("'T_Hello'");
    }

    [Fact]
    public void LoadLocalizationFile_ShouldTranslateEveryLanguage_InAThreeLanguageCsv()
    {
        _tempDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(_tempDirectory, "Strings.csv"),
            "StringId,English,Spanish,French\nT_Cancel,Cancel,Cancelar,Annuler\n");
        _gumProject.LocalizationFiles.Add("Strings.csv");

        _fileCommands.LoadLocalizationFile();

        _localizationService.Languages.ShouldBe(new[] { "English", "Spanish", "French" });
        _localizationService.CurrentLanguage = 0;
        _localizationService.Translate("T_Cancel").ShouldBe("T_Cancel");
        _localizationService.CurrentLanguage = 1;
        _localizationService.Translate("T_Cancel").ShouldBe("Cancel");
        _localizationService.CurrentLanguage = 2;
        _localizationService.Translate("T_Cancel").ShouldBe("Cancelar");
        _localizationService.CurrentLanguage = 3;
        _localizationService.Translate("T_Cancel").ShouldBe("Annuler");
    }

    [Fact]
    public void LoadLocalizationFile_ShouldShowADialogAndStillRaiseLoaded_WhenAFileCannotBeParsed()
    {
        _tempDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(_tempDirectory, "Strings.resx"), "this is not xml");
        _gumProject.LocalizationFiles.Add("Strings.resx");
        bool raised = false;
        _fileCommands.LocalizationLoaded += () => raised = true;

        _fileCommands.LoadLocalizationFile();

        _mocker.GetMock<IDialogService>().Verify(
            d => d.ShowMessage(It.Is<string>(m => m.Contains("Error loading localization file(s)")),
                It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()),
            Times.Once);
        raised.ShouldBeTrue();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldLoadMultipleResx_WhenMultipleResxEntries()
    {
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_OK", "OK" } });
        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.resx"),
            new Dictionary<string, string> { { "B_Save", "Save" } });
        _gumProject.LocalizationFiles.Add("Strings.resx");
        _gumProject.LocalizationFiles.Add("Buttons.resx");

        _fileCommands.LoadLocalizationFile();

        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.CurrentLanguage = 1;
        _localizationService.Translate("T_OK").ShouldBe("OK");
        _localizationService.Translate("B_Save").ShouldBe("Save");
        _errorCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void LoadLocalizationFile_ShouldLoadSingleResx_WhenOneResxEntry()
    {
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_OK", "OK" } });
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"),
            new Dictionary<string, string> { { "T_OK", "Aceptar" } });
        _gumProject.LocalizationFiles.Add("Strings.resx");

        _fileCommands.LoadLocalizationFile();

        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.Languages.Count.ShouldBe(2);
        _localizationService.CurrentLanguage = 2;
        _localizationService.Translate("T_OK").ShouldBe("Aceptar");
        _errorCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void LoadLocalizationFile_ShouldReportError_WhenMixingCsvAndResx()
    {
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_OK", "OK" } });
        File.WriteAllText(Path.Combine(_tempDirectory, "Strings.csv"),
            "StringId,English\nT_OK,OK\n");
        _gumProject.LocalizationFiles.Add("Strings.resx");
        _gumProject.LocalizationFiles.Add("Strings.csv");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _errorCalls[0].ShouldContain("not all are .resx");
        _localizationService.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldReportError_WhenMultipleCsvEntries()
    {
        _tempDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(_tempDirectory, "A.csv"), "StringId,English\nT_OK,OK\n");
        File.WriteAllText(Path.Combine(_tempDirectory, "B.csv"), "StringId,English\nT_Cancel,Cancel\n");
        _gumProject.LocalizationFiles.Add("A.csv");
        _gumProject.LocalizationFiles.Add("B.csv");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _localizationService.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldRouteCollisionToOutputTab_WhenMultipleResxFilesDefineSameKey()
    {
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_Shared", "FromStrings" } });
        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.resx"),
            new Dictionary<string, string> { { "T_Shared", "FromButtons" } });
        _gumProject.LocalizationFiles.Add("Strings.resx");
        _gumProject.LocalizationFiles.Add("Buttons.resx");

        _fileCommands.LoadLocalizationFile();

        _outputCalls.Count.ShouldBe(1);
        _outputCalls[0].ShouldContain("T_Shared");
        _outputCalls[0].ShouldContain("Strings.resx");
        _outputCalls[0].ShouldContain("Buttons.resx");
    }

    [Fact]
    public void LoadLocalizationFile_ShouldReportError_WhenSingleCsvFileMissing()
    {
        _tempDirectory = CreateTempDirectory();
        _gumProject.LocalizationFiles.Add("DoesNotExist.csv");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _errorCalls[0].ShouldContain("DoesNotExist.csv");
        _localizationService.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldReportError_WhenSingleResxFileMissing()
    {
        // Single-RESX now flows through the multi-file path, so a missing file is reported
        // via AddError rather than silently no-opping (prior behavior).
        _tempDirectory = CreateTempDirectory();
        _gumProject.LocalizationFiles.Add("DoesNotExist.resx");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _errorCalls[0].ShouldContain("DoesNotExist.resx");
        _localizationService.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldWarnAndSkip_WhenBaseFileMissing()
    {
        // Production behavior: in the multi-file branch, missing files are reported via
        // AddError (not AddOutput), and remaining existing files still load.
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_OK", "OK" } });
        _gumProject.LocalizationFiles.Add("Strings.resx");
        _gumProject.LocalizationFiles.Add("DoesNotExist.resx");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _errorCalls[0].ShouldContain("DoesNotExist.resx");
        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.CurrentLanguage = 1;
        _localizationService.Translate("T_OK").ShouldBe("OK");
    }

    // Pins a second half of the case-only-rename bug (see RenameFolderDialogViewModelTests):
    // even once the "already exists" guard is fixed, MoveDirectory's manual recursive
    // copy-then-delete-source approach fails for a rename that only changes casing, because
    // source and destination are the same physical directory on a case-insensitive filesystem
    // (Windows) - CreateDirectory(destination) is a no-op, the files "move" to themselves, and
    // Directory.Delete(source) then throws because the directory is (still) not empty.
    [Fact]
    public void MoveDirectory_WhenSourceAndDestinationDifferOnlyByCase_ShouldRenameDirectoryOnDisk()
    {
        _tempDirectory = CreateTempDirectory();
        string oldDir = Path.Combine(_tempDirectory, "GameMenuScreens");
        Directory.CreateDirectory(oldDir);
        File.WriteAllText(Path.Combine(oldDir, "DialogueScreen.gusx"), "content");

        string newDir = Path.Combine(_tempDirectory, "gamemenuscreens");

        _fileCommands.MoveDirectory(oldDir, newDir);

        Directory.GetDirectories(_tempDirectory)
            .Select(Path.GetFileName)
            .ShouldBe(new[] { "gamemenuscreens" });
        File.Exists(Path.Combine(newDir, "DialogueScreen.gusx")).ShouldBeTrue();
    }

    // Where the file system keeps Foo and foo apart, moving Foo to foo is a move into another
    // folder, which merges the same way as a move to any other existing folder. Returns early
    // where no case-sensitive directory can be made (a default macOS volume).
    [Fact]
    public void MoveDirectory_WhenACaseOnlyDifferentDestinationIsASeparateFolder_ShouldMergeIntoIt()
    {
        _tempDirectory = CaseSensitiveTempDirectory.TryCreate();
        if (_tempDirectory == null)
        {
            return;
        }
        string source = Path.Combine(_tempDirectory, "Foo");
        string destination = Path.Combine(_tempDirectory, "foo");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(source, "A.gusx"), "a");
        File.WriteAllText(Path.Combine(destination, "B.gusx"), "b");

        _fileCommands.MoveDirectory(source, destination);

        Directory.GetDirectories(_tempDirectory).Select(Path.GetFileName).ShouldBe(new[] { "foo" });
        Directory.GetFiles(destination).Select(Path.GetFileName).OrderBy(name => name)
            .ShouldBe(new[] { "A.gusx", "B.gusx" });
    }

    [Fact]
    public void GetFullFileName_ShouldReturnJsonExtension_WhenProjectIsJsonFormat()
    {
        _tempDirectory = CreateTempDirectory();
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumj");
        ComponentSave component = new() { Name = "MyComponent" };

        FilePath result = _fileCommands.GetFullFileName(component);

        FilePath expectedDirectory = FileManager.GetDirectory(_gumProject.FullFileName);
        FilePath expected = expectedDirectory.Original + "Components\\MyComponent.gucj";
        result.FullPath.ShouldBe(expected.FullPath);
    }

    [Fact]
    public void GetFullPathXmlFile_ForBehavior_ShouldReturnJsonExtension_WhenProjectIsJsonFormat()
    {
        _tempDirectory = CreateTempDirectory();
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumj");
        _gumProject.BehaviorReferences.Add(new BehaviorReference { Name = "ButtonBehavior" });
        BehaviorSave behavior = new() { Name = "ButtonBehavior" };

        FilePath result = _fileCommands.GetFullPathXmlFile(behavior);

        FilePath expectedDirectory = FileManager.GetDirectory(_gumProject.FullFileName);
        FilePath expected = expectedDirectory.Original + "Behaviors\\ButtonBehavior.behj";
        result.FullPath.ShouldBe(expected.FullPath);
    }

    [Fact]
    public void ForceSaveElement_ShouldWriteJsonFile_WhenProjectIsJsonFormat()
    {
        _tempDirectory = CreateTempDirectory();
        Directory.CreateDirectory(Path.Combine(_tempDirectory, "Components"));
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumj");

        ComponentSave component = new() { Name = "MyComponent" };
        component.States.Add(new Gum.DataTypes.Variables.StateSave { Name = "Default" });
        _gumProject.Components.Add(component);
        _gumProject.ComponentReferences.Add(new ElementReference
        {
            Name = "MyComponent",
            ElementType = ElementType.Component
        });

        bool isNew;
        _projectManager.Setup(p => p.AskUserForProjectNameIfNecessary(out isNew)).Returns(true);

        _fileCommands.ForceSaveElement(component);

        string jsonPath = Path.Combine(_tempDirectory, "Components", "MyComponent.gucj");
        string xmlPath = Path.Combine(_tempDirectory, "Components", "MyComponent.gucx");

        File.Exists(xmlPath).ShouldBeFalse("An XML .gucx file must NOT be written for a .gumj project");
        File.Exists(jsonPath).ShouldBeTrue("The component must be saved as .gucj");
    }

    [Fact]
    public void TryAutoSaveElement_ShouldLeaveTheElementUnsaved_WhenAutoSaveIsOff_UntilItIsSaved()
    {
        _tempDirectory = CreateTempDirectory();
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumx");
        _gumProject.ComponentReferences.Add(new ElementReference { Name = "Button", ElementType = ElementType.Component });
        _projectManager.Setup(p => p.AutoSave).Returns(false);
        bool isProjectNew = false;
        _projectManager.Setup(p => p.AskUserForProjectNameIfNecessary(out isProjectNew)).Returns(true);
        ComponentSave button = new() { Name = "Button", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });

        _fileCommands.TryAutoSaveElement(button);

        File.Exists(Path.Combine(_tempDirectory, "Components", "Button.gucx")).ShouldBeFalse();
        _unsavedChangesTracker.HasUnsavedChanges(button).ShouldBeTrue();

        _fileCommands.ForceSaveElement(button);

        _unsavedChangesTracker.HasUnsavedChanges(button).ShouldBeFalse();
    }

    [Fact]
    public void TryAutoSaveElement_ShouldLeaveTheElementUnsaved_WhenAutoSaveIsOnButItsFileIsReadOnly()
    {
        _tempDirectory = CreateTempDirectory();
        _gumProject.FullFileName = Path.Combine(_tempDirectory, "MyProject.gumx");
        _gumProject.ComponentReferences.Add(new ElementReference { Name = "Button", ElementType = ElementType.Component });
        _projectManager.Setup(p => p.AutoSave).Returns(true);
        bool isProjectNew = false;
        _projectManager.Setup(p => p.AskUserForProjectNameIfNecessary(out isProjectNew)).Returns(true);
        ComponentSave button = new() { Name = "Button", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        string buttonFile = Path.Combine(_tempDirectory, "Components", "Button.gucx");
        Directory.CreateDirectory(Path.GetDirectoryName(buttonFile)!);
        File.WriteAllText(buttonFile, "on disk");
        File.SetAttributes(buttonFile, FileAttributes.ReadOnly);
        try
        {
            _fileCommands.TryAutoSaveElement(button);

            _projectManager.Verify(p => p.ShowReadOnlyDialog(It.IsAny<string>()), Times.Once);
            _unsavedChangesTracker.HasUnsavedChanges(button).ShouldBeTrue();
            _outputCalls.ShouldNotContain(output => output.StartsWith("Saved "));
        }
        finally
        {
            File.SetAttributes(buttonFile, FileAttributes.Normal);
        }
    }

    #region Helpers

    private static string CreateResxContent(Dictionary<string, string> entries)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<root>");
        sb.AppendLine("  <resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader>");
        sb.AppendLine("  <resheader name=\"version\"><value>2.0</value></resheader>");

        foreach (KeyValuePair<string, string> entry in entries)
        {
            sb.AppendLine($"  <data name=\"{entry.Key}\" xml:space=\"preserve\">");
            sb.AppendLine($"    <value>{entry.Value}</value>");
            sb.AppendLine("  </data>");
        }

        sb.AppendLine("</root>");
        return sb.ToString();
    }

    private static string CreateTempDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(),
            "GumFileCommandsTests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void WriteResxFile(string filePath, Dictionary<string, string> entries)
    {
        File.WriteAllText(filePath, CreateResxContent(entries));
    }

    #endregion
}
