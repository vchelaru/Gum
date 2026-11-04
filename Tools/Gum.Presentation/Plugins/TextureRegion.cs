namespace Gum.Plugins;

/// <summary>
/// A named rectangle, in texture pixels, that a plugin reports for a source file so the Texture
/// Coordinates tab can outline it and let the user apply it as the texture rectangle (#7354).
/// </summary>
/// <param name="Name">Shown while the pointer is over the region.</param>
/// <param name="X">Left edge in texture pixels.</param>
/// <param name="Y">Top edge in texture pixels.</param>
/// <param name="Width">Width in texture pixels. A region with no area is ignored.</param>
/// <param name="Height">Height in texture pixels. A region with no area is ignored.</param>
public readonly record struct TextureRegion(string Name, int X, int Y, int Width, int Height);
