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
            if (isDown) _input.HeldButtons.Add(MouseButton.Left);
            else _input.HeldButtons.Remove(MouseButton.Left);
        }

        public void Scroll(int notches) => _input.RaiseScroll(notches);
    }

    protected override SkiaHostTests.IHostCursorRig CreateRig() => new BrutalCursorRig();

    [Theory]
    [InlineData(Cursors.SizeNS, Cursors.SizeNS)]
    [InlineData(Cursors.SizeWE, Cursors.SizeWE)]
    [InlineData(Cursors.SizeNWSE, Cursors.SizeNWSE)]
    [InlineData(Cursors.SizeNESW, Cursors.SizeNESW)]
    public void CustomCursor_ResizeShape_SetsMatchingShape(Cursors shape, Cursors expected)
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

        input.LastCursorShape.ShouldBe(Cursors.Arrow);
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
        input.HeldButtons.Add(MouseButton.Left);
        Gum.Input.Cursor cursor = new();
        cursor.AttachBrutalInput(input);
        cursor.IsMobile = true;

        cursor.Activity(0);

        cursor.PrimaryDown.ShouldBeFalse();
        cursor.LastInputDevice.ShouldBe(InputDevice.TouchScreen);
    }

    [Theory]
    [InlineData(MouseButton.Left, true, false, false)]
    [InlineData(MouseButton.Middle, false, true, false)]
    [InlineData(MouseButton.Right, false, false, true)]
    public void Activity_HeldButton_SetsOnlyTheMatchingDownFlag(MouseButton held, bool primary, bool middle, bool secondary)
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
