using Brutal.GlfwApi;
using Gum.Input;
using Gum.Wireframe;
using Shouldly;

namespace BrutalGum.Tests;

/// <summary>Position, button and scroll mapping from the window, and the resize cursor shapes.</summary>
public class CursorBrutalTests : SkiaHostTests.CursorTestsBase
{
    private sealed class BrutalCursorRig : SkiaHostTests.IHostCursorRig
    {
        private readonly FakeBrutalInput _input = new();
        private readonly Gum.Input.Cursor _cursor = new();

        public BrutalCursorRig()
        {
            _cursor.AttachBrutalInput(_input);
        }

        public Gum.Wireframe.ICursor Cursor => _cursor;

        public void SetPosition(int x, int y)
        {
            _input.CursorX = x;
            _input.CursorY = y;
        }

        public void SetPrimaryDown(bool isDown)
        {
            if (isDown) _input.HeldButtons.Add(GlfwMouseButton.Left);
            else _input.HeldButtons.Remove(GlfwMouseButton.Left);
        }

        public void Scroll(int notches) => _input.RaiseScroll(notches);
    }

    protected override SkiaHostTests.IHostCursorRig CreateRig() => new BrutalCursorRig();

    [Theory]
    [InlineData(Cursors.SizeNS, GlfwStandardCursor.ResizeNs)]
    [InlineData(Cursors.SizeWE, GlfwStandardCursor.ResizeEw)]
    [InlineData(Cursors.SizeNWSE, GlfwStandardCursor.ResizeNwse)]
    [InlineData(Cursors.SizeNESW, GlfwStandardCursor.ResizeNesw)]
    public void CustomCursor_ResizeShape_SetsMatchingGlfwShape(Cursors shape, GlfwStandardCursor expected)
    {
        FakeBrutalInput input = new();
        Gum.Input.Cursor cursor = new();
        cursor.AttachBrutalInput(input);

        cursor.CustomCursor = shape;

        input.LastCursorShape.ShouldBe(expected);
    }

    [Fact]
    public void CustomCursor_Null_SetsArrow()
    {
        FakeBrutalInput input = new();
        Gum.Input.Cursor cursor = new();
        cursor.AttachBrutalInput(input);

        cursor.CustomCursor = null;

        input.LastCursorShape.ShouldBe(GlfwStandardCursor.Arrow);
    }

    [Fact]
    public void Dispose_UnsubscribesFromScroll()
    {
        FakeBrutalInput input = new();
        Gum.Input.Cursor cursor = new();
        cursor.AttachBrutalInput(input);

        cursor.Dispose();

        input.ScrolledSubscribers.ShouldBe(0);
    }

    [Fact]
    public void Activity_IsMobile_IgnoresMouse()
    {
        FakeBrutalInput input = new() { CursorX = 37, CursorY = 52 };
        input.HeldButtons.Add(GlfwMouseButton.Left);
        Gum.Input.Cursor cursor = new();
        cursor.AttachBrutalInput(input);
        cursor.IsMobile = true;

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBeFalse();
        cursor.LastInputDevice.ShouldBe(InputDevice.TouchScreen);
    }
}
