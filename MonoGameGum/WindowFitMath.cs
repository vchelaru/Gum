#if MONOGAME || KNI || FNA
namespace MonoGameGum;
#elif RAYLIB
namespace RaylibGum;
#endif

internal static class WindowFitMath
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
