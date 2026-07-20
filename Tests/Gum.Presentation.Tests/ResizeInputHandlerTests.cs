using Gum.DataTypes;
using Gum.Input;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins ResizeInputHandler.GetCursorToShow's resize-side-to-cursor mapping — converted from
/// returning a WinForms Cursor to the neutral GumCursorKind as part of relocating the
/// input-handler family to headless Gum.Presentation (#3846).
/// </summary>
public class ResizeInputHandlerTests
{
    private static ResizeInputHandler CreateSut(ResizeSide sideOver, out Mock<IGumCursorState> cursor)
    {
        var handlesVisual = new Mock<IResizeHandlesVisual>();
        handlesVisual.SetupGet(h => h.Visible).Returns(true);
        handlesVisual.Setup(h => h.GetSideOver(It.IsAny<float>(), It.IsAny<float>())).Returns(sideOver);

        var mockCursor = new Mock<IGumCursorState>();
        // UpdateHover only updates _sideOver on a push or while the button is up (not mid-drag).
        mockCursor.SetupGet(c => c.PrimaryPush).Returns(true);
        cursor = mockCursor;

        var wireframeObjectManager = new Mock<IWireframeObjectManager>();
        wireframeObjectManager.Setup(w => w.GetRepresentation(It.IsAny<ElementSave>())).Returns(new GraphicalUiElement());

        var context = EditorContextTestHelper.Create(
            wireframeObjectManager: wireframeObjectManager.Object,
            cursor: mockCursor.Object);

        var sut = new ResizeInputHandler(context, handlesVisual.Object);
        sut.UpdateHover(0f, 0f);
        return sut;
    }

    [Theory]
    [InlineData(ResizeSide.TopLeft, GumCursorKind.SizeNWSE)]
    [InlineData(ResizeSide.BottomRight, GumCursorKind.SizeNWSE)]
    [InlineData(ResizeSide.TopRight, GumCursorKind.SizeNESW)]
    [InlineData(ResizeSide.BottomLeft, GumCursorKind.SizeNESW)]
    [InlineData(ResizeSide.Top, GumCursorKind.SizeNS)]
    [InlineData(ResizeSide.Bottom, GumCursorKind.SizeNS)]
    [InlineData(ResizeSide.Left, GumCursorKind.SizeWE)]
    [InlineData(ResizeSide.Right, GumCursorKind.SizeWE)]
    public void GetCursorToShow_ShouldMapResizeSideToMatchingCursorKind(ResizeSide sideOver, GumCursorKind expected)
    {
        // Arrange
        var sut = CreateSut(sideOver, out _);

        // Act
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBe(expected);
    }

    [Fact]
    public void GetCursorToShow_ShouldReturnNull_WhenNoSideIsOver()
    {
        // Arrange
        var sut = CreateSut(ResizeSide.None, out _);

        // Act
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBeNull();
    }
}
