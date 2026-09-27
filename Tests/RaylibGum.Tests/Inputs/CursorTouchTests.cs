using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using System.Numerics;

namespace RaylibGum.Tests.Inputs;

public class CursorTouchTests
{
    [Fact]
    public void CreateTouchCollection_CopiesEachRaylibTouchPoint()
    {
        int[] ids = { 7, 9 };
        Vector2[] positions = { new Vector2(10, 20), new Vector2(30, 40) };

        TouchCollection touches = Cursor.CreateTouchCollection(2, i => ids[i], i => positions[i]);

        touches.Count.ShouldBe(2);
        touches[0].Id.ShouldBe(7);
        touches[0].Position.ShouldBe(new Vector2(10, 20));
        touches[1].Id.ShouldBe(9);
        touches[1].Position.ShouldBe(new Vector2(30, 40));
    }

    [Fact]
    public void Activity_UsesMouse_WhenRaylibReportsNoTouches()
    {
        Cursor cursor = new Cursor();

        cursor.Activity(0, Mouse(x: 50, y: 60, pressed: true), NoTouches());

        cursor.LastInputDevice.ShouldBe(InputDevice.Mouse);
        cursor.X.ShouldBe(50);
        cursor.Y.ShouldBe(60);
        cursor.PrimaryPush.ShouldBeTrue();
    }

    [Fact]
    public void PrimaryDoubleClick_UsesTouchTolerance_WhenRaylibMirrorsTouchesToTheMouse()
    {
        Cursor cursor = new Cursor();
        cursor.MouseDoubleClickTolerance = 4;
        cursor.TouchDoubleClickTolerance = 100;

        // raylib's web backend mirrors the first touch into the mouse position and left button,
        // so the mouse state is present and pressed while the finger is down.
        Tap(cursor, time: 0, x: 100, y: 100);
        cursor.Activity(0.1, Mouse(x: 160, y: 100, pressed: true), Touches(160, 100));

        cursor.LastInputDevice.ShouldBe(InputDevice.TouchScreen);
        cursor.X.ShouldBe(160);
        cursor.PrimaryPush.ShouldBeTrue();

        cursor.Activity(0.15, Mouse(x: 160, y: 100, pressed: false), NoTouches());

        cursor.PrimaryClick.ShouldBeTrue();
        cursor.PrimaryDoubleClick.ShouldBeTrue();

        // The mouse release that accompanied the touch release must not register as a second click.
        cursor.Activity(0.2, Mouse(x: 160, y: 100, pressed: false), NoTouches());
        cursor.LastInputDevice.ShouldBe(InputDevice.Mouse);
        cursor.PrimaryClick.ShouldBeFalse();
    }

    private static void Tap(Cursor cursor, double time, int x, int y)
    {
        cursor.Activity(time, Mouse(x, y, pressed: true), Touches(x, y));
        cursor.Activity(time + 0.02, Mouse(x, y, pressed: false), NoTouches());
    }

    private static MouseState Mouse(int x, int y, bool pressed) => new MouseState
    {
        X = x,
        Y = y,
        LeftButton = pressed ? ButtonState.Pressed : ButtonState.Released,
    };

    private static TouchCollection Touches(int x, int y) =>
        Cursor.CreateTouchCollection(1, _ => 0, _ => new Vector2(x, y));

    private static TouchCollection NoTouches() =>
        Cursor.CreateTouchCollection(0, _ => 0, _ => Vector2.Zero);
}
