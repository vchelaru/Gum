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

namespace GumToolUnitTests.Wireframe;

/// <summary>
/// Tests for SelectionManager.HighlightedIpsoChanged callback behavior.
/// </summary>
public class SelectionManagerHighlightTests : BaseTestClass
{
    private readonly SelectionManager _selectionManager;
    private readonly Mock<LayerService> _layerService;

    public SelectionManagerHighlightTests()
    {
        SystemManagers.Default = new SystemManagers();
        SystemManagers.Default.Renderer = new Renderer();
        SystemManagers.Default.Renderer.AddLayer();
        SystemManagers.Default.ShapeManager = new RenderingLibrary.Math.Geometry.ShapeManager();
        SystemManagers.Default.TextManager = new TextManager();

        var mockSelectedState = new Mock<ISelectedState>();
        mockSelectedState.Setup(x => x.SelectedInstances).Returns(() => new List<InstanceSave>());

        var projectManager = new Mock<IProjectManager>();
        projectManager
            .Setup(x => x.GeneralSettingsFile)
            .Returns(new Gum.Settings.GeneralSettingsFile());

        _selectionManager = new SelectionManager(
            mockSelectedState.Object,
            Mock.Of<IUndoManager>(),
            Mock.Of<IEditingManager>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IHotkeyManager>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            Mock.Of<IWireframeObjectManager>(),
            projectManager.Object,
            Mock.Of<IGuiCommands>(),
            Mock.Of<IElementCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<IUiSettingsService>());

        _layerService = new Mock<LayerService>();
        _selectionManager.Initialize(_layerService.Object);
    }

    [Fact]
    public void HighlightedIpsoChanged_ShouldFire_WhenHighlightedIpsoChanges()
    {
        // Arrange
        var gue = new GraphicalUiElement();
        IPositionedSizedObject? receivedIpso = null;
        int callCount = 0;
        _selectionManager.HighlightedIpsoChanged += (ipso) =>
        {
            receivedIpso = ipso;
            callCount++;
        };

        // Act
        _selectionManager.HighlightedIpso = gue;

        // Assert
        callCount.ShouldBe(1);
        receivedIpso.ShouldBe(gue);
    }

    [Fact]
    public void HighlightedIpsoChanged_ShouldNotFire_WhenSameValueSetTwice()
    {
        // Arrange
        var gue = new GraphicalUiElement();
        _selectionManager.HighlightedIpso = gue;

        int callCount = 0;
        _selectionManager.HighlightedIpsoChanged += (ipso) => callCount++;

        // Act
        _selectionManager.HighlightedIpso = gue;

        // Assert
        callCount.ShouldBe(0);
    }

    [Fact]
    public void HighlightedIpsoChanged_ShouldFireWithNull_WhenCleared()
    {
        // Arrange
        var gue = new GraphicalUiElement();
        _selectionManager.HighlightedIpso = gue;

        IPositionedSizedObject? receivedIpso = gue; // initialize to non-null to verify it becomes null
        int callCount = 0;
        _selectionManager.HighlightedIpsoChanged += (ipso) =>
        {
            receivedIpso = ipso;
            callCount++;
        };

        // Act
        _selectionManager.HighlightedIpso = null;

        // Assert
        callCount.ShouldBe(1);
        receivedIpso.ShouldBeNull();
    }
}
