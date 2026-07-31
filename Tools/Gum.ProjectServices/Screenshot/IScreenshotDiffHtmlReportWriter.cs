namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Writes a <see cref="ScreenshotDiffResult"/> to a side-by-side HTML report for visual triage.
/// </summary>
public interface IScreenshotDiffHtmlReportWriter
{
    /// <summary>
    /// Writes the report to <paramref name="outputPath"/> and returns that same path.
    /// </summary>
    string Write(ScreenshotDiffResult result, string outputPath);
}
