namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Parameters for a screenshot render operation.
/// </summary>
public class ScreenshotRequest
{
    /// <summary>
    /// Absolute path to the .gumx project file.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Name of the Screen or Component to render.
    /// </summary>
    public required string ElementName { get; init; }

    /// <summary>
    /// Absolute or relative path for the output PNG file.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Width of the output image in pixels. Defaults to the project canvas width.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Height of the output image in pixels. Defaults to the project canvas height.
    /// </summary>
    public int? Height { get; init; }
}
