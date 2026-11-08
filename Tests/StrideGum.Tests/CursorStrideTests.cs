using Gum.Input;
using Moq;
using Shouldly;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Input;
using System.Collections.Generic;

namespace StrideGum.Tests;

/// <summary>
/// Unit tests for the Stride cursor: position and button mapping from an <see cref="IMouseDevice"/>,
/// and scroll accumulation from the <see cref="MouseWheelEvent"/> listener. The cursor is attached to
/// a mocked mouse device and read through its public surface after <c>Activity</c>.
/// </summary>
public class CursorStrideTests : SkiaHostTests.CursorTestsBase
{
    private sealed class StrideCursorRig : SkiaHostTests.IHostCursorRig
    {
        private readonly Cursor _cursor;
        private readonly Mock<IMouseDevice> _mouse;

        public StrideCursorRig()
        {
            (_cursor, _mouse) = CreateAttachedCursor();
        }

        public Gum.Wireframe.ICursor Cursor => _cursor;

        // Stride's position is normalized (0..1) and Cursor.Stride scales it by SurfaceSize (800x600).
        public void SetPosition(int x, int y) => _mouse.SetupGet(m => m.Position).Returns(new Vector2(x / 800f, y / 600f));

        public void SetPrimaryDown(bool isDown) => _mouse.SetupGet(m => m.DownButtons).Returns(
            new ReadOnlySet<MouseButton>(isDown ? new HashSet<MouseButton> { MouseButton.Left } : new HashSet<MouseButton>()));

        public void Scroll(int notches) =>
            ((IInputEventListener<MouseWheelEvent>)_cursor).ProcessEvent(new MouseWheelEvent { WheelDelta = notches });
    }

    protected override SkiaHostTests.IHostCursorRig CreateRig() => new StrideCursorRig();

    private static (Cursor cursor, Mock<IMouseDevice> mouse) CreateAttachedCursor()
    {
        var mouse = new Mock<IMouseDevice>();
        mouse.SetupGet(m => m.SurfaceSize).Returns(new Vector2(800, 600));
        mouse.SetupGet(m => m.DownButtons).Returns(new ReadOnlySet<MouseButton>(new HashSet<MouseButton>()));

        var cursor = new Cursor();
        cursor.AttachStrideInput(mouse.Object);
        return (cursor, mouse);
    }

    [Fact]
    public void GetMouseState_ReturnsDefault_WhenNoMouseIsAttached()
    {
        var cursor = new Cursor();
        cursor.AttachStrideInput(null);

        Should.NotThrow(() => cursor.Activity(0));
        cursor.PrimaryDown.ShouldBeFalse();
    }
}
