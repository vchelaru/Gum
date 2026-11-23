#nullable enable
using RenderingLibrary.Math;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Where a nine-slice cuts its source texture: how many texels each border band takes from the
/// left, top, right and bottom edges of the used source region. One source for every runtime.
/// </summary>
public readonly struct NineSliceSourceCut : System.IEquatable<NineSliceSourceCut>
{
    /// <summary>Texels the left band takes from the source region.</summary>
    public int Left { get; }
    /// <summary>Texels the top band takes from the source region.</summary>
    public int Top { get; }
    /// <summary>Texels the right band takes from the source region.</summary>
    public int Right { get; }
    /// <summary>Texels the bottom band takes from the source region.</summary>
    public int Bottom { get; }

    public NineSliceSourceCut(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    /// Resolves each side: its own value when set, else <paramref name="frameWidth"/>, else a third of
    /// the used region along that side's axis. Values are rounded to whole texels; negative ones count as 0.
    /// </summary>
    public static NineSliceSourceCut Compute(int usedWidth, int usedHeight, float? frameWidth,
        float? left, float? top, float? right, float? bottom)
    {
        int defaultHorizontal = (usedWidth + 1) / 3;
        int defaultVertical = (usedHeight + 1) / 3;

        return new NineSliceSourceCut(
            Resolve(left ?? frameWidth, defaultHorizontal),
            Resolve(top ?? frameWidth, defaultVertical),
            Resolve(right ?? frameWidth, defaultHorizontal),
            Resolve(bottom ?? frameWidth, defaultVertical));
    }

    public bool Equals(NineSliceSourceCut other) =>
        Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;

    public override bool Equals(object? obj) => obj is NineSliceSourceCut other && Equals(other);

    public override int GetHashCode() => ((Left * 397 ^ Top) * 397 ^ Right) * 397 ^ Bottom;

    private static int Resolve(float? value, int fallback)
    {
        if (value == null)
        {
            return fallback;
        }

        return System.Math.Max(0, MathFunctions.RoundToInt(value.Value));
    }
}
