using System;

namespace GumPreview;

/// <summary>
/// Converts between window points and back-buffer pixels for a high-DPI (Retina) window, where one
/// point covers <see cref="Factor"/> pixels.
/// </summary>
internal readonly struct BackingScale
{
    public BackingScale(double factor)
    {
        Factor = factor;
    }

    public double Factor { get; }

    public bool IsScaled => Factor != 1;

    // Rounds up so the window's pixels always cover the whole canvas.
    public (int width, int height) ToPoints(int pixelWidth, int pixelHeight) =>
        ((int)Math.Ceiling(pixelWidth / Factor), (int)Math.Ceiling(pixelHeight / Factor));

    public (int width, int height) ToPixels(int pointWidth, int pointHeight) =>
        ((int)Math.Round(pointWidth * Factor), (int)Math.Round(pointHeight * Factor));
}
