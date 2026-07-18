namespace TextureCoordinateSelectionPlugin.Models;

public class ExposedTextureCoordinateSet
{
    public string SourceObjectName { get; set; } = string.Empty;
    public string? ExposedLeftName { get; set; }
    public string? ExposedTopName { get; set; }
    public string? ExposedWidthName { get; set; }
    public string? ExposedHeightName { get; set; }
}
