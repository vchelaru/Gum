using Gum;
using Gum.Forms;
using Raylib_cs;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// Exercises <see cref="GumService.EnableZoomToWindow"/>/<see cref="GumService.EnableExpandToWindow"/>
/// end-to-end on Raylib. <c>WindowFitMathTests</c> already proves the pure math in
/// <c>WindowFitMath</c> is correct; these tests instead prove the Raylib-specific wiring around
/// it — <c>GumService.GetWindowSize</c>'s <c>#elif RAYLIB</c> branch (<c>Raylib.GetRenderWidth/
/// GetRenderHeight</c>) and applying the computed zoom/canvas onto the real
/// <see cref="SystemManagers"/> camera — actually works. Issue #3571.
/// </summary>
public class GumServiceWindowFitTests
{
    [Fact]
    public void EnableExpandToWindow_AppliesComputedCanvasAndZoom_ToRealRaylibCamera()
    {
        GumService.Default.Uninitialize();
        try
        {
            GumService.Default.Initialize(DefaultVisualsVersion.V2);

            GumService.Default.EnableExpandToWindow(defaultZoom: 2f);

            int windowWidth = Raylib.GetRenderWidth();
            int windowHeight = Raylib.GetRenderHeight();

            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(2f);
            GumService.Default.CanvasWidth.ShouldBe(windowWidth / 2f);
            GumService.Default.CanvasHeight.ShouldBe(windowHeight / 2f);
        }
        finally
        {
            GumService.Default.Uninitialize();
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }

    [Fact]
    public void ApplyFitForSize_HeightDominantZoom_ScalesCameraZoomRelativeToCapturedReferenceSize()
    {
        GumService.Default.Uninitialize();
        try
        {
            GumService.Default.Initialize(DefaultVisualsVersion.V2);

            GumService.Default.EnableZoomToWindow(WindowZoomMode.HeightDominant, defaultZoom: 1f);

            // The reference resolution is captured on this first call/fit, from the real
            // (hidden) Raylib window's physical framebuffer size.
            int referenceWidth = Raylib.GetRenderWidth();
            int referenceHeight = Raylib.GetRenderHeight();
            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(1f);
            GumService.Default.CanvasWidth.ShouldBe((float)referenceWidth);
            GumService.Default.CanvasHeight.ShouldBe((float)referenceHeight);

            // Doubling both dimensions at height-dominant fit should double the zoom while the
            // canvas snaps back to the reference size (mirrors WindowFitMathTests' pure-math case).
            GumService.Default.ApplyFitForSize(referenceWidth * 2, referenceHeight * 2);

            SystemManagers.Default.Renderer.Camera.Zoom.ShouldBe(2f);
            GumService.Default.CanvasWidth.ShouldBe((float)referenceWidth);
            GumService.Default.CanvasHeight.ShouldBe((float)referenceHeight);
        }
        finally
        {
            GumService.Default.Uninitialize();
            TestAssemblyInitialize.ApplyDefaultTestState();
        }
    }
}
