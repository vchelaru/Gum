namespace Gum.ProjectServices.SvgExport;

/// <summary>
/// Parameters for an SVG export operation.
/// </summary>
public class SvgExportRequest
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
    /// Absolute or relative path for the output SVG file.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Width of the output SVG in pixels. Defaults to the project canvas width.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Height of the output SVG in pixels. Defaults to the project canvas height.
    /// </summary>
    public int? Height { get; init; }
}
