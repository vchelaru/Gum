using InputLibrary;
using Gum.Services;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using Shouldly;

namespace Gum.Presentation.Tests.Input;

/// <summary>
/// The polled cursor reads the host's pointer sample each frame and derives pushes, clicks, and
/// double clicks from consecutive samples, without touching any platform input API.
/// </summary>
public class CursorTests
{
    private sealed class FakeHost : IInputHostControl
    {
        public bool Focused { get; set; } = true;
        public int Width { get; set; } = 100;
        public int Height { get; set; } = 100;
        public CursorKind Cursor { get; set; }
        public HostPointerState Pointer { get; set; }
        public HostPointerState GetPointerState() => Pointer;
        public KeyboardState GetKeyboardState() => new KeyboardState();
    }

    private static (Cursor cursor, FakeHost host) Create(float displayScale = 1)
    {
        FakeHost host = new FakeHost();
        Cursor cursor = new Cursor();
        cursor.Initialize(host, new CanvasDisplayScale { DisplayScale = displayScale });
        return (cursor, host);
    }

    private static void PushMoveRelease(Cursor cursor, FakeHost host, float pushX, float farthestX, float releaseX, double time)
    {
        host.Pointer = new HostPointerState(pushX, 10, true, false, false);
        cursor.Activity(time);
        host.Pointer = new HostPointerState(farthestX, 10, true, false, false);
        cursor.Activity(time + 0.01);
        host.Pointer = new HostPointerState(releaseX, 10, true, false, false);
        cursor.Activity(time + 0.02);
        host.Pointer = new HostPointerState(releaseX, 10, false, false, false);
        cursor.Activity(time + 0.03);
    }

