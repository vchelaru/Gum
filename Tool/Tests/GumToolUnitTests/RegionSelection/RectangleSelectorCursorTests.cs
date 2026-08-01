using FlatRedBall.SpecializedXnaControls.RegionSelection;
using InputLibrary;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Drawing;

namespace GumToolUnitTests.RegionSelection;

/// <summary>
/// Pins RectangleSelector's cursor reporting after it was converted from returning a WinForms
/// <c>Cursor</c> (and assigning the process-wide <c>Cursor.Current</c>) to returning the neutral
/// <see cref="CursorKind"/> and assigning it on the <see cref="IInputHostControl"/> host, so the
/// selector works over a WPF-native canvas as well as a WinForms one.
/// </summary>
public class RectangleSelectorCursorTests
{
    // RectangleSelector only needs Renderer.Camera (for handle-position math), so build
    // a bare SystemManagers instead of the full SystemManagers.Initialize, which requires
    // a real GraphicsDevice for its SpriteRenderer.
    private static RectangleSelector CreateSelector() =>
        new RectangleSelector(new SystemManagers { Renderer = new Renderer() });

    // A host the Cursor reports as "in window" - IsInWindow maps the (0,0) default mouse state
    // through PointToClient and checks it against Width/Height.
    private static Mock<IInputHostControl> CreateHostInWindow()
    {
        Mock<IInputHostControl> host = new Mock<IInputHostControl>();
        host.Setup(h => h.PointToClient(It.IsAny<Point>())).Returns(new Point(10, 10));
        host.SetupGet(h => h.Width).Returns(100);
        host.SetupGet(h => h.Height).Returns(100);
        host.SetupProperty(h => h.Cursor, CursorKind.Arrow);
        return host;
    }

    private static Cursor CreateCursorOver(IInputHostControl host)
    {
        Cursor cursor = new Cursor();
        cursor.Initialize(host);
        return cursor;
    }

    [Theory]
    [InlineData(ResizeSide.TopLeft, CursorKind.SizeNWSE)]
    [InlineData(ResizeSide.BottomRight, CursorKind.SizeNWSE)]
    [InlineData(ResizeSide.TopRight, CursorKind.SizeNESW)]
    [InlineData(ResizeSide.BottomLeft, CursorKind.SizeNESW)]
    [InlineData(ResizeSide.Top, CursorKind.SizeNS)]
    [InlineData(ResizeSide.Bottom, CursorKind.SizeNS)]
    [InlineData(ResizeSide.Left, CursorKind.SizeWE)]
    [InlineData(ResizeSide.Right, CursorKind.SizeWE)]
    [InlineData(ResizeSide.Middle, CursorKind.SizeAll)]
    public void GetCursorToSet_ShouldMapGrabbedSideToMatchingCursorKind(ResizeSide sideGrabbed, CursorKind expected)
    {
        RectangleSelector selector = CreateSelector();
        selector.Visible = true;
        selector.SideGrabbed = sideGrabbed;

        selector.GetCursorToSet(CreateCursorOver(CreateHostInWindow().Object)).ShouldBe(expected);
    }

    [Fact]
    public void GetCursorToSet_ShouldReturnArrow_WhenNoSideIsGrabbedAndResetsCursorIfNotOver()
    {
        RectangleSelector selector = CreateSelector();
        selector.Visible = false;
        selector.ResetsCursorIfNotOver = true;

        selector.GetCursorToSet(CreateCursorOver(CreateHostInWindow().Object)).ShouldBe(CursorKind.Arrow);
    }

    [Fact]
    public void GetCursorToSet_ShouldReturnNull_WhenNoSideIsGrabbedAndDoesNotResetCursor()
    {
        RectangleSelector selector = CreateSelector();
        selector.Visible = false;
        selector.ResetsCursorIfNotOver = false;

        selector.GetCursorToSet(CreateCursorOver(CreateHostInWindow().Object)).ShouldBeNull();
    }

    [Fact]
    public void Activity_ShouldAssignTheCursorKindOnTheInputHost_WhenAutoSetsCursor()
    {
        Mock<IInputHostControl> host = CreateHostInWindow();
        Keyboard keyboard = new Keyboard();
        keyboard.Initialize(host.Object);

        RectangleSelector selector = CreateSelector();
        selector.Visible = true;
        selector.AutoSetsCursor = true;
        selector.SideGrabbed = ResizeSide.Left;

        selector.Activity(CreateCursorOver(host.Object), keyboard, host.Object);

        host.Object.Cursor.ShouldBe(CursorKind.SizeWE);
    }
}
