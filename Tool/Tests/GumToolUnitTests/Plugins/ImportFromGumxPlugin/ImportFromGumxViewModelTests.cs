using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
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

    public ImportFromGumxViewModelTests()
    {
        GumxSourceService sourceService = new GumxSourceService();
        GumxDependencyResolver resolver = new GumxDependencyResolver();
        FakeProjectState projectState = new FakeProjectState();
        GumxImportService importService = new GumxImportService(
            new FakeImportLogic(), projectState, new FakeFileCommands(), sourceService);

        _sut = new ImportFromGumxViewModel(sourceService, resolver, importService, projectState);
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

    private class FakeImportLogic : IImportLogic
    {
        public ComponentSave? ImportComponent(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
            => new ComponentSave();
        public ScreenSave? ImportScreen(FilePath filePath, string? desiredDirectory = null, bool saveProject = true)
            => new ScreenSave();
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
        public void SaveGeneralSettings() { }
        public void SaveIfDiffers(FilePath filePath, string contents) { }
    }
}
