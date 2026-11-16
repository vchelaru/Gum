using System.Collections.Generic;
using System.Linq;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using static MonoGameGum.Tests.RenderingLibraries.BatchOrdererScenes;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Characterization of the exact draw order, batch grouping and break tallies
/// <see cref="BatchKeyGroupedOrderer"/> produces for a range of scenes. Written green against the
/// pairwise precedence-graph build so any change to how that graph is built (#6179) must reproduce
/// these outputs exactly.
/// </summary>
public class BatchKeyGroupedOrdererPinTests : BaseTestClass
{
    private static string Run(Layer layer, out string tallies)
    {
        BatchKeyGroupedOrderer orderer = new BatchKeyGroupedOrderer();
        List<DrawCommand> commands = new List<DrawCommand>();
        orderer.BuildDrawList(layer, commands);
        tallies = $"{orderer.MergeBlockedByOverlapCount}/{orderer.NoCandidateInWindowBreakCount}/{orderer.HardBoundaryTransitionCount}";
        return string.Join(",", Describe(commands).Select(d => d.Replace("DrawRenderable:", "")));
    }

    // FNV-1a: a stable digest (string.GetHashCode is randomized per process).
    private static uint Digest(string text)
    {
        uint hash = 2166136261;
        foreach (char c in text)
        {
            hash = (hash ^ c) * 16777619;
        }
        return hash;
    }

    internal static Layer OverlapStaircase()
    {
        List<IRenderableIpso> items = new List<IRenderableIpso>();
        for (int i = 0; i < 8; i++)
        {
            items.Add(new OrdererFake($"s{i}", i % 2 == 0 ? Sprites : Shapes) { X = i * 8, Y = 0, Width = 10, Height = 10 });
        }
        return BuildLayer(items);
    }

    internal static Layer NonOverlappingGrid()
    {
        object texA = new object();
        object texB = new object();
        List<IRenderableIpso> items = new List<IRenderableIpso>();
        for (int i = 0; i < 12; i++)
        {
            string key = i % 3 == 0 ? Shapes : Sprites;
            object? sortKey = key == Sprites ? (i % 2 == 0 ? texA : texB) : null;
            items.Add(new OrdererFake($"g{i}", key) { X = (i % 4) * 20, Y = (i / 4) * 20, Width = 10, Height = 10, BatchSortKey = sortKey });
        }
        return BuildLayer(items);
    }

    internal static Layer NestedCards()
    {
        object font = new object();
        object icon = new object();
        List<IRenderableIpso> roots = new List<IRenderableIpso>();
        for (int card = 0; card < 4; card++)
        {
            // Cards 0-2 are spread out; card 3 overlaps card 2.
            float dx = card < 3 ? card * 100 : 2 * 100 + 40;
            OrdererFake container = new OrdererFake($"card{card}", "") { X = dx, Y = 0, Width = 80, Height = 112 };
            AddChildAtAbsolute(container, $"bg{card}", Shapes, dx, 0, 80, 112);
            OrdererFake inner = AddChildAtAbsolute(container, $"inner{card}", "", dx + 2, 2, 76, 108);
            AddChildAtAbsolute(inner, $"rank{card}", Sprites, dx + 5, 4, 14, 30, font);
            AddChildAtAbsolute(inner, $"suit{card}", Sprites, dx + 10, 40, 40, 40, icon);
            AddChildAtAbsolute(container, $"border{card}", Shapes, dx, 0, 80, 112);
            roots.Add(container);
        }
        return BuildLayer(roots);
    }

    internal static Layer StackedSameBounds()
    {
        List<IRenderableIpso> items = new List<IRenderableIpso>();
        for (int i = 0; i < 6; i++)
        {
            items.Add(new OrdererFake($"z{i}", i % 2 == 0 ? Sprites : Shapes) { X = 0, Y = 0, Width = 50, Height = 50 });
        }
        items.Add(new OrdererFake("far1", Sprites) { X = 500, Y = 500, Width = 5, Height = 5 });
        items.Add(new OrdererFake("far2", Shapes) { X = 600, Y = 500, Width = 5, Height = 5 });
        // Shares an edge with the stack but does not intersect it.
        items.Add(new OrdererFake("edge", Sprites) { X = 50, Y = 0, Width = 10, Height = 10 });
        return BuildLayer(items);
    }

