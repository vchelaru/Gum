namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Defines a service that renders a Gum element to a PNG file.
/// </summary>
/// <remarks>
/// Implementations are backend-specific (e.g., MonoGame, Skia). The CLI uses
/// <c>MonoGameScreenshotService</c> by default. Future backends can be added by
/// implementing this interface.
/// </remarks>
public interface IScreenshotService
{
    /// <summary>
    /// Renders the specified element and writes the result to a PNG file.
    /// </summary>
    ScreenshotResult TakeScreenshot(ScreenshotRequest request);
}
