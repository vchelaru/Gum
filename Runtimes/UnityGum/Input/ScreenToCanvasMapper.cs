using System.Numerics;

namespace Gum.Input;

/// <summary>
/// Converts a Unity screen position (pixels, origin at the bottom left) to Gum canvas pixels (origin at
/// the top left), scaling when the canvas Gum draws into is a different size than the screen.
/// </summary>
public class ScreenToCanvasMapper
{
    private float _screenWidth;
    private float _screenHeight;
    private float _canvasPixelWidth;
    private float _canvasPixelHeight;

    /// <summary>
    /// Sets the screen size and the pixel size of the canvas Gum draws into. Call when either changes.
    /// </summary>
    public void SetSizes(int screenWidth, int screenHeight, int canvasPixelWidth, int canvasPixelHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _canvasPixelWidth = canvasPixelWidth;
        _canvasPixelHeight = canvasPixelHeight;
    }

    /// <summary>
    /// Maps <paramref name="screenPosition"/> to canvas pixels. Returns the origin until
    /// <see cref="SetSizes"/> has been given a non-zero screen size.
    /// </summary>
    public Vector2 Map(Vector2 screenPosition)
    {
        if (_screenWidth <= 0 || _screenHeight <= 0)
        {
            return Vector2.Zero;
        }

        return new Vector2(
            screenPosition.X * _canvasPixelWidth / _screenWidth,
            (_screenHeight - screenPosition.Y) * _canvasPixelHeight / _screenHeight);
    }
}
