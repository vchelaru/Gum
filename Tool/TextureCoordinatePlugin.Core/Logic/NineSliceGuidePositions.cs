namespace TextureCoordinateSelectionPlugin.Logic;

/// <summary>Where the four nine-slice cut guides sit, in texture coordinates.</summary>
public record struct NineSliceGuideLines(float Left, float Right, float Top, float Bottom);

/// <summary>Computes where a nine-slice's cut guides sit inside the selected texture region.</summary>
public interface INineSliceGuidePositions
{
    /// <summary>
    /// Each side's guide sits at that side's own cut when set, else at <paramref name="frameWidth"/>, else a
    /// third of the region along that side's axis.
    /// </summary>
    NineSliceGuideLines Compute(float regionLeft, float regionTop, float regionRight, float regionBottom,
        float? frameWidth, float? left, float? top, float? right, float? bottom);
}

/// <inheritdoc cref="INineSliceGuidePositions"/>
public class NineSliceGuidePositions : INineSliceGuidePositions
{
    /// <inheritdoc/>
    public NineSliceGuideLines Compute(float regionLeft, float regionTop, float regionRight, float regionBottom,
        float? frameWidth, float? left, float? top, float? right, float? bottom)
    {
        float thirdWidth = (regionRight - regionLeft) / 3.0f;
        float thirdHeight = (regionBottom - regionTop) / 3.0f;

        return new NineSliceGuideLines(
            Left: regionLeft + (left ?? frameWidth ?? thirdWidth),
            Right: regionRight - (right ?? frameWidth ?? thirdWidth),
            Top: regionTop + (top ?? frameWidth ?? thirdHeight),
            Bottom: regionBottom - (bottom ?? frameWidth ?? thirdHeight));
    }
}
