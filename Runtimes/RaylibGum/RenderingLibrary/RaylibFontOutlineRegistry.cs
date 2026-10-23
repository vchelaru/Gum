using System.Collections.Generic;
using System.Numerics;

namespace RenderingLibrary;

/// <summary>
/// Records the outline thickness of a font the native TTF fallback loaded (no font creator), keyed by the atlas
/// texture id of the loaded raylib font (#6639). A creator bakes an outline into the atlas; a plain TTF atlas has
/// none, so <c>Text</c> draws the glyphs again at offsets in black behind the fill, which is what Skia's halo shows.
/// </summary>
/// <remarks>
/// Same shape as <see cref="RaylibFontShadowRegistry"/>: <see cref="Raylib_cs.Font"/> is a value struct with no
/// room for the thickness. Entries are removed when the owning <see cref="ManagedFont"/> is disposed, since raylib
/// may reuse the freed texture id. Accessed only on the single render/content-load thread.
/// </remarks>
internal static class RaylibFontOutlineRegistry
{
    private static readonly Dictionary<uint, int> _thicknessByTextureId = new();

    /// <summary>
    /// Records the thickness for the font with the given atlas texture id; a thickness of 0 clears the entry, so a
    /// reused texture id never keeps the outline of the font that had it before.
    /// </summary>
    public static void Register(uint textureId, int thickness)
    {
        if (thickness > 0)
        {
            _thicknessByTextureId[textureId] = thickness;
        }
        else
        {
            _thicknessByTextureId.Remove(textureId);
        }
    }

    public static bool TryGet(uint textureId, out int thickness)
    {
        return _thicknessByTextureId.TryGetValue(textureId, out thickness);
    }

    public static void Remove(uint textureId)
    {
        _thicknessByTextureId.Remove(textureId);
    }

    private static readonly Dictionary<int, Vector2[]> _offsetsByRadius = new();

    /// <summary>
    /// The pixel offsets, within <paramref name="radius"/> of the origin and not the origin itself, at which the
    /// glyphs are drawn to form a round outline of that radius in pixels. The array is shared.
    /// </summary>
    public static Vector2[] GetOffsets(int radius)
    {
        if (!_offsetsByRadius.TryGetValue(radius, out Vector2[]? offsets))
        {
            List<Vector2> list = new();
            // The +1 admits the diagonal pixels a radius-1 outline needs; the square grid keeps the passes 1 pixel apart.
            int limit = radius * radius + 1;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if ((x != 0 || y != 0) && x * x + y * y <= limit)
                    {
                        list.Add(new Vector2(x, y));
                    }
                }
            }
            offsets = list.ToArray();
            _offsetsByRadius[radius] = offsets;
        }
        return offsets;
    }
}
