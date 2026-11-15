using Gum.Input;
using Shouldly;
using XnaButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using XnaMouseState = Microsoft.Xna.Framework.Input.MouseState;

namespace KniSkiaGum.Tests;

/// <summary>
/// Tests for the KNI cursor: position, buttons and wheel come from KNI's mouse state, read through
/// the cursor's public surface after <c>Activity</c>.
/// </summary>
public class CursorKniTests : SkiaHostTests.CursorTestsBase
{
    private sealed class KniCursorRig : SkiaHostTests.IHostCursorRig
    {
        private readonly Cursor _cursor;
        private int _x;
        private int _y;
        private int _wheel;
        private XnaButtonState _left = XnaButtonState.Released;

        public KniCursorRig()
        {
            _cursor = new Cursor { ReadMouse = () => Mouse(x: _x, y: _y, wheel: _wheel, left: _left) };
        }

        public Gum.Wireframe.ICursor Cursor => _cursor;

        public void SetPosition(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public void SetPrimaryDown(bool isDown) => _left = isDown ? XnaButtonState.Pressed : XnaButtonState.Released;

        // KNI's wheel value is a running total in 120-per-notch units.
        public void Scroll(int notches) => _wheel += notches * 120;
    }

    protected override SkiaHostTests.IHostCursorRig CreateRig() => new KniCursorRig();

    private static XnaMouseState Mouse(
        int x = 0,
        int y = 0,
        int wheel = 0,
        XnaButtonState left = XnaButtonState.Released,
        XnaButtonState middle = XnaButtonState.Released,
        XnaButtonState right = XnaButtonState.Released) =>
        new XnaMouseState(x, y, wheel, left, middle, right, XnaButtonState.Released, XnaButtonState.Released);

    [Fact]
    public void Activity_MapsEachButtonToItsOwnState()
    {
        Cursor cursor = new Cursor
        {
            ReadMouse = () => Mouse(right: XnaButtonState.Pressed, middle: XnaButtonState.Pressed)
        };

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBeFalse();
        cursor.SecondaryDown.ShouldBeTrue();
        cursor.MiddleDown.ShouldBeTrue();
    }

    [Fact]
    public void Activity_IsMobile_IgnoresMouse()
    {
        Cursor cursor = new Cursor { ReadMouse = () => Mouse(x: 37, left: XnaButtonState.Pressed) };
        cursor.IsMobile = true;

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBeFalse();
        cursor.X.ShouldBe(0);
    }

    [Fact]
    public void ScrollWheelChange_IsWholeNotches_FromKniRunningTotal()
    {
        // KNI's wheel value is a running total in 120-per-notch units, and passes straight through.
        int wheel = 0;
        Cursor cursor = new Cursor { ReadMouse = () => Mouse(wheel: wheel) };
        cursor.Activity(0);

        wheel = 240;
        cursor.Activity(0.1);

        cursor.ScrollWheelChange.ShouldBe(2);
    }
}
