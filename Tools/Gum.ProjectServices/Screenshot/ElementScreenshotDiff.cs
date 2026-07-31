namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Per-element result of a <see cref="ScreenshotDiffService"/> run.
/// </summary>
public class ElementScreenshotDiff
{
    /// <summary>Name of the Screen or Component that was rendered.</summary>
    public required string ElementName { get; init; }

    /// <summary>Whether both backends' renders matched within tolerance.</summary>
    public required bool Matches { get; init; }

    /// <summary>Set when either backend failed to render the element; <see cref="Matches"/> is false.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Path to backend A's rendered PNG, when rendering succeeded.</summary>
    public string? BackendAPath { get; init; }

    /// <summary>Path to backend B's rendered PNG, when rendering succeeded.</summary>
    public string? BackendBPath { get; init; }

    /// <summary>
    /// Count of pixels that differ beyond tolerance even after the proximity check absorbs
    /// positional jitter — see <c>Gum.ImageDiff.PixelComparer.CompareApproximate</c>.
    /// </summary>
    public int MismatchedPixelCount { get; init; }

    /// <summary>Total pixel count of the compared images.</summary>
    public int TotalPixelCount { get; init; }

    /// <summary><see cref="MismatchedPixelCount"/> as a percentage of <see cref="TotalPixelCount"/>.</summary>
    public double MismatchPercentage { get; init; }

    /// <summary>Bounding box of all real mismatches, when <see cref="Matches"/> is false.</summary>
    public int? BoundingBoxMinX { get; init; }

    /// <inheritdoc cref="BoundingBoxMinX"/>
    public int? BoundingBoxMinY { get; init; }

    /// <inheritdoc cref="BoundingBoxMinX"/>
    public int? BoundingBoxMaxX { get; init; }

    /// <inheritdoc cref="BoundingBoxMinX"/>
    public int? BoundingBoxMaxY { get; init; }

    /// <summary>Set instead of the pixel-diff fields when the two renders have different dimensions.</summary>
    public string? DimensionMismatchDescription { get; init; }
}
