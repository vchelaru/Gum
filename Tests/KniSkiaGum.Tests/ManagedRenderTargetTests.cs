using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;
using XnaGameTime = Microsoft.Xna.Framework.GameTime;

namespace KniSkiaGum.Tests;

/// <summary>
/// <c>GumService.Draw()</c> owns the render target like MonoGameGum's Draw owns its pass: it creates the
/// target at the back buffer size, recreates it when the size changes, clears and presents it. The target
/// is a fake because a real one needs a GPU device. These tests re-initialize the assembly-wide service,
/// so each one puts it back.
/// </summary>
public class ManagedRenderTargetTests : BaseTestClass
{
    private class FakeTarget : IKniSkiaTarget
    {
        private readonly SKBitmap _bitmap;
        private readonly SKCanvas _canvas;

        public FakeTarget(int width, int height)
        {
            Width = width;
            Height = height;
            _bitmap = new SKBitmap(width, height);
            _canvas = new SKCanvas(_bitmap);
        }

        public int Width { get; }
        public int Height { get; }
        public int BeginCount { get; private set; }
        public int PresentCount { get; private set; }
        public int AbortCount { get; private set; }
        public bool IsDisposed { get; private set; }
        public SKColor PixelAt(int x, int y) => _bitmap.GetPixel(x, y);

        public SKCanvas Canvas => _canvas;

        public void Begin()
        {
            BeginCount++;
            _canvas.Clear(SKColors.Transparent);
        }

        public void End() => PresentCount++;

        public void EndWithoutDrawing()
        {
            // The holder's own capture pass during Create is not a failed frame; BeginCount tells them apart.
            if (BeginCount > 1)
            {
                AbortCount++;
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
            _canvas.Dispose();
            _bitmap.Dispose();
        }
    }

    private readonly List<FakeTarget> _created = new();
    private (int width, int height) _windowSize = (640, 480);

    private void Reinitialize()
    {
        GumService.Default.Uninitialize();
        GumService.Default.InitializeWithoutWindow(_windowSize.width, _windowSize.height);
        GumService.Default.Cursor.ReadMouse = () => default;
        GumService.Default.Keyboard.ReadKeyboard = () => default;
        GumService.Default.WindowSizeOverride = () => _windowSize;
        GumService.Default.TargetFactoryOverride = (w, h) =>
        {
            FakeTarget target = new FakeTarget(w, h);
            _created.Add(target);
            return target;
        };
    }

    private static void RestoreSharedService()
    {
        GumService.Default.WindowSizeOverride = null;
        GumService.Default.TargetFactoryOverride = null;
        GumService.Default.Uninitialize();

        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTests.SkiaHostTestFramework.CanvasWidth, SkiaHostTests.SkiaHostTestFramework.CanvasHeight));
        GumService.Default.InitializeWithoutWindow(
            surface.Canvas, SkiaHostTests.SkiaHostTestFramework.CanvasWidth, SkiaHostTests.SkiaHostTestFramework.CanvasHeight);
        GumService.Default.Cursor.ReadMouse = () => default;
        GumService.Default.Keyboard.ReadKeyboard = () => default;

