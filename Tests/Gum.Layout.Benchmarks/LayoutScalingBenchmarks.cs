using Shouldly;
using Xunit.Abstractions;

namespace Gum.Layout.Benchmarks;

/// <summary>
/// Layout cost versus tree size for the shapes most likely to expose superlinear behavior. Each test prints a
/// table (and writes it next to the test binary as <c>layout-benchmark-*.txt</c>). These measure; the only
/// assertions are that the scenario actually laid something out.
/// </summary>
public class LayoutScalingBenchmarks : LayoutBenchmarkTestBase
{
    private static readonly int[] WideSizes = { 100, 200, 400, 800, 1600, 3200 };
    private static readonly int[] DeepSizes = { 2, 4, 8, 12, 16, 20, 24, 32 };

    private readonly LayoutBenchmarkRunner _runner;

    public LayoutScalingBenchmarks(ITestOutputHelper output)
    {
        _runner = new LayoutBenchmarkRunner(output);
    }

    [Fact]
    public void FlatRegular()
    {
        AssertLaidOut(_runner.RunSeries(nameof(FlatRegular), WideSizes, LayoutTreeShapes.FlatRegular));
    }

    [Fact]
    public void VerticalStack()
    {
        AssertLaidOut(_runner.RunSeries(nameof(VerticalStack), WideSizes, LayoutTreeShapes.VerticalStack));
    }

    [Fact]
    public void StackOfContentSizedRows()
    {
        AssertLaidOut(_runner.RunSeries(nameof(StackOfContentSizedRows), WideSizes, LayoutTreeShapes.StackOfContentSizedRows));
    }

    [Fact]
    public void RatioRow()
    {
        AssertLaidOut(_runner.RunSeries(nameof(RatioRow), WideSizes, LayoutTreeShapes.RatioRow));
    }

    [Fact]
    public void RatioNestedChain()
    {
        AssertLaidOut(_runner.RunSeries(nameof(RatioNestedChain), DeepSizes, LayoutTreeShapes.RatioNestedChain));
    }

    [Fact]
    public void DeepContentSized()
    {
        AssertLaidOut(_runner.RunSeries(nameof(DeepContentSized), DeepSizes, LayoutTreeShapes.DeepContentSized));
    }

    [Fact]
    public void DeepPercentOfParent()
    {
        AssertLaidOut(_runner.RunSeries(nameof(DeepPercentOfParent), DeepSizes, LayoutTreeShapes.DeepPercentOfParent));
    }

    [Fact]
    public void ZigZag()
    {
        AssertLaidOut(_runner.RunSeries(nameof(ZigZag), DeepSizes, LayoutTreeShapes.ZigZag));
    }

    private static void AssertLaidOut(List<BenchmarkSample> samples)
    {
        samples.ShouldNotBeEmpty();
        samples.Where(s => !s.Abandoned).ShouldAllBe(s => s.CallsPerRun > 0);
    }
}
