using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.Settings;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using Shouldly;
using System;
using System.IO;
using System.Reflection;
using ToolsUtilities;

namespace GumToolUnitTests.Plugins.ImportFromGumxPlugin;

/// <summary>
/// Tests for GumxImportService conflict detection and asset gating.
/// Uses a real GumxSourceService backed by a temp directory, with
/// hand-rolled test doubles for IImportLogic, IProjectState, and IFileCommands.
/// </summary>
public class GumxImportServiceTests : IDisposable
{
    // ── temp directories ──────────────────────────────────────────────────
    private readonly string _projectDir;
    private readonly string _sourceDir;

    // ── fakes ─────────────────────────────────────────────────────────────
    private readonly FakeImportLogic _importLogic = new();
    private readonly FakeFileCommands _fileCommands = new();
    private readonly FakeProjectState _projectState;

    // ── system under test ─────────────────────────────────────────────────
    private readonly GumxSourceService _sourceService = new();
    private readonly GumxImportService _sut;

    public GumxImportServiceTests()
    {
        _projectDir = Path.Combine(Path.GetTempPath(), $"GumImportTests_{Guid.NewGuid():N}");
        _sourceDir  = Path.Combine(Path.GetTempPath(), $"GumSourceTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(_projectDir);
        Directory.CreateDirectory(_sourceDir);

        var gumProject = new GumProjectSave { FullFileName = Path.Combine(_projectDir, "Test.gumx") };
        _projectState  = new FakeProjectState(_projectDir, gumProject);

        _sut = new GumxImportService(_importLogic, _projectState, _fileCommands, _sourceService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_projectDir)) Directory.Delete(_projectDir, recursive: true);
        if (Directory.Exists(_sourceDir))  Directory.Delete(_sourceDir,  recursive: true);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    /// <summary>Writes minimal component XML to the source directory so the service can fetch it.</summary>
    private void WriteSourceComponent(string name)
    {
        string file = Path.Combine(_sourceDir, "Components", $"{name}.{GumProjectSave.ComponentExtension}");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, $"<ComponentSave><Name>{name}</Name></ComponentSave>");
    }

