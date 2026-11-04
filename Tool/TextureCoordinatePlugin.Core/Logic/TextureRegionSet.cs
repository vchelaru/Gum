using System.Collections.Generic;
using Gum.Plugins;

namespace TextureCoordinateSelectionPlugin.Logic;

/// <summary>
/// The regions plugins reported for the shown texture, cleaned up for drawing and hit-testing.
/// A region with no area is dropped, one that sticks out of the texture is clamped to it, and one
/// entirely outside the texture is dropped, so every region it holds can be applied as a texture
/// rectangle as-is.
/// </summary>
public sealed class TextureRegionSet
{
    public static TextureRegionSet Empty { get; } = new TextureRegionSet(new List<TextureRegion>(), 0, 0);

    private readonly List<TextureRegion> _regions;

    public TextureRegionSet(IEnumerable<TextureRegion> reported, int textureWidth, int textureHeight)
    {
        _regions = new List<TextureRegion>();
        foreach (TextureRegion region in reported)
        {
            int left = System.Math.Max(0, region.X);
            int top = System.Math.Max(0, region.Y);
            int right = System.Math.Min(textureWidth, region.X + region.Width);
            int bottom = System.Math.Min(textureHeight, region.Y + region.Height);
            if (region.Width <= 0 || region.Height <= 0 || right <= left || bottom <= top)
            {
                continue;
            }

            _regions.Add(new TextureRegion(region.Key, region.Name, left, top, right - left, bottom - top));
        }
    }

    /// <summary>The usable regions, in the order the plugins reported them.</summary>
    public IReadOnlyList<TextureRegion> Regions => _regions;

    public bool IsEmpty => _regions.Count == 0;

    /// <summary>
    /// The region containing the texture point (<paramref name="x"/>, <paramref name="y"/>), or null
    /// when it is outside every region. A region owns its left and top edges but not its right and
    /// bottom ones, so neighbors that share an edge never both match. Where regions overlap the
    /// smallest area wins, so a small sprite on top of a large panel stays reachable; equal areas
    /// go to the one reported first.
    /// </summary>
    public TextureRegion? HitTest(float x, float y)
    {
        TextureRegion? best = null;
        long bestArea = long.MaxValue;
        foreach (TextureRegion region in _regions)
        {
            if (x < region.X || y < region.Y || x >= region.X + region.Width || y >= region.Y + region.Height)
            {
                continue;
            }

            long area = (long)region.Width * region.Height;
            if (area < bestArea)
            {
                best = region;
                bestArea = area;
            }
        }
        return best;
    }
}
