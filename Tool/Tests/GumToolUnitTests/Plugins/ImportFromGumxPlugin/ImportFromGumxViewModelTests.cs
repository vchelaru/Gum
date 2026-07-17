using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.Services.Dialogs;
using Gum.Settings;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ToolsUtilities;

namespace GumToolUnitTests.Plugins.ImportFromGumxPlugin;

/// <summary>
/// Tests for ImportFromGumxViewModel's leaf-inclusion bookkeeping. Specifically,
/// regression coverage for issue #2642 — user clicks on Behavior/Standard checkboxes
/// were being wiped on the next dispatcher tick because RecomputeTransitiveDependencies
/// rebuilt those groups purely from the dependency resolver.
/// </summary>
public class ImportFromGumxViewModelTests
{
    private readonly ImportFromGumxViewModel _sut;

    private readonly FakeDialogService _dialogService;
    private readonly FakeProjectState _projectState;

    public ImportFromGumxViewModelTests()
    {
        GumxSourceService sourceService = new GumxSourceService();
        GumxDependencyResolver resolver = new GumxDependencyResolver();
        _projectState = new FakeProjectState();
        GumxImportService importService = new GumxImportService(
            new FakeImportLogic(), _projectState, new FakeFileCommands(), sourceService);

        _dialogService = new FakeDialogService();
        _sut = new ImportFromGumxViewModel(sourceService, resolver, importService, _projectState, _dialogService);
    }

    [Fact]
    public void RecomputeTransitiveDependencies_BehaviorCheckedByUser_RemainsExplicit()
    {
        GumProjectSave source = new GumProjectSave();
        source.Behaviors.Add(new BehaviorSave { Name = "MyBehavior" });

        _sut.InitializeFromProjectForTesting(source);
        ImportTreeNodeViewModel behaviorLeaf = FindLeaf("MyBehavior", ElementItemType.Behavior);

        behaviorLeaf.InclusionState = InclusionState.Explicit;
        _sut.RecomputeTransitiveDependencies();

        behaviorLeaf.InclusionState.ShouldBe(InclusionState.Explicit);
    }

    [Fact]
    public void RecomputeTransitiveDependencies_StandardCheckedByUser_RemainsExplicit()
    {
        GumProjectSave source = new GumProjectSave();
        source.StandardElements.Add(new StandardElementSave
        {
            Name = "Text",
            States = new List<StateSave> { new StateSave { Name = "Default", Variables = new List<VariableSave>() } }
        });

        _sut.InitializeFromProjectForTesting(source);
        ImportTreeNodeViewModel standardLeaf = FindLeaf("Text", ElementItemType.Standard);

        standardLeaf.InclusionState = InclusionState.Explicit;
        _sut.RecomputeTransitiveDependencies();

        standardLeaf.InclusionState.ShouldBe(InclusionState.Explicit);
    }

    [Fact]
    public void RecomputeTransitiveDependencies_BehaviorUncheckedByUser_RemainsNotIncluded()
    {
        GumProjectSave source = new GumProjectSave();
        source.Behaviors.Add(new BehaviorSave { Name = "MyBehavior" });

        _sut.InitializeFromProjectForTesting(source);
        ImportTreeNodeViewModel behaviorLeaf = FindLeaf("MyBehavior", ElementItemType.Behavior);

        behaviorLeaf.InclusionState = InclusionState.Explicit;
        _sut.RecomputeTransitiveDependencies();
        behaviorLeaf.InclusionState = InclusionState.NotIncluded;
        _sut.RecomputeTransitiveDependencies();

        behaviorLeaf.InclusionState.ShouldBe(InclusionState.NotIncluded);
    }

