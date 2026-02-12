using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace GumToolUnitTests.Wireframe;

/// <summary>
/// Tests for SelectionManager methods related to rectangle selection:
/// DeselectAll, Select, and ToggleSelection.
/// </summary>
public class SelectionManagerRectangleTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _mockSelectedState;
    private readonly Mock<IWireframeObjectManager> _mockWireframeManager;
    private readonly SelectionManager _selectionManager;
    private readonly Mock<LayerService> _layerService;
    private readonly List<InstanceSave> _selectedInstances;
    private readonly Mock<IProjectManager> _projectManager;

    public SelectionManagerRectangleTests()
    {
        SystemManagers.Default = new SystemManagers();
        SystemManagers.Default.Renderer = new Renderer();
        SystemManagers.Default.Renderer.AddLayer();
        SystemManagers.Default.ShapeManager = new RenderingLibrary.Math.Geometry.ShapeManager();

        SystemManagers.Default.TextManager = new TextManager();

        _mockSelectedState = new Mock<ISelectedState>();
        _mockWireframeManager = new Mock<IWireframeObjectManager>();
        _selectedInstances = new List<InstanceSave>();

        _projectManager = new Mock<IProjectManager>();
        _projectManager
            .Setup(x => x.GeneralSettingsFile)
            .Returns(new Gum.Settings.GeneralSettingsFile ());

        // Setup SelectedInstances to return our test list
        _mockSelectedState.Setup(x => x.SelectedInstances).Returns(() => _selectedInstances);
        _mockSelectedState.SetupSet(x => x.SelectedInstance = It.IsAny<InstanceSave?>())
            .Callback<InstanceSave?>(value =>
            {
                _selectedInstances.Clear();
                if (value != null)
                {
                    _selectedInstances.Add(value);
                }
            });

        _selectionManager = new SelectionManager(
            _mockSelectedState.Object,
            Mock.Of<IUndoManager>(),
            Mock.Of<IEditingManager>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IHotkeyManager>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            _mockWireframeManager.Object,
            _projectManager.Object,
            Mock.Of<IGuiCommands>(),
            Mock.Of<IElementCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<IUiSettingsService>());

        _layerService = new Mock<LayerService>();

        _selectionManager.Initialize(_layerService.Object);
    }

    #region DeselectAll Tests

    [Fact]
    public void DeselectAll_ShouldClearSelectedInstance()
    {
        // Arrange
        var instance = new InstanceSave { Name = "TestInstance" };
        _selectedInstances.Add(instance);

        // Act
        _selectionManager.DeselectAll();

        // Assert
        _mockSelectedState.VerifySet(x => x.SelectedInstance = null, Times.Once);
    }

    [Fact]
    public void DeselectAll_ShouldClearLocalSelection()
    {
        // Arrange
        var gue = new GraphicalUiElement();
        gue.Tag = new InstanceSave { Name = "TestInstance" };
        _selectionManager.SelectedGue = gue;

        // Act
        _selectionManager.DeselectAll();

        // Assert
        _selectionManager.SelectedGue.ShouldBeNull();
    }

    #endregion

    #region Select Tests

    [Fact]
    public void Select_ShouldDeselectAll_WhenGivenEmptyList()
    {
        // Arrange
        var instance = new InstanceSave { Name = "ExistingInstance" };
        _selectedInstances.Add(instance);

        // Act
        _selectionManager.Select(new List<GraphicalUiElement>());

        // Assert
        _selectionManager.SelectedGue.ShouldBeNull();
    }

    [Fact]
    public void Select_ShouldDeselectAll_WhenGivenNull()
    {
        // Arrange
        var instance = new InstanceSave { Name = "ExistingInstance" };
        _selectedInstances.Add(instance);

        // Act
        _selectionManager.Select(null!);

        // Assert
        _selectionManager.SelectedGue.ShouldBeNull();
    }

    [Fact]
    public void Select_ShouldSelectMultipleElements_WhenGivenMultipleGues()
    {
        // Arrange
        var instance1 = new InstanceSave { Name = "Instance1" };
        var instance2 = new InstanceSave { Name = "Instance2" };
        var instance3 = new InstanceSave { Name = "Instance3" };

        var gue1 = new GraphicalUiElement();
        gue1.Tag = instance1;
        var gue2 = new GraphicalUiElement();
        gue2.Tag = instance2;
        var gue3 = new GraphicalUiElement();
        gue3.Tag = instance3;

        var elementStack = new List<ElementWithState>();
        _mockSelectedState.Setup(x => x.GetTopLevelElementStack()).Returns(elementStack);
        _mockWireframeManager.Setup(x => x.GetRepresentation(instance1, elementStack)).Returns(gue1);
        _mockWireframeManager.Setup(x => x.GetRepresentation(instance2, elementStack)).Returns(gue2);
        _mockWireframeManager.Setup(x => x.GetRepresentation(instance3, elementStack)).Returns(gue3);

        var elementsToSelect = new List<GraphicalUiElement> { gue1, gue2, gue3 };

        // Act
        _selectionManager.Select(elementsToSelect);

        // Assert
        _mockSelectedState.VerifySet(x => x.SelectedInstances = It.Is<List<InstanceSave>>(
            list => list.Count == 3 &&
                    list.Contains(instance1) &&
                    list.Contains(instance2) &&
                    list.Contains(instance3)),
            Times.Once);
    }

    [Fact]
    public void Select_ShouldIgnoreElementsWithoutInstanceSaveTag()
    {
        // Arrange
        var instance1 = new InstanceSave { Name = "Instance1" };
        var gue1 = new GraphicalUiElement();
        gue1.Tag = instance1;

        var gue2 = new GraphicalUiElement();
        gue2.Tag = new ComponentSave(); // Not an InstanceSave

        var elementStack = new List<ElementWithState>();
        _mockSelectedState.Setup(x => x.GetTopLevelElementStack()).Returns(elementStack);
        _mockWireframeManager.Setup(x => x.GetRepresentation(instance1, elementStack)).Returns(gue1);

        var elementsToSelect = new List<GraphicalUiElement> { gue1, gue2 };

        // Act
        _selectionManager.Select(elementsToSelect);

        // Assert
        _mockSelectedState.VerifySet(x => x.SelectedInstances = It.Is<List<InstanceSave>>(
            list => list.Count == 1 && list.Contains(instance1)),
            Times.Once);
    }

    #endregion

    #region ToggleSelection Tests

    [Fact]
    public void ToggleSelection_ShouldAddElement_WhenNotCurrentlySelected()
    {
        // Arrange
        var existingInstance = new InstanceSave { Name = "Existing" };
        var newInstance = new InstanceSave { Name = "New" };
        _selectedInstances.Add(existingInstance);

        var gue = new GraphicalUiElement();
        gue.Tag = newInstance;

        var elementStack = new List<ElementWithState>();
        _mockSelectedState.Setup(x => x.GetTopLevelElementStack()).Returns(elementStack);
        _mockWireframeManager.Setup(x => x.GetRepresentation(existingInstance, elementStack))
            .Returns(new GraphicalUiElement { Tag = existingInstance });
        _mockWireframeManager.Setup(x => x.GetRepresentation(newInstance, elementStack))
            .Returns(gue);

        // Act
        _selectionManager.ToggleSelection(gue);

        // Assert
        _mockSelectedState.VerifySet(x => x.SelectedInstances = It.Is<List<InstanceSave>>(
            list => list.Count == 2 &&
                    list.Contains(existingInstance) &&
                    list.Contains(newInstance)),
            Times.Once);
    }

    [Fact]
    public void ToggleSelection_ShouldRemoveElement_WhenCurrentlySelected()
    {
        // Arrange
        var instance1 = new InstanceSave { Name = "Instance1" };
        var instance2 = new InstanceSave { Name = "Instance2" };
        _selectedInstances.Add(instance1);
        _selectedInstances.Add(instance2);

        var gue = new GraphicalUiElement();
        gue.Tag = instance2;

        var elementStack = new List<ElementWithState>();
        _mockSelectedState.Setup(x => x.GetTopLevelElementStack()).Returns(elementStack);
        _mockWireframeManager.Setup(x => x.GetRepresentation(instance1, elementStack))
            .Returns(new GraphicalUiElement { Tag = instance1 });

        // Act
        _selectionManager.ToggleSelection(gue);

        // Assert
        _mockSelectedState.VerifySet(x => x.SelectedInstances = It.Is<List<InstanceSave>>(
            list => list.Count == 1 && list.Contains(instance1)),
            Times.Once);
    }

    [Fact]
    public void ToggleSelection_ShouldDeselectAll_WhenTogglingLastSelectedElement()
    {
        // Arrange
        var instance = new InstanceSave { Name = "OnlyInstance" };
        _selectedInstances.Add(instance);

        var gue = new GraphicalUiElement();
        gue.Tag = instance;

        // Act
        _selectionManager.ToggleSelection(gue);

        // Assert
        _selectionManager.SelectedGue.ShouldBeNull();
    }

    [Fact]
    public void ToggleSelection_ShouldDoNothing_WhenElementHasNoInstanceSaveTag()
    {
        // Arrange
        var existingInstance = new InstanceSave { Name = "Existing" };
        _selectedInstances.Add(existingInstance);

        var gue = new GraphicalUiElement();
        gue.Tag = new ComponentSave(); // Not an InstanceSave

        // Act
        _selectionManager.ToggleSelection(gue);

        // Assert - Should not change selection
        _selectedInstances.Count.ShouldBe(1);
        _selectedInstances[0].ShouldBe(existingInstance);
    }

    [Fact]
    public void ToggleSelection_ShouldDoNothing_WhenElementIsNull()
    {
        // Arrange
        var existingInstance = new InstanceSave { Name = "Existing" };
        _selectedInstances.Add(existingInstance);

        // Act
        _selectionManager.ToggleSelection(null!);

        // Assert - Should not change selection
        _selectedInstances.Count.ShouldBe(1);
        _selectedInstances[0].ShouldBe(existingInstance);
    }

    #endregion
}
