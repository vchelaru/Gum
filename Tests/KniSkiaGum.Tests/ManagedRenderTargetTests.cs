using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
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

        public List<SKColor> PresentedPixels { get; } = new();

        public void End()
        {
            PresentCount++;
            PresentedPixels.Add(_bitmap.GetPixel(25, 25));
        }

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
    public void GumBatch_BeforeAndAfterDraw_EachPassIsPresentedInCallOrder_OnTheSameTarget()
    {
        try
        {
            Reinitialize();
            AddRedSquare();
            RectangleRuntime green = CreateSquare(SKColors.Green);
            RectangleRuntime blue = CreateSquare(SKColors.Blue);
            GumBatch gumBatch = new GumBatch();

            gumBatch.Begin();
            gumBatch.Draw(green);
            gumBatch.End();
            GumService.Default.Draw();
            gumBatch.Begin();
            gumBatch.Draw(blue);
            gumBatch.End();

            _created.Count.ShouldBe(1);
            _created[0].PresentCount.ShouldBe(3);
            _created[0].PresentedPixels.Select(pixel => pixel.ToString()).ShouldBe(
                new[] { SKColors.Green, SKColors.Red, SKColors.Blue }.Select(color => color.ToString()));
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void GumBatch_BeginWithACanvas_DoesNotOpenAFrameOnTheTarget()
    {
        try
        {
            Reinitialize();
            using SKBitmap bitmap = new SKBitmap(64, 64);
            using SKCanvas canvas = new SKCanvas(bitmap);
            GumBatch gumBatch = new GumBatch();

            gumBatch.Begin(canvas);
            gumBatch.Draw(CreateSquare(SKColors.Green));
            gumBatch.End();

            _created.ShouldBeEmpty();
            bitmap.GetPixel(25, 25).ShouldBe(SKColors.Green);
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
            GraphicalUiElement thrower = new GraphicalUiElement(new ThrowingShape());
            GumService.Default.Root.AddChild(thrower);

            Should.Throw<InvalidOperationException>(() => GumService.Default.Draw());

            _created[0].PresentCount.ShouldBe(0);
            _created[0].AbortCount.ShouldBe(1);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    private class ThrowingShape : SkiaGum.Renderables.RenderableShapeBase
    {
        public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation) =>
            throw new InvalidOperationException("boom");
    }

    private static RectangleRuntime CreateSquare(SKColor color)
    {
        RectangleRuntime rectangle = new RectangleRuntime();
        rectangle.Width = 50;
        rectangle.Height = 50;
        rectangle.IsFilled = true;
        rectangle.FillColor = color;
        return rectangle;
    }

    [Fact]
    public void Draw_WhileAGumBatchPassIsOpen_ThrowsNamingEnd_AndTheNextFrameRecovers()
    {
        try
        {
            Reinitialize();
            AddRedSquare();
            GumBatch gumBatch = new GumBatch();
            gumBatch.Begin();

            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => GumService.Default.Draw());

            exception.Message.ShouldContain("GumBatch.End");
            _created[0].PresentCount.ShouldBe(0);

            GumService.Default.Draw();

            _created[0].PresentCount.ShouldBe(1);
            _created[0].PresentedPixels[0].ShouldBe(SKColors.Red);
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