    [Fact]
    public void RecomputeTransitiveDependencies_ComponentRequiresBehavior_BehaviorIsAutoChecked()
    {
        // Component depends on behavior via ElementBehaviorReference.BehaviorName
        BehaviorSave behavior = new BehaviorSave { Name = "Clickable" };
        ComponentSave component = new ComponentSave
        {
            Name = "Button",
            Behaviors = new List<ElementBehaviorReference>
            {
                new ElementBehaviorReference { BehaviorName = "Clickable" }
            }
        };

        GumProjectSave source = new GumProjectSave();
        source.Behaviors.Add(behavior);
        source.Components.Add(component);

        _sut.InitializeFromProjectForTesting(source);
        ImportTreeNodeViewModel componentLeaf = FindLeaf("Button", ElementItemType.Component);
        ImportTreeNodeViewModel behaviorLeaf = FindLeaf("Clickable", ElementItemType.Behavior);

        componentLeaf.InclusionState = InclusionState.Explicit;
        _sut.RecomputeTransitiveDependencies();

        behaviorLeaf.InclusionState.ShouldBe(InclusionState.Explicit);
    }

    [Fact]
    public void RecomputeTransitiveDependencies_AutoCheckedBehaviorThenComponentUnchecked_BehaviorUnchecks()
    {
        // Verifies auto-add does NOT pollute the user-explicit set:
        // when the selecting component goes away, the behavior should follow.
        BehaviorSave behavior = new BehaviorSave { Name = "Clickable" };
        ComponentSave component = new ComponentSave
        {
            Name = "Button",
            Behaviors = new List<ElementBehaviorReference>
            {
                new ElementBehaviorReference { BehaviorName = "Clickable" }
            }
        };

        GumProjectSave source = new GumProjectSave();
        source.Behaviors.Add(behavior);
        source.Components.Add(component);

        _sut.InitializeFromProjectForTesting(source);
        ImportTreeNodeViewModel componentLeaf = FindLeaf("Button", ElementItemType.Component);
        ImportTreeNodeViewModel behaviorLeaf = FindLeaf("Clickable", ElementItemType.Behavior);

        componentLeaf.InclusionState = InclusionState.Explicit;
        _sut.RecomputeTransitiveDependencies();
        behaviorLeaf.InclusionState.ShouldBe(InclusionState.Explicit); // sanity

        componentLeaf.InclusionState = InclusionState.NotIncluded;
        _sut.RecomputeTransitiveDependencies();

        behaviorLeaf.InclusionState.ShouldBe(InclusionState.NotIncluded);
    }

    [Fact]
    public void RecomputeTransitiveDependencies_UserPickedBehaviorPlusAutoRequired_BothPersist()
    {
        // User explicitly picks BehaviorA; component requires BehaviorB.
        // After the component is unchecked, BehaviorA stays (user-explicit)
        // and BehaviorB drops (was only auto-added).
        BehaviorSave behaviorA = new BehaviorSave { Name = "BehaviorA" };
        BehaviorSave behaviorB = new BehaviorSave { Name = "BehaviorB" };
        ComponentSave component = new ComponentSave
        {
            Name = "Button",
            Behaviors = new List<ElementBehaviorReference>
            {
                new ElementBehaviorReference { BehaviorName = "BehaviorB" }
            }
        };

        GumProjectSave source = new GumProjectSave();
        source.Behaviors.Add(behaviorA);
        source.Behaviors.Add(behaviorB);
        source.Components.Add(component);

        _sut.InitializeFromProjectForTesting(source);
        ImportTreeNodeViewModel componentLeaf = FindLeaf("Button", ElementItemType.Component);
        ImportTreeNodeViewModel behaviorALeaf = FindLeaf("BehaviorA", ElementItemType.Behavior);
        ImportTreeNodeViewModel behaviorBLeaf = FindLeaf("BehaviorB", ElementItemType.Behavior);

        behaviorALeaf.InclusionState = InclusionState.Explicit;
        componentLeaf.InclusionState = InclusionState.Explicit;
        _sut.RecomputeTransitiveDependencies();
        behaviorALeaf.InclusionState.ShouldBe(InclusionState.Explicit);
        behaviorBLeaf.InclusionState.ShouldBe(InclusionState.Explicit);

        componentLeaf.InclusionState = InclusionState.NotIncluded;
        _sut.RecomputeTransitiveDependencies();

        behaviorALeaf.InclusionState.ShouldBe(InclusionState.Explicit);
        behaviorBLeaf.InclusionState.ShouldBe(InclusionState.NotIncluded);
    }

