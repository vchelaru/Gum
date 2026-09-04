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
public class CursorStrideTests
{
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
    public void Activity_MapsMousePosition()
    {
        (Cursor cursor, Mock<IMouseDevice> mouse) = CreateAttachedCursor();
        // Position is normalized (0..1); Cursor.Stride scales by SurfaceSize (800x600) to pixels.
        mouse.SetupGet(m => m.Position).Returns(new Vector2(0.5f, 0.5f));

        cursor.Activity(0);

        cursor.X.ShouldBe(400);
        cursor.Y.ShouldBe(300);
    }

    [Fact]
    public void Activity_MapsPrimaryButtonDown()
    {
        (Cursor cursor, Mock<IMouseDevice> mouse) = CreateAttachedCursor();
        mouse.SetupGet(m => m.DownButtons).Returns(new ReadOnlySet<MouseButton>(new HashSet<MouseButton> { MouseButton.Left }));

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBeTrue();
    }

    [Fact]
    public void Scroll_AccumulatesIntoScrollWheelDelta()
    {
        (Cursor cursor, Mock<IMouseDevice> mouse) = CreateAttachedCursor();

        // Two notches up. Stride reports a per-event delta; the cursor scales to the XNA detent
        // convention (120 units/notch) into a running total, and the shared Cursor exposes the
        // per-frame change as ScrollWheelChange = (current - lastFrame) / 120.
        ((IInputEventListener<MouseWheelEvent>)cursor).ProcessEvent(new MouseWheelEvent { WheelDelta = 2f });
        cursor.Activity(0);

        cursor.ScrollWheelChange.ShouldBe(2);
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
