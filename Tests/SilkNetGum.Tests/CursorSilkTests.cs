using Gum.Input;
using Moq;
using Shouldly;
using Silk.NET.Input;
using System.Collections.Generic;
using System.Numerics;

namespace SilkNetGum.Tests;

/// <summary>
/// Unit tests for the Silk cursor: position and button mapping from an <see cref="IMouse"/>, and
/// scroll accumulation from the <see cref="IMouse.Scroll"/> event. The cursor is attached to a
/// mocked input context and read through its public surface after <c>Activity</c>.
/// </summary>
public class CursorSilkTests
{
    private static (Cursor cursor, Mock<IMouse> mouse) CreateAttachedCursor()
    {
        var mouse = new Mock<IMouse>();
        mouse.SetupGet(m => m.ScrollWheels).Returns(new List<ScrollWheel>());

        var context = new Mock<IInputContext>();
        context.SetupGet(c => c.Mice).Returns(new List<IMouse> { mouse.Object });

        var cursor = new Cursor();
        cursor.AttachSilkInput(context.Object);
        return (cursor, mouse);
    }

    [Fact]
    public void Activity_MapsMousePosition()
    {
        (Cursor cursor, Mock<IMouse> mouse) = CreateAttachedCursor();
        mouse.SetupGet(m => m.Position).Returns(new Vector2(37, 52));

        cursor.Activity(0);

        cursor.X.ShouldBe(37);
        cursor.Y.ShouldBe(52);
    }

    [Fact]
    public void Activity_MapsPrimaryButtonDown()
    {
        (Cursor cursor, Mock<IMouse> mouse) = CreateAttachedCursor();
        mouse.Setup(m => m.IsButtonPressed(MouseButton.Left)).Returns(true);

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBeTrue();
    }

    [Fact]
    public void Scroll_AccumulatesIntoScrollWheelDelta()
    {
        (Cursor cursor, Mock<IMouse> mouse) = CreateAttachedCursor();

        // Two notches up. Silk reports a per-event delta; the cursor scales to the XNA detent
        // convention (120 units/notch) into a running total, and the shared Cursor exposes the
        // per-frame change as ScrollWheelChange = (current - lastFrame) / 120.
        mouse.Raise(m => m.Scroll += null, mouse.Object, new ScrollWheel(0, 2));
        cursor.Activity(0);

        cursor.ScrollWheelChange.ShouldBe(2);
    }
}
