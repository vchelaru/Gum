using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace BrutalGum.Tests;

/// <summary>
/// EnableExpandToWindow given a display's scale, as the High-DPI docs use it: the canvas shrinks by the scale, the
/// camera zooms by it, and drawing and clicks both follow.
/// </summary>
public class WindowFitTests : BaseTestClass
{
    private double _time = 100;

    private void Frame()
    {
        GumService.Default.Update(_time += 1);
        GumService.Default.Draw();
    }

    [Fact]
    public void EnableExpandToWindow_AfterTheFramebufferResizes_KeepsTheScale()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 1200, FramebufferHeight = 900 };

        TestAssemblyInitialize.WithBackend(input, _ =>
        {
            GumService.Default.EnableExpandToWindow(defaultZoom: 1.5f);
            Frame();
            input.FramebufferWidth = 1500;
            input.FramebufferHeight = 600;

            Frame();
            Frame();

            GraphicalUiElement.CanvasWidth.ShouldBe(1000);
            GraphicalUiElement.CanvasHeight.ShouldBe(400);
            GumService.Default.Root.GetAbsoluteWidth().ShouldBe(1000);
            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(1.5f);
        });
    }

    [Fact]
    public void EnableExpandToWindow_CalledAgain_AppliesTheNewScaleToTheSameFramebuffer()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 1200, FramebufferHeight = 900 };

        TestAssemblyInitialize.WithBackend(input, _ =>
        {
            GumService.Default.EnableExpandToWindow(defaultZoom: 1f);
            Frame();

            GumService.Default.EnableExpandToWindow(defaultZoom: 1.5f);
            Frame();

            GraphicalUiElement.CanvasWidth.ShouldBe(800);
            GraphicalUiElement.CanvasHeight.ShouldBe(600);
            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(1.5f);
        });
    }

    [Fact]
    public void Draw_WithEnableExpandToWindow_DrawsAtTheScale()
    {
        FakeBrutalInput input = new() { FramebufferWidth = 300, FramebufferHeight = 150 };

        TestAssemblyInitialize.WithBackend(input, backend =>
        {
            GumService.Default.EnableExpandToWindow(defaultZoom: 1.5f);
            ColoredRectangleRuntime rectangle = new() { X = 20, Y = 20, Width = 100, Height = 40, Color = SKColors.Red };
            rectangle.AddToRoot();

            Frame();

            // 20..120 by 20..60 in Gum units is 30..180 by 30..90 in framebuffer pixels.
            backend.Targets[^1].PixelAt(27, 60).Alpha.ShouldBe((byte)0);
            backend.Targets[^1].PixelAt(33, 60).ShouldBe(SKColors.Red);
            backend.Targets[^1].PixelAt(177, 87).ShouldBe(SKColors.Red);
            backend.Targets[^1].PixelAt(183, 60).Alpha.ShouldBe((byte)0);
        });
    }

    // Each click point is inside the button only through the camera zoom: unzoomed it is past the bottom right corner.
    [Theory]
    // macOS Retina: the framebuffer is twice the window.
    [InlineData(400, 300, 800, 600, 2f, 110.0, 50.0)]
    // Windows at 150%: the framebuffer is the window.
    [InlineData(1200, 900, 1200, 900, 1.5f, 180.0, 75.0)]
    public void Update_WithEnableExpandToWindow_ClicksTheButtonDrawnAtTheWindowPoint(
        int windowWidth, int windowHeight, int framebufferWidth, int framebufferHeight, float displayScale,
        double windowX, double windowY)
    {
        FakeBrutalInput input = new()
        {
            WindowWidth = windowWidth, WindowHeight = windowHeight,
            FramebufferWidth = framebufferWidth, FramebufferHeight = framebufferHeight,
        };

        TestAssemblyInitialize.WithBackend(input, _ =>
        {
            GumService.Default.EnableExpandToWindow(displayScale);
            Button button = new() { X = 50, Y = 20, Width = 100, Height = 40 };
            button.AddToRoot();
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
