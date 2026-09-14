using InputLibrary;
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

    private static (Cursor cursor, FakeHost host) Create()
    {
        FakeHost host = new FakeHost();
        Cursor cursor = new Cursor();
        cursor.Initialize(host);
        return (cursor, host);
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
