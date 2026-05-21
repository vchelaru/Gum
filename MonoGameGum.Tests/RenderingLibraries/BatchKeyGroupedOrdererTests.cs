using System.Collections.Generic;
using System.Collections.ObjectModel;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Unit tests for <see cref="BatchKeyGroupedOrderer"/>. The orderer reorders DFS draws within
/// a layer/clip-bounded window so that runs of same-<see cref="IRenderable.BatchKey"/> draws
/// become contiguous, while preserving the relative order of any two draws whose absolute
/// bounds overlap. Tests cover each safety constraint plus the canonical alternation pattern
/// that motivates the orderer (sprite/text/shape rows in a list).
/// </summary>
public class BatchKeyGroupedOrdererTests : BaseTestClass
{
    private sealed class FakeRenderable : IRenderableIpso
    {
        public FakeRenderable(string name, string batchKey = "SpriteBatch")
        {
            Name = name;
            BatchKey = batchKey;
            Visible = true;
            Children = new ObservableCollection<IRenderableIpso>();
        }

        public string Name { get; set; }
        public bool Visible { get; set; }
        public bool ClipsChildren { get; set; }
        public bool IsRenderTarget { get; set; }
        public ObservableCollection<IRenderableIpso> Children { get; }

        public IRenderableIpso? Parent { get; set; }
        IVisible? IVisible.Parent => Parent;
        public bool AbsoluteVisible => Visible;

        public void SetParentDirect(IRenderableIpso? newParent) => Parent = newParent;

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Rotation { get; set; }
        public bool FlipHorizontal { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public object? Tag { get; set; }

        public int Alpha => 255;
        public ColorOperation ColorOperation => ColorOperation.Modulate;
        public Gum.BlendState BlendState => Gum.BlendState.NonPremultiplied;
        public bool Wrap => false;

        public string BatchKey { get; set; }
        public void Render(ISystemManagers managers) { }
        public void PreRender() { }
        public void StartBatch(ISystemManagers managers) { }
        public void EndBatch(ISystemManagers managers) { }
    }

    private static FakeRenderable AddChild(FakeRenderable parent, string name, string batchKey = "SpriteBatch")
    {
        FakeRenderable child = new FakeRenderable(name, batchKey);
        child.SetParentDirect(parent);
        parent.Children.Add(child);
        return child;
    }

    private static Layer BuildLayer(params IRenderableIpso[] renderables)
    {
        Layer layer = new Layer();
        foreach (IRenderableIpso renderable in renderables)
        {
            layer.Add(renderable);
        }
        return layer;
    }

    private static List<string> Describe(List<DrawCommand> commands)
    {
        List<string> result = new List<string>();
        foreach (DrawCommand cmd in commands)
        {
            string targetName = ((FakeRenderable)cmd.Target).Name;
            result.Add($"{cmd.Kind}:{targetName}");
        }
        return result;
    }

    [Fact]
    public void BuildDrawList_AlternatingBatchKeys_GroupsSameKeyTogether()
    {
        // Three non-overlapping rows, each with a SpriteBatch item and an Apos.Shapes item.
        // Without reorder the DFS would alternate SB,Apos,SB,Apos,SB,Apos. The orderer
        // should pull SB items into one run and Apos into another.
        FakeRenderable sb1 = new FakeRenderable("sb1", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable apos1 = new FakeRenderable("apos1", "Apos.Shapes") { X = 50, Y = 0, Width = 10, Height = 10 };
        FakeRenderable sb2 = new FakeRenderable("sb2", "SpriteBatch") { X = 0, Y = 20, Width = 10, Height = 10 };
        FakeRenderable apos2 = new FakeRenderable("apos2", "Apos.Shapes") { X = 50, Y = 20, Width = 10, Height = 10 };
        FakeRenderable sb3 = new FakeRenderable("sb3", "SpriteBatch") { X = 0, Y = 40, Width = 10, Height = 10 };
        FakeRenderable apos3 = new FakeRenderable("apos3", "Apos.Shapes") { X = 50, Y = 40, Width = 10, Height = 10 };

        Layer layer = BuildLayer(sb1, apos1, sb2, apos2, sb3, apos3);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[]
        {
            "DrawRenderable:sb1",
            "DrawRenderable:sb2",
            "DrawRenderable:sb3",
            "DrawRenderable:apos1",
            "DrawRenderable:apos2",
            "DrawRenderable:apos3",
        });
    }

