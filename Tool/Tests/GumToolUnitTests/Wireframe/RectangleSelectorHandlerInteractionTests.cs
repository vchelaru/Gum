using Gum;
using Gum.Commands;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace GumToolUnitTests.Wireframe;

/// <summary>
/// Tests for RectangleSelector interaction with input handlers (resize, rotate, polygon points, etc.).
/// These tests verify the fix for the bug where dragging handles triggered rectangle selection.
/// </summary>
public class RectangleSelectorHandlerInteractionTests : BaseTestClass
{
    private readonly Mock<IHotkeyManager> _mockHotkeyManager;
    private readonly Mock<IWireframeObjectManager> _mockWireframeManager;
    private readonly Mock<ISelectionManager> _mockSelectionManager;
    private readonly Mock<IGuiCommands> _mockGuiCommands;
    private readonly Mock<Layer> _mockLayer;
    private readonly RectangleSelector _rectangleSelector;
    private bool _isShiftPressed;

    public RectangleSelectorHandlerInteractionTests()
    {
        _mockHotkeyManager = new Mock<IHotkeyManager>();
        _mockWireframeManager = new Mock<IWireframeObjectManager>();
        _mockSelectionManager = new Mock<ISelectionManager>();
        _mockGuiCommands = new Mock<IGuiCommands>();
        _mockLayer = new Mock<Layer>();

        // Setup the hotkey manager to use our test flag
        _mockHotkeyManager.Setup(x => x.MultiSelect.IsPressedInControl())
            .Returns(() => _isShiftPressed);

        _rectangleSelector = new RectangleSelector(
            _mockHotkeyManager.Object,
            _mockWireframeManager.Object,
            _mockSelectionManager.Object,
            _mockGuiCommands.Object,
            _mockLayer.Object);
    }

    private void SetShiftPressed(bool pressed)
    {
        _isShiftPressed = pressed;
    }

    #region Handler Interaction Tests

    [Fact]
    public void HandleDrag_ShouldNotActivate_WhenHandlerIsActive()
    {
        // Arrange - Simulate conditions where rectangle selector would normally activate
        SetShiftPressed(false);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);

        // Simulate a push to initialize state
        _rectangleSelector.HandlePush(100f, 100f);

        // Act - Call HandleDrag with isHandlerActive = true (e.g., user is dragging a resize handle)
        _rectangleSelector.HandleDrag(isHandlerActive: true);

        // Assert - Rectangle selector should NOT activate
        _rectangleSelector.IsActive.ShouldBeFalse();
        _rectangleSelector.HasMovedEnough.ShouldBeFalse();
    }

    [Fact]
    public void HandleDrag_ShouldNotActivate_WhenHandlerIsActiveAndShiftPressed()
    {
        // Arrange - Even with shift pressed (which normally forces activation)
        SetShiftPressed(true);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);

        _rectangleSelector.HandlePush(100f, 100f);

        // Act - Handler active should take precedence over shift key
        _rectangleSelector.HandleDrag(isHandlerActive: true);

        // Assert - Rectangle selector should NOT activate despite shift being pressed
        _rectangleSelector.IsActive.ShouldBeFalse();
        _rectangleSelector.HasMovedEnough.ShouldBeFalse();
    }

    [Fact]
    public void HandleDrag_ShouldNotActivate_WhenHandlerIsActiveAndNotOverBody()
    {
        // Arrange - Both conditions that normally trigger activation
        SetShiftPressed(false);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);

        _rectangleSelector.HandlePush(100f, 100f);

        // Act - Handler active should prevent activation
        _rectangleSelector.HandleDrag(isHandlerActive: true);

        // Assert
        _rectangleSelector.IsActive.ShouldBeFalse();
    }

    [Fact (Skip = "Cannot be tested without mocking InputLibrary.Cursor")]
    public void HandleDrag_ShouldActivate_WhenHandlerNotActiveAndConditionsMet()
    {
        // This test documents that when isHandlerActive = false,
        // normal activation logic should proceed.
        // 
        // Current limitation: This requires mocking InputLibrary.Cursor.Self
        // to simulate drag movement exceeding the threshold.
        //
        // Integration test would be better suited for this scenario.
        
        // Arrange
        SetShiftPressed(true);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);
        
        _rectangleSelector.HandlePush(100f, 100f);

        // Act
        // Would need to mock cursor movement here
        _rectangleSelector.HandleDrag(isHandlerActive: false);

        // Assert
        // _rectangleSelector.IsActive.ShouldBeTrue(); // After movement threshold
    }

    [Fact]
    public void HandlePush_ShouldNotImmediatelyActivate_PreventingShiftClickBug()
    {
        // Arrange - This tests the original bug fix: push should not activate selector
        // This allows shift+click multi-select to work without triggering rectangle selection
        SetShiftPressed(true);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);

        // Act - Just push, no drag
        _rectangleSelector.HandlePush(100f, 100f);

        // Assert - Should NOT be active yet (activation happens on drag)
        _rectangleSelector.IsActive.ShouldBeFalse();
        _rectangleSelector.HasMovedEnough.ShouldBeFalse();
    }

    [Fact]
    public void HandleRelease_ShouldNotSelectElements_WhenNeverActivated()
    {
        // Arrange - Simulate shift+click (push then release without drag)
        SetShiftPressed(true);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);
        
        _rectangleSelector.HandlePush(100f, 100f);
        // No HandleDrag call = no activation

        // Act
        _rectangleSelector.HandleRelease();

        // Assert - Should not call Select because selector was never activated
        _mockSelectionManager.Verify(x => x.Select(It.IsAny<System.Collections.Generic.IEnumerable<GraphicalUiElement>>()), Times.Never);
        _mockSelectionManager.Verify(x => x.ToggleSelection(It.IsAny<GraphicalUiElement>()), Times.Never);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void HandleDrag_ShouldHandleMultipleCalls_WhenHandlerIsActive()
    {
        // Arrange - Simulate repeated drag calls while handler is active
        SetShiftPressed(false);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);
        
        _rectangleSelector.HandlePush(100f, 100f);

        // Act - Multiple drag calls with handler active
        _rectangleSelector.HandleDrag(isHandlerActive: true);
        _rectangleSelector.HandleDrag(isHandlerActive: true);
        _rectangleSelector.HandleDrag(isHandlerActive: true);

        // Assert - Should remain inactive
        _rectangleSelector.IsActive.ShouldBeFalse();
        _rectangleSelector.HasMovedEnough.ShouldBeFalse();
    }

    [Fact]
    public void HandleDrag_WithDefaultParameter_ShouldBehaveLikeHandlerNotActive()
    {
        // This test verifies backward compatibility - calling HandleDrag() without
        // the parameter should behave as if isHandlerActive = false

        // When isHandlerActive = false (default) and conditions are met,
        // the rectangle selector SHOULD activate based on cursor movement

        // Arrange
        SetShiftPressed(false);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);

        _rectangleSelector.HandlePush(100f, 100f);

        // Act - Call with default parameter (isHandlerActive defaults to false)
        _rectangleSelector.HandleDrag();

        // Assert - Should activate because handler is not active and conditions are met
        _rectangleSelector.IsActive.ShouldBeTrue();
        _rectangleSelector.HasMovedEnough.ShouldBeTrue();
    }

    #endregion
}
