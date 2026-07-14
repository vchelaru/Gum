using SkiaSharp;

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// Thin I/O wrapper around <see cref="PixelComparer"/>: loads a checked-in baseline PNG from
/// <c>GoldenImages/Baselines</c> and compares it against a rendered <see cref="SKSurface"/>.
///
/// Baselines are approved snapshots, not derived from spec: to create or update one, run
/// once so the actual render is written under <c>GoldenImages/Actual</c>, review it, then
/// copy it into <c>Tests/SkiaGum.Tests/GoldenImages/Baselines/&lt;name&gt;.png</c> in source.
/// </summary>
public static class GoldenImageAssert
{
    public static void Matches(SKSurface actualSurface, string goldenImageName, byte tolerance = 2)
    {
        using SKImage actualImage = actualSurface.Snapshot();
        using SKBitmap actualBitmap = SKBitmap.FromImage(actualImage);

        string baselinePath = Path.Combine(AppContext.BaseDirectory, "GoldenImages", "Baselines", $"{goldenImageName}.png");

        if (!File.Exists(baselinePath))
        {
            string createdActualPath = WriteActual(actualBitmap, goldenImageName);
            throw new Xunit.Sdk.XunitException(
                $"No golden baseline found at '{baselinePath}'. Rendered output was written to " +
                $"'{createdActualPath}' — review it, then copy it into " +
                $"Tests/SkiaGum.Tests/GoldenImages/Baselines/{goldenImageName}.png to approve it as the baseline.");
        }

        using SKBitmap expectedBitmap = SKBitmap.Decode(baselinePath)
            ?? throw new InvalidOperationException($"Failed to decode baseline PNG at '{baselinePath}'.");

        PixelDiffResult diff = PixelComparer.Compare(expectedBitmap, actualBitmap, tolerance);

        if (!diff.Matches)
        {
            string actualPath = WriteActual(actualBitmap, goldenImageName);
            string reason = diff.DimensionMismatchDescription is not null
                ? $"dimension mismatch: {diff.DimensionMismatchDescription}"
                : $"pixel ({diff.DiffX}, {diff.DiffY}) differs by {diff.MaxChannelDifference} (tolerance {tolerance})";

            throw new Xunit.Sdk.XunitException(
                $"Golden image '{goldenImageName}' did not match: {reason}. " +
                $"Baseline: '{baselinePath}'. Actual: '{actualPath}'.");
        }
    }

    private static string WriteActual(SKBitmap actualBitmap, string goldenImageName)
    {
        string actualDirectory = Path.Combine(AppContext.BaseDirectory, "GoldenImages", "Actual");
        Directory.CreateDirectory(actualDirectory);

        string actualPath = Path.Combine(actualDirectory, $"{goldenImageName}.actual.png");
        using SKData encoded = actualBitmap.Encode(SKEncodedImageFormat.Png, quality: 100);
        using FileStream stream = File.Create(actualPath);
        encoded.SaveTo(stream);

        return actualPath;
    }
}
