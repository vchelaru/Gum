using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using System.Numerics;

namespace UnityGum.Tests;

/// <summary>
/// The pushed cursor reads nothing itself: the Unity host sets its state each frame and
/// <c>Activity</c> latches it.
/// </summary>
public class CursorUnityTests : SkiaHostTests.CursorTestsBase
{
    private sealed class UnityCursorRig : SkiaHostTests.IHostCursorRig
    {
        private readonly Cursor _cursor = new Cursor();
        private float _x;
        private float _y;
        private bool _leftDown;

        public Gum.Wireframe.ICursor Cursor => _cursor;

        public void SetPosition(int x, int y)
        {
            _x = x;
            _y = y;
            Push();
        }

        public void SetPrimaryDown(bool isDown)
        {
            _leftDown = isDown;
            Push();
        }

        public void Scroll(int notches) => _cursor.AddScrollNotches(notches);

        private void Push() => _cursor.SetMouseState(_x, _y, leftDown: _leftDown, middleDown: false, rightDown: false);
    }

    protected override SkiaHostTests.IHostCursorRig CreateRig() => new UnityCursorRig();

    [Fact]
    public void Activity_ReadsPushedMouseState()
    {
        Cursor cursor = new Cursor();
        cursor.SetMouseState(37.6f, 52.2f, leftDown: true, middleDown: false, rightDown: false);

        cursor.Activity(0);

        cursor.X.ShouldBe(37);
        cursor.Y.ShouldBe(52);
        cursor.PrimaryDown.ShouldBeTrue();
        cursor.PrimaryPush.ShouldBeTrue();
    }

    [Fact]
    public void Activity_ReleaseAfterPush_ReportsPrimaryClick()
    {
        Cursor cursor = new Cursor();
        cursor.SetMouseState(10, 10, leftDown: true, middleDown: false, rightDown: false);
        cursor.Activity(0);

        cursor.SetMouseState(10, 10, leftDown: false, middleDown: false, rightDown: false);
        cursor.Activity(0.016);

        cursor.PrimaryClick.ShouldBeTrue();
    }

    [Fact]
    public void Activity_MapsSecondaryAndMiddleButtons()
    {
        Cursor cursor = new Cursor();
        cursor.SetMouseState(0, 0, leftDown: false, middleDown: true, rightDown: true);

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBeFalse();
        cursor.SecondaryDown.ShouldBeTrue();
        cursor.MiddleDown.ShouldBeTrue();
    }

    [Fact]
    public void AddScrollNotches_ReportsWholeNotchesAsScrollWheelChange()
    {
        Cursor cursor = new Cursor();
        cursor.Activity(0);

        cursor.AddScrollNotches(1);
        cursor.AddScrollNotches(1);
        cursor.Activity(0.016);

        cursor.ScrollWheelChange.ShouldBe(2);

        cursor.AddScrollNotches(-1);
        cursor.Activity(0.032);

        cursor.ScrollWheelChange.ShouldBe(-1);
    }

    [Fact]
    public void Activity_IsMobile_ReadsPushedTouchesAndIgnoresMouse()
    {
        Cursor cursor = new Cursor();
        cursor.IsMobile = true;
        cursor.SetMouseState(5, 5, leftDown: true, middleDown: false, rightDown: false);
        cursor.SetTouches(new TouchCollection(new[] { new TouchLocation(1, new Vector2(120, 80)) }));

        cursor.Activity(0);

        cursor.LastInputDevice.ShouldBe(InputDevice.TouchScreen);
        cursor.X.ShouldBe(120);
        cursor.Y.ShouldBe(80);
        cursor.PrimaryDown.ShouldBeTrue();
    }
}
