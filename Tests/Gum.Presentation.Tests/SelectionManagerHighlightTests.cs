using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Input;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for SelectionManager.HighlightedIpsoChanged callback behavior.
/// </summary>
public class SelectionManagerHighlightTests : BaseTestClass
{
    private readonly SelectionManager _selectionManager;

    public SelectionManagerHighlightTests()
    {
        var mockSelectedState = new Mock<ISelectedState>();
        mockSelectedState.Setup(x => x.SelectedInstances).Returns(() => new List<InstanceSave>());

        _selectionManager = new SelectionManager(
            mockSelectedState.Object,
            Mock.Of<IUndoManager>(),
            Mock.Of<IContextMenuState>(),
            Mock.Of<Gum.Services.Dialogs.IDialogService>(),
            Mock.Of<IHotkeyManager>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IWireframeEditorFactory>(),
            Mock.Of<INineSliceCoordinateRefresher>(),
            Mock.Of<IPreciseHitTester>());

        _selectionManager.Initialize(
            new Layer(),
            new Camera(),
            Mock.Of<IGumCursorState>(),
            Mock.Of<ISelectionRectangleVisual>(),
            Mock.Of<IHighlightOutlineVisual>(),
            Mock.Of<IHighlightOverlayVisual>());
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
