using Gum.Input;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins RotationInputHandler.GetCursorToShow — converted from returning a WinForms Cursor to the
/// neutral GumCursorKind as part of relocating the input-handler family to headless
/// Gum.Presentation (#3846).
/// </summary>
public class RotationInputHandlerTests
{
    [Fact]
    public void GetCursorToShow_ShouldReturnHand_AfterUpdateHoverFindsCursorOverVisibleHandle()
    {
        // Arrange
        var handleVisual = new Mock<IRotationHandleVisual>();
        handleVisual.SetupGet(h => h.HandleVisible).Returns(true);
        handleVisual.Setup(h => h.HandleHasCursorOver(It.IsAny<float>(), It.IsAny<float>())).Returns(true);
        var context = EditorContextTestHelper.Create();
        var sut = new RotationInputHandler(context, handleVisual.Object);

        // Act
        sut.UpdateHover(0f, 0f);
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBe(GumCursorKind.Hand);
    }

    [Fact]
    public void GetCursorToShow_ShouldReturnNull_WhenHandleIsNotVisible()
    {
        // Arrange
        var handleVisual = new Mock<IRotationHandleVisual>();
        handleVisual.SetupGet(h => h.HandleVisible).Returns(false);
        handleVisual.Setup(h => h.HandleHasCursorOver(It.IsAny<float>(), It.IsAny<float>())).Returns(true);
        var context = EditorContextTestHelper.Create();
        var sut = new RotationInputHandler(context, handleVisual.Object);

        // Act
        sut.UpdateHover(0f, 0f);
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBeNull();
    }
}
