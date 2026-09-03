using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using MonoGameGum.TestsCommon;
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
        public object? BatchSortKey { get; set; }
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
    public void BuildDrawList_AlternatingBatchSortKeysWithinSameBatchKey_GroupsSameSortKeyTogether()
    {
        // Three non-overlapping rows, each a "frame" sprite and a "text" sprite - both
        // BatchKey="SpriteBatch" today, but different BatchSortKey (texture identity). Without
        // reorder DFS alternates frame,text,frame,text,frame,text. The orderer should pull
        // same-BatchSortKey items into one run each, exactly like it already does for BatchKey.
        object frameTexture = new object();
        object textTexture = new object();

        FakeRenderable frame1 = new FakeRenderable("frame1") { X = 0, Y = 0, Width = 10, Height = 10, BatchSortKey = frameTexture };
        FakeRenderable text1 = new FakeRenderable("text1") { X = 50, Y = 0, Width = 10, Height = 10, BatchSortKey = textTexture };
        FakeRenderable frame2 = new FakeRenderable("frame2") { X = 0, Y = 20, Width = 10, Height = 10, BatchSortKey = frameTexture };
        FakeRenderable text2 = new FakeRenderable("text2") { X = 50, Y = 20, Width = 10, Height = 10, BatchSortKey = textTexture };
        FakeRenderable frame3 = new FakeRenderable("frame3") { X = 0, Y = 40, Width = 10, Height = 10, BatchSortKey = frameTexture };
        FakeRenderable text3 = new FakeRenderable("text3") { X = 50, Y = 40, Width = 10, Height = 10, BatchSortKey = textTexture };

        Layer layer = BuildLayer(frame1, text1, frame2, text2, frame3, text3);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[]
        {
            "DrawRenderable:frame1",
            "DrawRenderable:frame2",
            "DrawRenderable:frame3",
            "DrawRenderable:text1",
            "DrawRenderable:text2",
            "DrawRenderable:text3",
        });
    }

    [Fact]
    public void BuildDrawList_RowsOfOverlappingFrameAndText_GroupsAcrossRowsByTexture()
    {
        // The actual stress-screen shape (#2697): the frame overlaps its own row's text (text
        // sits on top of the frame), so the precedence graph forces frame_i before text_i within
        // a row - unlike test above, where frame/text don't overlap at all. This pins that the
        // "stay on current bucket" tie-break still drains all frames before falling through to
        // text, because every frame is independently available (no cross-row dependency blocks
        // it), even though each row's text isn't available until that row's frame is chosen.
        const int RowCount = 4;
        object frameTexture = new object();
        object textTexture = new object();
        FakeRenderable[] all = new FakeRenderable[RowCount * 2];
        for (int i = 0; i < RowCount; i++)
        {
            FakeRenderable frame = new FakeRenderable($"frame{i}") { X = 0, Y = i * 40, Width = 100, Height = 30, BatchSortKey = frameTexture };
            FakeRenderable text = new FakeRenderable($"text{i}") { X = 10, Y = i * 40 + 5, Width = 80, Height = 20, BatchSortKey = textTexture };
            all[i * 2] = frame;
            all[i * 2 + 1] = text;
        }

        Layer layer = BuildLayer(all);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        List<string> expected = new List<string>();
        for (int i = 0; i < RowCount; i++)
        {
            expected.Add($"DrawRenderable:frame{i}");
        }
        for (int i = 0; i < RowCount; i++)
        {
            expected.Add($"DrawRenderable:text{i}");
        }
        Describe(commands).ShouldBe(expected);
    }

    // Allocation guard (#4200): unlike HierarchicalOrderer (#4190), this orderer's window Entry
    // lists and FlushWindow's precedence-graph scratch (indegree/successors/available) were
    // allocated fresh on every call. Exercises a top-level window, a clip subtree (a second,
    // nested window), and same-window overlap (a real precedence-graph edge, so `successors`
    // is actually populated) so every scratch structure the build touches is covered.
    [Fact]
    public void BuildDrawList_RowsWithClipSubtree_DoesNotAllocate()
    {
        const int RowCount = 3;
        FakeRenderable[] rows = new FakeRenderable[RowCount * 3];
        for (int i = 0; i < RowCount; i++)
        {
            FakeRenderable rect = new FakeRenderable($"rect{i}", "SpriteBatch") { X = 0, Y = i * 40, Width = 100, Height = 30 };
            FakeRenderable text = new FakeRenderable($"text{i}", "SpriteBatch") { X = 10, Y = i * 40 + 5, Width = 80, Height = 20 };
            FakeRenderable shape = new FakeRenderable($"shape{i}", "Apos.Shapes") { X = 200, Y = i * 40, Width = 20, Height = 20 };
            rows[i * 3] = rect;
            rows[i * 3 + 1] = text;
            rows[i * 3 + 2] = shape;
        }

        FakeRenderable clipParent = new FakeRenderable("clipParent", "SpriteBatch") { X = 300, Y = 0, Width = 50, Height = 50 };
        clipParent.ClipsChildren = true;
        FakeRenderable childInClip = AddChild(clipParent, "childInClip", "Apos.Shapes");
        childInClip.X = 5;
        childInClip.Y = 5;
        childInClip.Width = 10;
        childInClip.Height = 10;

        List<IRenderableIpso> topLevel = new List<IRenderableIpso>(rows) { clipParent };
        Layer layer = BuildLayer(topLevel.ToArray());
        List<DrawCommand> commands = new List<DrawCommand>();

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () => BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands),
            attempts: 3,
            warmupIterations: 50,
            measuredIterations: 500);

        // Liveness: a build that emitted nothing would trivially allocate nothing.
        Describe(commands).ShouldContain("DrawRenderable:rect0");
        Describe(commands).ShouldContain("DrawRenderable:childInClip");
        result.BytesPerIteration.ShouldBe(0);
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
    public void BuildDrawList_OverlapChain_RecordsMergeBlockedByOverlapAndNoCandidateBreaks()
    {
        // A chain: A(sb) overlaps B(apos), B(apos) overlaps C(sb), but A and C don't overlap each
        // other. Overlap forces the emit order A, B, C - C (sb) can't jump ahead of B to rejoin A's
        // run even though it shares A's key, so switching sb->apos is a real MergeBlockedByOverlap.
        // The following switch apos->sb has no remaining apos candidate at all - NoCandidateInWindow.
        // BuildDrawList no longer self-resets (issue #4575 follow-up: the tally now accumulates
        // until Renderer says a frame boundary passed, so it can span multiple Begin/End cycles).
        BatchKeyGroupedOrderer.Instance.ResetBreakTally();

        FakeRenderable a = new FakeRenderable("a", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable b = new FakeRenderable("b", "Apos.Shapes") { X = 5, Y = 0, Width = 10, Height = 10 };
        FakeRenderable c = new FakeRenderable("c", "SpriteBatch") { X = 12, Y = 0, Width = 10, Height = 10 };

        Layer layer = BuildLayer(a, b, c);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[]
        {
            "DrawRenderable:a",
            "DrawRenderable:b",
            "DrawRenderable:c",
        });
        BatchKeyGroupedOrderer.Instance.MergeBlockedByOverlapCount.ShouldBe(1);
        BatchKeyGroupedOrderer.Instance.NoCandidateInWindowBreakCount.ShouldBe(1);
    }

    [Fact]
    public void BuildDrawList_NonOverlappingAlternation_RecordsOnlyNoCandidateBreaks()
    {
        // Same non-overlapping scene as BuildDrawList_AlternatingBatchKeys_GroupsSameKeyTogether:
        // the orderer fully collapses to one sb run then one apos run, so the single break between
        // them is a genuine "nothing left to merge with" case, not an overlap block.
        BatchKeyGroupedOrderer.Instance.ResetBreakTally();

        FakeRenderable sb1 = new FakeRenderable("sb1", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable apos1 = new FakeRenderable("apos1", "Apos.Shapes") { X = 50, Y = 0, Width = 10, Height = 10 };
        FakeRenderable sb2 = new FakeRenderable("sb2", "SpriteBatch") { X = 0, Y = 20, Width = 10, Height = 10 };
        FakeRenderable apos2 = new FakeRenderable("apos2", "Apos.Shapes") { X = 50, Y = 20, Width = 10, Height = 10 };

        Layer layer = BuildLayer(sb1, apos1, sb2, apos2);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        BatchKeyGroupedOrderer.Instance.MergeBlockedByOverlapCount.ShouldBe(0);
        BatchKeyGroupedOrderer.Instance.NoCandidateInWindowBreakCount.ShouldBe(1);
    }

    [Fact]
    public void GetBreakGroups_RanksTheMostFrequentBreakFirst()
    {
        // Three identical, non-overlapping-with-each-other overlap chains (rows at Y=0,20,40),
        // each producing the same pair of breaks: sb->apos (blocked by overlap) and apos->sb (no
        // candidate). A fourth row uses distinct sb BatchSortKeys, so its breaks are separate,
        // less-frequent groups. The most frequent groups (count 3) must sort first.
        // SecondarySortOnY isolates each row into its own reorder window - without it the whole
        // layer is one window, and Kahn's "stay on the current key" tiebreaker drains every
        // available same-key item across ALL rows before switching, which mixes the rows' breaks
        // together instead of keeping each row's chain independent.
        BatchKeyGroupedOrderer.Instance.ResetBreakTally();

        Layer layer = BuildLayer();
        layer.SecondarySortOnY = true;
        for (int row = 0; row < 3; row++)
        {
            float y = row * 20;
            layer.Add(new FakeRenderable($"a{row}", "SpriteBatch") { X = 0, Y = y, Width = 10, Height = 10 });
            layer.Add(new FakeRenderable($"b{row}", "Apos.Shapes") { X = 5, Y = y, Width = 10, Height = 10 });
            layer.Add(new FakeRenderable($"c{row}", "SpriteBatch") { X = 12, Y = y, Width = 10, Height = 10 });
        }
        // Both d and f need a BatchSortKey distinct from the rows-0-2 default (null) - otherwise
        // f's break would merge into rows-0-2's apos->sb group (they'd share the same "to" key).
        layer.Add(new FakeRenderable("d", "SpriteBatch") { X = 0, Y = 100, Width = 10, Height = 10, BatchSortKey = new object() });
        layer.Add(new FakeRenderable("e", "Apos.Shapes") { X = 5, Y = 100, Width = 10, Height = 10 });
        layer.Add(new FakeRenderable("f", "SpriteBatch") { X = 12, Y = 100, Width = 10, Height = 10, BatchSortKey = new object() });

        List<DrawCommand> commands = new List<DrawCommand>();
        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        var groups = BatchKeyGroupedOrderer.Instance.GetBreakGroups();

        groups[0].Count.ShouldBe(3);
        // sb->apos (blocked), apos->sb (no candidate), and rows 0-2's shared NoPredecessor break
        // (each row's own "a{row}" starts its own Y-run with nothing before it, but all three
        // share the same (nothing) -> (SpriteBatch, null) identity, so they merge into one group).
        groups.Count(g => g.Count == 3).ShouldBe(3);
        // the distinct-BatchSortKey row's own pair, plus its own NoPredecessor break ("d" has a
        // distinct BatchSortKey, so it doesn't merge with rows 0-2's).
        groups.Count(g => g.Count == 1).ShouldBe(3);
    }

    [Fact]
    public void BuildDrawList_CalledTwiceWithoutReset_AccumulatesAcrossBothCalls()
    {
        // Renderer calls ResetBreakTally() at frame boundaries, not on every BuildDrawList call,
        // so multiple cycles in one host frame (FRB2's per-camera + overlay shape) accumulate into
        // one frame's total instead of the second cycle wiping out the first's tally.
        BatchKeyGroupedOrderer.Instance.ResetBreakTally();

        FakeRenderable a = new FakeRenderable("a", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable b = new FakeRenderable("b", "Apos.Shapes") { X = 5, Y = 0, Width = 10, Height = 10 };
        Layer layer = BuildLayer(a, b);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);
        BatchKeyGroupedOrderer.Instance.NoCandidateInWindowBreakCount.ShouldBe(1);

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);
        BatchKeyGroupedOrderer.Instance.NoCandidateInWindowBreakCount.ShouldBe(2);
        var groups = BatchKeyGroupedOrderer.Instance.GetBreakGroups();
        groups.Single(g => g.Reason == BatchKeyGroupedOrderer.BreakReason.NoCandidateInWindow).Count.ShouldBe(2);
        groups.Single(g => g.Reason == BatchKeyGroupedOrderer.BreakReason.NoPredecessor).Count.ShouldBe(2); // a starts each cycle with nothing before it
    }

    [Fact]
    public void GetBreakGroupsByType_FormatsAsShortTypeArrowWithCount()
    {
        BatchKeyGroupedOrderer.Instance.ResetBreakTally();

        FakeRenderable a = new FakeRenderable("a", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable b = new FakeRenderable("b", "Apos.Shapes") { X = 5, Y = 0, Width = 10, Height = 10 };
        Layer layer = BuildLayer(a, b);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        var typeGroups = BatchKeyGroupedOrderer.Instance.GetBreakGroupsByType();

        // Two entries: a's own NoPredecessor break (nothing preceded it) and the a->b switch itself.
        typeGroups.Count.ShouldBe(2);
        typeGroups.Single(g => g.FromRenderableType == typeof(FakeRenderable)).ToString().ShouldBe("FakeRenderable->FakeRenderable (1)");
    }

    [Fact]
    public void BuildDrawList_PlainContainerBetweenSameKeyItems_IsNotCountedAsABreak()
    {
        // A plain ContainerRuntime (empty BatchKey, not a render target or clip) is a complete
        // no-op at the real GPU level - BatchOrchestrator.OnRenderable treats empty BatchKey as
        // "participates in whatever batch the previous renderable established" (no flush), and
        // InvisibleRenderable.Render submits no vertices. So a Container physically interposed
        // between two same-key items (via the overlap chain A-container-B below) must not count
        // as a break, and must not stop the orderer from recognizing A and B as one contiguous
        // (sb,null) run once B unblocks.
        FakeRenderable a = new FakeRenderable("a", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable container = new FakeRenderable("container", "") { X = 5, Y = 0, Width = 10, Height = 10 };
        FakeRenderable b = new FakeRenderable("b", "SpriteBatch") { X = 12, Y = 0, Width = 10, Height = 10 };
        FakeRenderable c = new FakeRenderable("c", "Apos.Shapes") { X = 100, Y = 0, Width = 10, Height = 10 };

        BatchKeyGroupedOrderer.Instance.ResetBreakTally();
        Layer layer = BuildLayer(a, container, b, c);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        BatchKeyGroupedOrderer.Instance.MergeBlockedByOverlapCount.ShouldBe(0);
        BatchKeyGroupedOrderer.Instance.NoCandidateInWindowBreakCount.ShouldBe(1); // only b->c
        var groups = BatchKeyGroupedOrderer.Instance.GetBreakGroups();
        groups.Count.ShouldBe(2); // a's own NoPredecessor break, plus b->c
        var noCandidate = groups.Single(g => g.Reason == BatchKeyGroupedOrderer.BreakReason.NoCandidateInWindow);
        noCandidate.FromBatchKey.ShouldBe("SpriteBatch");
        noCandidate.ToBatchKey.ShouldBe("Apos.Shapes");
        groups.Single(g => g.Reason == BatchKeyGroupedOrderer.BreakReason.NoPredecessor).ToBatchKey.ShouldBe("SpriteBatch"); // a
    }

    [Fact]
    public void BuildDrawList_RenderTargetBetweenSameKeyItems_IsCountedAsABreak()
    {
        // Unlike a plain Container, a render target IS a real cost even though it also reports an
        // empty BatchKey: SubmitDrawRenderable/DrawRenderTargetToScreen give it its own
        // FlushAndReset + BeginSpriteBatch(effectOverride:) cycle. So (unlike the plain-container
        // test above) both the entry into it and the exit out of it must count as real breaks.
        FakeRenderable a = new FakeRenderable("a", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable renderTarget = new FakeRenderable("renderTarget", "")
        {
            X = 5, Y = 0, Width = 10, Height = 10, IsRenderTarget = true,
        };
        FakeRenderable b = new FakeRenderable("b", "SpriteBatch") { X = 12, Y = 0, Width = 10, Height = 10 };
        FakeRenderable c = new FakeRenderable("c", "Apos.Shapes") { X = 100, Y = 0, Width = 10, Height = 10 };

        BatchKeyGroupedOrderer.Instance.ResetBreakTally();
        Layer layer = BuildLayer(a, renderTarget, b, c);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        BatchKeyGroupedOrderer.Instance.HardBoundaryTransitionCount.ShouldBe(2); // a->renderTarget, renderTarget->b
        BatchKeyGroupedOrderer.Instance.MergeBlockedByOverlapCount.ShouldBe(0);
        BatchKeyGroupedOrderer.Instance.NoCandidateInWindowBreakCount.ShouldBe(1); // b->c
        BatchKeyGroupedOrderer.Instance.GetBreakGroups().Count.ShouldBe(4); // + a's own NoPredecessor break
    }

    [Fact]
    public void BuildDrawList_ClipInsideOneRow_DoesNotLeakRunningKeyIntoTheNextRow()
    {
        // Regression pin: the running-key state that must persist ACROSS a clip boundary (so
        // entering/exiting it is counted correctly, see the two clip tests below) must NOT persist
        // across a SecondarySortOnY row boundary - each row still has to reorder independently.
        // sbTop (row Y=0) ends the row on a "SpriteBatch" key by clipping; aposBottom (row Y=20)
        // must not see that leak into its own running state.
        FakeRenderable sbTop = new FakeRenderable("sbTop", "SpriteBatch") { Y = 0, Width = 10, Height = 10 };
        FakeRenderable clipTop = new FakeRenderable("clipTop", "SpriteBatch")
        {
            X = 50, Y = 0, Width = 10, Height = 10, ClipsChildren = true,
        };
        FakeRenderable aposBottom = new FakeRenderable("aposBottom", "Apos.Shapes") { Y = 20, Width = 10, Height = 10 };

        BatchKeyGroupedOrderer.Instance.ResetBreakTally();
        Layer layer = new Layer();
        layer.SecondarySortOnY = true;
        layer.Add(sbTop);
        layer.Add(clipTop);
        layer.Add(aposBottom);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        // aposBottom is the first (and only) item in its own row - if the fix regressed, it would
        // incorrectly compare against clipTop's "SpriteBatch" key instead of starting fresh.
        BatchKeyGroupedOrderer.Instance.HardBoundaryTransitionCount.ShouldBe(1); // sbTop -> clipTop only
        BatchKeyGroupedOrderer.Instance.MergeBlockedByOverlapCount.ShouldBe(0);
        BatchKeyGroupedOrderer.Instance.NoCandidateInWindowBreakCount.ShouldBe(0);
    }

    [Fact]
    public void BuildDrawList_EnteringAClip_IsCountedEvenWhenItsOwnKeyMatchesTheRunningKey()
    {
        // The bug this pins: entering a ClipsChildren renderable ALWAYS forces a real flush
        // (Renderer.AdjustRenderStates restarts SpriteBatch on any clip change), regardless of
        // whether the clip's own BatchKey happens to equal whatever was running. Before this fix,
        // clip transitions never reached the break-comparison at all (a clip is always the first
        // item of a freshly-flushed inner window, and "first emission" is unconditionally
        // skipped) - so a same-key clip like this one would have been invisible to every counter.
        FakeRenderable a = new FakeRenderable("a", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable clip = new FakeRenderable("clip", "SpriteBatch")
        {
            X = 50, Y = 0, Width = 10, Height = 10, ClipsChildren = true,
        };
        AddChild(clip, "child", "Apos.Shapes");

        BatchKeyGroupedOrderer.Instance.ResetBreakTally();
        Layer layer = BuildLayer(a, clip);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        BatchKeyGroupedOrderer.Instance.HardBoundaryTransitionCount.ShouldBe(1); // a -> clip, same key
        BatchKeyGroupedOrderer.Instance.MergeBlockedByOverlapCount.ShouldBe(0);
        BatchKeyGroupedOrderer.Instance.NoCandidateInWindowBreakCount.ShouldBe(1); // clip -> child (apos)
    }

    [Fact]
    public void BuildDrawList_ExitingAClip_ForcesTheNextItemEvenWhenItsKeyMatches()
    {
        // The other half of the same bug: Renderer.Draw's didClipChange exit branch ALSO forces a
        // flush when leaving a clip - so whatever comes next must count as a break too, even if
        // its key happens to match whatever was running inside the clip.
        FakeRenderable clip = new FakeRenderable("clip", "SpriteBatch")
        {
            X = 0, Y = 0, Width = 10, Height = 10, ClipsChildren = true,
        };
        FakeRenderable b = new FakeRenderable("b", "SpriteBatch") { X = 50, Y = 0, Width = 10, Height = 10 };

        BatchKeyGroupedOrderer.Instance.ResetBreakTally();
        Layer layer = BuildLayer(clip, b);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        BatchKeyGroupedOrderer.Instance.HardBoundaryTransitionCount.ShouldBe(1); // clip -> b, same key
        BatchKeyGroupedOrderer.Instance.MergeBlockedByOverlapCount.ShouldBe(0);
        BatchKeyGroupedOrderer.Instance.NoCandidateInWindowBreakCount.ShouldBe(0);
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
    public void BuildDrawList_Roots_CullsOffscreenSubtreeUsingSuppliedCullTestBoundsMapping()
    {
        // getScissorRectangle deliberately returns an IN-clip rectangle for "cull" -- if the
        // orderer used it (instead of getCullTestBounds) for the cull decision, "cull" would
        // wrongly survive. getCullTestBounds returns the true (far outside the margin) rectangle,
        // proving the subtree overload's cull decision is driven by getCullTestBounds, mirroring
        // the Layer overload's GetCullTestBoundsFor/GetScissorRectangleFor split (#4144, #4154).
        FakeRenderable clipContainer = new FakeRenderable("clipContainer");
        clipContainer.ClipsChildren = true;
        FakeRenderable keep = AddChild(clipContainer, "keep");
        FakeRenderable cull = AddChild(clipContainer, "cull");

        Rectangle clipRect = new Rectangle(0, 0, 100, 100);
        Rectangle insideClip = new Rectangle(10, 10, 20, 20);
        Rectangle farOutside = new Rectangle(1000, 1000, 10, 10);

        Rectangle GetScissorRectangle(IRenderableIpso r) => r == clipContainer ? clipRect : insideClip;
        Rectangle GetCullTestBounds(IRenderableIpso r) => r == cull ? farOutside : GetScissorRectangle(r);

        List<DrawCommand> commands = new List<DrawCommand>();
        BatchKeyGroupedOrderer.Instance.BuildDrawList(
            new List<IRenderableIpso> { clipContainer },
            commands,
            new ClipBoundsSource(GetScissorRectangle, GetCullTestBounds));

        Describe(commands).ShouldBe(new[]
        {
            "BeginClip:clipContainer",
            "DrawRenderable:clipContainer",
            "DrawRenderable:keep",
            "EndClip:clipContainer",
        });
    }

    [Fact]
    public void BuildDrawList_Roots_DepthFirstWalk_MatchesLayerOverload()
    {
        // Same BatchKey throughout so there is nothing to reorder -- isolates the roots-vs-layer
        // entry point parity from the batch-grouping behavior covered elsewhere in this class.
        FakeRenderable a = new FakeRenderable("a");
        FakeRenderable a1 = AddChild(a, "a1");
        AddChild(a1, "a1a");
        FakeRenderable b = new FakeRenderable("b");

        Layer layer = BuildLayer(a, b);
        List<DrawCommand> commandsFromLayer = new List<DrawCommand>();
        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commandsFromLayer);

        List<DrawCommand> commandsFromRoots = new List<DrawCommand>();
        BatchKeyGroupedOrderer.Instance.BuildDrawList(new List<IRenderableIpso> { a, b }, commandsFromRoots);

        Describe(commandsFromRoots).ShouldBe(Describe(commandsFromLayer));
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
    public void BuildDrawList_FirstItemInAWindow_IsRecordedAsANoPredecessorBreak()
    {
        // The very first item of a window has nothing to break from, but it is still a real,
        // separately-submitted GPU batch - so it must show up in GetBreakGroups() too, or a
        // caller summing group.Count to reconcile against DrawCallCount silently undercounts by
        // one per window (the confusion this test exists to prevent).
        BatchKeyGroupedOrderer.Instance.ResetBreakTally();

        FakeRenderable sb1 = new FakeRenderable("sb1", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        FakeRenderable apos1 = new FakeRenderable("apos1", "Apos.Shapes") { X = 50, Y = 0, Width = 10, Height = 10 };

        Layer layer = BuildLayer(sb1, apos1);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        var groups = BatchKeyGroupedOrderer.Instance.GetBreakGroups();
        groups.Count.ShouldBe(2);

        var noPredecessor = groups.Single(g => g.Reason == BatchKeyGroupedOrderer.BreakReason.NoPredecessor);
        noPredecessor.FromRenderableType.ShouldBe(typeof(BatchKeyGroupedOrderer.NoPredecessorMarker));
        noPredecessor.ToRenderableType.ShouldBe(typeof(FakeRenderable));
        noPredecessor.ToBatchKey.ShouldBe("SpriteBatch");
        noPredecessor.Count.ShouldBe(1);

        // Total draws this window = 2 (sb1, apos1). Every draw must now be attributable to
        // exactly one break group entry (NoPredecessor for the first, NoCandidateInWindow for the
        // switch to apos1) - the reconciliation the diagnostic exists to provide.
        groups.Sum(g => g.Count).ShouldBe(2);
    }

    [Fact]
    public void BuildDrawList_CalledTwiceWithoutReset_SumOfBreakGroupCountsMatchesTotalDrawsAcrossBothCycles()
    {
        // Mirrors FRB2's real per-camera + overlay shape: BuildDrawList runs once per cycle, and
        // Renderer only resets the break tally once per host frame - not per cycle. Each cycle's
        // own first item is a fresh NoPredecessor break, so the two cycles' single-item draws (2
        // total) must sum to 2 in GetBreakGroups(), not silently undercount to 0.
        BatchKeyGroupedOrderer.Instance.ResetBreakTally();

        FakeRenderable a = new FakeRenderable("a", "SpriteBatch") { X = 0, Y = 0, Width = 10, Height = 10 };
        Layer layer = BuildLayer(a);
        List<DrawCommand> commands = new List<DrawCommand>();

        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands); // cycle 1
        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands); // cycle 2

        var groups = BatchKeyGroupedOrderer.Instance.GetBreakGroups();
        var noPredecessor = groups.Single(g => g.Reason == BatchKeyGroupedOrderer.BreakReason.NoPredecessor);
        noPredecessor.Count.ShouldBe(2);
        groups.Sum(g => g.Count).ShouldBe(2); // 2 cycles x 1 draw each
    }

    [Fact]
    public void BuildDrawList_TwoNestedNonOverlappingCardGroups_DoesNotMergeAcrossContainers()
    {
        // Known limitation - see issue #4579. Two card-shaped groups (2x same-key "rectangle", 2x
        // same-key "text", 1x "icon" each), each wrapped in its own container (mirroring a real
        // Gum component instance), placed with zero bounding-box overlap between containers.
        // Coordinates are the ACTUAL bounds measured from a live 2-card TestScreenFrb run
        // (Solitaire sample, FlatRedBall2 repo) - not assumed ones.
        //
        // A flat (non-nested) version of this same scene merges perfectly (0 extra breaks per
        // additional group) - the orderer CAN merge same-key items across non-overlapping groups.
        // But once each group is wrapped in its own container, nothing merges across containers at
        // all: the container itself never matches the running batch key (it's a transparent
        // wrapper), so it's only reached via the "smallest DFS index" fallback tier - by which
        // point the algorithm has already drained the first card's non-matching items instead of
        // reaching into the second card's container while a same-key run was still active. This
        // pins the CURRENT (suboptimal) behavior so a future fix has a red-then-green target.
        BatchKeyGroupedOrderer.Instance.ResetBreakTally();
        object fontTex = new object();
        object iconTex = new object();

        Layer layer = BuildLayer();
        for (int card = 0; card < 2; card++)
        {
            float dx = card * 100; // card0 Entity.X=0, card1 Entity.X=100 (measured)
            // Real cards are NESTED: each CardGum.Visual is its own container (empty BatchKey,
            // full card bounds), and Background/Border/RankText1/RankText2/SuitIcon are its
            // CHILDREN - not flat top-level siblings like the previous version of this test
            // assumed. Testing whether that nesting (not bounds/keys, both already proven fine)
            // is what defeats cross-card merging.
            FakeRenderable cardContainer = new FakeRenderable($"card{card}", "") { X = 360 + dx, Y = 264, Width = 80, Height = 112 };
            layer.Add(cardContainer);

            FakeRenderable bg = AddChild(cardContainer, $"bg{card}", "Apos.Shapes");
            bg.X = 360 + dx; bg.Y = 264; bg.Width = 80; bg.Height = 112;

            FakeRenderable border = AddChild(cardContainer, $"border{card}", "Apos.Shapes");
            border.X = 360 + dx; border.Y = 264; border.Width = 80; border.Height = 112;

            FakeRenderable rank1 = AddChild(cardContainer, $"rank1_{card}");
            rank1.BatchSortKey = fontTex; rank1.X = 369 + dx; rank1.Y = 268; rank1.Width = 14; rank1.Height = 30;

            FakeRenderable rank2 = AddChild(cardContainer, $"rank2_{card}");
            rank2.BatchSortKey = fontTex; rank2.X = 369 + dx; rank2.Y = 268; rank2.Width = 14; rank2.Height = 30;

            FakeRenderable icon = AddChild(cardContainer, $"icon{card}");
            icon.BatchSortKey = iconTex; icon.X = 374 + dx; icon.Y = 294; icon.Width = 52; icon.Height = 52;
        }

        List<DrawCommand> commands = new List<DrawCommand>();
        BatchKeyGroupedOrderer.Instance.BuildDrawList(layer, commands);

        // Fully sequential per container - bg1/border1 never get pulled forward to merge with
        // bg0/border0, even though nothing overlaps and their keys match exactly.
        Describe(commands).ShouldBe(new[]
        {
            "DrawRenderable:card0",
            "DrawRenderable:bg0",
            "DrawRenderable:border0",
            "DrawRenderable:rank1_0",
            "DrawRenderable:rank2_0",
            "DrawRenderable:icon0",
            "DrawRenderable:card1",
            "DrawRenderable:bg1",
            "DrawRenderable:border1",
            "DrawRenderable:rank1_1",
            "DrawRenderable:rank2_1",
            "DrawRenderable:icon1",
        });
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