    private void WriteSourceScreen(string name)
    {
        string file = Path.Combine(_sourceDir, "Screens", $"{name}.{GumProjectSave.ScreenExtension}");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, $"<ScreenSave><Name>{name}</Name></ScreenSave>");
    }

    /// <summary>Creates a component with a single asset variable reference in its default state.</summary>
    private static ComponentSave ComponentWithAsset(string name, string assetRelativePath)
    {
        var variable = new VariableSave { Name = "Texture", Value = assetRelativePath };
        var defaultState = new StateSave { Name = "Default", Variables = new() { variable } };
        return new ComponentSave { Name = name, States = new() { defaultState } };
    }

    private static ComponentSave ComponentNoAssets(string name) =>
        new ComponentSave { Name = name, States = new() { new StateSave { Name = "Default", Variables = new() } } };

    private static ScreenSave ScreenNoAssets(string name) =>
        new ScreenSave { Name = name, States = new() { new StateSave { Name = "Default", Variables = new() } } };

    private static GumProjectSave SourceProject() => new GumProjectSave();

    // ── conflict detection ────────────────────────────────────────────────

    [Fact]
    public void ImportAsync_ComponentConflict_ReturnsConflictingNameAndWritesNothing()
    {
        // Arrange
        string componentName = "MyButton";
        string conflictingName = componentName; // no subfolder remapping

        // Pre-create the destination file to simulate a conflict
        string destComponentDir = Path.Combine(_projectDir, "Components");
        Directory.CreateDirectory(destComponentDir);
        File.WriteAllText(
            Path.Combine(destComponentDir, $"{conflictingName}.{GumProjectSave.ComponentExtension}"),
            "existing");

        WriteSourceComponent(componentName);

        var component = ComponentNoAssets(componentName);
        var source    = SourceProject();
        source.Components.Add(component);

        var selections = new ImportSelections
        {
            DirectComponents    = new() { component },
            TransitiveComponents = new(),
            DirectScreens       = new(),
            Behaviors           = new(),
            Standards           = new(),
        };

        // Act
        var result = _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "").GetAwaiter().GetResult();

        // Assert
        result.ConflictingElements.ShouldContain(conflictingName);
        result.SkippedElements.ShouldBeEmpty();
        _importLogic.ImportedComponentPaths.ShouldBeEmpty();
    }

    [Fact]
    public void ImportAsync_NoConflicts_ConflictingElementsIsEmpty()
    {
        // Arrange — no pre-existing destination files
        string componentName = "CleanButton";
        WriteSourceComponent(componentName);

        var component = ComponentNoAssets(componentName);
        var source    = SourceProject();
        source.Components.Add(component);

        var selections = new ImportSelections
        {
            DirectComponents     = new() { component },
            TransitiveComponents = new(),
            DirectScreens        = new(),
            Behaviors            = new(),
            Standards            = new(),
        };

        // Act
        var result = _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "").GetAwaiter().GetResult();

        // Assert
        result.ConflictingElements.ShouldBeEmpty();
    }

    [Fact]
    public void ImportAsync_ScreenConflict_ReturnsConflictingScreenName()
    {
        // Arrange
        string screenName = "GameScreen";

        string destScreenDir = Path.Combine(_projectDir, "Screens");
        Directory.CreateDirectory(destScreenDir);
        File.WriteAllText(
            Path.Combine(destScreenDir, $"{screenName}.{GumProjectSave.ScreenExtension}"),
            "existing");

        WriteSourceScreen(screenName);

        var screen = ScreenNoAssets(screenName);
        var source = SourceProject();
        source.Screens.Add(screen);

        var selections = new ImportSelections
        {
            DirectComponents     = new(),
            TransitiveComponents = new(),
            DirectScreens        = new() { screen },
            Behaviors            = new(),
            Standards            = new(),
        };

        // Act
        var result = _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "").GetAwaiter().GetResult();

        // Assert
        result.ConflictingElements.ShouldContain(screenName);
        _importLogic.ImportedScreenPaths.ShouldBeEmpty();
    }

    [Fact]
    public void ImportAsync_StandardConflict_StandardIsNotFlaggedAsConflict()
    {
        // Arrange — standards are intentionally overwritten, never flagged as conflicts
        string standardName = "Text";

        // Pre-create the destination standard file
        string destStandardsDir = Path.Combine(_projectDir, "Standards");
        Directory.CreateDirectory(destStandardsDir);
        File.WriteAllText(
            Path.Combine(destStandardsDir, $"{standardName}.{GumProjectSave.StandardExtension}"),
            "existing");

        // Also write the source standard file
        string srcStandardsDir = Path.Combine(_sourceDir, "Standards");
        Directory.CreateDirectory(srcStandardsDir);
        File.WriteAllText(
            Path.Combine(srcStandardsDir, $"{standardName}.{GumProjectSave.StandardExtension}"),
            $"<StandardElementSave><Name>{standardName}</Name></StandardElementSave>");

        var standard = new StandardElementSave
        {
            Name   = standardName,
            States = new() { new StateSave { Name = "Default", Variables = new() } }
        };
        var source = SourceProject();
        source.StandardElements.Add(standard);

        var selections = new ImportSelections
        {
            DirectComponents     = new(),
            TransitiveComponents = new(),
            DirectScreens        = new(),
            Behaviors            = new(),
            Standards            = new() { standard },
        };

        // Act
        var result = _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "").GetAwaiter().GetResult();

        // Assert
        result.ConflictingElements.ShouldBeEmpty();
    }

    // ── asset gating ──────────────────────────────────────────────────────

    [Fact]
    public void ImportAsync_AssetMissing_ElementIsSkippedAndNotWritten()
    {
        // Arrange
        string componentName  = "SpriteButton";
        string missingAsset   = "images/button.png";

        WriteSourceComponent(componentName);
        // Deliberately do NOT create the asset file in _sourceDir

        var component = ComponentWithAsset(componentName, missingAsset);
        var source    = SourceProject();
        source.Components.Add(component);

        var selections = new ImportSelections
        {
            DirectComponents     = new() { component },
            TransitiveComponents = new(),
            DirectScreens        = new(),
            Behaviors            = new(),
            Standards            = new(),
        };

        // Act
        var result = _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "").GetAwaiter().GetResult();

        // Assert
        result.SkippedElements.ShouldContain(componentName);
        result.ConflictingElements.ShouldBeEmpty();
        _importLogic.ImportedComponentPaths.ShouldBeEmpty();
    }

    [Fact]
    public void ImportAsync_AssetPresent_ElementIsImportedAndNotSkipped()
    {
        // Arrange
        string componentName  = "SpriteButton";
        string assetRelPath   = "images/button.png";

        WriteSourceComponent(componentName);

        // Create the asset file in the source directory
        string assetDir  = Path.Combine(_sourceDir, "images");
        Directory.CreateDirectory(assetDir);
        File.WriteAllBytes(Path.Combine(assetDir, "button.png"), new byte[] { 1, 2, 3 });

        var component = ComponentWithAsset(componentName, assetRelPath);
        var source    = SourceProject();
        source.Components.Add(component);

        var selections = new ImportSelections
        {
            DirectComponents     = new() { component },
            TransitiveComponents = new(),
            DirectScreens        = new(),
            Behaviors            = new(),
            Standards            = new(),
        };

        // Act
        var result = _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "").GetAwaiter().GetResult();

        // Assert
        result.SkippedElements.ShouldBeEmpty();
        result.ConflictingElements.ShouldBeEmpty();
        _importLogic.ImportedComponentPaths.ShouldNotBeEmpty();
    }

    // ── test doubles ──────────────────────────────────────────────────────

    private class FakeImportLogic : IImportLogic
    {
        public List<string> ImportedComponentPaths { get; } = new();
        public List<string> ImportedScreenPaths    { get; } = new();
        public List<string> ImportedBehaviorPaths  { get; } = new();

        public ComponentSave? ImportComponent(FilePath filePath, string desiredDirectory = null, bool saveProject = true)
        {
            ImportedComponentPaths.Add(filePath.FullPath);
            return new ComponentSave();
        }

        public ScreenSave? ImportScreen(FilePath filePath, string desiredDirectory = null, bool saveProject = true)
        {
            ImportedScreenPaths.Add(filePath.FullPath);
            return new ScreenSave();
        }

        public BehaviorSave ImportBehavior(FilePath filePath, string desiredDirectory = null, bool saveProject = false)
        {
            ImportedBehaviorPaths.Add(filePath.FullPath);
            return new BehaviorSave();
        }
    }

    private class FakeProjectState : IProjectState
    {
        public FakeProjectState(string projectDirectory, GumProjectSave gumProjectSave)
        {
            ProjectDirectory = projectDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            GumProjectSave   = gumProjectSave;
        }

        public GumProjectSave  GumProjectSave  { get; }
        public GeneralSettingsFile GeneralSettings => new GeneralSettingsFile();
        public string? ProjectDirectory { get; }
        public FilePath ComponentFilePath => new FilePath(Path.Combine(ProjectDirectory!, "Components"));
        public FilePath ScreenFilePath    => new FilePath(Path.Combine(ProjectDirectory!, "Screens"));
        public FilePath BehaviorFilePath  => new FilePath(Path.Combine(ProjectDirectory!, "Behaviors"));
        public bool NeedsToSaveProject    => false;
    }

    private class FakeFileCommands : IFileCommands
    {
        public FilePath? ProjectDirectory => null;

        public bool TryAutoSaveProject(bool forceSaveContainedElements = false) => false;
        public void LoadProject(string fileName) { }
        public void DeleteDirectory(FilePath filePath) { }
        public void MoveToRecycleBin(FilePath filePath) { }
        public string[] GetFiles(string path) => Array.Empty<string>();
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => Array.Empty<string>();
        public string ReadAllText(string path) => File.ReadAllText(path);
        public void MoveDirectory(string source, string destination) { }
        public void SaveEmbeddedResource(Assembly assembly, string resourceName, string targetFileName) { }
        public void TryAutoSaveCurrentObject() { }
        public void TryAutoSaveCurrentElement() { }
        public void TryAutoSaveElement(ElementSave elementSave) { }
        public void TryAutoSaveBehavior(BehaviorSave behavior) { }
        public void TryAutoSaveObject(object objectToSave) { }
        public void NewProject() { }
        public void ForceSaveProject(bool forceSaveContainedElements = false) { }
        public void ForceSaveElement(ElementSave element) { }
        public FilePath GetFullFileName(ElementSave element) => new FilePath(string.Empty);
        public void LoadLocalizationFile() { }
        public FilePath GetFullPathXmlFile(BehaviorSave behaviorSave) => new FilePath(string.Empty);
        public void SaveGeneralSettings() { }
        public void SaveIfDiffers(FilePath filePath, string contents) { }
    }
}
