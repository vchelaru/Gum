namespace Gum;

/// <summary>
/// Pure zoom/canvas-size math backing <see cref="WindowFitController"/>. Lives in GumCommon (not a
/// MonoGame/Raylib-specific project) so any host — including Skia-family hosts via
/// <c>GumServiceSkiaBase</c> — can compute the same fit behavior (issue #4452).
/// </summary>
public static class WindowFitMath
{
    public static (float zoom, float canvasWidth, float canvasHeight) ComputeZoom(
        int windowWidth, int windowHeight,
        int referenceWidth, int referenceHeight,
        WindowZoomMode mode, float defaultZoom)
    {
        float zoom = mode == WindowZoomMode.HeightDominant
            ? windowHeight / (float)referenceHeight
            : windowWidth / (float)referenceWidth;
        zoom *= defaultZoom;

        return (zoom, windowWidth / zoom, windowHeight / zoom);
    }

    public static (float zoom, float canvasWidth, float canvasHeight) ComputeExpand(
        int windowWidth, int windowHeight,
        float defaultZoom)
    {
        return (defaultZoom, windowWidth / defaultZoom, windowHeight / defaultZoom);
    }
}
