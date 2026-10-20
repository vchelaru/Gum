using System.Collections.Generic;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using static MonoGameGum.Tests.RenderingLibraries.BatchOrdererScenes;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Pins how many bounds-intersection tests <see cref="BatchKeyGroupedOrderer"/> performs while
/// building its precedence graph (#6179). Counts, not timings, so the result is deterministic.
/// </summary>
public class BatchKeyGroupedOrdererPairCountTests : BaseTestClass
{
    private static int CountPairTests(List<IRenderableIpso> roots, out int nodeCount)
    {
        nodeCount = CountNodes(roots);
        BatchKeyGroupedOrderer orderer = new BatchKeyGroupedOrderer();
        orderer.BuildDrawList(BuildLayer(roots), new List<DrawCommand>());
        return orderer.PairTestCount;
    }

    [Fact]
    public void BuildDrawList_165NodeVerticalList_TestsFarFewerThanAllPairs()
    {
        int pairTests = CountPairTests(BuildVerticalList(33, new object(), new object()), out int nodes);

        nodes.ShouldBe(165);
        // All pairs would be 165 * 164 / 2 = 13530. Each row only overlaps its own five nodes.
        pairTests.ShouldBeLessThan(1000);
    }

    [Fact]
    public void BuildDrawList_165NodeHorizontalList_TestsFarFewerThanAllPairs()
    {
        int pairTests = CountPairTests(BuildHorizontalList(33, new object(), new object()), out int nodes);

        nodes.ShouldBe(165);
        pairTests.ShouldBeLessThan(1000);
    }

    [Fact]
    public void BuildDrawList_AllNodesOverlap_TestsEveryPair()
    {
        // The dense worst case: every pair genuinely intersects, so nothing can be skipped.
        List<IRenderableIpso> roots = new List<IRenderableIpso>();
        for (int i = 0; i < 20; i++)
        {
            roots.Add(new OrdererFake($"n{i}") { X = 0, Y = 0, Width = 50, Height = 50 });
        }

        int pairTests = CountPairTests(roots, out _);

        pairTests.ShouldBeGreaterThanOrEqualTo(20 * 19 / 2);
    }

    [Fact]
    public void BuildDrawList_ResetsPairTestCountEachBuild()
    {
        BatchKeyGroupedOrderer orderer = new BatchKeyGroupedOrderer();
        Layer layer = BuildLayer(BuildVerticalList(5, new object(), new object()));
        List<DrawCommand> commands = new List<DrawCommand>();

        orderer.BuildDrawList(layer, commands);
        int first = orderer.PairTestCount;
        orderer.BuildDrawList(layer, commands);

        first.ShouldBeGreaterThan(0);
        orderer.PairTestCount.ShouldBe(first);
    }
}
