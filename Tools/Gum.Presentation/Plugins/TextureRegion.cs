namespace Gum.Plugins;

/// <summary>
/// A named rectangle, in texture pixels, that a plugin reports for a source file so the Texture
/// Coordinates tab can outline it and let the user apply it as the texture rectangle (#7354).
/// </summary>
/// <param name="Key">Identifies the region within its source file, like a layer id or a frame id.
/// Unique among the regions a plugin reports for one file; two regions may share a Name but never a Key.</param>
/// <param name="Name">Shown while the pointer is over the region. For people to read, and may repeat.</param>
/// <param name="X">Left edge in texture pixels.</param>
/// <param name="Y">Top edge in texture pixels.</param>
/// <param name="Width">Width in texture pixels. A region with no area is ignored.</param>
/// <param name="Height">Height in texture pixels. A region with no area is ignored.</param>
public readonly record struct TextureRegion(string Key, string Name, int X, int Y, int Width, int Height);
