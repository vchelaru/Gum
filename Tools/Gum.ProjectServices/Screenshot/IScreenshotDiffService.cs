namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Renders every Screen and Component in a project through two <see cref="IScreenshotService"/>
/// backends and diffs each pair pixel-for-pixel.
/// </summary>
public interface IScreenshotDiffService
{
    /// <summary>
    /// Runs the diff described by <paramref name="request"/>.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">The project could not be loaded.</exception>
    ScreenshotDiffResult Diff(ScreenshotDiffRequest request);
}
