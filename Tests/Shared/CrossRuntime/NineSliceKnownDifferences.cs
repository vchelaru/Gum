namespace CrossRuntimeTests;

/// <summary>Why one cell differs between MonoGame and Skia, and whether MonoGame's own picture of it depends on the GPU.</summary>
/// <param name="Reason">What is known about the difference.</param>
/// <param name="RasterizerDependent">
/// True when MonoGame's picture of this cell changes with the rasterizer (the macOS GPU the baselines
/// come from versus Mesa llvmpipe, the software OpenGL the Windows CI uses), so the MonoGame sheet test
/// cannot compare it exactly.
/// </param>
public sealed record KnownDifference(string Reason, bool RasterizerDependent);

/// <summary>
/// The cells where Skia is known to differ from the MonoGame baselines and the right answer is
/// undecided (or MonoGame is the one that looks wrong), keyed "sheet/case". SkiaGum.Tests requires this
/// list to be exactly the cells that differ, so a fix must remove its entry; MonoGameGum.IntegrationTests
/// skips the rasterizer-dependent ones when it compares MonoGame with its own baselines.
/// </summary>
public static class NineSliceKnownDifferences
{
    // A band stretched or shrunk so that a destination pixel center lands exactly on a texel
    // boundary: the rasterizer decides which side wins. The macOS GPU, Mesa llvmpipe (Windows CI) and
    // Skia's CPU raster do not all agree. Read from the pictures and from the CI run; not isolated to
    // a setting of any of them.
    private const string SamplingTie = "Nearest sampling tie on a texel boundary";

    // MonoGame's odd-height rule (NineSlice.cs: Height <= 2 * frame && Height % 2 == 1 makes the bottom
    // band one texel row shorter) counts world units, not pixels, and fires for any odd Height up to
    // twice the frame, not only for 1. At zoom 2 and 3 a 1 high nine-slice therefore draws half its
    // height. Checked by disabling the rule: MonoGame then matches Skia in exactly these cells.
    private const string MonoGameOddHeight = "MonoGame's odd-height rule drops a texel row (world units, not pixels)";

    // The odd-height rule above also changes MonoGame's picture in these cells, but each one differs
    // from Skia with the rule disabled as well, so the rule is involved and not the whole cause. The
    // rest is the same texel-boundary tie as SamplingTie; not isolated further.
    private const string SamplingTieWithOddHeight = "Nearest sampling tie on a texel boundary, and the odd-height rule changes MonoGame here";

    // At zoom 1.5 the nine-slice is 4.5 pixels; MonoGame draws 4 and Skia 5 pixels with different
    // edge texels. Which is right is undecided. (6x6-f2-3x3-z1.5 is also changed by the odd-height rule
    // but differs from Skia without it, like the other cells here.)
    private const string FractionalEdges = "Fractional pixel edges at zoom 1.5";

    public static IReadOnlyDictionary<string, KnownDifference> All { get; } = Build();

    public static bool IsRasterizerDependent(string sheet, string caseName) =>
        All.TryGetValue($"{sheet}/{caseName}", out KnownDifference? known) && known.RasterizerDependent;

    private static Dictionary<string, KnownDifference> Build()
    {
        Dictionary<string, KnownDifference> known = new();
        void Add(string sheet, string reason, bool rasterizerDependent, params string[] cases)
        {
            foreach (string name in cases)
            {
                known[$"{sheet}/{name}"] = new KnownDifference(reason, rasterizerDependent);
            }
        }

        Add("frame1-3x16", SamplingTie, true, "1x3", "2x3", "3x3", "5x3", "1x5", "2x5", "3x5", "5x5");
        Add("frame2-6x6", SamplingTie, true, "5x1", "5x2", "5x3", "5x4", "1x5", "2x5", "3x5", "4x5", "5x5", "6x5", "9x5", "5x6", "5x9");
        Add("frame3-9x9", SamplingTie, true, "5x1", "3x6", "5x6", "3x7", "5x7", "3x12", "5x12");
        Add("frame3-9x9", SamplingTieWithOddHeight, true, "5x3", "3x5", "5x5");
        Add("borderscale0.5-6x6", SamplingTie, true,
            "2x1", "3x1", "4x1", "6x1", "1x2", "2x2", "3x2", "4x2", "6x2", "1x3", "2x3", "3x3", "4x3", "6x3",
            "1x4", "2x4", "3x4", "4x4", "6x4", "1x6", "2x6", "3x6", "4x6", "6x6");
        Add("zoom", SamplingTie, true, "3x16-f1-3x3-z1", "3x16-f1-5x5-z1", "6x6-f2-5x5-z1",
            "3x16-f1-5x5-z3", "6x6-f2-5x5-z3");
        Add("zoom", SamplingTieWithOddHeight, true, "6x6-f2-3x3-z2", "6x6-f2-3x3-z3");
        Add("zoom", MonoGameOddHeight, false, "3x16-f1-3x1-z2", "3x16-f1-2x1-z2", "3x16-f1-3x1-z3", "3x16-f1-2x1-z3");
        Add("zoom", FractionalEdges, true, "3x16-f1-3x1-z1.5", "3x16-f1-3x2-z1.5", "3x16-f1-3x3-z1.5", "3x16-f1-5x5-z1.5",
            "3x16-f1-2x1-z1.5", "6x6-f2-3x3-z1.5", "6x6-f2-4x4-z1.5", "6x6-f2-5x5-z1.5");
        Add("fractional", FractionalEdges, true, "3x3-f1-2.5x2.5-z1.5", "3x3-f1-3.5x3.5-z1.5");
        return known;
    }
}
