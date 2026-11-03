using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.TextureCoordinates;

/// <summary>
/// A tiny stand-in for a sprite-sheet plugin: reports the given regions for one file name through
/// <see cref="PluginBase.GetTextureRegionsForFile"/>, the way a real plugin would. Also the sample
/// of the API for plugin authors, so it stays this small.
/// </summary>
internal sealed class SampleRegionPlugin : PluginBase
{
    private readonly string _fileName;
    private readonly Func<IReadOnlyList<TextureRegion>> _regions;

    public SampleRegionPlugin(string fileName, IEnumerable<TextureRegion> regions, bool throws = false)
        : this(fileName, () => throws ? throw new InvalidOperationException("buggy plugin") : regions.ToList())
    {
    }

    public SampleRegionPlugin(string fileName, Func<IReadOnlyList<TextureRegion>> regions)
    {
        _fileName = fileName;
        _regions = regions;
    }

    /// <summary>How many times the tab asked this plugin for regions.</summary>
    public int QueryCount { get; private set; }

    public override string FriendlyName => "Sample region plugin";

    public override void StartUp() => GetTextureRegionsForFile += HandleGetTextureRegions;

    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    private IReadOnlyList<TextureRegion>? HandleGetTextureRegions(FilePath file)
    {
        QueryCount++;
        // Only the file this plugin knows; anything else is not its business.
        return file.FileNameNoPath == _fileName ? _regions() : null;
    }

    public static IReadOnlyList<TextureRegion> FromSheet(IEnumerable<(string Name, int X, int Y, int Width, int Height)> sheet) =>
        sheet.Select(item => new TextureRegion(item.Name, item.X, item.Y, item.Width, item.Height)).ToList();
}