    [Fact]
    public void RecomputeTransitiveDependencies_PopulateItems_ClearsUserExplicitTracking()
    {
        GumProjectSave first = new GumProjectSave();
        first.Behaviors.Add(new BehaviorSave { Name = "First" });

        _sut.InitializeFromProjectForTesting(first);
        ImportTreeNodeViewModel firstLeaf = FindLeaf("First", ElementItemType.Behavior);
        firstLeaf.InclusionState = InclusionState.Explicit;

        // Reload with a different project — user-explicit set should reset
        GumProjectSave second = new GumProjectSave();
        second.Behaviors.Add(new BehaviorSave { Name = "Second" });
        _sut.InitializeFromProjectForTesting(second);

        ImportTreeNodeViewModel secondLeaf = FindLeaf("Second", ElementItemType.Behavior);
        _sut.RecomputeTransitiveDependencies();

        secondLeaf.InclusionState.ShouldBe(InclusionState.NotIncluded);
    }

    // ── standard diff rows (#2779) ────────────────────────────────────────

    [Fact]
    public void RecomputeTransitiveDependencies_DifferingStandard_PopulatesDiffRows()
    {
        // Source Text standard has an extra category; destination Text does not.
        // A component referencing Text should cause the standard to flag as differing,
        // and the corresponding tree node should expose diff rows.
        StandardElementSave sourceText = new StandardElementSave { Name = "Text" };
        sourceText.Categories.Add(new StateSaveCategory { Name = "TextColor" });
        sourceText.States.Add(new StateSave { Name = "Default" });

        StandardElementSave destText = new StandardElementSave { Name = "Text" };
        destText.States.Add(new StateSave { Name = "Default" });

        ComponentSave button = new ComponentSave { Name = "Button" };
        button.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Text" });

        GumProjectSave source = new GumProjectSave();
        source.Components.Add(button);
        source.StandardElements.Add(sourceText);

        _projectState.GumProjectSave.StandardElements.Add(destText);

        _sut.InitializeFromProjectForTesting(source);
        ImportTreeNodeViewModel buttonLeaf = FindLeaf("Button", ElementItemType.Component);
        ImportTreeNodeViewModel textStandardLeaf = FindLeaf("Text", ElementItemType.Standard);

        buttonLeaf.InclusionState = InclusionState.Explicit;
        _sut.RecomputeTransitiveDependencies();

        textStandardLeaf.HasStandardDiffRows.ShouldBeTrue();
        textStandardLeaf.StandardDiffRows!.ShouldContain(r => r.Kind == "Category added" && r.Summary == "TextColor");
    }

    [Fact]
    public void RecomputeTransitiveDependencies_StandardMatchingDestination_HasNoDiffRows()
    {
        // Identical source and destination standard — nothing to diff, no expander.
        StandardElementSave sourceText = new StandardElementSave { Name = "Text" };
        sourceText.States.Add(new StateSave { Name = "Default" });

        StandardElementSave destText = new StandardElementSave { Name = "Text" };
        destText.States.Add(new StateSave { Name = "Default" });

        GumProjectSave source = new GumProjectSave();
        source.StandardElements.Add(sourceText);
        _projectState.GumProjectSave.StandardElements.Add(destText);

        _sut.InitializeFromProjectForTesting(source);
        ImportTreeNodeViewModel textStandardLeaf = FindLeaf("Text", ElementItemType.Standard);

        _sut.RecomputeTransitiveDependencies();

        textStandardLeaf.HasStandardDiffRows.ShouldBeFalse();
        textStandardLeaf.StandardDiffRows.ShouldBeNull();
    }

    // ── browse command (#3263) ────────────────────────────────────────────

