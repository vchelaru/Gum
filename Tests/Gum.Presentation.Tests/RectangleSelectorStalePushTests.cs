using Gum;
using Gum.Commands;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins #4545: dragging the editor's own scroll bar or panel-divider thumb (outside the wireframe
/// canvas) and moving the mouse into the canvas while still holding the button showed the marquee
/// selector, even though no push ever happened inside the canvas for that drag.
///
/// Root cause: HandleRelease's early-return path (an ordinary click that never activated the
/// selector - e.g. clicking empty canvas space to deselect) never reset _hasValidPush, so the flag
/// stayed true indefinitely after that click. HandleDrag's only guard against acting on an
/// unrelated drag is "if (!_hasValidPush) return;" - with the flag stuck true, any later
/// PrimaryDown sequence (including one that entered the canvas already held, like a WinForms
/// scroll bar drag) computed a "drag distance" from the stale old click position to wherever the
/// cursor entered, which is almost always past the activation threshold.
/// </summary>
public class RectangleSelectorStalePushTests
{
    private readonly Mock<IHotkeyManager> _mockHotkeyManager;
    private readonly Mock<IWireframeObjectManager> _mockWireframeManager;
    private readonly Mock<ISelectionManager> _mockSelectionManager;
    private readonly Mock<IGuiCommands> _mockGuiCommands;
    private readonly Mock<ISelectionRectangleVisual> _mockSelectionRectangleVisual;
    private readonly Mock<IGumCursorState> _mockCursor;
    private readonly Camera _camera;
    private readonly RectangleSelector _rectangleSelector;
    private bool _isShiftPressed;

    public RectangleSelectorStalePushTests()
    {
        _mockHotkeyManager = new Mock<IHotkeyManager>();
        _mockWireframeManager = new Mock<IWireframeObjectManager>();
        _mockSelectionManager = new Mock<ISelectionManager>();
        _mockGuiCommands = new Mock<IGuiCommands>();
        _mockSelectionRectangleVisual = new Mock<ISelectionRectangleVisual>();
        _mockCursor = new Mock<IGumCursorState>();
        _camera = new Camera { Zoom = 1f };

        _mockHotkeyManager.Setup(x => x.IsPressedInControl(It.IsAny<KeyCombination>()))
            .Returns(() => _isShiftPressed);

        _rectangleSelector = new RectangleSelector(
            _mockHotkeyManager.Object,
            _mockWireframeManager.Object,
            _mockSelectionManager.Object,
            _mockGuiCommands.Object,
            _camera,
            _mockCursor.Object,
            _mockSelectionRectangleVisual.Object);
    }

    private void SetCursorPosition(float x, float y)
    {
        _mockCursor.SetupGet(c => c.X).Returns(x);
        _mockCursor.SetupGet(c => c.Y).Returns(y);
    }

    [Fact]
    public void HandleDrag_ShouldNotActivate_AfterAnUnactivatedClickWithNoNewPush()
    {
        _isShiftPressed = false;
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);

        // An ordinary click on empty canvas space: push, then release without ever dragging far
        // enough to activate the selector.
        SetCursorPosition(100f, 100f);
        _rectangleSelector.HandlePush(100f, 100f);
        _rectangleSelector.HandleRelease();

        _rectangleSelector.IsActive.ShouldBeFalse();

        // Simulate the scroll bar / divider scenario: the mouse button went down outside the
        // canvas (so HandlePush was never called for this press) and the cursor is dragged into
        // the canvas far from the earlier click - with no intervening HandlePush.
        SetCursorPosition(400f, 400f);
        _rectangleSelector.HandleDrag(isHandlerActive: false);

        _rectangleSelector.IsActive.ShouldBeFalse();
        _rectangleSelector.HasMovedEnough.ShouldBeFalse();
    }

    [Fact]
    public void HandleRelease_ShouldStillAllowANewDragToActivate_AfterAnEarlierUnactivatedClick()
    {
        _isShiftPressed = false;
        _mockSelectionManager.Setup(x => x.IsOverBody).Returns(false);

        // An earlier ordinary click that never activated the selector.
        SetCursorPosition(100f, 100f);
        _rectangleSelector.HandlePush(100f, 100f);
        _rectangleSelector.HandleRelease();

        // A genuine new push-and-drag inside the canvas should still work normally afterward.
        SetCursorPosition(200f, 200f);
        _rectangleSelector.HandlePush(200f, 200f);
        SetCursorPosition(220f, 200f);
        _rectangleSelector.HandleDrag(isHandlerActive: false);

        _rectangleSelector.IsActive.ShouldBeTrue();
    }
}
