using Gum.Input;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins how GrabbedState turns cursor movement into drag movement around the 6-pixel dead zone
/// (#5257): the movement made inside the dead zone is applied once the drag starts, so the grabbed
/// point stays under the cursor.
/// </summary>
public class GrabbedStateDragTests
{
    private static (GrabbedState sut, Mock<IGumCursorState> cursor) CreatePushedSut(float pushX, float pushY, float displayScale = 1)
    {
        Mock<IGumCursorState> cursor = new Mock<IGumCursorState>();
        cursor.SetupGet(c => c.X).Returns(pushX);
        cursor.SetupGet(c => c.Y).Returns(pushY);
        GrabbedState sut = new GrabbedState(
            Mock.Of<ISelectedState>(),
            Mock.Of<IWireframeObjectManager>(),
            cursor.Object,
            new CanvasDisplayScale { DisplayScale = displayScale });
        sut.HandlePush();
        cursor.SetupGet(c => c.PrimaryDown).Returns(true);
        return (sut, cursor);
    }

    private static void MoveCursor(Mock<IGumCursorState> cursor, float x, float y, float xChange, float yChange)
    {
        cursor.SetupGet(c => c.X).Returns(x);
        cursor.SetupGet(c => c.Y).Returns(y);
        cursor.SetupGet(c => c.XChange).Returns(xChange);
        cursor.SetupGet(c => c.YChange).Returns(yChange);
    }

    [Fact]
    public void BeginDragFrame_AppliesTheWholeOffsetSincePush_OnTheFirstFrame_AndTheFrameChangeAfter()
    {
        (GrabbedState sut, Mock<IGumCursorState> cursor) = CreatePushedSut(pushX: 100, pushY: 50);

        MoveCursor(cursor, x: 104, y: 51, xChange: 2, yChange: 1);
        sut.HasMovedEnough.ShouldBeFalse();

        MoveCursor(cursor, x: 108, y: 53, xChange: 2, yChange: 1);
        sut.HasMovedEnough.ShouldBeTrue();
        sut.BeginDragFrame();
        sut.DragXChange.ShouldBe(8);
        sut.DragYChange.ShouldBe(3);

        MoveCursor(cursor, x: 110, y: 52, xChange: 2, yChange: -1);
        sut.BeginDragFrame();
        sut.DragXChange.ShouldBe(2);
        sut.DragYChange.ShouldBe(-1);
    }

    // The dead zone is in device-independent pixels, so on a 200% display it covers twice the
    // physical pixels (#5554).
    [Theory]
    [InlineData(1f, 6f, false)]
    [InlineData(1f, 7f, true)]
    [InlineData(2f, 7f, false)]
    [InlineData(2f, 12f, false)]
    [InlineData(2f, 13f, true)]
    public void HasMovedEnough_DeadZoneScalesWithDisplayScale(float displayScale, float offset, bool expected)
    {
        (GrabbedState sut, Mock<IGumCursorState> cursor) = CreatePushedSut(pushX: 100, pushY: 50, displayScale);

        MoveCursor(cursor, x: 100, y: 50 + offset, xChange: 0, yChange: offset);

        sut.HasMovedEnough.ShouldBe(expected);
    }

    [Fact]
    public void HasMovedEnough_StaysTrue_WhenTheCursorReturnsInsideTheDeadZoneAfterTheDragStarted()
    {
        (GrabbedState sut, Mock<IGumCursorState> cursor) = CreatePushedSut(pushX: 100, pushY: 50);
        MoveCursor(cursor, x: 110, y: 50, xChange: 10, yChange: 0);
        sut.BeginDragFrame();

        MoveCursor(cursor, x: 102, y: 50, xChange: -8, yChange: 0);

        sut.HasMovedEnough.ShouldBeTrue();
    }

    [Fact]
    public void HandlePush_RestartsTheDeadZone()
    {
        (GrabbedState sut, Mock<IGumCursorState> cursor) = CreatePushedSut(pushX: 100, pushY: 50);
        MoveCursor(cursor, x: 110, y: 50, xChange: 10, yChange: 0);
        sut.BeginDragFrame();

        sut.HandlePush();
        MoveCursor(cursor, x: 112, y: 50, xChange: 2, yChange: 0);

        sut.HasMovedEnough.ShouldBeFalse();
    }
}
