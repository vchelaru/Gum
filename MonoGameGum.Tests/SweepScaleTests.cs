using Shouldly;
using Xunit;

namespace MonoGameGum.Tests;

public class SweepScaleTests
{
    [Fact]
    public void Cases_ShouldReturnTheFullCount_WhenRunningFull()
    {
        SweepScale.Cases(1500, runFull: true).ShouldBe(1500);
    }

    [Fact]
    public void Cases_ShouldReturnATenth_WhenNotRunningFull()
    {
        SweepScale.Cases(1500, runFull: false).ShouldBe(150);
    }

    [Fact]
    public void Cases_ShouldNotGoBelowTwenty_WhenNotRunningFull()
    {
        SweepScale.Cases(100, runFull: false).ShouldBe(20);
    }

    [Fact]
    public void Includes_ShouldRunEveryCase_WhenRunningFull()
    {
        int included = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (SweepScale.Includes(i, runFull: true)) included++;
        }

        included.ShouldBe(1000);
    }

    [Fact]
    public void Includes_ShouldRunAboutATenth_WhenNotRunningFull()
    {
        int included = 0;
        for (int i = 0; i < 1100; i++)
        {
            if (SweepScale.Includes(i, runFull: false)) included++;
        }

        included.ShouldBe(100);
    }

    [Fact]
    public void Includes_ShouldVisitBothValuesOfEveryBitOfAPowerOfTwoLoop_WhenNotRunningFull()
    {
        // A nested loop of two-way choices flattens to index bits; an even stride would never pick the last bit set.
        bool[] seenSet = new bool[5];
        bool[] seenClear = new bool[5];
        for (int i = 0; i < 32 * 11; i++)
        {
            if (!SweepScale.Includes(i, runFull: false)) continue;
            for (int bit = 0; bit < 5; bit++)
            {
                if ((i & (1 << bit)) != 0) seenSet[bit] = true; else seenClear[bit] = true;
            }
        }

        seenSet.ShouldAllBe(seen => seen);
        seenClear.ShouldAllBe(seen => seen);
    }
}
