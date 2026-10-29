using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using static MonoGameGum.Tests.RenderingLibraries.BatchOrdererScenes;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Splash-damage tests around #7032: draw-order correctness and texture-run merging of
/// <see cref="BatchKeyGroupedOrderer"/> for component-shaped scenes (free-container roots holding
/// sprites, nine-slices, text pieces and shapes), with clips, render targets and invisible nodes.
/// The invariant everywhere: reordering must never change the visual result, so overlapping
/// renderables keep their depth-first order.
/// </summary>
public class BatchKeyGroupedOrdererSplashTests : BaseTestClass
{
    private static readonly object TexBg = new object();
    private static readonly object TexFont = new object();
    private static readonly object TexIcon = new object();

    // ---------------------------------------------------------------- helpers

    private static List<DrawCommand> Build(IEnumerable<IRenderableIpso> roots, out BatchKeyGroupedOrderer orderer, bool secondarySortOnY = false)
    {
        orderer = new BatchKeyGroupedOrderer();
        List<DrawCommand> commands = new List<DrawCommand>();
        orderer.BuildDrawList(BuildLayer(roots, secondarySortOnY), commands);
        return commands;
    }

    private static List<string> Names(List<DrawCommand> commands) =>
        commands.Where(c => c.Kind == DrawCommandKind.DrawRenderable)
            .Select(c => ((OrdererFake)c.Target).Name!).ToList();

    private static int KeyRuns(List<DrawCommand> commands)
    {
        int runs = 0;
        string? key = null;
        object? sortKey = null;
        foreach (DrawCommand command in commands)
        {
            OrdererFake item = (OrdererFake)command.Target;
            if (command.Kind != DrawCommandKind.DrawRenderable || item.BatchKey.Length == 0)
            {
                continue;
            }
            if (key == null || key != item.BatchKey || !Equals(sortKey, item.BatchSortKey))
            {
                runs++;
            }
            key = item.BatchKey;
            sortKey = item.BatchSortKey;
        }
        return runs;
    }

    /// <summary>One component instance: a free-container root with a background, optional icon, text pieces and focus shape.</summary>
    private static OrdererFake Component(string id, float x, float y, float w, float h,
        string bgKey = Sprites, object? bgTex = null, int textPieces = 1, bool icon = false, bool focusShape = false)
    {
        OrdererFake root = new OrdererFake($"c{id}", "") { X = x, Y = y, Width = w, Height = h };
        AddChild(root, $"bg{id}", bgKey, 0, 0, w, h, bgKey == Sprites ? (bgTex ?? TexBg) : null);
        if (icon)
        {
            AddChild(root, $"icon{id}", Sprites, 2, 2, 10, 10, TexIcon);
        }
        for (int p = 0; p < textPieces; p++)
        {
            AddChild(root, $"t{id}_{p}", Sprites, 14 + p * 6, 4, 5, 10, TexFont);
        }
        if (focusShape)
        {
            AddChild(root, $"focus{id}", Shapes, 1, 1, w - 2, h - 2);
        }
        return root;
    }

    private static OrdererFake Stack(int count, float spacing, Func<int, float, OrdererFake> make, float width = 100, float height = 20)
    {
        OrdererFake stack = new OrdererFake("stack", "") { X = 0, Y = 0, Width = width, Height = count * (height + spacing) };
        for (int i = 0; i < count; i++)
        {
            OrdererFake child = make(i, i * (height + spacing));
            child.SetParentDirect(stack);
            stack.Children.Add(child);
        }
        return stack;
    }

    private static IRenderableIpso[] One(IRenderableIpso item) => new[] { item };

    private static void ShouldBeInOrder(List<string> names, params string[] expectedSubsequence)
    {
        int last = -1;
        foreach (string expected in expectedSubsequence)
        {
            int index = names.IndexOf(expected);
            index.ShouldBeGreaterThan(last, $"{expected} should come after the previous item in {string.Join(",", names)}");
            last = index;
        }
    }

