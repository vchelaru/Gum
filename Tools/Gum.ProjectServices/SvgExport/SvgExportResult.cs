namespace Gum.ProjectServices.SvgExport;

/// <summary>
/// Result of an SVG export operation.
/// </summary>
public class SvgExportResult
{
    /// <summary>Gets whether the SVG was written successfully.</summary>
    public bool Success { get; private init; }

    /// <summary>Gets the error message if <see cref="Success"/> is <c>false</c>.</summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>Gets the path the SVG was written to if <see cref="Success"/> is <c>true</c>.</summary>
    public string? OutputPath { get; private init; }

    /// <summary>
    /// Creates a successful result with the given output path.
    /// </summary>
    public static SvgExportResult Succeeded(string outputPath) =>
        new() { Success = true, OutputPath = outputPath };

    /// <summary>
    /// Creates a failed result with the given error message.
    /// </summary>
    public static SvgExportResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
