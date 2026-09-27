using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Shouldly;
using Xunit;
using Cursor = MonoGameGum.Input.Cursor;

namespace MonoGameGum.Tests.Input;

public class CursorDoubleClickTests
{
    [Fact]
    public void PrimaryDoubleClick_IsFalse_WhenSecondClickIsFarFromFirst()
    {
        Cursor cursor = new Cursor(null);

        MouseClick(cursor, time: 0, x: 100, y: 100);
        MousePush(cursor, time: 0.1, x: 120, y: 100);
        MouseRelease(cursor, time: 0.15, x: 120, y: 100);

        cursor.PrimaryClick.ShouldBeTrue();
        cursor.PrimaryDoubleClick.ShouldBeFalse();
    }

    [Fact]
    public void PrimaryDoubleClick_IsTrue_WhenSecondClickIsNearFirst()
    {
        Cursor cursor = new Cursor(null);

        MouseClick(cursor, time: 0, x: 100, y: 100);
        MousePush(cursor, time: 0.1, x: 102, y: 97);
        MouseRelease(cursor, time: 0.15, x: 102, y: 97);

        cursor.PrimaryDoubleClick.ShouldBeTrue();
    }

    [Fact]
    public void PrimaryDoubleClick_IsFalse_WhenFirstReleaseEndedADrag()
    {
        Cursor cursor = new Cursor(null);

        MousePush(cursor, time: 0, x: 100, y: 100);
        MouseDown(cursor, time: 0.02, x: 140, y: 100);
        MouseRelease(cursor, time: 0.04, x: 140, y: 100);
        MousePush(cursor, time: 0.1, x: 140, y: 100);
        MouseRelease(cursor, time: 0.15, x: 140, y: 100);

        cursor.PrimaryDoubleClick.ShouldBeFalse();
    }

    [Fact]
    public void PrimaryDoubleClick_IsFalse_WhenSecondReleaseEndsADrag()
    {
        Cursor cursor = new Cursor(null);

        MouseClick(cursor, time: 0, x: 100, y: 100);
        MousePush(cursor, time: 0.1, x: 100, y: 100);
        // Leaves the drag dead zone, then comes back before releasing.
        MouseDown(cursor, time: 0.12, x: 140, y: 100);
        MouseRelease(cursor, time: 0.15, x: 100, y: 100);

        cursor.PrimaryDoubleClick.ShouldBeFalse();
    }

    [Fact]
    public void PrimaryDoubleClick_UsesTouchTolerance_ForTouchInput()
    {
        Cursor cursor = new Cursor(null);
        // Farther apart than the mouse tolerance, within the touch tolerance.
        cursor.MouseDoubleClickTolerance = 4;
        cursor.TouchDoubleClickTolerance = 100;

        TouchTap(cursor, time: 0, x: 100, y: 100);
        TouchDown(cursor, time: 0.1, x: 160, y: 100);
        TouchRelease(cursor, time: 0.15);

        cursor.LastInputDevice.ShouldBe(InputDevice.TouchScreen);
        cursor.PrimaryDoubleClick.ShouldBeTrue();
    }

    [Fact]
    public void PrimaryDoubleClick_UsesSetTolerances()
    {
        Cursor cursor = new Cursor(null);
        cursor.MouseDoubleClickTolerance = 50;
        cursor.TouchDoubleClickTolerance = 10;

        MouseClick(cursor, time: 0, x: 100, y: 100);
        MouseClick(cursor, time: 0.1, x: 140, y: 100);
        bool mouseDoubleClick = cursor.PrimaryDoubleClick;

        TouchTap(cursor, time: 1, x: 100, y: 100);
        TouchDown(cursor, time: 1.1, x: 140, y: 100);
        TouchRelease(cursor, time: 1.15);
        bool touchDoubleClick = cursor.PrimaryDoubleClick;

        mouseDoubleClick.ShouldBeTrue();
        touchDoubleClick.ShouldBeFalse();
    }

    [Fact]
    public void PrimaryDoublePush_IsFalse_WhenSecondPushIsFarFromFirst()
    {
        Cursor cursor = new Cursor(null);

        MouseClick(cursor, time: 0, x: 100, y: 100);
        MousePush(cursor, time: 0.1, x: 120, y: 100);

        cursor.PrimaryPush.ShouldBeTrue();
        cursor.PrimaryDoublePush.ShouldBeFalse();
    }

    [Fact]
    public void SecondaryDoubleClick_IsFalse_OnFrameAfterDoubleClick()
    {
        Cursor cursor = new Cursor(null);

        MouseRightClick(cursor, time: 0, x: 100, y: 100);
        MouseRightClick(cursor, time: 0.1, x: 100, y: 100);
        bool doubleClickOnRelease = cursor.SecondaryDoubleClick;

        Frame(cursor, time: 0.2, new MouseState(100, 100, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));

        doubleClickOnRelease.ShouldBeTrue();
        cursor.SecondaryDoubleClick.ShouldBeFalse();
    }

    private static void MouseClick(Cursor cursor, double time, int x, int y)
    {
        MousePush(cursor, time, x, y);
        MouseRelease(cursor, time + 0.02, x, y);
    }

    private static void MousePush(Cursor cursor, double time, int x, int y) => MouseDown(cursor, time, x, y);

    private static void MouseDown(Cursor cursor, double time, int x, int y) =>
        Frame(cursor, time, new MouseState(x, y, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));

    private static void MouseRelease(Cursor cursor, double time, int x, int y) =>
        Frame(cursor, time, new MouseState(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));

    private static void MouseRightClick(Cursor cursor, double time, int x, int y)
    {
        Frame(cursor, time, new MouseState(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Pressed, ButtonState.Released, ButtonState.Released));
        Frame(cursor, time + 0.02, new MouseState(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));
    }

    private static void Frame(Cursor cursor, double time, MouseState mouseState) =>
        cursor.Activity(time, mouseState, new TouchCollection());

    private static void TouchTap(Cursor cursor, double time, int x, int y)
    {
        TouchDown(cursor, time, x, y);
        TouchRelease(cursor, time + 0.02);
    }

    private static void TouchDown(Cursor cursor, double time, int x, int y)
    {
        TouchLocation touch = new TouchLocation(1, TouchLocationState.Pressed, new Vector2(x, y));
        cursor.Activity(time, null, new TouchCollection(new[] { touch }), isMobile: true);
    }

    // A lifted finger reports no touches; the cursor keeps the last touch position.
    private static void TouchRelease(Cursor cursor, double time) =>
        cursor.Activity(time, null, new TouchCollection(), isMobile: true);
}