    // ------------------------------------------------------- merging (component scenes)

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(25)]
    public void Stack_OfButtons_MergesBackgroundsThenTexts_RegardlessOfCount(int count)
    {
        OrdererFake stack = Stack(count, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        List<DrawCommand> commands = Build(One(stack), out BatchKeyGroupedOrderer orderer);

        KeyRuns(commands).ShouldBe(2);
        orderer.MergeBlockedByOverlapCount.ShouldBe(0);
        orderer.HardBoundaryTransitionCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Stack_OfButtons_MultiPieceText_StaysOneTextRun(int pieces)
    {
        // CharacterByCharacter text is one sprite per character; SinglePass is one renderable.
        OrdererFake stack = Stack(5, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20, textPieces: pieces));
        List<DrawCommand> commands = Build(One(stack), out _);

        KeyRuns(commands).ShouldBe(2);
        Names(commands).Count(n => n.StartsWith("t")).ShouldBe(5 * pieces);
    }

    [Fact]
    public void Stack_OfButtonsWithIcons_GivesOneRunPerTexture()
    {
        OrdererFake stack = Stack(5, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20, icon: true));
        KeyRuns(Build(One(stack), out _)).ShouldBe(3);
    }

    [Fact]
    public void Stack_OfButtonsWithFocusShape_GivesOneRunPerKind()
    {
        OrdererFake stack = Stack(5, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20, focusShape: true));
        List<DrawCommand> commands = Build(One(stack), out _);

        KeyRuns(commands).ShouldBe(3);
        List<string> names = Names(commands);
        for (int i = 0; i < 5; i++)
        {
            ShouldBeInOrder(names, $"bg{i}", $"t{i}_0", $"focus{i}");
        }
    }

    [Fact]
    public void Stack_OfShapeBackgroundButtons_MergesShapesThenTexts()
    {
        OrdererFake stack = Stack(6, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20, bgKey: Shapes));
        KeyRuns(Build(One(stack), out _)).ShouldBe(2);
    }

    [Fact]
    public void Row_OfButtons_MergesLikeAStack()
    {
        OrdererFake row = new OrdererFake("row", "") { X = 0, Y = 0, Width = 600, Height = 20 };
        for (int i = 0; i < 6; i++)
        {
            OrdererFake child = Component(i.ToString(), i * 104, 0, 100, 20);
            child.SetParentDirect(row);
            row.Children.Add(child);
        }
        KeyRuns(Build(One(row), out _)).ShouldBe(2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Stack_OfButtonsInsideNestedWrappers_StillMerges(int wrapperDepth)
    {
        OrdererFake stack = Stack(4, 4, (i, y) =>
        {
            OrdererFake current = Component(i.ToString(), 0, 0, 100, 20);
            for (int d = 0; d < wrapperDepth; d++)
            {
                OrdererFake wrapper = new OrdererFake($"w{i}_{d}", "") { X = 0, Y = d == wrapperDepth - 1 ? y : 0, Width = 100, Height = 20 };
                current.SetParentDirect(wrapper);
                wrapper.Children.Add(current);
                current = wrapper;
            }
            return current;
        });
        KeyRuns(Build(One(stack), out _)).ShouldBe(2);
    }

    [Fact]
    public void TwoStacks_SideBySide_MergeIntoOneBackgroundRunAndOneTextRun()
    {
        OrdererFake left = Stack(3, 4, (i, y) => Component($"L{i}", 0, y, 100, 20));
        OrdererFake right = Stack(3, 4, (i, y) => Component($"R{i}", 0, y, 100, 20));
        right.X = 200;
        KeyRuns(Build(new IRenderableIpso[] { left, right }, out _)).ShouldBe(2);
    }

    [Fact]
    public void Grid_OfCards_MergesBackgroundsThenTexts()
    {
        List<IRenderableIpso> roots = new List<IRenderableIpso>();
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                roots.Add(Component($"{r}_{c}", c * 60, r * 40, 50, 30));
            }
        }
        KeyRuns(Build(roots, out _)).ShouldBe(2);
    }

    [Fact]
    public void Stack_OfButtons_WithPanelBackgroundOnSameTexture_ContinuesTheRunIntoButtonBackgrounds()
    {
        OrdererFake stack = Stack(5, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        OrdererFake panel = new OrdererFake("panelBg", Sprites) { X = 0, Y = 0, Width = 100, Height = 120, BatchSortKey = TexBg };
        panel.SetParentDirect(stack);
        stack.Children.Insert(0, panel);

        List<DrawCommand> commands = Build(One(stack), out _);
        List<string> names = Names(commands);
        KeyRuns(commands).ShouldBe(2);
        names.IndexOf("panelBg").ShouldBeLessThan(names.IndexOf("bg0"));
    }

    [Fact]
    public void Stack_OfButtons_WithPanelBackgroundOnOtherTexture_KeepsPanelUnderEverything()
    {
        object panelTex = new object();
        OrdererFake stack = Stack(4, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        OrdererFake panel = new OrdererFake("panelBg", Sprites) { X = 0, Y = 0, Width = 100, Height = 100, BatchSortKey = panelTex };
        panel.SetParentDirect(stack);
        stack.Children.Insert(0, panel);

        List<DrawCommand> commands = Build(One(stack), out _);
        List<string> names = Names(commands);

        names[0].ShouldBe("stack");
        names[1].ShouldBe("panelBg");
        KeyRuns(commands).ShouldBe(3);
    }

    // ---------------------------------------------------------------- overlap order

    [Fact]
    public void Stack_OfOverlappingButtons_KeepsEachButtonOverThePreviousOne()
    {
        // Negative spacing (-8): button i+1 paints over button i's text.
        OrdererFake stack = Stack(4, -8, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        List<string> names = Names(Build(One(stack), out _));

        for (int i = 0; i < 3; i++)
        {
            ShouldBeInOrder(names, $"t{i}_0", $"bg{i + 1}");
        }
    }

    [Fact]
    public void Stack_OfButtons_WithOverlayDrawnLast_KeepsOverlayOnTop()
    {
        OrdererFake stack = Stack(4, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        AddChild(stack, "overlay", Sprites, 0, 0, 100, 100, TexBg);
        List<string> names = Names(Build(One(stack), out _));

        names[^1].ShouldBe("overlay");
    }

    [Fact]
    public void Components_WhereOneInstanceCoversAnother_KeepDepthFirstOrderBetweenThem()
    {
        OrdererFake first = Component("A", 0, 0, 100, 20);
        OrdererFake second = Component("B", 10, 0, 100, 20);
        List<string> names = Names(Build(new IRenderableIpso[] { first, second }, out _));

        ShouldBeInOrder(names, "tA_0", "bgB");
    }

    [Fact]
    public void Wrapper_ThatOverlapsAnEarlierDraw_StaysBehindIt()
    {
        OrdererFake early = new OrdererFake("early", Sprites) { X = 0, Y = 0, Width = 50, Height = 50, BatchSortKey = TexFont };
        OrdererFake component = Component("A", 10, 10, 100, 20);
        List<string> names = Names(Build(new IRenderableIpso[] { early, component }, out _));

        ShouldBeInOrder(names, "early", "bgA");
    }

    // ---------------------------------------------------------------- hard boundaries

    [Fact]
    public void ClippedStack_DrawsInsideItsClip_AndMerges()
    {
        OrdererFake viewer = new OrdererFake("viewer", "") { X = 0, Y = 0, Width = 120, Height = 80, ClipsChildren = true };
        OrdererFake stack = Stack(3, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        stack.SetParentDirect(viewer);
        viewer.Children.Add(stack);

        List<DrawCommand> commands = Build(One(viewer), out _);

        commands.First().Kind.ShouldBe(DrawCommandKind.BeginClip);
        commands.Last().Kind.ShouldBe(DrawCommandKind.EndClip);
        KeyRuns(commands).ShouldBe(2);
    }

    [Fact]
    public void TwoClippedStacks_DoNotMergeAcrossTheirClips()
    {
        OrdererFake Viewer(string id, float x)
        {
            OrdererFake viewer = new OrdererFake($"viewer{id}", "") { X = x, Y = 0, Width = 120, Height = 80, ClipsChildren = true };
            OrdererFake stack = Stack(2, 4, (i, y) => Component($"{id}{i}", 0, y, 100, 20));
            stack.SetParentDirect(viewer);
            viewer.Children.Add(stack);
            return viewer;
        }

        List<DrawCommand> commands = Build(new IRenderableIpso[] { Viewer("A", 0), Viewer("B", 300) }, out BatchKeyGroupedOrderer orderer);

        commands.Count(c => c.Kind == DrawCommandKind.BeginClip).ShouldBe(2);
        commands.Count(c => c.Kind == DrawCommandKind.EndClip).ShouldBe(2);
        int firstEnd = commands.FindIndex(c => c.Kind == DrawCommandKind.EndClip);
        int secondBegin = commands.FindLastIndex(c => c.Kind == DrawCommandKind.BeginClip);
        firstEnd.ShouldBeLessThan(secondBegin);
        orderer.HardBoundaryTransitionCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ClippedChildInsideAButton_IsNeverDrawnOutsideItsClip()
    {
        OrdererFake stack = Stack(3, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        OrdererFake clipper = AddChild((OrdererFake)stack.Children[1], "clipper", Sprites, 0, 0, 100, 20, TexFont);
        clipper.ClipsChildren = true;
        AddChild(clipper, "inClip", Sprites, 5, 5, 10, 10, TexIcon);

        List<DrawCommand> commands = Build(One(stack), out _);
        int begin = commands.FindIndex(c => c.Kind == DrawCommandKind.BeginClip);
        int end = commands.FindIndex(c => c.Kind == DrawCommandKind.EndClip);
        int inClip = commands.FindIndex(c => c.Kind == DrawCommandKind.DrawRenderable && ((OrdererFake)c.Target).Name == "inClip");

        inClip.ShouldBeGreaterThan(begin);
        inClip.ShouldBeLessThan(end);
    }

    [Fact]
    public void RenderTargetBetweenButtons_IsDrawnOnceAndItsChildrenAreSkipped()
    {
        OrdererFake stack = Stack(3, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        OrdererFake middle = (OrdererFake)stack.Children[1];
        middle.IsRenderTarget = true;
        middle.BatchKey = Sprites;

        List<string> names = Names(Build(One(stack), out BatchKeyGroupedOrderer orderer));

        names.Count(n => n == "c1").ShouldBe(1);
        names.ShouldNotContain("bg1");
        names.ShouldContain("bg0");
        names.ShouldContain("bg2");
        orderer.HardBoundaryTransitionCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void InvisibleButton_IsSkippedWithItsChildren_AndTheRestStillMerge()
    {
        OrdererFake stack = Stack(4, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        ((OrdererFake)stack.Children[1]).Visible = false;

        List<DrawCommand> commands = Build(One(stack), out _);
        List<string> names = Names(commands);

        names.ShouldNotContain("c1");
        names.ShouldNotContain("bg1");
        names.ShouldNotContain("t1_0");
        KeyRuns(commands).ShouldBe(2);
    }

    [Fact]
    public void SecondarySortOnY_MergesWithinARowButNotAcrossRows()
    {
        List<IRenderableIpso> roots = new List<IRenderableIpso>();
        for (int row = 0; row < 3; row++)
        {
            for (int c = 0; c < 3; c++)
            {
                roots.Add(Component($"{row}{c}", c * 110, row * 40, 100, 20));
            }
        }

        List<DrawCommand> commands = Build(roots, out _, secondarySortOnY: true);
        List<string> names = Names(commands);

        KeyRuns(commands).ShouldBe(6);
        for (int row = 0; row < 2; row++)
        {
            names.IndexOf($"t{row}2_0").ShouldBeLessThan(names.IndexOf($"bg{row + 1}0"));
        }
    }

    // ---------------------------------------------------------------- bookkeeping

    [Fact]
    public void Stack_OfButtons_DrawsEveryNodeExactlyOnce()
    {
        OrdererFake stack = Stack(7, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20, textPieces: 3, icon: true, focusShape: true));
        List<string> names = Names(Build(One(stack), out _));

        names.Count.ShouldBe(CountNodes(One(stack)));
        names.Distinct().Count().ShouldBe(names.Count);
    }

    [Fact]
    public void Stack_OfButtons_BreakGroupsReconcileWithRunCount()
    {
        OrdererFake stack = Stack(6, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20, icon: true, focusShape: true));
        List<DrawCommand> commands = Build(One(stack), out BatchKeyGroupedOrderer orderer);

        orderer.GetBreakGroups().Sum(g => g.Count).ShouldBe(KeyRuns(commands));
    }

    [Fact]
    public void ResetBreakTally_AfterAStackBuild_ClearsEverything()
    {
        OrdererFake stack = Stack(4, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        Build(One(stack), out BatchKeyGroupedOrderer orderer);
        orderer.GetBreakGroups().Count.ShouldBeGreaterThan(0);

        orderer.ResetBreakTally();

        orderer.GetBreakGroups().Count.ShouldBe(0);
        orderer.NoCandidateInWindowBreakCount.ShouldBe(0);
    }

    [Fact]
    public void Stack_OfButtons_ProducesTheSameDrawListOnRepeatedBuilds()
    {
        OrdererFake stack = Stack(6, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        BatchKeyGroupedOrderer orderer = new BatchKeyGroupedOrderer();
        Layer layer = BuildLayer(One(stack));
        List<DrawCommand> first = new List<DrawCommand>();
        List<DrawCommand> second = new List<DrawCommand>();

        orderer.BuildDrawList(layer, first);
        orderer.BuildDrawList(layer, second);

        Describe(second).ShouldBe(Describe(first));
    }

    [Fact]
    public void Stack_OfManyButtons_PicksInFarFewerStepsThanQuadraticInNodeCount()
    {
        OrdererFake stack = Stack(100, 4, (i, y) => Component(i.ToString(), 0, y, 100, 20));
        Build(One(stack), out BatchKeyGroupedOrderer orderer);

        int nodes = CountNodes(One(stack));
        orderer.PickStepCount.ShouldBeLessThan(nodes * 40);
    }

    // ---------------------------------------------------------------- randomized invariants

    private sealed class Profile
    {
        public double ContainerChance = 0.4;
        public double ClipChance;
        public double RenderTargetChance;
        public double InvisibleChance;
        public double ShapeChance = 0.2;
        public int TextureCount = 3;
        public int MaxDepth = 3;
        public int MaxChildren = 5;
        public int MinSpacing = 0;
        public int MaxSpacing = 6;
        public bool SecondarySort;
        public bool NoBoundaries => ClipChance == 0 && RenderTargetChance == 0;
    }

    public static IEnumerable<object[]> ProfileNames() => new[]
    {
        "Components", "NestedComponents", "Overlapping", "SingleTexture", "ManyTextures", "Clipped",
        "RenderTargets", "Invisible", "Everything", "Rows", "ShapesHeavy", "Sparse",
    }.Select(n => new object[] { n });

    private static Profile GetProfile(string name) => name switch
    {
        "Components" => new Profile { ContainerChance = 0.6, MaxDepth = 2 },
        "NestedComponents" => new Profile { ContainerChance = 0.8, MaxDepth = 5, MaxChildren = 3 },
        "Overlapping" => new Profile { ContainerChance = 0.6, MinSpacing = -8, MaxSpacing = 0 },
        "SingleTexture" => new Profile { TextureCount = 1, ShapeChance = 0 },
        "ManyTextures" => new Profile { TextureCount = 8 },
        "Clipped" => new Profile { ClipChance = 0.15 },
        "RenderTargets" => new Profile { RenderTargetChance = 0.12 },
        "Invisible" => new Profile { InvisibleChance = 0.15 },
        "Everything" => new Profile { ClipChance = 0.1, RenderTargetChance = 0.08, InvisibleChance = 0.1, MinSpacing = -4 },
        "Rows" => new Profile { SecondarySort = true, MaxDepth = 2 },
        "ShapesHeavy" => new Profile { ShapeChance = 0.6 },
        _ => new Profile { MinSpacing = 20, MaxSpacing = 60 },
    };

    private static List<IRenderableIpso> RandomScene(Profile profile, int seed)
    {
        Random random = new Random(seed);
        object[] textures = Enumerable.Range(0, profile.TextureCount).Select(_ => new object()).ToArray();
        int counter = 0;

        OrdererFake MakeNode(int depth)
        {
            string name = $"n{counter++}";
            OrdererFake node;
            if (depth < profile.MaxDepth && random.NextDouble() < profile.ContainerChance)
            {
                node = new OrdererFake(name, "");
            }
            else if (random.NextDouble() < profile.ShapeChance)
            {
                node = new OrdererFake(name, Shapes);
            }
            else
            {
                node = new OrdererFake(name, Sprites) { BatchSortKey = textures[random.Next(textures.Length)] };
            }

            if (random.NextDouble() < profile.ClipChance) { node.ClipsChildren = true; }
            if (random.NextDouble() < profile.RenderTargetChance) { node.IsRenderTarget = true; }
            if (random.NextDouble() < profile.InvisibleChance) { node.Visible = false; }
            return node;
        }

        void Fill(OrdererFake parent, int depth)
        {
            bool canHaveChildren = parent.BatchKey.Length == 0 || parent.ClipsChildren;
            if (!canHaveChildren || depth >= profile.MaxDepth)
            {
                return;
            }
            int count = random.Next(1, profile.MaxChildren + 1);
            bool vertical = random.Next(2) == 0;
            float cursor = 0;
            for (int i = 0; i < count; i++)
            {
                OrdererFake child = MakeNode(depth + 1);
                int spacing = random.Next(profile.MinSpacing, profile.MaxSpacing + 1);
                int size = random.Next(8, 24);
                float pos = System.Math.Max(0, cursor);
                // Keep every child inside its parent's bounds.
                if (vertical)
                {
                    child.X = 0;
                    child.Y = System.Math.Min(pos, parent.Height - 1);
                    child.Width = parent.Width;
                    child.Height = System.Math.Min(size, parent.Height - child.Y);
                }
                else
                {
                    child.Y = 0;
                    child.X = System.Math.Min(pos, parent.Width - 1);
                    child.Height = parent.Height;
                    child.Width = System.Math.Min(size, parent.Width - child.X);
                }
                cursor = (vertical ? child.Y + child.Height : child.X + child.Width) + spacing;
                child.SetParentDirect(parent);
                parent.Children.Add(child);
                Fill(child, depth + 1);
            }
        }

        List<IRenderableIpso> roots = new List<IRenderableIpso>();
        int rootCount = random.Next(1, 5);
        for (int r = 0; r < rootCount; r++)
        {
            OrdererFake root = new OrdererFake($"r{r}", "")
            {
                X = profile.SecondarySort ? r * 10 : r * 40,
                Y = profile.SecondarySort ? (r % 2) * 100 : random.Next(0, 30),
                Width = 160,
                Height = 160,
            };
            if (random.NextDouble() < profile.ClipChance) { root.ClipsChildren = true; }
            if (random.NextDouble() < profile.InvisibleChance) { root.Visible = false; }
            Fill(root, 0);
            roots.Add(root);
        }
        return roots;
    }

    /// <summary>Depth-first list of the nodes the renderer would visit, with each node's clip-ancestor chain.</summary>
    private static void CollectExpected(IRenderableIpso node, List<IRenderableIpso> order,
        Dictionary<IRenderableIpso, List<IRenderableIpso>> clips, List<IRenderableIpso> activeClips)
    {
        if (!node.Visible)
        {
            return;
        }
        List<IRenderableIpso> chain = activeClips;
        if (node.ClipsChildren)
        {
            chain = new List<IRenderableIpso>(activeClips) { node };
        }
        order.Add(node);
        clips[node] = chain;
        if (node.IsRenderTarget)
        {
            return;
        }
        foreach (IRenderableIpso child in node.Children)
        {
            CollectExpected(child, order, clips, chain);
        }
    }

    private static void AssertDrawListIsPixelEquivalent(List<IRenderableIpso> roots, List<DrawCommand> commands, string context)
    {
        List<IRenderableIpso> expectedOrder = new List<IRenderableIpso>();
        Dictionary<IRenderableIpso, List<IRenderableIpso>> expectedClips = new Dictionary<IRenderableIpso, List<IRenderableIpso>>();
        foreach (IRenderableIpso root in roots)
        {
            CollectExpected(root, expectedOrder, expectedClips, new List<IRenderableIpso>());
        }

        // Every visible node exactly once.
        List<IRenderableIpso> drawn = commands.Where(c => c.Kind == DrawCommandKind.DrawRenderable).Select(c => c.Target).ToList();
        drawn.Count.ShouldBe(expectedOrder.Count, context);
        drawn.Distinct().Count().ShouldBe(drawn.Count, context);

        // Clip scopes: balanced, and each draw sits inside exactly its clip ancestors.
        List<IRenderableIpso> active = new List<IRenderableIpso>();
        foreach (DrawCommand command in commands)
        {
            switch (command.Kind)
            {
                case DrawCommandKind.BeginClip:
                    active.Add(command.Target);
                    break;
                case DrawCommandKind.EndClip:
                    active.Count.ShouldBeGreaterThan(0, context);
                    active[^1].ShouldBeSameAs(command.Target, context);
                    active.RemoveAt(active.Count - 1);
                    break;
                default:
                    active.SequenceEqual(expectedClips[command.Target])
                        .ShouldBeTrue($"{context}: clip scope of {((OrdererFake)command.Target).Name}");
                    break;
            }
        }
        active.Count.ShouldBe(0, context);

        // Overlapping draws keep their depth-first order.
        Dictionary<IRenderableIpso, int> position = new Dictionary<IRenderableIpso, int>();
        for (int i = 0; i < drawn.Count; i++)
        {
            position[drawn[i]] = i;
        }
        List<Rectangle> bounds = expectedOrder.Select(n => n.GetAbsoluteBounds()).ToList();
        for (int a = 0; a < expectedOrder.Count; a++)
        {
            for (int b = a + 1; b < expectedOrder.Count; b++)
            {
                if (bounds[a].IntersectsWith(bounds[b]))
                {
                    position[expectedOrder[a]].ShouldBeLessThan(position[expectedOrder[b]],
                        $"{context}: {((OrdererFake)expectedOrder[a]).Name} overlaps {((OrdererFake)expectedOrder[b]).Name} but was drawn after it");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ProfileNames))]
    public void RandomScenes_KeepPixelOrder_ClipScopes_AndDrawEachNodeOnce(string profileName)
    {
        Profile profile = GetProfile(profileName);
        BatchKeyGroupedOrderer orderer = new BatchKeyGroupedOrderer();
        List<DrawCommand> commands = new List<DrawCommand>();

        int cases = SweepScale.Cases(300);
        for (int seed = 0; seed < cases; seed++)
        {
            List<IRenderableIpso> roots = RandomScene(profile, seed);
            orderer.BuildDrawList(BuildLayer(roots, profile.SecondarySort), commands);
            AssertDrawListIsPixelEquivalent(roots, commands, $"{profileName} seed {seed}");
        }
    }

    [Theory]
    [MemberData(nameof(ProfileNames))]
    public void RandomScenes_WithoutHardBoundaries_NeverSwitchTextureMoreThanHierarchical(string profileName)
    {
        Profile profile = GetProfile(profileName);
        if (!profile.NoBoundaries || profile.SecondarySort)
        {
            return;
        }

        int cases = SweepScale.Cases(300);
        for (int seed = 0; seed < cases; seed++)
        {
            List<DrawCommand> grouped = new List<DrawCommand>();
            List<DrawCommand> hierarchical = new List<DrawCommand>();
            new BatchKeyGroupedOrderer().BuildDrawList(BuildLayer(RandomScene(profile, seed)), grouped);
            HierarchicalOrderer.Instance.BuildDrawList(BuildLayer(RandomScene(profile, seed)), hierarchical);

            KeyRuns(grouped).ShouldBeLessThanOrEqualTo(KeyRuns(hierarchical), $"{profileName} seed {seed}");
        }
    }

    // The seeds where the greedy pick alone produced more key runs than HierarchicalOrderer
    // (#7051). The sweep above reaches them only at full scale; this pins them at any scale and
    // checks the diagnostic tallies still reconcile when the depth-first order is the one emitted.
    [Theory]
    [InlineData("Invisible", 61)]
    [InlineData("Overlapping", 192)]
    [InlineData("NestedComponents", 169)]
    public void KnownSeeds_WhereGreedyAloneWasWorse_NeverSwitchTextureMoreThanHierarchical(string profileName, int seed)
    {
        Profile profile = GetProfile(profileName);
        List<IRenderableIpso> roots = RandomScene(profile, seed);
        List<DrawCommand> grouped = Build(roots, out BatchKeyGroupedOrderer orderer);
        List<DrawCommand> hierarchical = new List<DrawCommand>();
        HierarchicalOrderer.Instance.BuildDrawList(BuildLayer(RandomScene(profile, seed)), hierarchical);

        KeyRuns(grouped).ShouldBeLessThanOrEqualTo(KeyRuns(hierarchical));
        orderer.GetBreakGroups().Sum(g => g.Count).ShouldBe(KeyRuns(grouped));
        AssertDrawListIsPixelEquivalent(roots, grouped, $"{profileName} seed {seed}");
    }

    [Theory]
    [MemberData(nameof(ProfileNames))]
    public void RandomScenes_WithoutHardBoundaries_BreakGroupsReconcileWithRunCount(string profileName)
    {
        Profile profile = GetProfile(profileName);
        if (!profile.NoBoundaries || profile.SecondarySort)
        {
            return;
        }

        int cases = SweepScale.Cases(300);
        for (int seed = 0; seed < cases; seed++)
        {
            List<DrawCommand> commands = Build(RandomScene(profile, seed), out BatchKeyGroupedOrderer orderer);
            orderer.GetBreakGroups().Sum(g => g.Count).ShouldBe(KeyRuns(commands), $"{profileName} seed {seed}");
        }
    }
}
