using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace GumToolUnitTests.Managers;

public class DeleteLogicTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly DeleteLogic _deleteLogic;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IReferenceFinder> _referenceFinder;
    private readonly GumProjectSave _gumProject;

    public DeleteLogicTests()
    {
        _mocker = new AutoMocker();
        _selectedState = _mocker.GetMock<ISelectedState>();
        _dialogService = _mocker.GetMock<IDialogService>();
        _guiCommands = _mocker.GetMock<IGuiCommands>();
        _fileCommands = _mocker.GetMock<IFileCommands>();
        _pluginManager = _mocker.GetMock<IPluginManager>();
        _wireframeObjectManager = _mocker.GetMock<IWireframeObjectManager>();
        _projectManager = _mocker.GetMock<IProjectManager>();
        _referenceFinder = _mocker.GetMock<IReferenceFinder>();

        _referenceFinder
            .Setup(r => r.GetReferencesToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(new ElementReferences());
        _referenceFinder
            .Setup(r => r.GetReferencesToInstance(It.IsAny<ElementSave>(), It.IsAny<InstanceSave>(), It.IsAny<string>()))
            .Returns(new InstanceReferences());
        _referenceFinder
            .Setup(r => r.GetReferencesToBehavior(It.IsAny<BehaviorSave>(), It.IsAny<string>()))
            .Returns(new BehaviorReferences());

        _deleteLogic = new DeleteLogic(
            _selectedState.Object,
            _dialogService.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _pluginManager.Object,
            _wireframeObjectManager.Object,
            _projectManager.Object,
            _referenceFinder.Object);

        _gumProject = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _gumProject;
        _projectManager.Setup(m => m.GumProjectSave).Returns(_gumProject);
    }

    [Fact]
    public void GetFolderDeletionBlocker_EmptyFolder_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try
        {
            DeleteLogic.GetFolderDeletionBlocker(tempDir).ShouldBeNull();
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void GetFolderDeletionBlocker_FilesAndSubdirectories_ReturnsCombinedBlocker()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "a.txt"), "");
        Directory.CreateDirectory(Path.Combine(tempDir, "subfolder"));
        try
        {
            var result = DeleteLogic.GetFolderDeletionBlocker(tempDir);
            result.ShouldBe("contains a file and a folder");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetFolderDeletionBlocker_FolderWithFiles_ReturnsBlocker()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "test.txt"), "");
        try
        {
            var result = DeleteLogic.GetFolderDeletionBlocker(tempDir);
            result.ShouldNotBeNull();
            result.ShouldBe("contains a file");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetFolderDeletionBlocker_FolderWithSubdirectories_ReturnsBlocker()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tempDir, "subfolder"));
        try
        {
            var result = DeleteLogic.GetFolderDeletionBlocker(tempDir);
            result.ShouldNotBeNull();
            result.ShouldBe("contains a folder");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetFolderDeletionBlocker_MultipleFiles_ReturnsPluralBlocker()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "a.txt"), "");
        File.WriteAllText(Path.Combine(tempDir, "b.txt"), "");
        try
        {
            var result = DeleteLogic.GetFolderDeletionBlocker(tempDir);
            result.ShouldBe("contains 2 files");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetFolderDeletionBlocker_NonExistentFolder_ReturnsNull()
    {
        var fakePath = Path.Combine(Path.GetTempPath(), "GumTest_DoesNotExist_" + Guid.NewGuid());
        DeleteLogic.GetFolderDeletionBlocker(fakePath).ShouldBeNull();
    }

    [Fact]
    public void PerformConfirmedMixedTypeDelete_DeletingAllInstances_SelectsOwningElement()
    {
        // Issue #3025: deleting every selected instance left selection empty, so the
        // editor went blank. With no surviving sibling, selection should fall back to
        // the owning screen/component.
        ScreenSave screen = CreateScreenWithInstances("InstA", "InstB");
        TrackSelectionState();
        List<InstanceSave> instances = screen.Instances.ToList();

        _deleteLogic.PerformConfirmedMixedTypeDelete(
            new List<ElementSave>(),
            new List<BehaviorSave>(),
            instances,
            instances.Cast<object>().ToArray(),
            optionsWindow: null);

        _selectedState.Object.SelectedInstance.ShouldBeNull();
        _selectedState.Object.SelectedElement.ShouldBe(screen);
    }

    [Fact]
    public void PerformConfirmedMixedTypeDelete_DeletingSubsetOfInstances_SelectsRemainingSibling()
    {
        // Issue #3025: deleting multiple instances and confirming deselected everything,
        // leaving the editor blank. A surviving sibling should be selected instead.
        ScreenSave screen = CreateScreenWithInstances("InstA", "InstB", "InstC");
        TrackSelectionState();
        InstanceSave instC = screen.Instances.Single(i => i.Name == "InstC");
        List<InstanceSave> toDelete = screen.Instances.Where(i => i.Name != "InstC").ToList();

        _deleteLogic.PerformConfirmedMixedTypeDelete(
            new List<ElementSave>(),
            new List<BehaviorSave>(),
            toDelete,
            toDelete.Cast<object>().ToArray(),
            optionsWindow: null);

        _selectedState.Object.SelectedInstance.ShouldBe(instC);
    }

    [Fact]
    public void PerformConfirmedMixedTypeDelete_MultipleInstancesInSameElement_FiresInstancesDeleteOnPluginManager()
    {
        // Regression: deleting multiple instances at once never fired InstancesDelete on the
        // plugin manager, so plugins listening for instance removal (e.g. the codegen plugin
        // that regenerates a screen's .Generated.cs) never refreshed.
        ScreenSave screen = CreateScreenWithInstances("InstA", "InstB");
        List<InstanceSave> instances = screen.Instances.ToList();

        _deleteLogic.PerformConfirmedMixedTypeDelete(
            new List<ElementSave>(),
            new List<BehaviorSave>(),
            instances,
            instances.Cast<object>().ToArray(),
            optionsWindow: null);

        _pluginManager.Verify(
            p => p.InstancesDelete(
                screen,
                It.Is<InstanceSave[]>(arr =>
                    arr.Length == 2 &&
                    arr.Contains(instances[0]) &&
                    arr.Contains(instances[1]))),
            Times.Once);
    }

    [Fact]
    public void PerformConfirmedSingleInstanceDelete_DeletingChildWithParentInstance_SelectsParentInstance()
    {
        // Characterization: a child with a parent instance and no siblings under that parent
        // falls back to the parent instance.
        ScreenSave screen = CreateScreenWithInstances("Parent");
        InstanceSave parent = screen.Instances[0];
        InstanceSave child = AddChild(screen, "Child", "Parent");
        TrackSelectionState();
        _selectedState.Object.SelectedInstance = child;

        _deleteLogic.PerformConfirmedSingleInstanceDelete(
            selectedInstance: child,
            selectedElement: screen,
            selectedBehavior: null,
            objectsDeleted: new[] { child },
            optionsWindow: null);

        _selectedState.Object.SelectedInstance.ShouldBe(parent);
    }

    [Fact]
    public void PerformConfirmedSingleInstanceDelete_DeletingLastSibling_SelectsPreviousSibling()
    {
        // Characterization: deleting the last sibling falls back to the previous one.
        ScreenSave screen = CreateScreenWithInstances("InstA", "InstB", "InstC");
        InstanceSave instB = screen.Instances.Single(i => i.Name == "InstB");
        InstanceSave instC = screen.Instances.Single(i => i.Name == "InstC");
        TrackSelectionState();
        _selectedState.Object.SelectedInstance = instC;

        _deleteLogic.PerformConfirmedSingleInstanceDelete(
            selectedInstance: instC,
            selectedElement: screen,
            selectedBehavior: null,
            objectsDeleted: new[] { instC },
            optionsWindow: null);

        _selectedState.Object.SelectedInstance.ShouldBe(instB);
    }

    [Fact]
    public void PerformConfirmedSingleInstanceDelete_DeletingMiddleSibling_SelectsNextSibling()
    {
        // Characterization: deleting a middle sibling falls back to the next one.
        ScreenSave screen = CreateScreenWithInstances("InstA", "InstB", "InstC");
        InstanceSave instB = screen.Instances.Single(i => i.Name == "InstB");
        InstanceSave instC = screen.Instances.Single(i => i.Name == "InstC");
        TrackSelectionState();
        _selectedState.Object.SelectedInstance = instB;

        _deleteLogic.PerformConfirmedSingleInstanceDelete(
            selectedInstance: instB,
            selectedElement: screen,
            selectedBehavior: null,
            objectsDeleted: new[] { instB },
            optionsWindow: null);

        _selectedState.Object.SelectedInstance.ShouldBe(instC);
    }

    [Fact]
    public void PerformConfirmedSingleInstanceDelete_DeletingOnlyChildWithNoParent_KeepsOwningElementSelected()
    {
        // Characterization: deleting the sole top-level instance leaves no sibling or parent,
        // so selection falls back to the owning element (instance cleared).
        ScreenSave screen = CreateScreenWithInstances("Inst");
        InstanceSave instance = screen.Instances[0];
        TrackSelectionState();
        _selectedState.Object.SelectedInstance = instance;

        _deleteLogic.PerformConfirmedSingleInstanceDelete(
            selectedInstance: instance,
            selectedElement: screen,
            selectedBehavior: null,
            objectsDeleted: new[] { instance },
            optionsWindow: null);

        _selectedState.Object.SelectedInstance.ShouldBeNull();
        _selectedState.Object.SelectedElement.ShouldBe(screen);
    }

    [Fact]
    public void PerformConfirmedSingleInstanceDelete_LiveSelectedElementIsNull_RestoresOwningElementBeforeFiringInstanceDelete()
    {
        // Reproduces the production NRE: the user has only the instance highlighted in the
        // tree, something in the delete chain (e.g. a DeleteConfirmed handler) clears live
        // SelectedElement before the InstanceDelete plugin event fires, and CodeOutputPlugin
        // then NREs reading live SelectedElement in RefreshCodeDisplay.
        ScreenSave screen = CreateScreenWithInstances("Inst");
        InstanceSave instance = screen.Instances[0];

        ElementSave? liveSelectedElement = null;
        _selectedState.SetupGet(s => s.SelectedElement).Returns(() => liveSelectedElement);
        _selectedState.SetupSet(s => s.SelectedElement = It.IsAny<ElementSave?>())
            .Callback<ElementSave?>(e => liveSelectedElement = e);

        ElementSave? selectedAtFireTime = null;
        _pluginManager
            .Setup(p => p.InstanceDelete(It.IsAny<ElementSave>(), instance))
            .Callback(() => selectedAtFireTime = liveSelectedElement);

        _deleteLogic.PerformConfirmedSingleInstanceDelete(
            selectedInstance: instance,
            selectedElement: screen,
            selectedBehavior: null,
            objectsDeleted: new[] { instance },
            optionsWindow: null);

        selectedAtFireTime.ShouldBe(screen);
    }

    [Fact]
    public void RemoveInstance_FiresInstanceDelete_WithOwningElementSelected()
    {
        // Plugins handling InstanceDelete (e.g. CodeOutputPlugin) read
        // _selectedState.SelectedElement to know which element to regenerate code for.
        // If the owning element is not the live SelectedElement when the event fires,
        // those plugins NRE (see MainCodeOutputPlugin.RefreshCodeDisplay) or silently
        // misroute. Simulate the in-the-wild state where the owning element has been
        // cleared from SelectedState before delete reaches RemoveInstance.
        ScreenSave screen = CreateScreenWithInstances("Inst");
        InstanceSave instance = screen.Instances[0];

        ElementSave? liveSelectedElement = null;
        _selectedState.SetupGet(s => s.SelectedElement).Returns(() => liveSelectedElement);
        _selectedState.SetupSet(s => s.SelectedElement = It.IsAny<ElementSave?>())
            .Callback<ElementSave?>(e => liveSelectedElement = e);

        ElementSave? selectedAtFireTime = null;
        _pluginManager
            .Setup(p => p.InstanceDelete(screen, instance))
            .Callback(() => selectedAtFireTime = liveSelectedElement);

        _deleteLogic.RemoveInstance(instance, screen);

        selectedAtFireTime.ShouldBe(screen);
    }

    [Fact]
    public void RemoveBehavior_RemovesBehaviorAndReferences()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "TestBehavior" };
        _gumProject.Behaviors.Add(behavior);
        _gumProject.BehaviorReferences.Add(new BehaviorReference { Name = behavior.Name });

        _deleteLogic.RemoveBehavior(behavior);

        _gumProject.Behaviors.ShouldNotContain(behavior);
        _gumProject.BehaviorReferences.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveBehavior_RemovesLinkFromReferencingElement()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "TestBehavior" };
        _gumProject.Behaviors.Add(behavior);

        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behavior.Name });
        _gumProject.Components.Add(component);

        _deleteLogic.RemoveBehavior(behavior);

        component.Behaviors.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveElement_Component_RemovesFromProjectLists()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        _gumProject.Components.Add(component);
        _gumProject.ComponentReferences.Add(new ElementReference { Name = component.Name });

        _deleteLogic.RemoveElement(component);

        _gumProject.Components.ShouldNotContain(component);
        _gumProject.ComponentReferences.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveElement_Screen_RemovesFromProjectLists()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        _gumProject.Screens.Add(screen);
        _gumProject.ScreenReferences.Add(new ElementReference { Name = screen.Name });

        _deleteLogic.RemoveElement(screen);

        _gumProject.Screens.ShouldNotContain(screen);
        _gumProject.ScreenReferences.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveInstance_WithChildren_RemovesOnlyInstance()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        var child = AddChild(screen, "Child", "Parent");

        _deleteLogic.RemoveInstance(parent, screen);

        screen.Instances.ShouldNotContain(parent);
        screen.Instances.ShouldContain(child, "Child should still exist after parent removal");
    }

    [Fact]
    public void RemoveParentReferencesToInstance_RemovesParentVariables()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        AddChild(screen, "Child1", "Parent");
        AddChild(screen, "Child2", "Parent");

        _deleteLogic.RemoveParentReferencesToInstance(parent, screen);

        screen.DefaultState.Variables
            .Where(v => v.GetRootName() == "Parent" && v.Value as string == "Parent")
            .ShouldBeEmpty("All parent references to Parent should be removed");
    }

    [Fact]
    public void RemoveParentReferencesToInstance_WithDottedReference_RemovesIt()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        AddChild(screen, "Child", "Parent.Container");

        _deleteLogic.RemoveParentReferencesToInstance(parent, screen);

        screen.DefaultState.Variables
            .Where(v => v.Value is string s && s.StartsWith("Parent."))
            .ShouldBeEmpty("Dotted parent references should be removed");
    }

    [Fact]
    public void GetDeleteImpactDetails_ElementWithDerivedComponents_ImpactDetailsContainsWarning()
    {
        // Arrange: ComponentA is the base type. ComponentB inherits from ComponentA.
        // When GetReferencesToElement is called for ComponentA, it should return a change
        // set that contains ComponentB in ElementsWithBaseTypeReference.
        // GetDeleteImpactDetails() on that result should contain a warning about ComponentB.
        ComponentSave componentA = new ComponentSave { Name = "ComponentA" };
        componentA.States.Add(new StateSave { Name = "Default", ParentContainer = componentA });

        ComponentSave componentB = new ComponentSave { Name = "ComponentB", BaseType = "ComponentA" };
        componentB.States.Add(new StateSave { Name = "Default", ParentContainer = componentB });

        ElementReferences impactChanges = new ElementReferences();
        impactChanges.ElementsWithBaseTypeReference.Add(componentB);

        _referenceFinder
            .Setup(r => r.GetReferencesToElement(componentA, componentA.Name))
            .Returns(impactChanges);

        string impactDetails = impactChanges.GetDeleteImpactDetails();

        impactDetails.ShouldNotBeNullOrEmpty();
        impactDetails.ShouldContain("ComponentB");
        impactDetails.ShouldContain("lose their base type");
    }

    [Fact]
    public void GetDeleteImpactDetails_ElementWithInstancesOfType_ImpactDetailsContainsWarning()
    {
        // Arrange: a screen has an instance of ComponentA.
        // Deleting ComponentA should warn about that instance becoming invalid.
        ComponentSave componentA = new ComponentSave { Name = "ComponentA" };
        componentA.States.Add(new StateSave { Name = "Default", ParentContainer = componentA });

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave instance = new InstanceSave { Name = "myComponent", BaseType = "ComponentA", ParentContainer = screen };
        screen.Instances.Add(instance);

        ElementReferences impactChanges = new ElementReferences();
        impactChanges.InstancesWithBaseTypeReference.Add((screen, instance));

        string impactDetails = impactChanges.GetDeleteImpactDetails();

        impactDetails.ShouldNotBeNullOrEmpty();
        impactDetails.ShouldContain("myComponent");
        impactDetails.ShouldContain("lose their type and become invalid");
    }

    [Fact]
    public void GetDeleteImpactDetails_NoReferences_ReturnsEmpty()
    {
        ElementReferences impactChanges = new ElementReferences();

        string impactDetails = impactChanges.GetDeleteImpactDetails();

        impactDetails.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void GetDeleteImpactDetails_WhenContainerIsAlsoBeingDeleted_ReturnsEmpty()
    {
        // Arrange: Item contains an instance of ItemContainer.
        // Deleting ItemContainer alone would warn that Item's instance will become invalid.
        // But since Item is also being deleted, the warning is irrelevant.
        var itemContainer = new ComponentSave { Name = "ItemContainer" };
        var item = new ComponentSave { Name = "Item" };
        var instance = new InstanceSave { Name = "itemContainerInstance", BaseType = "ItemContainer", ParentContainer = item };
        item.Instances.Add(instance);

        var impactChanges = new ElementReferences();
        impactChanges.InstancesWithBaseTypeReference.Add((item, instance));

        impactChanges.ExcludeContainersBeingDeleted([item, itemContainer]);

        impactChanges.GetDeleteImpactDetails().ShouldBeNullOrEmpty();
    }

    [Fact]
    public void ShowDeleteDialogMessage_WhenBothElementAndItsContainerAreDeleted_OmitsImpactWarning()
    {
        // Arrange: Item contains an instance of ItemContainer.
        // Deleting only ItemContainer would warn that Item's instance will become invalid.
        // When both are deleted together, that warning should be suppressed.
        var itemContainer = new ComponentSave { Name = "ItemContainer" };
        var item = new ComponentSave { Name = "Item" };
        var instance = new InstanceSave { Name = "itemContainerInstance", BaseType = "ItemContainer", ParentContainer = item };
        item.Instances.Add(instance);

        var impactChanges = new ElementReferences();
        impactChanges.InstancesWithBaseTypeReference.Add((item, instance));

        _referenceFinder
            .Setup(r => r.GetReferencesToElement(itemContainer, itemContainer.Name))
            .Returns(impactChanges);

        // Capture the impact text before BuildDeleteDialogMessage mutates impactChanges,
        // so we can assert it is absent without hardcoding any UI strings.
        var unfilteredImpact = impactChanges.GetDeleteImpactDetails();
        unfilteredImpact.ShouldNotBeNullOrEmpty("setup: ItemContainer deletion should have a suppressible warning");

        Array objectsToDelete = new object[] { item, itemContainer };
        var message = _deleteLogic.BuildDeleteDialogMessage(objectsToDelete);

        message.ShouldNotContain(unfilteredImpact);
    }

    [Fact]
    public void BuildDeleteDialogMessage_BehaviorWithReferencingElements_ShowsImpactDetails()
    {
        // Arrange: behavior is referenced by a component.
        BehaviorSave behavior = new BehaviorSave { Name = "Focusable" };
        ComponentSave button = new ComponentSave { Name = "Button" };

        BehaviorReferences impactChanges = new BehaviorReferences();
        impactChanges.ElementsWithBehaviorReference.Add((button, new ElementBehaviorReference { BehaviorName = "Focusable" }));

        _referenceFinder
            .Setup(r => r.GetReferencesToBehavior(behavior, behavior.Name))
            .Returns(impactChanges);

        Array objectsToDelete = new object[] { behavior };
        string message = _deleteLogic.BuildDeleteDialogMessage(objectsToDelete);

        message.ShouldContain("Button");
    }

    [Fact]
    public void BuildDeleteDialogMessage_InstanceWithOrphanedParentVariable_ShowsImpactDetails()
    {
        // Arrange: deleting an instance that has a Parent variable reference in another element.
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave instance = new InstanceSave { Name = "container", ParentContainer = screen };
        screen.Instances.Add(instance);
        _gumProject.Screens.Add(screen);

        VariableSave parentVar = new VariableSave { Name = "child.Parent", Value = "otherInstance.container" };
        ScreenSave otherScreen = new ScreenSave { Name = "OtherScreen" };

        InstanceReferences impactChanges = new InstanceReferences();
        impactChanges.ParentVariablesInOtherElements.Add((otherScreen, parentVar));

        _referenceFinder
            .Setup(r => r.GetReferencesToInstance(screen, instance, instance.Name))
            .Returns(impactChanges);

        Array objectsToDelete = new object[] { instance };
        string message = _deleteLogic.BuildDeleteDialogMessage(objectsToDelete);

        message.ShouldContain("OtherScreen");
    }

    [Fact]
    public void BuildDeleteDialogMessage_InstanceWithNoOrphans_ContainsNoImpactWarning()
    {
        // Arrange: no orphaned references — impact section should be absent.
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave instance = new InstanceSave { Name = "sprite", ParentContainer = screen };
        screen.Instances.Add(instance);
        _gumProject.Screens.Add(screen);

        // default mock already returns empty InstanceReferences

        Array objectsToDelete = new object[] { instance };
        string message = _deleteLogic.BuildDeleteDialogMessage(objectsToDelete);

        string impactDetails = new InstanceReferences().GetDeleteImpactDetails();
        // no impact section — message should only contain the item name, no extra impact text
        message.ShouldContain("sprite");
        message.ShouldNotContain("invalid");
    }

    /// <summary>
    /// Wires the ISelectedState mock so SelectedInstance/SelectedInstances/SelectedElement
    /// behave like the real backing store (reads reflect the most recent write), which the
    /// post-delete selection-fallback logic depends on.
    /// </summary>
    private void TrackSelectionState()
    {
        List<InstanceSave> selectedInstances = new();
        InstanceSave? selectedInstance = null;
        ElementSave? selectedElement = null;

        _selectedState.SetupGet(s => s.SelectedInstances).Returns(() => selectedInstances);
        _selectedState.SetupSet(s => s.SelectedInstances = It.IsAny<IEnumerable<InstanceSave>>())
            .Callback<IEnumerable<InstanceSave>>(value =>
            {
                selectedInstances = value?.ToList() ?? new();
                selectedInstance = selectedInstances.FirstOrDefault();
            });

        _selectedState.SetupGet(s => s.SelectedInstance).Returns(() => selectedInstance);
        _selectedState.SetupSet(s => s.SelectedInstance = It.IsAny<InstanceSave?>())
            .Callback<InstanceSave?>(value =>
            {
                selectedInstance = value;
                selectedInstances = value == null ? new() : new() { value };
            });

        _selectedState.SetupGet(s => s.SelectedElement).Returns(() => selectedElement);
        _selectedState.SetupSet(s => s.SelectedElement = It.IsAny<ElementSave?>())
            .Callback<ElementSave?>(value => selectedElement = value);
    }

    private ScreenSave CreateScreenWithInstances(params string[] instanceNames)
    {
        var screen = new ScreenSave();
        screen.Name = "TestScreen";

        var defaultState = new StateSave();
        defaultState.ParentContainer = screen;
        defaultState.Name = "Default";
        screen.States.Add(defaultState);

        foreach (var name in instanceNames)
        {
            var instance = new InstanceSave();
            instance.Name = name;
            instance.ParentContainer = screen;
            screen.Instances.Add(instance);
        }

        return screen;
    }

    private InstanceSave AddChild(ScreenSave screen, string childName, string parentName)
    {
        var child = new InstanceSave();
        child.Name = childName;
        child.ParentContainer = screen;
        screen.Instances.Add(child);

        screen.DefaultState.SetValue($"{childName}.Parent", parentName, "string");

        return child;
    }
}
