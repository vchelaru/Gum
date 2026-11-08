using Gum.Input;
using Gum.Wireframe;
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
public class CursorSilkTests : SkiaHostTests.CursorTestsBase
{
    private sealed class SilkCursorRig : SkiaHostTests.IHostCursorRig
    {
        private readonly Cursor _cursor;
        private readonly Mock<IMouse> _mouse;

        public SilkCursorRig()
        {
            (_cursor, _mouse) = CreateAttachedCursor();
        }

        public Gum.Wireframe.ICursor Cursor => _cursor;

        public void SetPosition(int x, int y) => _mouse.SetupGet(m => m.Position).Returns(new Vector2(x, y));

        public void SetPrimaryDown(bool isDown) => _mouse.Setup(m => m.IsButtonPressed(MouseButton.Left)).Returns(isDown);

        public void Scroll(int notches) => _mouse.Raise(m => m.Scroll += null, _mouse.Object, new ScrollWheel(0, notches));
    }

    protected override SkiaHostTests.IHostCursorRig CreateRig() => new SilkCursorRig();

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
    public void Activity_IsMobile_IgnoresMouse()
    {
        (Cursor cursor, Mock<IMouse> mouse) = CreateAttachedCursor();
        mouse.SetupGet(m => m.Position).Returns(new Vector2(37, 52));
        mouse.Setup(m => m.IsButtonPressed(MouseButton.Left)).Returns(true);
        cursor.IsMobile = true;

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBeFalse();
        cursor.X.ShouldBe(0);
        cursor.LastInputDevice.ShouldBe(InputDevice.TouchScreen);
    }

    [Fact]
    public void CustomCursor_SizeWE_SetsHResizeStandardCursor()
    {
        (Cursor cursor, Mock<IMouse> mouse) = CreateAttachedCursor();
        var silkCursor = new Mock<Silk.NET.Input.ICursor>();
        silkCursor.SetupAllProperties();
        mouse.SetupGet(m => m.Cursor).Returns(silkCursor.Object);

        cursor.CustomCursor = Cursors.SizeWE;

        silkCursor.Object.StandardCursor.ShouldBe(StandardCursor.HResize);
        silkCursor.Object.Type.ShouldBe(CursorType.Standard);
    }

    [Fact]
    public void CustomCursor_Null_SetsArrowStandardCursor()
    {
        (Cursor cursor, Mock<IMouse> mouse) = CreateAttachedCursor();
        var silkCursor = new Mock<Silk.NET.Input.ICursor>();
        silkCursor.SetupAllProperties();
        mouse.SetupGet(m => m.Cursor).Returns(silkCursor.Object);

        cursor.CustomCursor = null;

        silkCursor.Object.StandardCursor.ShouldBe(StandardCursor.Arrow);
    }
}
