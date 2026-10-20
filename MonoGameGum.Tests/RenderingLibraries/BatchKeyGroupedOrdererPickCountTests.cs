using System.Collections.Generic;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using static MonoGameGum.Tests.RenderingLibraries.BatchOrdererScenes;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Pins how many window entries <see cref="BatchKeyGroupedOrderer"/> examines while picking the
/// next draw in its topological sort (#6302). Counts, not timings, so the result is deterministic.
/// </summary>
public class BatchKeyGroupedOrdererPickCountTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public BatchKeyGroupedOrdererPickCountTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private int CountPickSteps(List<IRenderableIpso> roots, out int nodeCount)
    {
        nodeCount = CountNodes(roots);
        BatchKeyGroupedOrderer orderer = new BatchKeyGroupedOrderer();
        orderer.BuildDrawList(BuildLayer(roots), new List<DrawCommand>());
        _output.WriteLine($"nodes={nodeCount} pickSteps={orderer.PickStepCount}");
        return orderer.PickStepCount;
    }

    /// <summary>Non-overlapping nodes cycling through two batch keys and two textures, all available at once.</summary>
    private static List<IRenderableIpso> BuildDisjointNodes(int count)
    {
        object textureA = new object();
        object textureB = new object();
        List<IRenderableIpso> roots = new List<IRenderableIpso>();
        for (int i = 0; i < count; i++)
        {
            string batchKey = i % 2 == 0 ? Sprites : Shapes;
            object? sortKey = batchKey == Sprites ? (i % 4 == 0 ? textureA : textureB) : null;
            roots.Add(new OrdererFake($"n{i}", batchKey) { X = i * 20, Y = 0, Width = 10, Height = 10, BatchSortKey = sortKey });
        }
        return roots;
    }

    [Fact]
    public void BuildDrawList_165NodeVerticalList_PicksWithoutRescanningAvailable()
    {
        int steps = CountPickSteps(BuildVerticalList(33, new object(), new object()), out int nodes);

        nodes.ShouldBe(165);
        steps.ShouldBeLessThan(2000);
    }

    [Fact]
    public void BuildDrawList_500NodeVerticalList_PickStepsGrowNearLinearly()
    {
        int steps = CountPickSteps(BuildVerticalList(100, new object(), new object()), out int nodes);

        nodes.ShouldBe(500);
        steps.ShouldBeLessThan(7000);
    }

    [Fact]
    public void BuildDrawList_500DisjointNodes_PickStepsGrowNearLinearly()
    {
        int steps = CountPickSteps(BuildDisjointNodes(500), out int nodes);

        nodes.ShouldBe(500);
        // Every node is available from the start, so a per-pick rescan of them costs ~n^2.
        steps.ShouldBeLessThan(15000);
    }

    [Fact]
    public void BuildDrawList_ResetsPickStepCountEachBuild()
    {
        BatchKeyGroupedOrderer orderer = new BatchKeyGroupedOrderer();
        Layer layer = BuildLayer(BuildVerticalList(5, new object(), new object()));
        List<DrawCommand> commands = new List<DrawCommand>();

        orderer.BuildDrawList(layer, commands);
        int first = orderer.PickStepCount;
        orderer.BuildDrawList(layer, commands);

        first.ShouldBeGreaterThan(0);
        orderer.PickStepCount.ShouldBe(first);
    }
}
