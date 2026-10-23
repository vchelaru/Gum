using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using XnaTouchCollection = Microsoft.Xna.Framework.Input.Touch.TouchCollection;
using XnaTouchLocation = Microsoft.Xna.Framework.Input.Touch.TouchLocation;
using XnaTouchLocationState = Microsoft.Xna.Framework.Input.Touch.TouchLocationState;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace KniSkiaGum.Tests;

/// <summary>
/// Tests for the KNI cursor's touch input: KNI's touch panel drives position and the primary button
/// as it does on KniGum, read through the cursor's public surface after <c>Activity</c>.
/// </summary>
public class CursorKniTouchTests : BaseTestClass
{
    private static XnaTouchCollection Touches(params (int x, int y)[] points) =>
        new XnaTouchCollection(points
            .Select((p, i) => new XnaTouchLocation(i + 1, XnaTouchLocationState.Pressed, new XnaVector2(p.x, p.y)))
            .ToArray());

    private static XnaTouchCollection NoTouches() => new XnaTouchCollection(Array.Empty<XnaTouchLocation>());

    [Fact]
    public void Activity_TouchPress_SetsPositionFromFirstTouch_AndPrimaryPush()
    {
        Cursor cursor = new Cursor { ReadMouse = () => default, ReadTouches = () => Touches((40, 50), (90, 95)) };

        cursor.Activity(0);

        cursor.LastInputDevice.ShouldBe(InputDevice.TouchScreen);
        cursor.X.ShouldBe(40);
        cursor.Y.ShouldBe(50);
        cursor.PrimaryPush.ShouldBeTrue();
        cursor.PrimaryDown.ShouldBeTrue();
    }

    [Fact]
    public void Activity_TouchMove_UpdatesPosition_WhileDown()
    {
        XnaTouchCollection touches = Touches((40, 50));
        Cursor cursor = new Cursor { ReadMouse = () => default, ReadTouches = () => touches };
        cursor.Activity(0);

        touches = Touches((60, 75));
        cursor.Activity(0.02);

        cursor.X.ShouldBe(60);
        cursor.Y.ShouldBe(75);
        cursor.PrimaryPush.ShouldBeFalse();
        cursor.PrimaryDown.ShouldBeTrue();
    }

    [Fact]
    public void Activity_TouchRelease_KeepsLastPosition_AndPrimaryClick()
    {
        XnaTouchCollection touches = Touches((40, 50));
        Cursor cursor = new Cursor { ReadMouse = () => default, ReadTouches = () => touches };
        cursor.Activity(0);

        touches = NoTouches();
        cursor.Activity(0.02);

        cursor.LastInputDevice.ShouldBe(InputDevice.TouchScreen);
        cursor.X.ShouldBe(40);
        cursor.Y.ShouldBe(50);
        cursor.PrimaryDown.ShouldBeFalse();
        cursor.PrimaryClick.ShouldBeTrue();
    }

    [Fact]
    public void Activity_IsMobile_OffsetsTouchByWindowClientBounds()
    {
        // As on KniGum: on mobile, touches are relative to the client bounds, so the bounds' origin is added.
        TestGameWindow window = new TestGameWindow { Bounds = new Microsoft.Xna.Framework.Rectangle(10, 20, 300, 200) };
        Cursor cursor = new Cursor(window) { ReadMouse = () => default, ReadTouches = () => Touches((40, 50)) };
        cursor.IsMobile = true;

        cursor.Activity(0);

        cursor.X.ShouldBe(50);
        cursor.Y.ShouldBe(70);
    }

    [Fact]
    public void Activity_IsMobile_NegativeFractionalTouch_TruncatesBeforeOffset()
    {
        // KniGum converts the touch position to int before adding the client-bounds origin, so -0.5
        // truncates to 0 and lands on the origin; adding first would give (int)9.5 = 9.
        TestGameWindow window = new TestGameWindow { Bounds = new Microsoft.Xna.Framework.Rectangle(10, 20, 300, 200) };
        XnaTouchCollection touches = new XnaTouchCollection(new[]
        {
            new XnaTouchLocation(1, XnaTouchLocationState.Pressed, new XnaVector2(-0.5f, -0.5f)),
        });
        Cursor cursor = new Cursor(window) { ReadMouse = () => default, ReadTouches = () => touches };
        cursor.IsMobile = true;

        cursor.Activity(0);

        cursor.X.ShouldBe(10);
        cursor.Y.ShouldBe(20);
    }

    [Fact]
    public void Activity_NoTouchPanel_KeepsLastFrameTouches()
    {
        // As on KniGum: a frame with no touch panel reuses the last frame's touches instead of
        // reading them as released.
        XnaTouchCollection? touches = Touches((40, 50));
        Cursor cursor = new Cursor { ReadMouse = () => default, ReadTouches = () => touches };
        cursor.Activity(0);

        touches = null;
        cursor.Activity(0.02);

        cursor.X.ShouldBe(40);
        cursor.Y.ShouldBe(50);
        cursor.PrimaryDown.ShouldBeTrue();
        cursor.PrimaryClick.ShouldBeFalse();
    }

    [Fact]
    public void Activity_NotMobile_DoesNotOffsetTouch()
    {
        TestGameWindow window = new TestGameWindow { Bounds = new Microsoft.Xna.Framework.Rectangle(10, 20, 300, 200) };
        Cursor cursor = new Cursor(window) { ReadMouse = () => default, ReadTouches = () => Touches((40, 50)) };
        cursor.IsMobile = false;

        cursor.Activity(0);

        cursor.X.ShouldBe(40);
        cursor.Y.ShouldBe(50);
    }

    [Fact]
    public void Update_TouchTapOverButton_FiresClick()
    {
        Button button = new Button();
        button.AddToRoot();
        bool wasClicked = false;
        button.Click += (_, _) => wasClicked = true;

        XnaTouchCollection touches = Touches((5, 5));
        Cursor cursor = new Cursor { ReadMouse = () => default, ReadTouches = () => touches };
        FormsUtilities.SetCursor(cursor);

        GumService.Default.Update(0);
        touches = NoTouches();
        GumService.Default.Update(0.05);

        wasClicked.ShouldBeTrue();
    }
}
