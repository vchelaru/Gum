using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using System.Numerics;

namespace UnityGum.Tests;

/// <summary>
/// The pushed cursor reads nothing itself: the Unity host sets its state each frame and
/// <c>Activity</c> latches it.
/// </summary>
public class CursorUnityTests
{
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
