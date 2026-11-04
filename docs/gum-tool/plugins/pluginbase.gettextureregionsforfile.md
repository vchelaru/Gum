# GetTextureRegionsForFile

## Introduction

The GetTextureRegionsForFile event lets a plugin tell the Texture Coordinates tab about named rectangles inside a Source File, such as the sprites of a sprite sheet or the slices of a PSD. The tab outlines every region reported for the selected object's Source File and shows a region's name while the cursor is over it. Clicking inside a region sets the object's Texture Address to Custom and its Texture Left, Top, Width and Height to that region, as one undo step, without moving the camera. Dragging does not apply a region. A double-click outside every region still creates the default rectangle.

The event receives the Source File as a FilePath and returns a list of TextureRegion, or null (or an empty list) if the plugin does not know the file. Each TextureRegion has a Key, a Name, and an X, Y, Width and Height in texture pixels. The Key identifies the region within its file (a layer id, a frame id), so it must be unique among the regions a plugin reports for that file. The Name is what the tab shows under the cursor, and several regions may share one.

Available in December 2026, or now if building Gum from source.

## Code Example

The following reports two regions for a file called Sheet.png. Add the following to your plugin's **StartUp** function.

```csharp
GetTextureRegionsForFile += file =>
{
    if (file.FileNameNoPath != "Sheet.png")
    {
        return null;
    }

    return new[]
    {
        new TextureRegion("hero", "Hero", 0, 0, 48, 64),
        new TextureRegion("coin", "Coin", 48, 0, 24, 24),
    };
};
```

## Complete Plugin Example

The following plugin reads regions from a JSON file that sits beside the image, so `Sheet.png` takes its regions from `Sheet.png.regions.json`. It is a full plugin class, so see [Plugins](README.md) for the project setup.

```csharp
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using System.Text.Json;
using ToolsUtilities;

[Export(typeof(PluginBase))]
public class RegionsFromJsonPlugin : PluginBase
{
    public override string FriendlyName => "Regions From JSON";

    public override Version Version => new Version(1, 0);

    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    public override void StartUp()
    {
        GetTextureRegionsForFile += HandleGetTextureRegionsForFile;
    }

    private IReadOnlyList<TextureRegion>? HandleGetTextureRegionsForFile(FilePath file)
    {
        string regionsFile = file.FullPath + ".regions.json";
        if (!File.Exists(regionsFile))
        {
            return null;
        }

        List<TextureRegion> regions = new List<TextureRegion>();
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(regionsFile));
        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            regions.Add(new TextureRegion(
                item.GetProperty("id").GetString() ?? "",
                item.GetProperty("name").GetString() ?? "",
                item.GetProperty("x").GetInt32(),
                item.GetProperty("y").GetInt32(),
                item.GetProperty("width").GetInt32(),
                item.GetProperty("height").GetInt32()));
        }
        return regions;
    }
}
```

The JSON file is an array with one entry per region:

```json
[
  { "id": "hero", "name": "Hero", "x": 0, "y": 0, "width": 48, "height": 64 },
  { "id": "coin", "name": "Coin", "x": 48, "y": 0, "width": 24, "height": 24 }
]
```

## Details

* If more than one plugin reports regions for the same file, the lists are combined.
* The tab asks once per Source File and remembers the answer until the project is loaded again or the file changes on disk, so the event should be cheap.
* A region with no width or height is ignored. A region that extends past the texture is cut to the texture, and one entirely outside it is ignored.
* Where regions overlap, the smallest region under the cursor is used.
* A plugin that throws is disabled for the session, like any other plugin event, and the tab keeps working.
