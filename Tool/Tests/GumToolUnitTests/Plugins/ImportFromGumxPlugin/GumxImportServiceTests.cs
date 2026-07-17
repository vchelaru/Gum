using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
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
    private readonly FakeImportLogic _importLogic;
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
        _importLogic   = new FakeImportLogic(_projectDir);

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

    /// <summary>
    /// Builds a ComponentSave with a single VariableReferences line in its default state.
    /// Mirrors how the live project model holds these — a VariableListSave whose root name is
    /// "VariableReferences" with a List&lt;string&gt; of "LeftSide = RightSide" entries.
    /// </summary>
    private static ComponentSave ComponentWithVariableReference(string name, string variableReferenceLine)
    {
        VariableListSave<string> varList = new VariableListSave<string>
        {
            Type = "string",
            Name = "VariableReferences"
        };
        varList.Value.Add(variableReferenceLine);
        StateSave defaultState = new StateSave
        {
            Name = "Default",
            Variables = new(),
            VariableLists = new() { varList }
        };
        return new ComponentSave { Name = name, States = new() { defaultState } };
    }

    private static ScreenSave ScreenWithVariableReference(string name, string variableReferenceLine)
    {
        VariableListSave<string> varList = new VariableListSave<string>
        {
            Type = "string",
            Name = "VariableReferences"
        };
        varList.Value.Add(variableReferenceLine);
        StateSave defaultState = new StateSave
        {
            Name = "Default",
            Variables = new(),
            VariableLists = new() { varList }
        };
        return new ScreenSave { Name = name, States = new() { defaultState } };
    }

    private static ComponentSave ComponentWithBaseType(string name, string baseType) =>
        new ComponentSave
        {
            Name = name,
            BaseType = baseType,
            States = new() { new StateSave { Name = "Default", Variables = new() } }
        };

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
        // On Overwrite the existing destination file is replaced with a serialization of the
        // in-memory source ComponentSave. ImportLogic is NOT called because the element is
        // already in the project — the post-import save+reload picks up the new content.
        // We use BaseType as a sentinel: the source has BaseType "Sentinel", the pre-existing
        // file does not, so we can confirm the source replaced the destination.
        string componentName = "Button";

        string destComponentDir = Path.Combine(_projectDir, "Components");
        Directory.CreateDirectory(destComponentDir);
        string destPath = Path.Combine(destComponentDir, $"{componentName}.{GumProjectSave.ComponentExtension}");
        File.WriteAllText(destPath, "<ComponentSave><Name>Button</Name><BaseType>Original</BaseType></ComponentSave>");

        ComponentSave component = ComponentWithBaseType(componentName, baseType: "Sentinel");
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

        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: "",
            conflictResolution: ConflictResolution.Overwrite);

        result.ConflictingElements.ShouldBeEmpty();
        string writtenContent = File.ReadAllText(destPath);
        writtenContent.ShouldContain("Sentinel");
        writtenContent.ShouldNotContain("Original");
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
    //
    // Tests assert against the final on-disk file the real ImportLogic pipeline produces — the
    // FakeImportLogic in this file serializes the in-memory ComponentSave through
    // ElementSave.Save (same code path the production TryAutoSaveElement uses), so we exercise
    // the deserialize-resave race that caused the original bug.

    [Fact]
    public async Task ImportAsync_WithSubfolder_RewritesVariableReferenceRightHandSideToRemappedName()
    {
        // Issue #2839: when importing components into a subfolder, the right-hand side of
        // VariableReferences entries that point at other simultaneously-imported components
        // must be updated to reflect the new subfolder location.
        string subfolder = "Theme";

        ComponentSave a = ComponentWithVariableReference("A", "Red = Components/Styles.Primary.Red");
        ComponentSave styles = ComponentNoAssets("Styles");
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
            selections, source, _sourceDir, destinationSubfolder: subfolder);

        result.ConflictingElements.ShouldBeEmpty();
        string aDestPath = Path.Combine(
            _projectDir, "Components", subfolder, $"A.{GumProjectSave.ComponentExtension}");
        string writtenContent = File.ReadAllText(aDestPath);
        writtenContent.ShouldContain("Red = Components/Theme/Styles.Primary.Red");
        writtenContent.ShouldNotContain("Red = Components/Styles.Primary.Red");
    }

    [Fact]
    public async Task ImportAsync_WithSubfolder_RewritesScreenVariableReferenceToRemappedScreenName()
    {
        // Screen analogue: a Screen with a VariableReferences entry pointing at another
        // simultaneously-imported Screen must have its qualified prefix (Screens/Other →
        // Screens/Subfolder/Other) rewritten on import.
        string subfolder = "Levels";

        ScreenSave main = ScreenWithVariableReference("MainScreen", "Title = Screens/OtherScreen.HeaderText.Value");
        ScreenSave other = ScreenNoAssets("OtherScreen");
        GumProjectSave source = SourceProject();
        source.Screens.Add(main);
        source.Screens.Add(other);

        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new(),
            TransitiveComponents = new(),
            DirectScreens = new() { main, other },
            Behaviors = new(),
            Standards = new(),
        };

        ImportResult result = await _sut.ImportAsync(
            selections, source, _sourceDir, destinationSubfolder: subfolder);

        result.ConflictingElements.ShouldBeEmpty();
        string mainDestPath = Path.Combine(
            _projectDir, "Screens", subfolder, $"MainScreen.{GumProjectSave.ScreenExtension}");
        string writtenContent = File.ReadAllText(mainDestPath);
        writtenContent.ShouldContain("Title = Screens/Levels/OtherScreen.HeaderText.Value");
        writtenContent.ShouldNotContain("Title = Screens/OtherScreen.HeaderText.Value");
    }

    [Fact]
    public async Task ImportAsync_WithSubfolder_RewritesBaseTypeReferenceToRemappedComponent()
    {
        // The element's BaseType points at another imported component; under subfolder import,
        // the BaseType must be rewritten to the new qualified location.
        string subfolder = "Theme";

        ComponentSave derived = ComponentWithBaseType("RedButton", baseType: "Button");
        ComponentSave baseComponent = ComponentNoAssets("Button");
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
        ComponentSave registered = _importLogic.RegisteredComponents
            .Where(c => c.Name == $"{subfolder}/RedButton").ShouldHaveSingleItem();
        registered.BaseType.ShouldBe($"{subfolder}/Button");
    }

    [Fact]
    public async Task ImportAsync_NoSubfolder_LeavesVariableReferencesUnchanged()
    {
        // Regression guard: when destinationSubfolder is empty, qualifiedNameMap is empty
        // and the right-hand side of VariableReferences must pass through untouched.
        const string originalReference = "Red = Components/Styles.Primary.Red";

        ComponentSave a = ComponentWithVariableReference("A", originalReference);
        ComponentSave styles = ComponentNoAssets("Styles");
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
        string aDestPath = Path.Combine(_projectDir, "Components", $"A.{GumProjectSave.ComponentExtension}");
        string writtenContent = File.ReadAllText(aDestPath);
        writtenContent.ShouldContain(originalReference);
    }

    [Fact]
    public async Task ImportAsync_WithSubfolder_LeavesReferenceToNonImportedElementUnchanged()
    {
        // If the right-hand side points at an element NOT being imported, the qualified prefix
        // must be preserved so it resolves against the destination project's existing element.
        const string externalReference = "Red = Components/ExternalStyles.Primary.Red";
        string subfolder = "Theme";

        ComponentSave a = ComponentWithVariableReference("A", externalReference);
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
            _projectDir, "Components", subfolder, $"A.{GumProjectSave.ComponentExtension}");
        string writtenContent = File.ReadAllText(aDestPath);
        writtenContent.ShouldContain(externalReference);
    }

    [Fact]
    public async Task ImportAsync_WithSubfolder_DoesNotMutateSourceComponent()
    {
        // The source ComponentSave in selections (which is the live model held by the source
        // GumProjectSave) must NOT be mutated by the import — the import preview dialog may
        // still reference it after the import completes. We achieve this by cloning the source
        // before mutating; this test pins that invariant.
        string subfolder = "Theme";
        const string originalReference = "Red = Components/Styles.Primary.Red";

        ComponentSave a = ComponentWithVariableReference("A", originalReference);
        ComponentSave styles = ComponentNoAssets("Styles");
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

        await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: subfolder);

        // Source side: name, reference, and reference target are all original.
        a.Name.ShouldBe("A");
        VariableListSave aRefs = a.DefaultState.VariableLists
            .Where(l => l.GetRootName() == "VariableReferences").ShouldHaveSingleItem();
        aRefs.ValueAsIList[0].ShouldBe(originalReference);
    }

    [Fact]
    public async Task ImportAsync_WithSubfolder_WrittenFileRoundTripsToCorrectInMemoryReference()
    {
        // End-to-end protection: load the on-disk file back through the same serializer
        // production uses (ElementReference.DeserializeElement) and assert the in-memory
        // VariableReferences list carries the rewritten right-hand side. Defends against
        // serializer quirks that could store the string differently than expected.
        string subfolder = "Theme";

        ComponentSave a = ComponentWithVariableReference("A", "Red = Components/Styles.Primary.Red");
        ComponentSave styles = ComponentNoAssets("Styles");
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

        await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: subfolder);

        string aDestPath = Path.Combine(
            _projectDir, "Components", subfolder, $"A.{GumProjectSave.ComponentExtension}");
        ComponentSave reloaded = ElementReference.DeserializeElement<ComponentSave>(
            aDestPath, GumProjectSave.NativeVersion);

        reloaded.Name.ShouldBe($"{subfolder}/A");
        VariableListSave reloadedRefs = reloaded.DefaultState.VariableLists
            .Where(l => l.GetRootName() == "VariableReferences").ShouldHaveSingleItem();
        reloadedRefs.ValueAsIList[0].ShouldBe("Red = Components/Theme/Styles.Primary.Red");
    }

    // ── test doubles ──────────────────────────────────────────────────────

    /// <summary>
    /// Test double that mimics the real ImportLogic's serialize-to-disk behavior. The earlier
    /// version of this fake just recorded the FilePath and returned an empty ComponentSave,
    /// which hid the bug that motivated the GumxImportService refactor: real ImportLogic
    /// deserializes the just-written file and TryAutoSaveElement immediately re-serializes
    /// the in-memory model back to disk, so any XML-string rewrites done by GumxImportService
    /// were silently overwritten. Tests must assert on the final on-disk file.
    /// </summary>
    private class FakeImportLogic : IImportLogic
    {
        private readonly string _projectDir;
        private readonly List<ComponentSave> _registeredComponents = new();
        private readonly List<ScreenSave> _registeredScreens = new();

        public FakeImportLogic(string projectDir) { _projectDir = projectDir; }

        public List<string> ImportedComponentPaths { get; } = new();
        public List<string> ImportedScreenPaths    { get; } = new();
        public List<string> ImportedBehaviorPaths  { get; } = new();

        /// <summary>Components registered via the in-memory overload, in import order.</summary>
        public IReadOnlyList<ComponentSave> RegisteredComponents => _registeredComponents;
        public IReadOnlyList<ScreenSave>    RegisteredScreens    => _registeredScreens;

        public ComponentSave? ImportComponent(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
        {
            ImportedComponentPaths.Add(filePath.FullPath);
            return new ComponentSave();
        }

        public ComponentSave? ImportComponent(ComponentSave component, bool saveProject = true)
        {
            _registeredComponents.Add(component);
            string destPath = Path.Combine(
                _projectDir, "Components", $"{component.Name}.{GumProjectSave.ComponentExtension}");
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            component.Save(destPath, useCompactFormat: true);
            ImportedComponentPaths.Add(destPath);
            return component;
        }

        public ScreenSave? ImportScreen(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
        {
            ImportedScreenPaths.Add(filePath.FullPath);
            return new ScreenSave();
        }

        public ScreenSave? ImportScreen(ScreenSave screen, bool saveProject = true)
        {
            _registeredScreens.Add(screen);
            string destPath = Path.Combine(
                _projectDir, "Screens", $"{screen.Name}.{GumProjectSave.ScreenExtension}");
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            screen.Save(destPath, useCompactFormat: true);
            ImportedScreenPaths.Add(destPath);
            return screen;
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
        public bool EffectiveUseStandardsPalette => true;
        public byte OutlineColorR => 255;
        public byte OutlineColorG => 255;
        public byte OutlineColorB => 255;
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
        public FilePath? GetFullPathXmlFile(ElementSave element, string elementName) => new FilePath(string.Empty);
        public void SaveGeneralSettings() { }
        public void SaveIfDiffers(FilePath filePath, string contents) { }
    }
}
