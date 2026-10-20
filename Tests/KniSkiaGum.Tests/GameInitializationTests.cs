using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;
using XnaGameTime = Microsoft.Xna.Framework.GameTime;

namespace KniSkiaGum.Tests;

/// <summary>
/// The KNI Skia host initializes like MonoGameGum: from the game, with no canvas, so a game can build
/// its first screen in <c>Game.Initialize</c>, and it exposes the last <see cref="XnaGameTime"/> the way
/// MonoGameGum does. These tests re-initialize the assembly-wide service, so each one puts it back.
/// </summary>
public class GameInitializationTests : BaseTestClass
{
    private const int Width = 640;
    private const int Height = 480;

    /// <summary>
    /// Tears the shared service down and initializes it again with no canvas, the way a game does in
    /// <c>Game.Initialize</c>. The test must call <see cref="RestoreSharedService"/> when done.
    /// </summary>
    private static void ReinitializeWithoutCanvas()
    {
        GumService.Default.Uninitialize();
        GumService.Default.InitializeWithoutWindow(Width, Height);
        GumService.Default.Cursor.ReadMouse = () => default;
        GumService.Default.Keyboard.ReadKeyboard = () => default;
    }

    private static void RestoreSharedService()
    {
        GumService.Default.Uninitialize();

        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.InitializeWithoutWindow(surface.Canvas, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
        GumService.Default.Cursor.ReadMouse = () => default;
        GumService.Default.Keyboard.ReadKeyboard = () => default;

        SkiaHostTestBase.CaptureRenderableBaseline();
        FrameworkElement.KeyboardsForUiControl.Clear();
    }

    [Fact]
    public void InitializeWithoutCanvas_SizesTheCanvasAndRoot_FromTheGivenSize()
    {
        try
        {
            ReinitializeWithoutCanvas();

            GumService.Default.IsInitialized.ShouldBeTrue();
            GraphicalUiElement.CanvasWidth.ShouldBe(Width);
            GraphicalUiElement.CanvasHeight.ShouldBe(Height);
            GumService.Default.Root.AbsoluteWidth.ShouldBe(Width);
            GumService.Default.Root.AbsoluteHeight.ShouldBe(Height);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void InitializeWithoutCanvas_AllowsBuildingAndUpdatingAScreen_BeforeTheFirstDraw()
    {
        try
        {
            ReinitializeWithoutCanvas();

            StackPanel panel = new StackPanel();
            panel.AddToRoot();
            Button button = new Button();
            button.Text = "Play";
            panel.AddChild(button);
            Label label = new Label();
            label.Text = "Hello";
            panel.AddChild(label);

            GumService.Default.Update(new XnaGameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016)));

            button.Visual.AbsoluteWidth.ShouldBeGreaterThan(0);
            label.Visual.AbsoluteHeight.ShouldBeGreaterThan(0);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void FirstDrawCanvas_IsWhatGumRendersTo_AfterInitializingWithoutCanvas()
    {
        try
        {
            ReinitializeWithoutCanvas();
            RectangleRuntime rectangle = new RectangleRuntime();
            rectangle.Width = 50;
            rectangle.Height = 50;
            rectangle.IsFilled = true;
            rectangle.FillColor = SKColors.Red;
            GumService.Default.Root.AddChild(rectangle);

            using SKSurface surface = SKSurface.Create(new SKImageInfo(Width, Height));
            GumService.Default.Draw(surface.Canvas);

            using SKBitmap bitmap = new SKBitmap(Width, Height);
            surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0).ShouldBeTrue();
            bitmap.GetPixel(25, 25).Red.ShouldBe((byte)255);
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Draw_WithoutAnyCanvas_ThrowsAndNamesTheCanvasOverload()
    {
        try
        {
            ReinitializeWithoutCanvas();

            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => GumService.Default.Draw());

            exception.Message.ShouldContain("Draw(SKCanvas)");
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Game_BeforeInitializeWithAGame_Throws()
    {
        // The seam initializes without a game, so Game has nothing to return, as in MonoGameGum
        // before Initialize.
        Should.Throw<InvalidOperationException>(() => _ = GumService.Default.Game);
    }

    [Fact]
    public void GameTime_IsNull_UntilTheFirstUpdate()
    {
        try
        {
            ReinitializeWithoutCanvas();

            GumService.Default.GameTime.ShouldBeNull();
            IGumService.Default!.GameTime.ShouldBeNull();
        }
        finally
        {
            RestoreSharedService();
        }
    }

    [Fact]
    public void Update_WithGameTime_ExposesItAsGameTime()
    {
        XnaGameTime gameTime = new XnaGameTime(TimeSpan.FromSeconds(12.5), TimeSpan.FromSeconds(0.25));

        GumService.Default.Update(gameTime);

        GumService.Default.GameTime.ShouldBeSameAs(gameTime);
        IGumService.Default!.GameTime.ShouldBe(12.5f);
    }

    // A game that zooms shrinks the canvas after Initialize (CanvasWidth = ClientWidth / zoom); the KNI
    // Update(GameTime) path has to resize Root to it, as MonoGame does, or children lay out off screen.
    [Fact]
    public void Update_WithGameTime_SizesRootToTheCurrentCanvas()
    {
        GraphicalUiElement.CanvasWidth = 120;
        GraphicalUiElement.CanvasHeight = 60;

        GumService.Default.Update(new XnaGameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016)));

        GumService.Default.Root.GetAbsoluteWidth().ShouldBe(120);
        GumService.Default.Root.GetAbsoluteHeight().ShouldBe(60);
    }

    [Fact]
    public void Update_WithGameTime_PumpsFormsInputWithTotalSeconds()
    {
        Mock<ICursor> cursor = new Mock<ICursor>();
        FormsUtilities.SetCursor(cursor.Object);

        GumService.Default.Update(new XnaGameTime(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(0.016)));

        cursor.Verify(c => c.Activity(7), Times.Once());
    }

    [Fact]
    public void Update_WithGameTimeAndRoot_ExposesGameTimeAndPumpsInput()
    {
        Mock<ICursor> cursor = new Mock<ICursor>();
        FormsUtilities.SetCursor(cursor.Object);
        ContainerRuntime customRoot = new ContainerRuntime();
        XnaGameTime gameTime = new XnaGameTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(0.016));

        GumService.Default.Update(gameTime, customRoot);

        GumService.Default.GameTime.ShouldBeSameAs(gameTime);
        cursor.Verify(c => c.Activity(3), Times.Once());
    }

    [Fact]
    public void Update_WithGameTimeAndRoots_ExposesGameTimeAndPumpsInput()
    {
        Mock<ICursor> cursor = new Mock<ICursor>();
        FormsUtilities.SetCursor(cursor.Object);
        List<GraphicalUiElement> roots = new List<GraphicalUiElement> { new ContainerRuntime(), new ContainerRuntime() };
        XnaGameTime gameTime = new XnaGameTime(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(0.016));

        GumService.Default.Update(gameTime, roots);

        GumService.Default.GameTime.ShouldBeSameAs(gameTime);
        cursor.Verify(c => c.Activity(4), Times.Once());
    }

    [Fact]
    public void Update_WithGameTimeAndRoot_AnimatesThatRootInsteadOfTheServiceRoot()
    {
        FrameCounter inCustomRoot = new FrameCounter();
        FrameCounter inServiceRoot = new FrameCounter();
        inServiceRoot.AddToRoot();
        ContainerRuntime customRoot = new ContainerRuntime();
        customRoot.AddChild(inCustomRoot.Visual);

        // The shared service keeps its previous time across tests, so only the second frame's step is known.
        GumService.Default.Update(new XnaGameTime(TimeSpan.FromSeconds(1000), TimeSpan.FromSeconds(1)), customRoot);
        double afterFirstFrame = inCustomRoot.Seconds;
        GumService.Default.Update(new XnaGameTime(TimeSpan.FromSeconds(1000.5), TimeSpan.FromSeconds(0.5)), customRoot);

        (inCustomRoot.Seconds - afterFirstFrame).ShouldBe(0.5);
        inServiceRoot.Seconds.ShouldBe(0);
    }

    [Fact]
    public void EnableExpandToWindow_FollowsTheGameWindowSize()
    {
        try
        {
            ReinitializeWithoutCanvas();
            GumService.Default.WindowSizeOverride = () => (1000, 500);

            GumService.Default.EnableExpandToWindow();
            GumService.Default.Update(new XnaGameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016)));

            GraphicalUiElement.CanvasWidth.ShouldBe(1000);
            GraphicalUiElement.CanvasHeight.ShouldBe(500);
        }
        finally
        {
            GumService.Default.WindowSizeOverride = null;
            RestoreSharedService();
        }
    }

