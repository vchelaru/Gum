using Gum;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;

namespace BrutalGum.Tests;

/// <summary>
/// <see cref="IBrutalInput.DisplayScale"/> and <see cref="GumService.FollowDisplayScale"/>: on a high-density display
/// Gum lays out in window units, so the UI keeps its size, and a click still lands on the control drawn under it.
/// </summary>
public class DisplayScaleTests : BaseTestClass
{
    /// <summary>A host that reports the operating system's scale itself, as GLFW's content scale does.</summary>
    private sealed class ScaleReportingInput : FakeBrutalInput, IBrutalInput
    {
        public float DisplayScale { get; set; } = 1f;
    }

    private double _time = 100;

    public override void Dispose()
    {
        GumService.Default.FollowDisplayScale = false;
        base.Dispose();
    }

    private void Frame() => GumService.Default.Update(_time += 1);

    [Theory]
    [InlineData(400, 800, 2f)]
    [InlineData(800, 800, 1f)]
    [InlineData(0, 0, 1f)]
    public void DisplayScale_ByDefault_IsTheFramebufferOverTheWindow_OrOneWhileMinimized(int windowWidth, int framebufferWidth, float expected)
    {
        IBrutalInput input = new FakeBrutalInput { WindowWidth = windowWidth, FramebufferWidth = framebufferWidth };

        input.DisplayScale.ShouldBe(expected);
    }

    [Fact]
    public void FollowDisplayScale_SetTrue_SizesTheCanvasInWindowUnitsAndZoomsByTheScale()
    {
        ScaleReportingInput input = new()
        {
            WindowWidth = 1200, WindowHeight = 900, FramebufferWidth = 1200, FramebufferHeight = 900, DisplayScale = 1.5f,
        };

        TestAssemblyInitialize.WithBackend(input, _ =>
        {
            GumService.Default.FollowDisplayScale = true;

            GraphicalUiElement.CanvasWidth.ShouldBe(800);
            GraphicalUiElement.CanvasHeight.ShouldBe(600);
            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(1.5f);
        });
    }

    [Fact]
    public void Draw_AfterTheDisplayScaleChanges_ReappliesTheNewScale()
    {
        ScaleReportingInput input = new()
        {
            WindowWidth = 1200, WindowHeight = 900, FramebufferWidth = 1200, FramebufferHeight = 900, DisplayScale = 1f,
        };

        TestAssemblyInitialize.WithBackend(input, _ =>
        {
            GumService.Default.FollowDisplayScale = true;
            GumService.Default.Draw();
            // The window moved to a display at 150%; the framebuffer keeps its size, as on Windows.
            input.DisplayScale = 1.5f;

            GumService.Default.Draw();

            GraphicalUiElement.CanvasWidth.ShouldBe(800);
            GraphicalUiElement.CanvasHeight.ShouldBe(600);
            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(1.5f);
        });
    }

    [Fact]
    public void Draw_AfterMovingFromARetinaDisplayToAStandardOne_KeepsTheCanvasInWindowUnits()
    {
        ScaleReportingInput input = new()
        {
            WindowWidth = 400, WindowHeight = 300, FramebufferWidth = 800, FramebufferHeight = 600, DisplayScale = 2f,
        };

        TestAssemblyInitialize.WithBackend(input, _ =>
        {
            GumService.Default.FollowDisplayScale = true;
            GumService.Default.Draw();
            // macOS: the window keeps its size in points, and the framebuffer and the scale halve together.
            input.FramebufferWidth = 400;
            input.FramebufferHeight = 300;
            input.DisplayScale = 1f;

            GumService.Default.Draw();

            GraphicalUiElement.CanvasWidth.ShouldBe(400);
            GraphicalUiElement.CanvasHeight.ShouldBe(300);
            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(1f);
        });
    }

    [Fact]
    public void FollowDisplayScale_SetBeforeInitialize_IsAppliedByInitialize()
    {
        ScaleReportingInput input = new()
        {
            WindowWidth = 400, WindowHeight = 300, FramebufferWidth = 800, FramebufferHeight = 600, DisplayScale = 2f,
        };
        GumService.Default.Uninitialize();
        try
        {
            GumService.Default.FollowDisplayScale = true;

            GumService.Default.Initialize(input, new FakeBrutalGpuBackend());

            GraphicalUiElement.CanvasWidth.ShouldBe(400);
            GraphicalUiElement.CanvasHeight.ShouldBe(300);
            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(2f);
        }
        finally
        {
            TestAssemblyInitialize.RestoreSharedService();
        }
    }

    [Fact]
    public void UpdateAndDraw_WithFollowDisplayScaleOff_KeepTheCanvasInFramebufferPixels()
    {
        ScaleReportingInput input = new()
        {
            WindowWidth = 400, WindowHeight = 300, FramebufferWidth = 800, FramebufferHeight = 600, DisplayScale = 2f,
        };

        TestAssemblyInitialize.WithBackend(input, _ =>
        {
            Frame();
            GumService.Default.Draw();

            GraphicalUiElement.CanvasWidth.ShouldBe(800);
            GraphicalUiElement.CanvasHeight.ShouldBe(600);
            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(1f);
        });
    }

    // Each click point lands inside the button only through the camera zoom: unzoomed, the same point is past its
    // bottom right corner.
    [Theory]
    // macOS Retina: the framebuffer is twice the window, so the cursor is doubled, then halved by the zoom.
    [InlineData(400, 300, 800, 600, 2f, 110.0, 50.0)]
    // Windows at 150%: the framebuffer is the window, and the zoom alone scales the cursor.
    [InlineData(1200, 900, 1200, 900, 1.5f, 180.0, 75.0)]
    public void Update_FollowingTheDisplayScale_ClickAtAWindowPointHitsTheButtonDrawnThere(
        int windowWidth, int windowHeight, int framebufferWidth, int framebufferHeight, float displayScale,
        double windowX, double windowY)
    {
        ScaleReportingInput input = new()
        {
            WindowWidth = windowWidth, WindowHeight = windowHeight,
            FramebufferWidth = framebufferWidth, FramebufferHeight = framebufferHeight,
            DisplayScale = displayScale,
        };

        TestAssemblyInitialize.WithBackend(input, _ =>
        {
            GumService.Default.FollowDisplayScale = true;
            Button button = new();
            button.AddToRoot();
            button.X = 50;
            button.Y = 20;
            button.Width = 100;
            button.Height = 40;
            int clicks = 0;
            button.Click += (_, _) => clicks++;
            input.CursorX = windowX;
            input.CursorY = windowY;
            Frame();

            input.HeldButtons.Add(MouseButton.Left);
            Frame();
            input.HeldButtons.Remove(MouseButton.Left);
            Frame();

            clicks.ShouldBe(1);
        });
    }
}