        SkiaHostTests.SkiaHostTestBase.CaptureRenderableBaseline();
        Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Clear();
    }

    private static RectangleRuntime AddRedSquare()
    {
        RectangleRuntime rectangle = new RectangleRuntime();
        rectangle.Width = 50;
        rectangle.Height = 50;
        rectangle.IsFilled = true;
        rectangle.FillColor = SKColors.Red;
        GumService.Default.Root.AddChild(rectangle);
        return rectangle;
    }

    [Fact]
    public void Draw_CreatesTheTargetAtTheWindowSize_DrawsIntoItAndPresentsOnce()
    {
        try
        {
            Reinitialize();
            AddRedSquare();

            GumService.Default.Draw();

            _created.Count.ShouldBe(1);
            FakeTarget target = _created[0];
            target.Width.ShouldBe(640);
            target.Height.ShouldBe(480);
            target.PresentCount.ShouldBe(1);
            target.PixelAt(25, 25).Red.ShouldBe((byte)255);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Draw_ThroughGumServiceSkiaBaseReference_RunsTheKniSkiaOverride()
    {
        try
        {
            Reinitialize();
            AddRedSquare();
            GumServiceSkiaBase baseReference = GumService.Default;

            baseReference.Draw();

            _created.Count.ShouldBe(1);
            _created[0].PresentCount.ShouldBe(1);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Draw_AtTheSameSize_ReusesTheTarget()
    {
        try
        {
            Reinitialize();

            GumService.Default.Draw();
            GumService.Default.Draw();
            GumService.Default.Draw();

            _created.Count.ShouldBe(1);
            _created[0].PresentCount.ShouldBe(3);
            _created[0].IsDisposed.ShouldBeFalse();
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Draw_AfterTheWindowGrows_RecreatesTheTargetAtTheNewSize_AndRelaysOutTheCanvas()
    {
        try
        {
            Reinitialize();
            GumService.Default.Draw();

            _windowSize = (800, 600);
            GumService.Default.Draw();

            _created.Count.ShouldBe(2);
            _created[0].IsDisposed.ShouldBeTrue();
            _created[1].Width.ShouldBe(800);
            _created[1].Height.ShouldBe(600);
            _created[1].PresentCount.ShouldBe(1);
            GraphicalUiElement.CanvasWidth.ShouldBe(800);
            GraphicalUiElement.CanvasHeight.ShouldBe(600);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Draw_AfterAResize_DrawsIntoTheNewTargetNotTheOldOne()
    {
        try
        {
            Reinitialize();
            AddRedSquare();
            GumService.Default.Draw();

            _windowSize = (800, 600);
            GumService.Default.Draw();

            _created[1].PixelAt(25, 25).Red.ShouldBe((byte)255);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Draw_ForAMinimizedWindow_DrawsNothingAndDoesNotCreateATarget()
    {
        try
        {
            Reinitialize();
            _windowSize = (0, 0);

            GumService.Default.Draw();

            _created.ShouldBeEmpty();
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void BeforeDrawAndAfterDraw_RunInsideThePass_AroundGumsOwnDraw()
    {
        try
        {
            Reinitialize();
            AddRedSquare();
            List<string> order = new();
            SKCanvas? beforeCanvas = null;
            GumService.Default.BeforeDraw += canvas =>
            {
                beforeCanvas = canvas;
                order.Add("before presented=" + _created[0].PresentCount);
                // Under Gum's own draw: the red square covers this pixel afterward.
                using SKPaint paint = new SKPaint { Color = SKColors.Blue };
                canvas.DrawRect(0, 0, 100, 100, paint);
            };
            GumService.Default.AfterDraw += canvas =>
            {
                order.Add("after presented=" + _created[0].PresentCount);
                using SKPaint paint = new SKPaint { Color = SKColors.Green };
                canvas.DrawRect(60, 60, 10, 10, paint);
            };

            GumService.Default.Draw();

            order.ShouldBe(new[] { "before presented=0", "after presented=0" });
            beforeCanvas.ShouldBeSameAs(_created[0].Canvas);
            _created[0].PixelAt(25, 25).Red.ShouldBe((byte)255);
            _created[0].PixelAt(75, 75).Blue.ShouldBe((byte)255);
            _created[0].PixelAt(65, 65).Green.ShouldBeGreaterThan((byte)100);
            _created[0].PresentCount.ShouldBe(1);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Draw_WhenDrawingThrows_EndsThePassWithoutPresenting_AndRethrows()
    {
        try
        {
            Reinitialize();
            GumService.Default.AfterDraw += _ => throw new InvalidOperationException("boom");

            Should.Throw<InvalidOperationException>(() => GumService.Default.Draw());

            _created[0].PresentCount.ShouldBe(0);
            _created[0].AbortCount.ShouldBe(1);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Uninitialize_DisposesTheTarget()
    {
        try
        {
            Reinitialize();
            GumService.Default.Draw();

            GumService.Default.Uninitialize();

            _created[0].IsDisposed.ShouldBeTrue();
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Update_AndDraw_NeedNothingElseFromTheGame_ForAFrame()
    {
        try
        {
            Reinitialize();

            GumService.Default.Update(new XnaGameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016)));
            GumService.Default.Draw();

            _created[0].PresentCount.ShouldBe(1);
        }
        finally
        {
            RestoreSharedService();
        }
    }
}
