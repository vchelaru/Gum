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
using System.Threading.Tasks;
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

    // ── helpers for writing source behaviors ──────────────────────────────

    private void WriteSourceBehavior(string name)
    {
        string file = Path.Combine(_sourceDir, "Behaviors", $"{name}.{BehaviorReference.Extension}");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, $"<BehaviorSave><Name>{name}</Name></BehaviorSave>");
    }

    // ── conflict detection ────────────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_ComponentConflict_ReturnsConflictingNameAndWritesNothing()
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

        ComponentSave component = ComponentNoAssets(componentName);
        GumProjectSave source   = SourceProject();
        source.Components.Add(component);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents    = new() { component },
            TransitiveComponents = new(),
            DirectScreens       = new(),
            Behaviors           = new(),
            Standards           = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "");

        // Assert
        result.ConflictingElements.ShouldContain(conflictingName);
        result.SkippedElements.ShouldBeEmpty();
        _importLogic.ImportedComponentPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_NoConflicts_ConflictingElementsIsEmpty()
    {
        // Arrange — no pre-existing destination files
        string componentName = "CleanButton";
        WriteSourceComponent(componentName);

        ComponentSave component = ComponentNoAssets(componentName);
        GumProjectSave source   = SourceProject();
        source.Components.Add(component);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents     = new() { component },
            TransitiveComponents = new(),
            DirectScreens        = new(),
            Behaviors            = new(),
            Standards            = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "");

        // Assert
        result.ConflictingElements.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_ScreenConflict_ReturnsConflictingScreenName()
    {
        // Arrange
        string screenName = "GameScreen";

        string destScreenDir = Path.Combine(_projectDir, "Screens");
        Directory.CreateDirectory(destScreenDir);
        File.WriteAllText(
            Path.Combine(destScreenDir, $"{screenName}.{GumProjectSave.ScreenExtension}"),
            "existing");

        WriteSourceScreen(screenName);

        ScreenSave screen      = ScreenNoAssets(screenName);
        GumProjectSave source  = SourceProject();
        source.Screens.Add(screen);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents     = new(),
            TransitiveComponents = new(),
            DirectScreens        = new() { screen },
            Behaviors            = new(),
            Standards            = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "");

        // Assert
        result.ConflictingElements.ShouldContain(screenName);
        _importLogic.ImportedScreenPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_StandardConflict_StandardIsNotFlaggedAsConflict()
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

        StandardElementSave standard = new StandardElementSave
        {
            Name   = standardName,
            States = new() { new StateSave { Name = "Default", Variables = new() } }
        };
        GumProjectSave source = SourceProject();
        source.StandardElements.Add(standard);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents     = new(),
            TransitiveComponents = new(),
            DirectScreens        = new(),
            Behaviors            = new(),
            Standards            = new() { standard },
        };

        // Act
        ImportResult result = await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "");

        // Assert
        result.ConflictingElements.ShouldBeEmpty();
    }

    // ── asset gating ──────────────────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_AssetMissing_ElementIsSkippedAndNotWritten()
    {
        // Arrange
        string componentName  = "SpriteButton";
        string missingAsset   = "images/button.png";

        WriteSourceComponent(componentName);
        // Deliberately do NOT create the asset file in _sourceDir

        ComponentSave component = ComponentWithAsset(componentName, missingAsset);
        GumProjectSave source   = SourceProject();
        source.Components.Add(component);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents     = new() { component },
            TransitiveComponents = new(),
            DirectScreens        = new(),
            Behaviors            = new(),
            Standards            = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "");

        // Assert
        result.SkippedElements.ShouldContain(componentName);
        result.ConflictingElements.ShouldBeEmpty();
        _importLogic.ImportedComponentPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_AssetPresent_ElementIsImportedAndNotSkipped()
    {
        // Arrange
        string componentName  = "SpriteButton";
        string assetRelPath   = "images/button.png";

        WriteSourceComponent(componentName);

        // Create the asset file in the source directory
        string assetDir  = Path.Combine(_sourceDir, "images");
        Directory.CreateDirectory(assetDir);
        File.WriteAllBytes(Path.Combine(assetDir, "button.png"), new byte[] { 1, 2, 3 });

        ComponentSave component = ComponentWithAsset(componentName, assetRelPath);
        GumProjectSave source   = SourceProject();
        source.Components.Add(component);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents     = new() { component },
            TransitiveComponents = new(),
            DirectScreens        = new(),
            Behaviors            = new(),
            Standards            = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "");

        // Assert
        result.SkippedElements.ShouldBeEmpty();
        result.ConflictingElements.ShouldBeEmpty();
        _importLogic.ImportedComponentPaths.ShouldNotBeEmpty();
    }

    // ── conflict resolution: Skip ─────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_SkipResolution_ConflictingComponentLeftUntouched_NonConflictingImported()
    {
        // Arrange — one conflicting component (already on disk), one new
        string conflictingName = "ExistingButton";
        string newName = "NewButton";

        string destComponentDir = Path.Combine(_projectDir, "Components");
        Directory.CreateDirectory(destComponentDir);
        string conflictingDestPath = Path.Combine(destComponentDir, $"{conflictingName}.{GumProjectSave.ComponentExtension}");
        const string originalContent = "<ComponentSave><Name>ExistingButton</Name><Marker>original</Marker></ComponentSave>";
        File.WriteAllText(conflictingDestPath, originalContent);

        WriteSourceComponent(conflictingName);
        WriteSourceComponent(newName);

        ComponentSave conflictingComponent = ComponentNoAssets(conflictingName);
        ComponentSave newComponent = ComponentNoAssets(newName);
        GumProjectSave source = SourceProject();
        source.Components.Add(conflictingComponent);
        source.Components.Add(newComponent);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new() { conflictingComponent, newComponent },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: "",
            conflictResolution: ConflictResolution.Skip);

        // Assert — conflicting file unchanged on disk, new file written, no cancellation
        result.ConflictingElements.ShouldBeEmpty();
        File.ReadAllText(conflictingDestPath).ShouldBe(originalContent);

        string newDestPath = Path.Combine(destComponentDir, $"{newName}.{GumProjectSave.ComponentExtension}");
        File.Exists(newDestPath).ShouldBeTrue();

        // ImportLogic was called only for the new component
        _importLogic.ImportedComponentPaths.Count.ShouldBe(1);
        _importLogic.ImportedComponentPaths[0].ShouldEndWith($"{newName}.{GumProjectSave.ComponentExtension}");
    }

    [Fact]
    public async Task ImportAsync_SkipResolution_ConflictingBehavior_NotWritten()
    {
        // Arrange
        string conflictingName = "ExistingBehavior";

        string destBehaviorDir = Path.Combine(_projectDir, "Behaviors");
        Directory.CreateDirectory(destBehaviorDir);
        string destPath = Path.Combine(destBehaviorDir, $"{conflictingName}.{BehaviorReference.Extension}");
        const string originalContent = "<BehaviorSave><Name>ExistingBehavior</Name><Marker>original</Marker></BehaviorSave>";
        File.WriteAllText(destPath, originalContent);

        WriteSourceBehavior(conflictingName);

        BehaviorSave behavior = new BehaviorSave { Name = conflictingName };
        GumProjectSave source = SourceProject();
        source.Behaviors.Add(behavior);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new(),
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new() { behavior },
            Standards = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: "",
            conflictResolution: ConflictResolution.Skip);

        // Assert
        result.ConflictingElements.ShouldBeEmpty();
        File.ReadAllText(destPath).ShouldBe(originalContent);
        _importLogic.ImportedBehaviorPaths.ShouldBeEmpty();
    }

    // ── conflict resolution: Overwrite ────────────────────────────────────

    [Fact]
    public async Task ImportAsync_OverwriteResolution_ConflictingComponent_FileReplaced()
    {
        // Arrange
        string componentName = "Button";

        string destComponentDir = Path.Combine(_projectDir, "Components");
        Directory.CreateDirectory(destComponentDir);
        string destPath = Path.Combine(destComponentDir, $"{componentName}.{GumProjectSave.ComponentExtension}");
        File.WriteAllText(destPath, "<ComponentSave><Name>Button</Name><Marker>original</Marker></ComponentSave>");

        // Source content has a different marker so we can verify the overwrite landed
        string srcDir = Path.Combine(_sourceDir, "Components");
        Directory.CreateDirectory(srcDir);
        const string newContent = "<ComponentSave><Name>Button</Name><Marker>updated</Marker></ComponentSave>";
        File.WriteAllText(Path.Combine(srcDir, $"{componentName}.{GumProjectSave.ComponentExtension}"), newContent);

        ComponentSave component = ComponentNoAssets(componentName);
        GumProjectSave source = SourceProject();
        source.Components.Add(component);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new() { component },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: "",
            conflictResolution: ConflictResolution.Overwrite);

        // Assert — file on disk replaced; ImportLogic NOT called for an already-registered element
        // (the project save+reload at end of import picks up the new content).
        result.ConflictingElements.ShouldBeEmpty();
        File.ReadAllText(destPath).ShouldBe(newContent);
        _importLogic.ImportedComponentPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_OverwriteResolution_ConflictingBehavior_FileReplaced()
    {
        // Arrange
        string behaviorName = "Clickable";

        string destBehaviorDir = Path.Combine(_projectDir, "Behaviors");
        Directory.CreateDirectory(destBehaviorDir);
        string destPath = Path.Combine(destBehaviorDir, $"{behaviorName}.{BehaviorReference.Extension}");
        File.WriteAllText(destPath, "<BehaviorSave><Name>Clickable</Name><Marker>original</Marker></BehaviorSave>");

        string srcDir = Path.Combine(_sourceDir, "Behaviors");
        Directory.CreateDirectory(srcDir);
        const string newContent = "<BehaviorSave><Name>Clickable</Name><Marker>updated</Marker></BehaviorSave>";
        File.WriteAllText(Path.Combine(srcDir, $"{behaviorName}.{BehaviorReference.Extension}"), newContent);

        BehaviorSave behavior = new BehaviorSave { Name = behaviorName };
        GumProjectSave source = SourceProject();
        source.Behaviors.Add(behavior);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new(),
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new() { behavior },
            Standards = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: "",
            conflictResolution: ConflictResolution.Overwrite);

        // Assert
        result.ConflictingElements.ShouldBeEmpty();
        File.ReadAllText(destPath).ShouldBe(newContent);
        _importLogic.ImportedBehaviorPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_OverwriteResolution_NonConflictingComponent_StillRegisteredViaImportLogic()
    {
        // A NEW (non-conflicting) component must still be registered via IImportLogic, even when
        // the resolution is Overwrite — Overwrite only changes the path for already-existing files.
        string newName = "NewButton";
        WriteSourceComponent(newName);

        ComponentSave newComponent = ComponentNoAssets(newName);
        GumProjectSave source = SourceProject();
        source.Components.Add(newComponent);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new() { newComponent },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: "",
            conflictResolution: ConflictResolution.Overwrite);

        // Assert
        result.ConflictingElements.ShouldBeEmpty();
        _importLogic.ImportedComponentPaths.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ImportAsync_DefaultResolutionStillCancelsOnConflict()
    {
        // Regression: omitting the conflictResolution parameter must preserve the legacy
        // "cancel and report" behavior expected by existing callers.
        string componentName = "MyButton";

        string destComponentDir = Path.Combine(_projectDir, "Components");
        Directory.CreateDirectory(destComponentDir);
        File.WriteAllText(
            Path.Combine(destComponentDir, $"{componentName}.{GumProjectSave.ComponentExtension}"),
            "existing");

        WriteSourceComponent(componentName);
        ComponentSave component = ComponentNoAssets(componentName);
        GumProjectSave source = SourceProject();
        source.Components.Add(component);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new() { component },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };

        // Act — no conflictResolution argument
        ImportResult result = await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "");

        // Assert
        result.ConflictingElements.ShouldContain(componentName);
        _importLogic.ImportedComponentPaths.ShouldBeEmpty();
    }

    // ── variable reference remapping (issue #2839) ────────────────────────

    [Fact]
    public async Task ImportAsync_WithSubfolder_RewritesVariableReferenceRightHandSideToRemappedName()
    {
        // Issue #2839: when importing components into a subfolder, the right-hand side of
        // VariableReferences entries that point at other simultaneously-imported components
        // must be updated to reflect the new subfolder location.

        string aName = "A";
        string stylesName = "Styles";
        string subfolder = "Theme";

        // Source A.gucx contains a VariableReferences list pointing at Styles
        string srcComponentsDir = Path.Combine(_sourceDir, "Components");
        Directory.CreateDirectory(srcComponentsDir);
        string aContent =
            "<ComponentSave>" +
              "<Name>A</Name>" +
              "<States>" +
                "<StateSave>" +
                  "<Name>Default</Name>" +
                  "<VariableLists>" +
                    "<VariableListSave>" +
                      "<Name>VariableReferences</Name>" +
                      "<Value>" +
                        "<string>Red = Components/Styles.Primary.Red</string>" +
                      "</Value>" +
                    "</VariableListSave>" +
                  "</VariableLists>" +
                "</StateSave>" +
              "</States>" +
            "</ComponentSave>";
        File.WriteAllText(Path.Combine(srcComponentsDir, $"{aName}.{GumProjectSave.ComponentExtension}"), aContent);
        WriteSourceComponent(stylesName);

        ComponentSave a = ComponentNoAssets(aName);
        ComponentSave styles = ComponentNoAssets(stylesName);
        GumProjectSave source = SourceProject();
        source.Components.Add(a);
        source.Components.Add(styles);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new() { a, styles },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };

        // Act
        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: subfolder);

        // Assert — the written A file's VariableReferences right-hand side reflects the new subfolder.
        result.ConflictingElements.ShouldBeEmpty();
        string aDestPath = Path.Combine(
            _projectDir, "Components", subfolder, $"{aName}.{GumProjectSave.ComponentExtension}");
        string writtenContent = File.ReadAllText(aDestPath);
        writtenContent.ShouldContain("Red = Components/Theme/Styles.Primary.Red");
        writtenContent.ShouldNotContain("Red = Components/Styles.Primary.Red");
    }

    [Fact]
    public async Task ImportAsync_WithSubfolder_RewritesScreenVariableReferenceToRemappedScreenName()
    {
        // Screen-side mirror of the component case: a Screen with a VariableReferences entry
        // pointing at another simultaneously-imported Screen must have its qualified prefix
        // (Screens/Other → Screens/Subfolder/Other) rewritten on import.
        string aName = "MainScreen";
        string otherName = "OtherScreen";
        string subfolder = "Levels";

        string srcScreensDir = Path.Combine(_sourceDir, "Screens");
        Directory.CreateDirectory(srcScreensDir);
        string aContent =
            "<ScreenSave>" +
              "<Name>MainScreen</Name>" +
              "<States>" +
                "<StateSave>" +
                  "<Name>Default</Name>" +
                  "<VariableLists>" +
                    "<VariableListSave>" +
                      "<Name>VariableReferences</Name>" +
                      "<Value>" +
                        "<string>Title = Screens/OtherScreen.HeaderText.Value</string>" +
                      "</Value>" +
                    "</VariableListSave>" +
                  "</VariableLists>" +
                "</StateSave>" +
              "</States>" +
            "</ScreenSave>";
        File.WriteAllText(Path.Combine(srcScreensDir, $"{aName}.{GumProjectSave.ScreenExtension}"), aContent);
        WriteSourceScreen(otherName);

        ScreenSave a = ScreenNoAssets(aName);
        ScreenSave other = ScreenNoAssets(otherName);
        GumProjectSave source = SourceProject();
        source.Screens.Add(a);
        source.Screens.Add(other);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new(),
            TransitiveComponents = new(),
            DirectScreens = new() { a, other },
            Behaviors = new(),
            Standards = new(),
        };

        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: subfolder);

        result.ConflictingElements.ShouldBeEmpty();
        string aDestPath = Path.Combine(
            _projectDir, "Screens", subfolder, $"{aName}.{GumProjectSave.ScreenExtension}");
        string writtenContent = File.ReadAllText(aDestPath);
        writtenContent.ShouldContain("Title = Screens/Levels/OtherScreen.HeaderText.Value");
        writtenContent.ShouldNotContain("Title = Screens/OtherScreen.HeaderText.Value");
    }

    [Fact]
    public async Task ImportAsync_WithSubfolder_RewritesBaseTypeReferenceToRemappedComponent()
    {
        // ApplyNameRemappings rewrites <BaseType> entries that point at another imported
        // component, so the BaseType chain stays intact after the subfolder relocation.
        string derivedName = "RedButton";
        string baseName = "Button";
        string subfolder = "Theme";

        string srcComponentsDir = Path.Combine(_sourceDir, "Components");
        Directory.CreateDirectory(srcComponentsDir);
        string derivedContent =
            "<ComponentSave>" +
              "<Name>RedButton</Name>" +
              "<BaseType>Button</BaseType>" +
            "</ComponentSave>";
        File.WriteAllText(
            Path.Combine(srcComponentsDir, $"{derivedName}.{GumProjectSave.ComponentExtension}"),
            derivedContent);
        WriteSourceComponent(baseName);

        ComponentSave derived = ComponentNoAssets(derivedName);
        ComponentSave baseComponent = ComponentNoAssets(baseName);
        GumProjectSave source = SourceProject();
        source.Components.Add(derived);
        source.Components.Add(baseComponent);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new() { derived, baseComponent },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };

        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: subfolder);

        result.ConflictingElements.ShouldBeEmpty();
        string derivedDestPath = Path.Combine(
            _projectDir, "Components", subfolder, $"{derivedName}.{GumProjectSave.ComponentExtension}");
        string writtenContent = File.ReadAllText(derivedDestPath);
        writtenContent.ShouldContain("<BaseType>Theme/Button</BaseType>");
        writtenContent.ShouldNotContain("<BaseType>Button</BaseType>");
    }

    [Fact]
    public async Task ImportAsync_NoSubfolder_LeavesVariableReferencesUnchanged()
    {
        // Regression guard: when destinationSubfolder is empty, qualifiedNameMap is empty
        // and the right-hand side of VariableReferences must pass through untouched.
        string aName = "A";
        string stylesName = "Styles";

        string srcComponentsDir = Path.Combine(_sourceDir, "Components");
        Directory.CreateDirectory(srcComponentsDir);
        const string originalReference = "Red = Components/Styles.Primary.Red";
        string aContent =
            "<ComponentSave>" +
              "<Name>A</Name>" +
              "<States>" +
                "<StateSave>" +
                  "<Name>Default</Name>" +
                  "<VariableLists>" +
                    "<VariableListSave>" +
                      "<Name>VariableReferences</Name>" +
                      "<Value>" +
                        $"<string>{originalReference}</string>" +
                      "</Value>" +
                    "</VariableListSave>" +
                  "</VariableLists>" +
                "</StateSave>" +
              "</States>" +
            "</ComponentSave>";
        File.WriteAllText(Path.Combine(srcComponentsDir, $"{aName}.{GumProjectSave.ComponentExtension}"), aContent);
        WriteSourceComponent(stylesName);

        ComponentSave a = ComponentNoAssets(aName);
        ComponentSave styles = ComponentNoAssets(stylesName);
        GumProjectSave source = SourceProject();
        source.Components.Add(a);
        source.Components.Add(styles);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new() { a, styles },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };

        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: "");

        result.ConflictingElements.ShouldBeEmpty();
        string aDestPath = Path.Combine(_projectDir, "Components", $"{aName}.{GumProjectSave.ComponentExtension}");
        string writtenContent = File.ReadAllText(aDestPath);
        writtenContent.ShouldContain(originalReference);
    }

    [Fact]
    public async Task ImportAsync_WithSubfolder_LeavesReferenceToNonImportedElementUnchanged()
    {
        // If the right-hand side points at an element NOT being imported, the qualified prefix
        // must be preserved so it resolves against the destination project's existing element.
        string aName = "A";
        string subfolder = "Theme";
        const string externalReference = "Red = Components/ExternalStyles.Primary.Red";

        string srcComponentsDir = Path.Combine(_sourceDir, "Components");
        Directory.CreateDirectory(srcComponentsDir);
        string aContent =
            "<ComponentSave>" +
              "<Name>A</Name>" +
              "<States>" +
                "<StateSave>" +
                  "<Name>Default</Name>" +
                  "<VariableLists>" +
                    "<VariableListSave>" +
                      "<Name>VariableReferences</Name>" +
                      "<Value>" +
                        $"<string>{externalReference}</string>" +
                      "</Value>" +
                    "</VariableListSave>" +
                  "</VariableLists>" +
                "</StateSave>" +
              "</States>" +
            "</ComponentSave>";
        File.WriteAllText(Path.Combine(srcComponentsDir, $"{aName}.{GumProjectSave.ComponentExtension}"), aContent);

        ComponentSave a = ComponentNoAssets(aName);
        GumProjectSave source = SourceProject();
        source.Components.Add(a);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new() { a },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };

        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: subfolder);

        result.ConflictingElements.ShouldBeEmpty();
        string aDestPath = Path.Combine(
            _projectDir, "Components", subfolder, $"{aName}.{GumProjectSave.ComponentExtension}");
        string writtenContent = File.ReadAllText(aDestPath);
        writtenContent.ShouldContain(externalReference);
    }

    // ── test doubles ──────────────────────────────────────────────────────

    private class FakeImportLogic : IImportLogic
    {
        public List<string> ImportedComponentPaths { get; } = new();
        public List<string> ImportedScreenPaths    { get; } = new();
        public List<string> ImportedBehaviorPaths  { get; } = new();

        public ComponentSave? ImportComponent(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
        {
            ImportedComponentPaths.Add(filePath.FullPath);
            return new ComponentSave();
        }

        public ScreenSave? ImportScreen(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
        {
            ImportedScreenPaths.Add(filePath.FullPath);
            return new ScreenSave();
        }

        public BehaviorSave ImportBehavior(FilePath filePath, string? desiredDirectory = null, bool saveProject = false)
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
        // CS0067: event is never raised — this fake only satisfies the IFileCommands contract;
        // tests in this file don't exercise localization loading.
#pragma warning disable CS0067
        public event Action? LocalizationLoaded;
#pragma warning restore CS0067
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
