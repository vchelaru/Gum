using Gum.Wireframe;

namespace AvaloniaCanvasSpike;

/// <summary>
/// A Gum scene rendered off-screen by an XNA-family backend and read back as an RGBA pixel
/// buffer, so any host that can blit a bitmap can show it. Sizes are in device pixels; the
/// Gum canvas is sized in world units as pixels divided by <see cref="Zoom"/>.
/// </summary>
public interface ICanvasRenderer : IDisposable
{
    /// <summary>Width of the pixel buffer in device pixels.</summary>
    int PixelWidth { get; }

    /// <summary>Height of the pixel buffer in device pixels.</summary>
    int PixelHeight { get; }

    /// <summary>Camera zoom. World units times zoom equals device pixels.</summary>
    float Zoom { get; set; }

    /// <summary>RGBA, row-major, <see cref="PixelWidth"/> times 4 bytes per row.</summary>
    byte[] PixelBuffer { get; }

    /// <summary>Milliseconds spent in the last <see cref="RenderFrame"/>, including readback.</summary>
    double LastFrameMilliseconds { get; }

    /// <summary>Milliseconds the last frame spent drawing the scene into the render target.</summary>
    double LastDrawMilliseconds { get; }

    /// <summary>Milliseconds the last frame spent reading the render target back to CPU.</summary>
    double LastReadbackMilliseconds { get; }

    /// <summary>Milliseconds the last frame spent presenting the backend's own hidden window.</summary>
    double LastPresentMilliseconds { get; }

    /// <summary>
    /// When true, the backend's swap to its hidden window is skipped. Nothing is ever shown
    /// there, so this isolates whether vsync on that window is stalling the frame.
    /// </summary>
    bool SkipPresent { get; set; }

    /// <summary>Name of the element currently loaded.</summary>
    string LoadedElementName { get; }

    /// <summary>Resizes the render target and pixel buffer, and relayouts the scene.</summary>
    void Resize(int pixelWidth, int pixelHeight);

    /// <summary>Renders one frame into <see cref="PixelBuffer"/>.</summary>
    void RenderFrame();

    /// <summary>Returns the deepest visible element containing the world point, or null.</summary>
    GraphicalUiElement? HitTest(float worldX, float worldY);
}
