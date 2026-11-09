using Brutal.GlfwApi;
using Gum;
using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;

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

    [Theory]
    [InlineData(GlfwMouseButton.Left, true, false, false)]
    [InlineData(GlfwMouseButton.Middle, false, true, false)]
    [InlineData(GlfwMouseButton.Right, false, false, true)]
    public void Activity_HeldButton_SetsOnlyTheMatchingDownFlag(GlfwMouseButton held, bool primary, bool middle, bool secondary)
    {
        FakeBrutalInput input = new();
        input.HeldButtons.Add(held);
        Gum.Input.Cursor cursor = new();
        cursor.AttachBrutalInput(input);

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBe(primary);
        cursor.MiddleDown.ShouldBe(middle);
        cursor.SecondaryDown.ShouldBe(secondary);
    }

    [Fact]
    public void Activity_NoButtonHeld_SetsNoDownFlag()
    {
        FakeBrutalInput input = new();
        Gum.Input.Cursor cursor = new();
        cursor.AttachBrutalInput(input);

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBeFalse();
        cursor.MiddleDown.ShouldBeFalse();
        cursor.SecondaryDown.ShouldBeFalse();
    }

    [Fact]
    public void Activity_ReportsWindowPosition()
    {
        FakeBrutalInput input = new() { CursorX = 37.9, CursorY = 52.2 };
        Gum.Input.Cursor cursor = new();
        cursor.AttachBrutalInput(input);

        cursor.Activity(0);

        cursor.X.ShouldBe(37);
        cursor.Y.ShouldBe(52);
    }

    [Fact]
    public void Initialize_Twice_DisposesThePreviousCursor()
    {
        FakeBrutalInput input = new();
        try
        {
            SKSurface first = SKSurface.Create(new SKImageInfo(64, 64));
            GumService.Default.Initialize(first.Canvas, input, 64, 64);
            input.ScrolledSubscribers.ShouldBe(1);

            SKSurface second = SKSurface.Create(new SKImageInfo(64, 64));
            GumService.Default.Initialize(second.Canvas, input, 64, 64);

            input.ScrolledSubscribers.ShouldBe(1);
        }
        finally
        {
            GumService.Default.Uninitialize();
            SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
            GumService.Default.Initialize(surface.Canvas, TestAssemblyInitialize.Input, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
            SkiaHostTestBase.CaptureRenderableBaseline();
        }
    }
}
