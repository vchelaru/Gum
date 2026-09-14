namespace HtmlToGumPlugin;

/// <summary>What one HTML import runs with: the source, how to read it, and where the screen goes.</summary>
public sealed class ImportOptions
{
    public string HtmlPath { get; set; } = "";
    public bool IsUrl { get; set; }
    public string Selector { get; set; } = "body";
    public string ScreenName { get; set; } = "ImportedScreen";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public bool NoResponsive { get; set; }
    public string DestinationSubfolder { get; set; } = "";
}
