namespace Gum.ProjectServices.SvgExport;

/// <summary>
/// Defines a service that renders a Gum element to an SVG file.
/// </summary>
public interface ISvgExportService
{
    /// <summary>
    /// Renders the specified element and writes the result to an SVG file.
    /// </summary>
    SvgExportResult ExportSvg(SvgExportRequest request);
}