    [Fact]
    public void Update_WhenTheGameIsInactive_SkipsInputButStillExposesGameTime()
    {
        Mock<ICursor> cursor = new Mock<ICursor>();
        FormsUtilities.SetCursor(cursor.Object);
        GumService.Default.IsGameActiveOverride = () => false;
        try
        {
            XnaGameTime gameTime = new XnaGameTime(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(0.016));

            GumService.Default.Update(gameTime);

            GumService.Default.GameTime.ShouldBeSameAs(gameTime);
            cursor.Verify(c => c.Activity(It.IsAny<double>()), Times.Never());
        }
        finally
        {
            GumService.Default.IsGameActiveOverride = null;
        }
    }

    [Fact]
    public void Uninitialize_ClearsGameTime()
    {
        try
        {
            GumService.Default.Update(new XnaGameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(0.016)));

            GumService.Default.Uninitialize();
            GumService.Default.InitializeWithoutWindow(Width, Height);

            GumService.Default.GameTime.ShouldBeNull();
        }
        finally
        {
            RestoreSharedService();
        }
    }

    private class FrameCounter : FrameworkElement, IUpdateEveryFrame
    {
        public FrameCounter() : base(new ContainerRuntime())
        {
        }

        public double Seconds { get; private set; }

        void IUpdateEveryFrame.Activity(double secondDifference) => Seconds += secondDifference;
    }
}