    // The thresholds are in device-independent pixels, so on a 200% display they cover twice the
    // physical pixels (#5554).
    [Theory]
    [InlineData(1f, 6f, true)]
    [InlineData(1f, 7f, false)]
    [InlineData(2f, 7f, true)]
    [InlineData(2f, 12f, true)]
    [InlineData(2f, 13f, false)]
    public void PrimaryDoubleClick_DragDeadZoneScalesWithDisplayScale(float displayScale, float wobble, bool expected)
    {
        (Cursor cursor, FakeHost host) = Create(displayScale);

        PushMoveRelease(cursor, host, pushX: 60, farthestX: 60 + wobble, releaseX: 60, time: 0);
        PushMoveRelease(cursor, host, pushX: 60, farthestX: 60 + wobble, releaseX: 60, time: 0.1);

        cursor.PrimaryDoubleClick.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1f, 4f, true)]
    [InlineData(1f, 5f, false)]
    [InlineData(2f, 8f, true)]
    [InlineData(2f, 9f, false)]
    public void PrimaryDoubleClick_ClickDistanceScalesWithDisplayScale(float displayScale, float secondClickOffset, bool expected)
    {
        (Cursor cursor, FakeHost host) = Create(displayScale);

        PushMoveRelease(cursor, host, pushX: 60, farthestX: 60, releaseX: 60, time: 0);
        float secondX = 60 + secondClickOffset;
        PushMoveRelease(cursor, host, pushX: secondX, farthestX: secondX, releaseX: secondX, time: 0.1);

        cursor.PrimaryDoubleClick.ShouldBe(expected);
    }

    [Fact]
    public void Activity_ReportsPositionFromTheHostSample()
    {
        (Cursor cursor, FakeHost host) = Create();
        host.Pointer = new HostPointerState(12, 34, false, false, false);

        cursor.Activity(0);

        cursor.X.ShouldBe(12);
        cursor.Y.ShouldBe(34);
        cursor.IsInWindow.ShouldBeTrue();
    }

    [Fact]
    public void IsInWindow_IsFalseOutsideTheHostBounds()
    {
        (Cursor cursor, FakeHost host) = Create();
        host.Pointer = new HostPointerState(-1, 10, false, false, false);
        cursor.Activity(0);
        cursor.IsInWindow.ShouldBeFalse();

        host.Pointer = new HostPointerState(10, 100, false, false, false);
        cursor.Activity(0);
        cursor.IsInWindow.ShouldBeFalse();
    }

    [Fact]
    public void PrimaryPushAndClick_ComeFromConsecutiveSamples()
    {
        (Cursor cursor, FakeHost host) = Create();
        host.Pointer = new HostPointerState(10, 10, false, false, false);
        cursor.Activity(0);

        host.Pointer = new HostPointerState(10, 10, true, false, false);
        cursor.Activity(0.01);
        cursor.PrimaryPush.ShouldBeTrue();
        cursor.PrimaryDown.ShouldBeTrue();
        cursor.PrimaryClick.ShouldBeFalse();

        host.Pointer = new HostPointerState(10, 10, false, false, false);
        cursor.Activity(0.02);
        cursor.PrimaryPush.ShouldBeFalse();
        cursor.PrimaryClick.ShouldBeTrue();
    }

    [Fact]
    public void PrimaryDoubleClick_RequiresTwoClicksWithinTheWindow()
    {
        (Cursor cursor, FakeHost host) = Create();
        void Click(double time)
        {
            host.Pointer = new HostPointerState(10, 10, true, false, false);
            cursor.Activity(time);
            host.Pointer = new HostPointerState(10, 10, false, false, false);
            cursor.Activity(time + 0.01);
        }

        Click(0);
        cursor.PrimaryDoubleClick.ShouldBeFalse();
        Click(0.1);
        cursor.PrimaryDoubleClick.ShouldBeTrue();
        Click(1.0);
        cursor.PrimaryDoubleClick.ShouldBeFalse();
    }

    // Two quick clicks on different objects are two selections, not a double click: a double click
    // punches through to the object underneath, so the second object ended up deselected.
    [Fact]
    public void PrimaryDoubleClick_RequiresTheSecondClickNearTheFirst()
    {
        (Cursor cursor, FakeHost host) = Create();
        void Click(float x, double time)
        {
            host.Pointer = new HostPointerState(x, 10, true, false, false);
            cursor.Activity(time);
            host.Pointer = new HostPointerState(x, 10, false, false, false);
            cursor.Activity(time + 0.01);
        }

        Click(10, 0);
        Click(60, 0.1);
        cursor.PrimaryDoubleClick.ShouldBeFalse("the second click was 50 pixels from the first");

        Click(60 + Cursor.MaximumPixelsBetweenClicksForDoubleClick, 0.2);
        cursor.PrimaryDoubleClick.ShouldBeTrue();
    }

    // A release that ends a drag is not a click: two quick drags ending near each other read as a
    // double click, which punches through and changes the canvas selection (#5286).
    [Fact]
    public void PrimaryDoubleClick_IgnoresAReleaseThatEndedADrag()
    {
        (Cursor cursor, FakeHost host) = Create();
        void Gesture(float pushX, float farthestX, float releaseX, double time)
        {
            host.Pointer = new HostPointerState(pushX, 10, true, false, false);
            cursor.Activity(time);
            host.Pointer = new HostPointerState(farthestX, 10, true, false, false);
            cursor.Activity(time + 0.01);
            host.Pointer = new HostPointerState(releaseX, 10, true, false, false);
            cursor.Activity(time + 0.02);
            host.Pointer = new HostPointerState(releaseX, 10, false, false, false);
            cursor.Activity(time + 0.03);
        }

        Gesture(pushX: 40, farthestX: 70, releaseX: 60, time: 0);
        Gesture(pushX: 50, farthestX: 80, releaseX: 60, time: 0.1);
        cursor.PrimaryDoubleClick.ShouldBeFalse("both releases ended drags");

        Gesture(pushX: 60, farthestX: 60, releaseX: 60, time: 0.2);
        cursor.PrimaryDoubleClick.ShouldBeFalse("the previous release ended a drag, so this is a first click");

        Gesture(pushX: 60, farthestX: 90, releaseX: 60, time: 0.3);
        cursor.PrimaryDoubleClick.ShouldBeFalse("the drag went out and came back, so it is still a drag");

        float jitter = GrabbedState.PixelsToMoveBeforeDrag;
        Gesture(pushX: 60, farthestX: 60 + jitter, releaseX: 60, time: 0.4);
        Gesture(pushX: 60, farthestX: 60 + jitter, releaseX: 60, time: 0.5);
        cursor.PrimaryDoubleClick.ShouldBeTrue("movement inside the drag dead zone is still a click");
    }

    [Fact]
    public void Pushes_AreIgnoredWhileTheHostIsNotFocused()
    {
        (Cursor cursor, FakeHost host) = Create();
        host.Focused = false;
        host.Pointer = new HostPointerState(10, 10, false, false, false);
        cursor.Activity(0);
        host.Pointer = new HostPointerState(10, 10, true, true, true);
        cursor.Activity(0.01);

        cursor.PrimaryPush.ShouldBeFalse();
        cursor.SecondaryPush.ShouldBeFalse();
        cursor.MiddleDown.ShouldBeFalse();
        cursor.PrimaryDownIgnoringIsInWindow.ShouldBeTrue();
    }

    [Fact]
    public void XChangeAndYChange_AreTheDeltaBetweenSamples()
    {
        (Cursor cursor, FakeHost host) = Create();
        host.Pointer = new HostPointerState(10, 10, false, false, false);
        cursor.Activity(0);
        host.Pointer = new HostPointerState(15, 7, false, false, false);
        cursor.Activity(0.01);

        cursor.XChange.ShouldBe(5);
        cursor.YChange.ShouldBe(-3);
    }

    [Fact]
    public void EndCursorSettingFrameStart_AssignsTheFirstRequestedKindOrArrow()
    {
        (Cursor cursor, FakeHost host) = Create();

        cursor.StartCursorSettingFrameStart();
        cursor.SetCursorKind(CursorKind.SizeNS);
        cursor.SetCursorKind(CursorKind.Hand);
        cursor.EndCursorSettingFrameStart();
        host.Cursor.ShouldBe(CursorKind.SizeNS);

        cursor.StartCursorSettingFrameStart();
        cursor.EndCursorSettingFrameStart();
        host.Cursor.ShouldBe(CursorKind.Arrow);
    }
}
