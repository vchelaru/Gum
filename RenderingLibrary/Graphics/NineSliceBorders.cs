using RenderingLibrary.Math;

namespace RenderingLibrary.Graphics;

/// <summary>
/// How thick the four border bands of a nine-slice draw, on screen (destination) and in the
/// texture (source), once each side's border scale and the nine-slice's size are applied. One
/// source for every runtime so they all agree, including the 0-scale case where a side collapses.
/// </summary>
internal readonly struct NineSliceBorders
{
    /// <summary>Destination thickness of the left band, in screen units.</summary>
    public float DestLeft { get; }
    /// <summary>Destination thickness of the top band, in screen units.</summary>
    public float DestTop { get; }
    /// <summary>Destination thickness of the right band, in screen units.</summary>
    public float DestRight { get; }
    /// <summary>Destination thickness of the bottom band, in screen units.</summary>
    public float DestBottom { get; }
    /// <summary>Texels the left band samples.</summary>
    public int SourceLeft { get; }
    /// <summary>Texels the top band samples.</summary>
    public int SourceTop { get; }
    /// <summary>Texels the right band samples.</summary>
    public int SourceRight { get; }
    /// <summary>Texels the bottom band samples.</summary>
    public int SourceBottom { get; }

    private NineSliceBorders(
        float destLeft, float destTop, float destRight, float destBottom,
        int sourceLeft, int sourceTop, int sourceRight, int sourceBottom)
    {
        DestLeft = destLeft;
        DestTop = destTop;
        DestRight = destRight;
        DestBottom = destBottom;
        SourceLeft = sourceLeft;
        SourceTop = sourceTop;
        SourceRight = sourceRight;
        SourceBottom = sourceBottom;
    }

    /// <summary>
    /// Computes the bands. <paramref name="fullOutsideWidth"/> and <paramref name="fullOutsideHeight"/>
    /// are the texel thickness of the frame; each scale multiplies that thickness for its side (a
    /// negative scale counts as 0). When a pair of opposing bands would overlap, both shrink in
    /// proportion to their scales and sample fewer texels, so their ratio is kept. A side with
    /// scale 0 draws nothing and the rest of the nine-slice fills its space.
    /// </summary>
    public static NineSliceBorders Compute(
        int fullOutsideWidth, int fullOutsideHeight,
        float scaleLeft, float scaleTop, float scaleRight, float scaleBottom,
        float width, float height)
    {
        ComputeAxis(fullOutsideWidth, scaleLeft, scaleRight, width,
            out float destLeft, out float destRight, out int sourceLeft, out int sourceRight);
        ComputeAxis(fullOutsideHeight, scaleTop, scaleBottom, height,
            out float destTop, out float destBottom, out int sourceTop, out int sourceBottom);

        return new NineSliceBorders(destLeft, destTop, destRight, destBottom,
            sourceLeft, sourceTop, sourceRight, sourceBottom);
    }

    private static void ComputeAxis(int full, float scaleNear, float scaleFar, float length,
        out float destNear, out float destFar, out int sourceNear, out int sourceFar)
    {
        float near = scaleNear > 0 ? scaleNear : 0;
        float far = scaleFar > 0 ? scaleFar : 0;
        float sum = near + far;

        if (sum <= 0 || full <= 0)
        {
            destNear = 0;
            destFar = 0;
            sourceNear = full;
            sourceFar = full;
            return;
        }

        if (length < 0)
        {
            length = 0;
        }

        if (full * sum > length)
        {
            destNear = length * near / sum;
            destFar = length * far / sum;
        }
        else
        {
            destNear = full * near;
            destFar = full * far;
        }

        int shared = System.Math.Min(full, MathFunctions.RoundToInt(length / sum));
        sourceNear = near > 0 ? shared : full;
        sourceFar = far > 0 ? shared : full;
    }
}
