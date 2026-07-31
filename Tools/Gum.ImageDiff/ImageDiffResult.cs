namespace Gum.ImageDiff;

/// <summary>
/// Result of <see cref="PixelComparer.CompareApproximate"/> — an aggregate, proximity-tolerant
/// comparison of two images, as opposed to <see cref="PixelDiffResult"/>'s single first-differing
/// pixel from an exact same-coordinate comparison.
/// </summary>
public readonly struct ImageDiffResult
{
    public bool Matches { get; }
    public int MismatchedPixelCount { get; }
    public int TotalPixelCount { get; }
    public double MismatchPercentage => TotalPixelCount == 0 ? 0 : (double)MismatchedPixelCount / TotalPixelCount * 100;
    public int? BoundingBoxMinX { get; }
    public int? BoundingBoxMinY { get; }
    public int? BoundingBoxMaxX { get; }
    public int? BoundingBoxMaxY { get; }
    public string? DimensionMismatchDescription { get; }

    private ImageDiffResult(
        bool matches,
        int mismatchedPixelCount,
        int totalPixelCount,
        int? boundingBoxMinX,
        int? boundingBoxMinY,
        int? boundingBoxMaxX,
        int? boundingBoxMaxY,
        string? dimensionMismatchDescription)
    {
        Matches = matches;
        MismatchedPixelCount = mismatchedPixelCount;
        TotalPixelCount = totalPixelCount;
        BoundingBoxMinX = boundingBoxMinX;
        BoundingBoxMinY = boundingBoxMinY;
        BoundingBoxMaxX = boundingBoxMaxX;
        BoundingBoxMaxY = boundingBoxMaxY;
        DimensionMismatchDescription = dimensionMismatchDescription;
    }

    public static ImageDiffResult Match(int totalPixelCount) =>
        new(matches: true, mismatchedPixelCount: 0, totalPixelCount: totalPixelCount,
            boundingBoxMinX: null, boundingBoxMinY: null, boundingBoxMaxX: null, boundingBoxMaxY: null,
            dimensionMismatchDescription: null);

    public static ImageDiffResult Mismatch(int mismatchedPixelCount, int totalPixelCount, int minX, int minY, int maxX, int maxY) =>
        new(matches: false, mismatchedPixelCount: mismatchedPixelCount, totalPixelCount: totalPixelCount,
            boundingBoxMinX: minX, boundingBoxMinY: minY, boundingBoxMaxX: maxX, boundingBoxMaxY: maxY,
            dimensionMismatchDescription: null);

    public static ImageDiffResult DimensionMismatch(int expectedWidth, int expectedHeight, int actualWidth, int actualHeight) =>
        new(matches: false, mismatchedPixelCount: 0, totalPixelCount: 0,
            boundingBoxMinX: null, boundingBoxMinY: null, boundingBoxMaxX: null, boundingBoxMaxY: null,
            dimensionMismatchDescription: $"expected {expectedWidth}x{expectedHeight}, actual {actualWidth}x{actualHeight}");
}
