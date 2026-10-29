using CrossRuntimeTests;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.CrossRuntime;

/// <summary>
/// The MonoGame baselines were rendered on macOS; the Windows CI renders with Mesa llvmpipe. The cells
/// below are the ones where the two rasterizers disagreed in the first full CI run of the sheet test
/// (#7038), and the MonoGame sheet test must not compare them exactly.
/// </summary>
public class NineSliceKnownDifferencesTests
{
    public static IEnumerable<object[]> CellsThatDifferedOnMesa => new[]
    {
        "fractional/3x3-f1-3.5x3.5-z1.5",
        "borderscale0.5-6x6/3x1", "borderscale0.5-6x6/3x2", "borderscale0.5-6x6/1x3", "borderscale0.5-6x6/2x3",
        "borderscale0.5-6x6/3x3", "borderscale0.5-6x6/4x3", "borderscale0.5-6x6/6x3", "borderscale0.5-6x6/3x4",
        "borderscale0.5-6x6/3x6",
        "frame3-9x9/3x5", "frame3-9x9/3x6", "frame3-9x9/3x7", "frame3-9x9/3x12",
        "frame1-3x16/5x5",
        "zoom/3x16-f1-5x5-z1", "zoom/3x16-f1-3x1-z1.5", "zoom/3x16-f1-3x2-z1.5", "zoom/3x16-f1-3x3-z1.5",
        "zoom/3x16-f1-5x5-z1.5", "zoom/6x6-f2-4x4-z1.5", "zoom/6x6-f2-5x5-z1.5", "zoom/3x16-f1-5x5-z3",
        "zoom/6x6-f2-3x3-z3", "zoom/6x6-f2-5x5-z3",
    }.Select(key => new object[] { key });

    [Theory]
    [MemberData(nameof(CellsThatDifferedOnMesa))]
    public void CellThatDifferedOnMesa_IsMarkedRasterizerDependent(string key)
    {
        string[] parts = key.Split('/');

        NineSliceKnownDifferences.IsRasterizerDependent(parts[0], parts[1]).ShouldBeTrue();
    }

    [Fact]
    public void MonoGameOddHeightCells_AreComparedExactly()
    {
        // MonoGame's own rule, no rasterizer involved: these four must keep failing the MonoGame
        // sheet test if the rule changes.
        string[] cells = { "3x16-f1-3x1-z2", "3x16-f1-2x1-z2", "3x16-f1-3x1-z3", "3x16-f1-2x1-z3" };

        cells.ShouldAllBe(cell => !NineSliceKnownDifferences.IsRasterizerDependent("zoom", cell));
    }

    [Fact]
    public void CellThatIsNotKnownToDiffer_IsNotRasterizerDependent()
    {
        NineSliceKnownDifferences.IsRasterizerDependent("default-3x3", "1x1").ShouldBeFalse();
    }
}
