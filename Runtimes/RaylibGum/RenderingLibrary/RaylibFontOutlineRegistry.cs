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

    public static void Register(uint textureId, int thickness)
    {
        _thicknessByTextureId[textureId] = thickness;
    }

    public static bool TryGet(uint textureId, out int thickness)
    {
        return _thicknessByTextureId.TryGetValue(textureId, out thickness) && thickness > 0;
    }

    public static void Remove(uint textureId)
    {
        _thicknessByTextureId.Remove(textureId);
    }

    private static readonly Dictionary<int, Vector2[]> _offsetsByRadius = new();

    /// <summary>
    /// The pixel offsets, within <paramref name="radius"/> of the origin and not the origin itself, at which the
    /// glyphs are drawn to form a round outline of that radius. The array is shared; the radius is rounded to a
    /// whole pixel so a scale animation does not grow the cache.
    /// </summary>
    public static Vector2[] GetOffsets(float radius)
    {
        int wholeRadius = System.Math.Max(1, (int)System.MathF.Round(radius));
        if (!_offsetsByRadius.TryGetValue(wholeRadius, out Vector2[]? offsets))
        {
            List<Vector2> list = new();
            // The +1 admits the diagonal pixels a radius-1 outline needs; the square grid keeps the passes 1 pixel apart.
            int limit = wholeRadius * wholeRadius + 1;
            for (int y = -wholeRadius; y <= wholeRadius; y++)
            {
                for (int x = -wholeRadius; x <= wholeRadius; x++)
                {
                    if ((x != 0 || y != 0) && x * x + y * y <= limit)
                    {
                        list.Add(new Vector2(x, y));
                    }
                }
            }
            offsets = list.ToArray();
            _offsetsByRadius[wholeRadius] = offsets;
        }
        return offsets;
    }
}