    [Fact]
    public async Task BrowseCommand_Cancelled_LeavesSourcePathUnchangedAndDoesNotLoad()
    {
        _dialogService.OpenFileStub = _ => null;

        await _sut.BrowseCommand.ExecuteAsync(null);

        _dialogService.OpenFileCallCount.ShouldBe(1);
        _sut.SourcePath.ShouldBeEmpty();
        _sut.ErrorMessage.ShouldBeNull();
        _sut.IsPreviewLoaded.ShouldBeFalse();
    }

    [Fact]
    public async Task BrowseCommand_FileChosen_SetsSourcePathAndAttemptsLoad()
    {
        // A path the user "picks" that is guaranteed not to exist, so the chained load
        // deterministically fails (no real .gumx required).
        string chosenPath = Path.Combine(
            Path.GetTempPath(), "GumImportBrowseTest_" + Guid.NewGuid().ToString("N") + ".gumx");
        _dialogService.OpenFileStub = _ => new List<string> { chosenPath };

        await _sut.BrowseCommand.ExecuteAsync(null);

        _dialogService.OpenFileCallCount.ShouldBe(1);
        _dialogService.LastOpenFileOptions.ShouldNotBeNull();
        _dialogService.LastOpenFileOptions!.Title.ShouldBe("Open Gum Project");
        _dialogService.LastOpenFileOptions.Filter.ShouldContain("*.gumx");

        _sut.SourcePath.ShouldBe(chosenPath);
        // The browse flow chains into LoadPreviewCommand; the chosen file does not exist,
        // so the load surfaces an error — proof the load was attempted, not skipped.
        _sut.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── conflict-resolution dialog (#2644) ────────────────────────────────

    [Fact]
    public void PromptConflictResolution_OffersSkipAndOverwriteAndAllowsCancel()
    {
        ConflictResolution? captured = null;
        _dialogService.ChoiceDialogStub = vm =>
        {
            // Validate the options surfaced to the user — Skip and Overwrite, plus a Cancel path.
            vm.OptionValues.Count.ShouldBe(2);
            vm.OptionValues.ShouldContain("Skip Existing");
            vm.OptionValues.ShouldContain("Overwrite All");
            vm.CanCancel.ShouldBeTrue();
            // Don't pick anything → SelectedValue is left as the default first option ("Skip Existing").
            // To simulate "Cancel", clear the selection.
            vm.SelectedValue = null;
        };

        captured = _sut.PromptConflictResolution(new[] { "Behaviors/Clickable", "Components/Button" });

        _dialogService.ShowChoiceCallCount.ShouldBe(1);
        captured.ShouldBeNull();
    }

    [Fact]
    public void PromptConflictResolution_UserPicksSkip_ReturnsSkip()
    {
        _dialogService.ChoiceDialogStub = vm => vm.SelectedValue = "Skip Existing";

        ConflictResolution? result = _sut.PromptConflictResolution(new[] { "Behaviors/Clickable" });

        result.ShouldBe(ConflictResolution.Skip);
    }

    [Fact]
    public void PromptConflictResolution_UserPicksOverwrite_ReturnsOverwrite()
    {
        _dialogService.ChoiceDialogStub = vm => vm.SelectedValue = "Overwrite All";

        ConflictResolution? result = _sut.PromptConflictResolution(new[] { "Components/Button" });

        result.ShouldBe(ConflictResolution.Overwrite);
    }

    private ImportTreeNodeViewModel FindLeaf(string fullName, ElementItemType elementType)
    {
        foreach (ImportTreeNodeViewModel root in _sut.RootNodes)
        {
            ImportTreeNodeViewModel? found = FindLeafRecursive(root, fullName, elementType);
            if (found != null) { return found; }
        }
        throw new InvalidOperationException($"Leaf not found: {fullName} ({elementType})");
    }

    private static ImportTreeNodeViewModel? FindLeafRecursive(
        ImportTreeNodeViewModel node, string fullName, ElementItemType elementType)
    {
        if (node.IsLeaf && node.ElementType == elementType && node.FullName == fullName)
        {
            return node;
        }
        foreach (ImportTreeNodeViewModel child in node.Children)
        {
            ImportTreeNodeViewModel? hit = FindLeafRecursive(child, fullName, elementType);
            if (hit != null) { return hit; }
        }
        return null;
    }

    // ── fakes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Records the last <see cref="Show{T}(Action{T}?, out T)"/> invocation. The IDialogService
    /// extension <c>ShowChoices&lt;T&gt;</c> resolves a <see cref="ChoiceDialogViewModel"/> via this
    /// method, so capturing here is enough to verify the conflict-resolution flow.
    /// </summary>
    private class FakeDialogService : IDialogService
    {
        public Action<ChoiceDialogViewModel>? ChoiceDialogStub { get; set; }
        public ChoiceDialogViewModel? LastChoiceDialog { get; private set; }
        public int ShowChoiceCallCount { get; private set; }

        public Func<OpenFileDialogOptions?, List<string>?>? OpenFileStub { get; set; }
        public OpenFileDialogOptions? LastOpenFileOptions { get; private set; }
        public int OpenFileCallCount { get; private set; }

        public MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null)
            => MessageDialogResult.Canceled;
        public bool Show<T>(T dialogViewModel) where T : DialogViewModel => false;
        public bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel
        {
            // Hand-roll a ChoiceDialogViewModel since the test bypasses DI.
            if (typeof(T) == typeof(ChoiceDialogViewModel))
            {
                ChoiceDialogViewModel choice = new ChoiceDialogViewModel();
                initializer?.Invoke((T)(object)choice);
                ChoiceDialogStub?.Invoke(choice);
                LastChoiceDialog = choice;
                ShowChoiceCallCount++;
                viewModel = (T)(object)choice;
                return false;
            }
            viewModel = null!;
            return false;
        }
        public string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null) => null;
        public List<string>? OpenFile(OpenFileDialogOptions? options = null)
        {
            LastOpenFileOptions = options;
            OpenFileCallCount++;
            return OpenFileStub?.Invoke(options);
        }
        public string? SaveFile(SaveFileDialogOptions? options = null) => null;
    }

