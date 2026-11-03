# GetTextureRegionsForFile

## Introduction

The GetTextureRegionsForFile event lets a plugin tell the Texture Coordinates tab about named rectangles inside a Source File, such as the sprites of a sprite sheet or the slices of a PSD. The tab outlines every region reported for the selected object's Source File and shows a region's name while the cursor is over it. Double-clicking inside a region sets the object's Texture Address to Custom and its Texture Left, Top, Width and Height to that region, as one undo step. A double-click outside every region still creates the default rectangle.

The event receives the Source File as a FilePath and returns a list of TextureRegion, or null (or an empty list) if the plugin does not know the file. Each TextureRegion has a Name and an X, Y, Width and Height in texture pixels.

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
        new TextureRegion("Hero", 0, 0, 48, 64),
        new TextureRegion("Coin", 48, 0, 24, 24),
    };
};
```

## Details

* If more than one plugin reports regions for the same file, the lists are combined.
* The tab asks once per Source File and remembers the answer until the project is loaded again or the file changes on disk, so the event should be cheap.
* A region with no width or height is ignored. A region that extends past the texture is cut to the texture, and one entirely outside it is ignored.
* Where regions overlap, the smallest region under the cursor is used.
* A plugin that throws is disabled for the session, like any other plugin event, and the tab keeps working.