    [Fact]
    public void BuildDrawList_ClippingBoundary_DoesNotReorderAcrossClip()
    {
        // The clip-bearing node bounds an independent reorder window. An Apos item before the
        // clip and an Apos item inside the clip must NOT be pulled together; the clip is a hard
        // boundary.
        FakeRenderable aposBefore = new FakeRenderable("aposBefore", "Apos.Shapes") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable clip = new FakeRenderable("clip", "SpriteBatch") { X = 100, Y = 0, Width = 50, Height = 50 };
        clip.ClipsChildren = true;
        FakeRenderable aposInside = AddChild(clip, "aposInside", "Apos.Shapes");
        aposInside.X = 5;
        aposInside.Y = 5;
        aposInside.Width = 10;
        aposInside.Height = 10;

        Layer layer = BuildLayer(aposBefore, clip);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[]
        {
            "DrawRenderable:aposBefore",
            "BeginClip:clip",
            "DrawRenderable:clip",
            "DrawRenderable:aposInside",
            "EndClip:clip",
        });
    }

    [Fact]
    public void BuildDrawList_DepthFirstWalk_WhenAllSameBatchKey_MatchesHierarchical()
    {
        // When every renderable has the same BatchKey there is nothing to reorder; output must
        // match HierarchicalOrderer's DFS pre-order.
        FakeRenderable a = new FakeRenderable("a");
        FakeRenderable a1 = AddChild(a, "a1");
        FakeRenderable a2 = AddChild(a, "a2");
        AddChild(a1, "a1a");
        FakeRenderable b = new FakeRenderable("b");

        Layer layer = BuildLayer(a, b);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[]
        {
            "DrawRenderable:a",
            "DrawRenderable:a1",
            "DrawRenderable:a1a",
            "DrawRenderable:a2",
            "DrawRenderable:b",
        });
    }

    [Fact]
    public void BuildDrawList_InvisibleRenderable_SkipsRenderableAndChildren()
    {
        FakeRenderable visible = new FakeRenderable("visible");
        FakeRenderable hidden = new FakeRenderable("hidden");
        hidden.Visible = false;
        AddChild(hidden, "hiddenChild");

        Layer layer = BuildLayer(visible, hidden);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[] { "DrawRenderable:visible" });
    }

    [Fact]
    public void BuildDrawList_IsRenderTargetNode_DoesNotRecurseIntoChildren()
    {
        FakeRenderable rt = new FakeRenderable("rt");
        rt.IsRenderTarget = true;
        AddChild(rt, "rtChild");

        Layer layer = BuildLayer(rt);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[] { "DrawRenderable:rt" });
    }

    [Fact]
    public void BuildDrawList_OverlappingDifferentKeys_PreservesRelativeOrder()
    {
        // sb1 and apos1 overlap at the same position. Even though they have different BatchKeys
        // the orderer must NOT reorder them past each other — overlap forces the relative DFS
        // order to be preserved.
        FakeRenderable sb1 = new FakeRenderable("sb1", "SpriteBatch") { X = 0, Y = 0, Width = 50, Height = 50 };
        FakeRenderable apos1 = new FakeRenderable("apos1", "Apos.Shapes") { X = 10, Y = 10, Width = 30, Height = 30 };
        FakeRenderable sb2 = new FakeRenderable("sb2", "SpriteBatch") { X = 200, Y = 0, Width = 10, Height = 10 };

        Layer layer = BuildLayer(sb1, apos1, sb2);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        // sb1 must come before apos1 (overlap). sb2 may move freely; it doesn't overlap apos1
        // (different position) so it can join sb1 in the SB run before apos1 is emitted.
        Describe(commands).ShouldBe(new[]
        {
            "DrawRenderable:sb1",
            "DrawRenderable:sb2",
            "DrawRenderable:apos1",
        });
    }

    [Fact]
    public void BuildDrawList_RenderUsingHierarchyFalse_DoesNotRecurse()
    {
        bool originalValue = Renderer.RenderUsingHierarchy;
        try
        {
            Renderer.RenderUsingHierarchy = false;

            FakeRenderable parent = new FakeRenderable("parent");
            AddChild(parent, "child");

            Layer layer = BuildLayer(parent);
            List<DrawCommand> commands = new List<DrawCommand>();

            BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

            Describe(commands).ShouldBe(new[] { "DrawRenderable:parent" });
        }
        finally
        {
            Renderer.RenderUsingHierarchy = originalValue;
        }
    }

    [Fact]
    public void BuildDrawList_RowsOfRectTextShape_GroupsAcrossRows()
    {
        // The canonical case the orderer exists to fix: rows of [rect, text, shape] where rect
        // and text are SpriteBatch, shape is Apos.Shapes. Within a row the rect overlaps the
        // text (text sits on the rect). The shape is positioned to the side and does not
        // overlap rect/text in this case. Rows do not overlap each other vertically. The
        // expected result is rect1,text1,rect2,text2,...,rectN,textN,shape1,...,shapeN — every
        // SpriteBatch draw before every shape draw, in DFS order within each group.
        const int RowCount = 4;
        FakeRenderable[] all = new FakeRenderable[RowCount * 3];
        for (int i = 0; i < RowCount; i++)
        {
            FakeRenderable rect = new FakeRenderable($"rect{i}", "SpriteBatch") { X = 0, Y = i * 40, Width = 100, Height = 30 };
            FakeRenderable text = new FakeRenderable($"text{i}", "SpriteBatch") { X = 10, Y = i * 40 + 5, Width = 80, Height = 20 };
            FakeRenderable shape = new FakeRenderable($"shape{i}", "Apos.Shapes") { X = 200, Y = i * 40, Width = 20, Height = 20 };
            all[i * 3] = rect;
            all[i * 3 + 1] = text;
            all[i * 3 + 2] = shape;
        }

        Layer layer = BuildLayer(all);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        List<string> expected = new List<string>();
        for (int i = 0; i < RowCount; i++)
        {
            expected.Add($"DrawRenderable:rect{i}");
            expected.Add($"DrawRenderable:text{i}");
        }
        for (int i = 0; i < RowCount; i++)
        {
            expected.Add($"DrawRenderable:shape{i}");
        }
        Describe(commands).ShouldBe(expected);
    }

    [Fact]
    public void BuildDrawList_SecondarySortOnY_OnlyReordersWithinSameYRun()
    {
        // SecondarySortOnY sorts the layer's renderables by Y; the orderer must respect that
        // partitioning. apos at Y=0 must NOT be pulled together with apos at Y=100 — they're
        // in different Y-runs.
        FakeRenderable sbTop = new FakeRenderable("sbTop", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable aposTop = new FakeRenderable("aposTop", "Apos.Shapes") { X = 50, Y = 0, Width = 10, Height = 10 };
        FakeRenderable sbBottom = new FakeRenderable("sbBottom", "SpriteBatch") { X = 0, Y = 100, Width = 10, Height = 10 };
        FakeRenderable aposBottom = new FakeRenderable("aposBottom", "Apos.Shapes") { X = 50, Y = 100, Width = 10, Height = 10 };

        Layer layer = new Layer();
        layer.SecondarySortOnY = true;
        // Add in an order that the layer's sort will preserve (Y already ascending).
        layer.Add(sbTop);
        layer.Add(aposTop);
        layer.Add(sbBottom);
        layer.Add(aposBottom);

        List<DrawCommand> commands = new List<DrawCommand>();
        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        // Top Y-run: sbTop, aposTop (reordered within run: SB first, Apos second — same as DFS
        // here since SB came first). Bottom Y-run: sbBottom, aposBottom. The two runs are NOT
        // merged.
        Describe(commands).ShouldBe(new[]
        {
            "DrawRenderable:sbTop",
            "DrawRenderable:aposTop",
            "DrawRenderable:sbBottom",
            "DrawRenderable:aposBottom",
        });
    }

    [Fact]
    public void BuildDrawList_ChildBoundsOutsideParent_FallsBackToParentBoundsForOverlap()
    {
        // Reproduces the scrollbar arrow icon bug: a child renderable whose computed bounds
        // sit outside its parent's bounds (because of rotation/origin/units the orderer
        // can't model directly). Without the parent-fallback, the icon has no precedence
        // edges to anything in its subtree — the topological sort's "stay on the current
        // batch key" tiebreaker then pulls it forward, ahead of the background it should
        // paint on top of. With the fallback, icon's effective bounds become the parent's,
        // restoring painter's order.
        //
        // Setup mirrors the real-world shape:
        //   - previousSpriteBatch establishes currentBucket=SpriteBatch.
        //   - buttonContainer is an empty-key parent (mimicking a ButtonInstance whose own
        //     visible draw is none — only its children paint).
        //   - background is the button's SB-keyed painted background.
        //   - icon's computed bounds sit far outside buttonContainer.
        FakeRenderable previousSpriteBatch = new FakeRenderable("previous", "SpriteBatch")
        {
            X = 0, Y = 0, Width = 100, Height = 30,
        };
        FakeRenderable buttonContainer = new FakeRenderable("buttonContainer", "")
        {
            X = 0, Y = 100, Width = 50, Height = 50,
        };
        FakeRenderable background = AddChild(buttonContainer, "background", "SpriteBatch");
        background.X = 0;
        background.Y = 0;
        background.Width = 50;
        background.Height = 50;
        FakeRenderable icon = AddChild(buttonContainer, "icon", "SpriteBatch");
        icon.X = 500;
        icon.Y = 500;
        icon.Width = 32;
        icon.Height = 32;

        Layer layer = BuildLayer(previousSpriteBatch, buttonContainer);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        List<string> result = Describe(commands);
        int backgroundIdx = result.IndexOf("DrawRenderable:background");
        int iconIdx = result.IndexOf("DrawRenderable:icon");
        backgroundIdx.ShouldBeLessThan(iconIdx);
    }

    [Fact]
    public void BuildDrawList_WithPreExistingCommands_ClearsDestinationFirst()
    {
        Layer layer = BuildLayer(new FakeRenderable("only"));
        List<DrawCommand> commands = new List<DrawCommand>();
        commands.Add(new DrawCommand(DrawCommandKind.DrawRenderable, new FakeRenderable("stale")));

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[] { "DrawRenderable:only" });
    }
}