    internal static Layer WindowBoundaries()
    {
        object tex = new object();
        List<IRenderableIpso> items = new List<IRenderableIpso>();
        items.Add(new OrdererFake("a", Shapes) { X = 0, Y = 0, Width = 10, Height = 10 });
        items.Add(new OrdererFake("b", Sprites) { X = 20, Y = 0, Width = 10, Height = 10, BatchSortKey = tex });

        OrdererFake clip = new OrdererFake("clip", Sprites) { X = 100, Y = 0, Width = 80, Height = 80, ClipsChildren = true };
        AddChildAtAbsolute(clip, "c1", Shapes, 105, 5, 20, 20);
        AddChildAtAbsolute(clip, "c2", Sprites, 140, 5, 20, 20, tex);
        AddChildAtAbsolute(clip, "c3", Shapes, 105, 40, 20, 20);
        OrdererFake nestedClip = AddChildAtAbsolute(clip, "nestedClip", Sprites, 130, 30, 40, 40);
        nestedClip.ClipsChildren = true;
        AddChildAtAbsolute(nestedClip, "n1", Shapes, 132, 32, 10, 10);
        AddChildAtAbsolute(nestedClip, "n2", Sprites, 150, 32, 10, 10, tex);
        items.Add(clip);

        items.Add(new OrdererFake("d", Shapes) { X = 200, Y = 0, Width = 10, Height = 10 });
        items.Add(new OrdererFake("target", "") { X = 240, Y = 0, Width = 30, Height = 30, IsRenderTarget = true });
        items.Add(new OrdererFake("e", Sprites) { X = 300, Y = 0, Width = 10, Height = 10, BatchSortKey = tex });
        items.Add(new OrdererFake("f", Shapes) { X = 320, Y = 0, Width = 10, Height = 10 });
        return BuildLayer(items);
    }

    internal static Layer SecondarySortRows()
    {
        List<IRenderableIpso> items = new List<IRenderableIpso>();
        for (int row = 0; row < 3; row++)
        {
            float y = row * 30;
            items.Add(new OrdererFake($"r{row}a", Sprites) { X = 0, Y = y, Width = 10, Height = 10 });
            items.Add(new OrdererFake($"r{row}b", Shapes) { X = 20, Y = y, Width = 10, Height = 10 });
            items.Add(new OrdererFake($"r{row}c", Sprites) { X = 40, Y = y, Width = 10, Height = 10 });
        }
        return BuildLayer(items, secondarySortOnY: true);
    }

    internal static Layer VerticalList165() => BuildLayer(BuildVerticalList(33, new object(), new object()));

    internal static Layer HorizontalList165() => BuildLayer(BuildHorizontalList(33, new object(), new object()));

    [Fact]
    public void OverlapStaircase_Pinned()
    {
        Run(OverlapStaircase(), out string tallies).ShouldBe("s0,s1,s2,s3,s4,s5,s6,s7");
        tallies.ShouldBe("6/1/0");
    }

    [Fact]
    public void NonOverlappingGrid_Pinned()
    {
        Run(NonOverlappingGrid(), out string tallies).ShouldBe("g0,g3,g6,g9,g1,g5,g7,g11,g2,g4,g8,g10");
        tallies.ShouldBe("0/2/0");
    }

    [Fact]
    public void NestedCards_Pinned()
    {
        Run(NestedCards(), out string tallies).ShouldBe("card0,card1,card2,bg0,bg1,bg2,inner0,inner1,inner2,rank0,rank1,rank2,suit0,suit1,suit2,border0,border1,border2,card3,bg3,inner3,rank3,suit3,border3");
        tallies.ShouldBe("4/2/0");
    }

    [Fact]
    public void StackedSameBounds_Pinned()
    {
        Run(StackedSameBounds(), out string tallies).ShouldBe("z0,far1,edge,z1,far2,z2,z3,z4,z5");
        tallies.ShouldBe("4/1/0");
    }

    [Fact]
    public void WindowBoundaries_Pinned()
    {
        Run(WindowBoundaries(), out string tallies).ShouldBe("a,b,BeginClip:clip,clip,c2,c1,c3,BeginClip:nestedClip,nestedClip,n2,n1,EndClip:nestedClip,EndClip:clip,d,f,target,e");
        tallies.ShouldBe("0/5/5");
    }

    [Fact]
    public void SecondarySortRows_Pinned()
    {
        Run(SecondarySortRows(), out string tallies).ShouldBe("r0a,r0c,r0b,r1a,r1c,r1b,r2a,r2c,r2b");
        tallies.ShouldBe("0/3/0");
    }

    [Fact]
    public void VerticalList165_Pinned()
    {
        string order = Run(VerticalList165(), out string tallies);
        order.Split(',').Length.ShouldBe(165);
        Digest(order).ShouldBe(1316084461u);
        tallies.ShouldBe("0/2/0");
    }

    [Fact]
    public void HorizontalList165_Pinned()
    {
        string order = Run(HorizontalList165(), out string tallies);
        order.Split(',').Length.ShouldBe(165);
        Digest(order).ShouldBe(3922243555u);
        tallies.ShouldBe("0/2/0");
    }
}
