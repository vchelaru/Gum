using Gum.Input;
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
}
