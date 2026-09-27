using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins MoveInputHandler.GetCursorToShow — converted from returning a WinForms Cursor to the
/// neutral GumCursorKind as part of relocating the input-handler family to headless
/// Gum.Presentation (#3846).
/// </summary>
public class MoveInputHandlerTests
{
    [Fact]
    public void GetCursorToShow_ShouldReturnSizeAll_WhenOverBodyAndBothAxesMovable()
    {
        // Arrange
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.SetupGet(s => s.IsOverBody).Returns(true);
        var context = EditorContextTestHelper.Create(selectionManager: selectionManager.Object);
        var sut = new MoveInputHandler(context);

        // Act
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBe(GumCursorKind.SizeAll);
    }

    [Fact]
    public void GetCursorToShow_ShouldReturnSizeWE_WhenOverBodyAndOnlyXMovable()
    {
        // Arrange
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.SetupGet(s => s.IsOverBody).Returns(true);
        var context = EditorContextTestHelper.Create(selectionManager: selectionManager.Object);
        context.IsYMovementEnabled = false;
        var sut = new MoveInputHandler(context);

        // Act
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBe(GumCursorKind.SizeWE);
    }

    [Fact]
    public void GetCursorToShow_ShouldReturnNull_WhenNotOverBody()
    {
        // Arrange
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.SetupGet(s => s.IsOverBody).Returns(false);
        var context = EditorContextTestHelper.Create(selectionManager: selectionManager.Object);
        var sut = new MoveInputHandler(context);

        // Act
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBeNull();
    }

    [Fact]
    public void HandlePush_WithACategoryButNoStateSelected_DoesNotStartAnEdit()
    {
        // Selecting a category in the States tab leaves no state to write to, so a drag would
        // throw on release when it saves the "changed" state.
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.SetupGet(s => s.IsOverBody).Returns(true);
        var selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(s => s.SelectedStateSave).Returns((StateSave?)null);
        var context = EditorContextTestHelper.Create(
            selectedState: selectedState.Object,
            selectionManager: selectionManager.Object);
        var sut = new MoveInputHandler(context);

        bool claimed = sut.HandlePush(0f, 0f);

        claimed.ShouldBeFalse();
        sut.IsActive.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void HandlePush_WithShiftHeld_ClaimsTheMove_OnlyOverASelectedBody(bool isOverSelectedBody, bool shouldClaim)
    {
        // #5258: Shift is both multi-select and axis lock. Over the selection it starts an
        // axis-locked move; over anything else it is left to the marquee to add to the selection.
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.SetupGet(s => s.IsOverBody).Returns(true);
        selectionManager.SetupGet(s => s.IsOverSelectedBody).Returns(isOverSelectedBody);
        var selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(s => s.SelectedStateSave).Returns(new StateSave());
        var hotkeyManager = new Mock<IHotkeyManager>();
        KeyCombination shift = KeyCombination.Shift();
        hotkeyManager.SetupGet(h => h.MultiSelect).Returns(shift);
        hotkeyManager.Setup(h => h.IsPressedInControl(shift)).Returns(true);
        var context = EditorContextTestHelper.Create(
            selectedState: selectedState.Object,
            selectionManager: selectionManager.Object,
            hotkeyManager: hotkeyManager.Object);
        var sut = new MoveInputHandler(context);

        bool claimed = sut.HandlePush(0f, 0f);

        claimed.ShouldBe(shouldClaim);
        sut.IsActive.ShouldBe(shouldClaim);
    }
}
