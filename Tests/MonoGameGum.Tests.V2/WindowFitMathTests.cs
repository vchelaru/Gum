using Gum;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.V2;

public class WindowFitMathTests
{
    [Fact]
    public void ComputeExpand_KeepsCanvasMatchingWindow_AtDefaultZoomOne()
    {
        var (zoom, canvasWidth, canvasHeight) = WindowFitMath.ComputeExpand(
            windowWidth: 1280, windowHeight: 720, defaultZoom: 1f);

        zoom.ShouldBe(1f);
        canvasWidth.ShouldBe(1280f);
        canvasHeight.ShouldBe(720f);
    }

    [Fact]
    public void ComputeExpand_ScalesCanvasInverselyToDefaultZoom()
    {
        var (zoom, canvasWidth, canvasHeight) = WindowFitMath.ComputeExpand(
            windowWidth: 1280, windowHeight: 720, defaultZoom: 2f);

        zoom.ShouldBe(2f);
        canvasWidth.ShouldBe(640f);
        canvasHeight.ShouldBe(360f);
    }

    [Fact]
    public void ComputeZoom_HeightDominant_ReturnsOne_AtReferenceResolution()
    {
        var (zoom, canvasWidth, canvasHeight) = WindowFitMath.ComputeZoom(
            windowWidth: 800, windowHeight: 600,
            referenceWidth: 800, referenceHeight: 600,
            WindowZoomMode.HeightDominant, defaultZoom: 1f);

        zoom.ShouldBe(1f);
        canvasWidth.ShouldBe(800f);
        canvasHeight.ShouldBe(600f);
    }

    [Fact]
    public void ComputeZoom_HeightDominant_ScalesByHeightRatio()
    {
        var (zoom, canvasWidth, canvasHeight) = WindowFitMath.ComputeZoom(
            windowWidth: 1600, windowHeight: 1200,
            referenceWidth: 800, referenceHeight: 600,
            WindowZoomMode.HeightDominant, defaultZoom: 1f);

        zoom.ShouldBe(2f);
        canvasWidth.ShouldBe(800f);
        canvasHeight.ShouldBe(600f);
    }

    [Fact]
    public void ComputeZoom_HeightDominant_IgnoresWidthChanges_ForZoomFactor()
    {
        // Height stayed at reference, width grew. Zoom should NOT change.
        var (zoom, canvasWidth, _) = WindowFitMath.ComputeZoom(
            windowWidth: 1600, windowHeight: 600,
            referenceWidth: 800, referenceHeight: 600,
            WindowZoomMode.HeightDominant, defaultZoom: 1f);

        zoom.ShouldBe(1f);
        // Wider window at same zoom means more horizontal canvas room.
        canvasWidth.ShouldBe(1600f);
    }

    [Fact]
    public void ComputeZoom_WidthDominant_ScalesByWidthRatio()
    {
        var (zoom, canvasWidth, canvasHeight) = WindowFitMath.ComputeZoom(
            windowWidth: 1600, windowHeight: 600,
            referenceWidth: 800, referenceHeight: 600,
            WindowZoomMode.WidthDominant, defaultZoom: 1f);

        zoom.ShouldBe(2f);
        canvasWidth.ShouldBe(800f);
        canvasHeight.ShouldBe(300f);
    }

    [Fact]
    public void ComputeZoom_AppliesDefaultZoomAsMultiplierOnTopOfFitZoom()
    {
        // At reference, fit-zoom = 1, defaultZoom = 2 → final zoom = 2.
        var (zoomAtReference, _, _) = WindowFitMath.ComputeZoom(
            windowWidth: 800, windowHeight: 600,
            referenceWidth: 800, referenceHeight: 600,
            WindowZoomMode.HeightDominant, defaultZoom: 2f);

        zoomAtReference.ShouldBe(2f);

        // At 2× reference height, fit-zoom = 2, defaultZoom = 2 → final zoom = 4.
        var (zoomAtDoubleHeight, _, _) = WindowFitMath.ComputeZoom(
            windowWidth: 800, windowHeight: 1200,
            referenceWidth: 800, referenceHeight: 600,
            WindowZoomMode.HeightDominant, defaultZoom: 2f);

        zoomAtDoubleHeight.ShouldBe(4f);
    }
}
