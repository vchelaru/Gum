namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Result of a screenshot render operation.
/// </summary>
public class ScreenshotResult
{
    /// <summary>Gets whether the screenshot was written successfully.</summary>
    public bool Success { get; private init; }

    /// <summary>Gets the error message if <see cref="Success"/> is <c>false</c>.</summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>Gets the path the PNG was written to if <see cref="Success"/> is <c>true</c>.</summary>
    public string? OutputPath { get; private init; }

    /// <summary>
    /// Creates a successful result with the given output path.
    /// </summary>
    public static ScreenshotResult Succeeded(string outputPath) =>
        new() { Success = true, OutputPath = outputPath };

    /// <summary>
    /// Creates a failed result with the given error message.
    /// </summary>
    public static ScreenshotResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
