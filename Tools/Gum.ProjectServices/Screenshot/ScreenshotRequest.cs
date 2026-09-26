using Gum.DataTypes;

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

    /// <summary>
    /// Background color to clear to before rendering. Defaults to fully transparent when null.
    /// </summary>
    public ScreenshotColor? BackgroundColor { get; init; }

    /// <summary>
    /// The size to render at: <see cref="Width"/>/<see cref="Height"/> when set, otherwise the
    /// project's canvas size, otherwise 800x600.
    /// </summary>
    public (int Width, int Height) ResolveSize(GumProjectSave project)
    {
        int width = Width ?? (project.DefaultCanvasWidth > 0 ? project.DefaultCanvasWidth : 800);
        int height = Height ?? (project.DefaultCanvasHeight > 0 ? project.DefaultCanvasHeight : 600);
        return (width, height);
    }
}