    private class FakeImportLogic : IImportLogic
    {
        public ComponentSave? ImportComponent(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
            => new ComponentSave();
        public ComponentSave? ImportComponent(ComponentSave component, bool saveProject = true)
            => component;
        public ScreenSave? ImportScreen(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
            => new ScreenSave();
        public ScreenSave? ImportScreen(ScreenSave screen, bool saveProject = true)
            => screen;
        public BehaviorSave ImportBehavior(FilePath filePath, string? desiredDirectory = null, bool saveProject = false)
            => new BehaviorSave();
    }

    private class FakeProjectState : IProjectState
    {
        public FakeProjectState()
        {
            GumProjectSave = new GumProjectSave();
        }

        public GumProjectSave GumProjectSave { get; }
        public GeneralSettingsFile GeneralSettings => new GeneralSettingsFile();
        public string? ProjectDirectory => null;
        public FilePath ComponentFilePath => new FilePath(string.Empty);
        public FilePath ScreenFilePath => new FilePath(string.Empty);
        public FilePath BehaviorFilePath => new FilePath(string.Empty);
        public bool NeedsToSaveProject => false;
    }

    private class FakeFileCommands : IFileCommands
    {
#pragma warning disable CS0067 // event unused — only here to satisfy the interface contract for these tests
        public event Action? LocalizationLoaded;
#pragma warning restore CS0067
        public FilePath? ProjectDirectory => null;
        public bool TryAutoSaveProject(bool forceSaveContainedElements = false) => false;
        public void LoadProject(string fileName) { }
        public void DeleteDirectory(FilePath filePath) { }
        public void MoveToRecycleBin(FilePath filePath) { }
        public string[] GetFiles(string path) => Array.Empty<string>();
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => Array.Empty<string>();
        public string ReadAllText(string path) => string.Empty;
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
