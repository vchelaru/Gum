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
            .Returns(new ElementRenameChanges());

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

        ElementRenameChanges impactChanges = new ElementRenameChanges();
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

        ElementRenameChanges impactChanges = new ElementRenameChanges();
        impactChanges.InstancesWithBaseTypeReference.Add((screen, instance));

        string impactDetails = impactChanges.GetDeleteImpactDetails();

        impactDetails.ShouldNotBeNullOrEmpty();
        impactDetails.ShouldContain("myComponent");
        impactDetails.ShouldContain("lose their type and become invalid");
    }

    [Fact]
    public void GetDeleteImpactDetails_NoReferences_ReturnsEmpty()
    {
        ElementRenameChanges impactChanges = new ElementRenameChanges();

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

        var impactChanges = new ElementRenameChanges();
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

        var impactChanges = new ElementRenameChanges();
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
