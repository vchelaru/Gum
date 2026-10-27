using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;

namespace UnityGum.Tests;

/// <summary>
/// EnableExpandToWindow / EnableZoomToWindow on the Unity host, where the screen size is whatever the
/// host last reported through Initialize/HandleResize (issue #6944). Each test initializes its own
/// 1280x720 service and restores the shared one afterwards.
/// </summary>
public class WindowFitTests : BaseTestClass
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;

    private static void WithScreen(Action test)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(ScreenWidth, ScreenHeight));
        GumService.Default.Uninitialize();
        GumService.Default.Initialize(surface.Canvas, ScreenWidth, ScreenHeight);
        try
        {
            test();
        }
        finally
        {
            GumService.Default.Uninitialize();
            using SKSurface shared = SKSurface.Create(
                new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
            GumService.Default.Initialize(shared.Canvas, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
            SkiaHostTestBase.CaptureRenderableBaseline();
        }
    }

    private static void RunFrames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GumService.Default.Update(i * 0.016);
        }
    }

    [Fact]
    public void EnableExpandToWindow_WithZoom_KeepsCanvasAtScreenOverZoom_AcrossFrames()
    {
        WithScreen(() =>
        {
            GumService.Default.EnableExpandToWindow(2f);

            RunFrames(10);

            GraphicalUiElement.CanvasWidth.ShouldBe(640);
            GraphicalUiElement.CanvasHeight.ShouldBe(360);
            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(2f);
        });
    }

    [Fact]
    public void EnableExpandToWindow_CalledTwice_StaysAtScreenOverZoom()
    {
        WithScreen(() =>
        {
            GumService.Default.EnableExpandToWindow(2f);
            GumService.Default.EnableExpandToWindow(2f);
            RunFrames(5);

            GraphicalUiElement.CanvasWidth.ShouldBe(640);
            GraphicalUiElement.CanvasHeight.ShouldBe(360);
        });
    }

    [Fact]
    public void EnableExpandToWindow_WithoutZoom_KeepsCanvasAtScreenSize()
    {
        WithScreen(() =>
        {
            GumService.Default.EnableExpandToWindow();

            RunFrames(5);

            GraphicalUiElement.CanvasWidth.ShouldBe(ScreenWidth);
            GraphicalUiElement.CanvasHeight.ShouldBe(ScreenHeight);
        });
    }

    [Fact]
    public void EnableExpandToWindow_AfterHostResize_FollowsNewScreenSizeAndStaysStable()
    {
        WithScreen(() =>
        {
            GumService.Default.EnableExpandToWindow(2f);
            RunFrames(3);

            GumService.Default.HandleResize(1920, 1080);
            RunFrames(5);

            GraphicalUiElement.CanvasWidth.ShouldBe(960);
            GraphicalUiElement.CanvasHeight.ShouldBe(540);
        });
    }

    [Fact]
    public void EnableZoomToWindow_KeepsReferenceCanvasAcrossFrames()
    {
        WithScreen(() =>
        {
            GumService.Default.EnableZoomToWindow(WindowZoomMode.HeightDominant, 2f);

            RunFrames(10);

            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(2f);
            GraphicalUiElement.CanvasWidth.ShouldBe(640);
            GraphicalUiElement.CanvasHeight.ShouldBe(360);
        });
    }

    [Fact]
    public void EnableZoomToWindow_AfterHostResize_ScalesFromTheOriginalReference()
    {
        WithScreen(() =>
        {
            GumService.Default.EnableZoomToWindow();
            RunFrames(3);

            GumService.Default.HandleResize(1920, 1080);
            RunFrames(5);

            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(1.5f);
            GraphicalUiElement.CanvasWidth.ShouldBe(1280);
            GraphicalUiElement.CanvasHeight.ShouldBe(720);
        });
    }

    [Fact]
    public void HandleResize_WithoutFitPolicy_SetsCanvasToTheNewSize()
    {
        WithScreen(() =>
        {
            GumService.Default.HandleResize(800, 600);

            GraphicalUiElement.CanvasWidth.ShouldBe(800);
            GraphicalUiElement.CanvasHeight.ShouldBe(600);
        });
    }
}
