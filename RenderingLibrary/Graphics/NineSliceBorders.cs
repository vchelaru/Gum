using RenderingLibrary.Math;

namespace RenderingLibrary.Graphics;

/// <summary>
/// How thick the four border bands of a nine-slice draw, on screen (destination) and in the
/// texture (source), once each side's border scale and the nine-slice's size are applied. One
/// source for every runtime so they all agree, including the 0-scale case where a side collapses.
/// </summary>
public readonly struct NineSliceBorders
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
    /// Computes the bands. <paramref name="cut"/> is the texel thickness of each side of the frame;
    /// each scale multiplies its side's thickness (a negative scale counts as 0). When a pair of
    /// opposing bands would overlap, both shrink by the same factor and sample fewer texels, so
    /// their ratio is kept. A side with scale 0 draws nothing and the rest of the nine-slice fills
    /// its space.
    /// </summary>
    public static NineSliceBorders Compute(
        NineSliceSourceCut cut,
        float scaleLeft, float scaleTop, float scaleRight, float scaleBottom,
        float width, float height)
    {
        ComputeAxis(cut.Left, cut.Right, scaleLeft, scaleRight, width,
            out float destLeft, out float destRight, out int sourceLeft, out int sourceRight);
        ComputeAxis(cut.Top, cut.Bottom, scaleTop, scaleBottom, height,
            out float destTop, out float destBottom, out int sourceTop, out int sourceBottom);

        return new NineSliceBorders(destLeft, destTop, destRight, destBottom,
            sourceLeft, sourceTop, sourceRight, sourceBottom);
    }

    private static void ComputeAxis(int fullNear, int fullFar, float scaleNear, float scaleFar, float length,
        out float destNear, out float destFar, out int sourceNear, out int sourceFar)
    {
        float near = scaleNear > 0 ? scaleNear : 0;
        float far = scaleFar > 0 ? scaleFar : 0;
        sourceNear = fullNear;
        sourceFar = fullFar;

        // What each band draws when the pair fits.
        float wantedNear = fullNear * near;
        float wantedFar = fullFar * far;
        float wanted = wantedNear + wantedFar;

        if (wanted <= 0)
        {
            destNear = 0;
            destFar = 0;
            return;
        }

        if (length < 0)
        {
            length = 0;
        }

        if (wanted <= length)
        {
            destNear = wantedNear;
            destFar = wantedFar;
            return;
        }

        destNear = length * wantedNear / wanted;
        destFar = length * wantedFar / wanted;
        if (near > 0)
        {
            sourceNear = System.Math.Min(fullNear, MathFunctions.RoundToInt(length * fullNear / wanted));
        }
        if (far > 0)
        {
            sourceFar = System.Math.Min(fullFar, MathFunctions.RoundToInt(length * fullFar / wanted));
        }
    }
}
