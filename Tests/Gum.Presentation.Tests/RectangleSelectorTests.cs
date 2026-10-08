using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Input;
using Gum.Managers;
using Gum.Services;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

public class RectangleSelectorTests
{
    private readonly Mock<IHotkeyManager> _mockHotkeyManager;
    private readonly Mock<IWireframeObjectManager> _mockWireframeManager;
    private readonly Mock<ISelectionManager> _mockSelectionManager;
    private readonly Mock<IGuiCommands> _mockGuiCommands;
    private readonly Mock<ISelectionRectangleVisual> _mockSelectionRectangleVisual;
    private readonly Mock<IGumCursorState> _mockCursor;
    private readonly Mock<IPreciseHitTester> _mockPreciseHitTester;
    private readonly Camera _camera;
    private readonly CanvasDisplayScale _displayScale;
    private readonly RectangleSelector _rectangleSelector;
    private bool _isShiftPressed;

    public RectangleSelectorTests()
    {
        _mockHotkeyManager = new Mock<IHotkeyManager>();
        _mockWireframeManager = new Mock<IWireframeObjectManager>();
        _mockSelectionManager = new Mock<ISelectionManager>();
        _mockGuiCommands = new Mock<IGuiCommands>();
        _mockSelectionRectangleVisual = new Mock<ISelectionRectangleVisual>();
        _mockCursor = new Mock<IGumCursorState>();
        _mockPreciseHitTester = new Mock<IPreciseHitTester>();
        _camera = new Camera { Zoom = 1f };
        _displayScale = new CanvasDisplayScale();

        // Setup the hotkey manager to use our test flag
        _mockHotkeyManager.Setup(x => x.IsPressedInControl(It.IsAny<KeyCombination>()))
            .Returns(() => _isShiftPressed);

        _rectangleSelector = new RectangleSelector(
            _mockHotkeyManager.Object,
            _mockWireframeManager.Object,
            _mockSelectionManager.Object,
            _mockGuiCommands.Object,
            _camera,
            _mockCursor.Object,
            _mockSelectionRectangleVisual.Object,
            _displayScale,
            _mockPreciseHitTester.Object);
    }

    private void SetShiftPressed(bool pressed)
    {
        _isShiftPressed = pressed;
    }

    private void SetCursorPosition(float x, float y)
    {
        _mockCursor.SetupGet(c => c.X).Returns(x);
        _mockCursor.SetupGet(c => c.Y).Returns(y);
    }

    #region HandlePush Tests

    [Fact]
    public void HandlePush_ShouldNotActivateImmediately_EvenWhenShiftHeld()
    {
        // Arrange - Even with shift held, push should not activate
        SetShiftPressed(true);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(true);

        // Act
        _rectangleSelector.HandlePush(100f, 50f);

        // Assert - Activation only happens on drag, not on push
        _rectangleSelector.IsActive.ShouldBeFalse();
        _rectangleSelector.HasMovedEnough.ShouldBeFalse();
    }

    [Fact]
    public void HandlePush_ShouldNotActivateImmediately_RegardlessOfConditions()
    {
        // Arrange - Test with conditions that would normally trigger activation
        SetShiftPressed(false);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);

        // Act
        _rectangleSelector.HandlePush(100f, 50f);

