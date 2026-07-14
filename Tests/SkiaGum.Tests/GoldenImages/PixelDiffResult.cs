namespace SkiaGum.Tests.GoldenImages;

public readonly struct PixelDiffResult
{
    public bool Matches { get; }
    public int? DiffX { get; }
    public int? DiffY { get; }
    public int MaxChannelDifference { get; }
    public string? DimensionMismatchDescription { get; }

    private PixelDiffResult(bool matches, int? diffX, int? diffY, int maxChannelDifference, string? dimensionMismatchDescription)
    {
        Matches = matches;
        DiffX = diffX;
        DiffY = diffY;
        MaxChannelDifference = maxChannelDifference;
        DimensionMismatchDescription = dimensionMismatchDescription;
    }

    public static PixelDiffResult Match() =>
        new(matches: true, diffX: null, diffY: null, maxChannelDifference: 0, dimensionMismatchDescription: null);

    public static PixelDiffResult Mismatch(int x, int y, int maxChannelDifference) =>
        new(matches: false, diffX: x, diffY: y, maxChannelDifference: maxChannelDifference, dimensionMismatchDescription: null);

    public static PixelDiffResult DimensionMismatch(int expectedWidth, int expectedHeight, int actualWidth, int actualHeight) =>
        new(matches: false, diffX: null, diffY: null, maxChannelDifference: 0,
            dimensionMismatchDescription: $"expected {expectedWidth}x{expectedHeight}, actual {actualWidth}x{actualHeight}");
}