        // Assert - HandlePush should NOT activate (activation happens on drag)
        // This allows shift+click multi-select to work
        _rectangleSelector.IsActive.ShouldBeFalse();
        _rectangleSelector.HasMovedEnough.ShouldBeFalse();
    }

    #endregion

    #region HandleDrag Tests

    [Fact]
    public void HandleDrag_ShouldDoNothing_WhenNotActive()
    {
        // Arrange
        var initialBounds = _rectangleSelector.Bounds;

        // Act
        _rectangleSelector.HandleDrag();

        // Assert
        _rectangleSelector.Bounds.ShouldBe(initialBounds);
    }

    [Fact]
    public void HandleDrag_ShouldUpdateBounds_WhenActive()
    {
        // Arrange - push at (10,10), then drag past the minimum drag distance (3px at zoom 1).
        SetShiftPressed(false);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);
        SetCursorPosition(10f, 10f);
        _rectangleSelector.HandlePush(10f, 10f);

        // Act
        SetCursorPosition(20f, 25f);
        _rectangleSelector.HandleDrag();

        // Assert
        _rectangleSelector.IsActive.ShouldBeTrue();
        _rectangleSelector.HasMovedEnough.ShouldBeTrue();
        _rectangleSelector.Bounds.ShouldBe((10f, 10f, 20f, 25f));
    }

    // The minimum drag is in device-independent pixels, so on a 200% display it covers twice the
    // physical pixels (#5554).
    [Theory]
    [InlineData(1f, 2f, false)]
    [InlineData(1f, 3f, true)]
    [InlineData(2f, 5f, false)]
    [InlineData(2f, 6f, true)]
    public void HandleDrag_MinimumDragScalesWithDisplayScale(float displayScale, float dragDistance, bool expected)
    {
        _displayScale.DisplayScale = displayScale;
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);
        SetCursorPosition(10f, 10f);
        _rectangleSelector.HandlePush(10f, 10f);

        SetCursorPosition(10f + dragDistance, 10f);
        _rectangleSelector.HandleDrag();

        _rectangleSelector.IsActive.ShouldBe(expected);
    }

    #endregion

    #region HandleRelease Tests

    [Fact]
    public void HandleRelease_ShouldSelectOnlyElementsTheHitTesterSaysOverlap()
    {
        GraphicalUiElement overlapping = new GraphicalUiElement();
        GraphicalUiElement notOverlapping = new GraphicalUiElement();
        _mockWireframeManager.Setup(x => x.GetAllVisibleElements())
            .Returns(new[] { overlapping, notOverlapping });
        _mockPreciseHitTester
            .Setup(x => x.IntersectsRectangle(overlapping, 0f, 0f, 20f, 20f))
            .Returns(true);
        _mockPreciseHitTester
            .Setup(x => x.IntersectsRectangle(notOverlapping, It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>()))
            .Returns(false);
        System.Collections.Generic.List<GraphicalUiElement> selected = new();
        _mockSelectionManager
            .Setup(x => x.Select(It.IsAny<System.Collections.Generic.IEnumerable<GraphicalUiElement>>()))
            .Callback<System.Collections.Generic.IEnumerable<GraphicalUiElement>>(elements => selected.AddRange(elements));
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);
        SetCursorPosition(0f, 0f);
        _rectangleSelector.HandlePush(0f, 0f);
        SetCursorPosition(20f, 20f);
        _rectangleSelector.HandleDrag();

        _rectangleSelector.HandleRelease();

        selected.ShouldBe(new[] { overlapping });
    }

    [Fact]
    public void HandleRelease_ShouldDoNothing_WhenNeverActivated()
    {
        // Arrange - Push but don't drag (selector never activates)
        SetShiftPressed(false);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);
        _rectangleSelector.HandlePush(100f, 100f);
        // No HandleDrag call = selector never activates

        // Act
        _rectangleSelector.HandleRelease();

        // Assert - Should not call any selection methods
        _mockSelectionManager.Verify(x => x.Select(It.IsAny<System.Collections.Generic.IEnumerable<GraphicalUiElement>>()), Times.Never);
        _mockSelectionManager.Verify(x => x.ToggleSelection(It.IsAny<GraphicalUiElement>()), Times.Never);
        _rectangleSelector.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void HandleRelease_ShouldRemainInactive_WhenWasNeverActivated()
    {
        // Arrange - Simulate shift+click scenario (push + release, no drag)
        SetShiftPressed(true);
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);
        _rectangleSelector.HandlePush(100f, 100f);
        // No drag = no activation

        // Act
        _rectangleSelector.HandleRelease();

        // Assert - Selector should remain inactive
        _rectangleSelector.IsActive.ShouldBeFalse();
        _rectangleSelector.HasMovedEnough.ShouldBeFalse();
    }

    #endregion

    #region GetCursorToShow Tests

    [Fact]
    public void GetCursorToShow_ShouldReturnCrosshair_WhenShiftHeld()
    {
        // Arrange
        SetShiftPressed(true);

        // Act
        var cursor = _rectangleSelector.GetCursorToShow();

        // Assert
        cursor.ShouldNotBeNull();
        cursor.ShouldBe(GumCursorKind.Cross);
    }

    [Fact]
    public void GetCursorToShow_ShouldReturnNull_WhenShiftNotHeld()
    {
        // Arrange
        SetShiftPressed(false);

        // Act
        var cursor = _rectangleSelector.GetCursorToShow();

        // Assert
        cursor.ShouldBeNull();
    }

    #endregion
}
