using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Graphics;

/// <summary>
/// <see cref="IRenderableOrderer"/> that reorders DFS draws within layer- and clip-bounded
/// windows so that runs of same-key draws become contiguous, keyed first by
/// <see cref="IRenderable.BatchKey"/> and, within that, by the finer
/// <see cref="IRenderable.BatchSortKey"/> (e.g. a Texture2D reference). Pixel-correct: any two
/// draws whose <see cref="IPositionedSizedObjectExtensionMethods.GetAbsoluteBounds"/> intersect
/// maintain their original DFS-relative order, so the painter's algorithm result is unchanged.
/// Opt in by setting <c>Renderer.SiblingOrdering = BatchKeyGroupedOrderer.Instance</c>.
/// </summary>
/// <remarks>
/// Two motivating cases. Cross-batch-type: a list whose items mix <c>SpriteBatch</c>-using and
/// <c>Apos.Shapes</c>-using renderables — in DFS order the renderer flushes on every batch-type
/// alternation, so BatchKey grouping alone collapses flushes from ~one-per-item to
/// one-per-distinct-key. Same-batch-type, different texture (#2697): a StackPanel mixing frame
/// images and text all report the same BatchKey ("SpriteBatch"), so BatchOrchestrator never
/// transitions between them, but SpriteBatch's own consecutive-same-texture batching still can't
/// merge non-adjacent same-texture draws — BatchSortKey grouping is what collapses that case.
/// </remarks>
public sealed class BatchKeyGroupedOrderer : IRenderableOrderer
{
    /// <summary>Shared stateless instance.</summary>
    public static readonly BatchKeyGroupedOrderer Instance = new BatchKeyGroupedOrderer();

    /// <summary>
    /// Number of times <see cref="FlushWindow"/> had to switch away from the running
    /// (<see cref="IRenderable.BatchKey"/>, <see cref="IRenderable.BatchSortKey"/>) even though
    /// another item still in the window shared it - the switch happened because overlap forced
    /// that item to wait behind something else, not because reordering couldn't have merged them.
    /// Accumulates across every <see cref="BuildDrawList(Layer, List{DrawCommand}, Camera?)"/> call
    /// until <see cref="ResetBreakTally"/> runs - <see cref="Renderer"/> calls that once per host
    /// frame (immediate-mode) or once per <c>Draw(SystemManagers)</c> call (layered), matching
    /// <see cref="RenderStateChangeStatistics"/>'s own reset cadence on each path. A caller driving
    /// this orderer directly (outside <see cref="Renderer"/>, e.g. a unit test) must call
    /// <see cref="ResetBreakTally"/> itself between builds it wants measured independently
    /// (issue #4575).
    /// </summary>
    public int MergeBlockedByOverlapCount { get; private set; }

    /// <summary>
    /// Number of times <see cref="FlushWindow"/> had to switch away from the running
    /// (<see cref="IRenderable.BatchKey"/>, <see cref="IRenderable.BatchSortKey"/>) because no
    /// remaining window entry shared it at all - genuine content alternation (or the window simply
    /// ran out), not something reordering could have fixed. Same reset cadence as
    /// <see cref="MergeBlockedByOverlapCount"/> (issue #4575).
    /// </summary>
    public int NoCandidateInWindowBreakCount { get; private set; }

    /// <summary>
    /// Number of times entering or exiting a <see cref="IRenderableIpso.ClipsChildren"/> renderable
    /// or a <see cref="IRenderableIpso.IsRenderTarget"/> renderable forced a real flush -
    /// unconditionally, regardless of whether the surrounding <see cref="IRenderable.BatchKey"/>/
    /// <see cref="IRenderable.BatchSortKey"/> happened to match. <see cref="Renderer.AdjustRenderStates"/>
    /// restarts <c>SpriteBatch</c> on every clip change, and <c>SubmitDrawRenderable</c>/
    /// <c>DrawRenderTargetToScreen</c> give a render target its own flush + bind/restore cycle - no
    /// amount of reordering can avoid either, so these are never <see cref="MergeBlockedByOverlapCount"/>
    /// candidates. Same reset cadence as <see cref="MergeBlockedByOverlapCount"/> (issue #4575
    /// follow-up - the first cut of this diagnostic missed these entirely, since clip/render-target
    /// boundaries don't flow through the normal same-window key comparison).
    /// </summary>
    public int HardBoundaryTransitionCount { get; private set; }

    /// <summary>Why a <see cref="BatchBreakGroup"/> break happened.</summary>
    public enum BreakReason
    {
        /// <summary>A same-key item was still pending, blocked by an overlap edge.</summary>
        BlockedByOverlap,

        /// <summary>Nothing pending shared the outgoing key at all.</summary>
        NoCandidateInWindow,

        /// <summary>
        /// A clip or render-target boundary forced this transition unconditionally - see
        /// <see cref="HardBoundaryTransitionCount"/>.
        /// </summary>
        HardBoundary,

        /// <summary>
        /// The first non-transparent item this build has emitted with nothing running yet to
        /// compare it against - no reordering choice was even possible, but it is still a real,
        /// separately-submitted draw call. Fires at the start of a top-level
        /// <see cref="BuildDrawList(Layer, List{DrawCommand}, Camera?)"/> call, a
        /// <see cref="Layer.SecondarySortOnY"/> row, or a clip subtree, whichever comes first -
        /// there is no single structural concept ("layer", "cycle") this lines up with, only "no
        /// predecessor existed yet." Without this reason a caller summing
        /// <see cref="BatchBreakGroup.Count"/> across <see cref="GetBreakGroups"/> to reconcile
        /// against the frame's total draw-call count would silently undercount by one per reset.
        /// </summary>
        NoPredecessor,
    }

    /// <summary>
    /// Sentinel <see cref="BatchBreakGroup.FromRenderableType"/>/<see cref="BatchBreakTypeGroup.FromRenderableType"/>
    /// used for a <see cref="BreakReason.NoPredecessor"/> break, whose "from" side has no real
    /// predecessor renderable. Named distinctly from <see cref="BreakReason.NoPredecessor"/> itself
    /// to avoid a same-named type/enum-value pair in this class's public surface.
    /// </summary>
    public sealed class NoPredecessorMarker
    {
        private NoPredecessorMarker() { }
    }

    /// <summary>
    /// One distinct kind of batch break and how many times it happened since the last
    /// <see cref="ResetBreakTally"/> - e.g. "46 times: SpriteRuntime (SpriteBatch/&lt;texture
    /// A&gt;) -&gt; RectangleRuntime (Apos.Shapes), blocked by overlap".
    /// <see cref="FromSortKey"/>/<see cref="ToSortKey"/> are raw <see cref="IRenderable.BatchSortKey"/>
    /// values (e.g. a <c>Texture2D</c> reference) - Gum core doesn't know the backend type, so a
    /// caller that does (FRB2, a sample) formats them (e.g. <c>((Texture2D)key)?.Name</c>) rather
    /// than Gum guessing a generic description. See <see cref="BatchBreakTypeGroup"/> for a
    /// coarser, renderable-type-only rollup that needs no such formatting.
    /// </summary>
    public readonly struct BatchBreakGroup
    {
        public string FromBatchKey { get; init; }
        public object? FromSortKey { get; init; }
        public Type FromRenderableType { get; init; }
        public string ToBatchKey { get; init; }
        public object? ToSortKey { get; init; }
        public Type ToRenderableType { get; init; }
        public BreakReason Reason { get; init; }
        public int Count { get; init; }
    }

    /// <summary>
    /// A break rollup keyed only by the two renderable types involved - drops <c>BatchKey</c>,
    /// <c>BatchSortKey</c>, and the overlap/no-candidate distinction that <see cref="BatchBreakGroup"/>
    /// carries. This is the "what's alternating, at a glance" view: <c>ToString()</c> prints e.g.
    /// <c>"Sprite-&gt;RoundedRectangle (14)"</c>, trimming the conventional "Runtime" suffix, ready
    /// to print with no caller-side formatting or aggregation.
    /// </summary>
    public readonly struct BatchBreakTypeGroup
    {
        public Type FromRenderableType { get; init; }
        public Type ToRenderableType { get; init; }
        public int Count { get; init; }

        public override string ToString() =>
            $"{TrimRuntimeSuffix(FromRenderableType.Name)}->{TrimRuntimeSuffix(ToRenderableType.Name)} ({Count})";

        private static string TrimRuntimeSuffix(string typeName) =>
            typeName.EndsWith("Runtime", StringComparison.Ordinal)
                ? typeName.Substring(0, typeName.Length - "Runtime".Length)
                : typeName;
    }

    private readonly struct TypeGroupKey : IEquatable<TypeGroupKey>
    {
        public readonly Type FromRenderableType;
        public readonly Type ToRenderableType;

        public TypeGroupKey(Type fromRenderableType, Type toRenderableType)
        {
            FromRenderableType = fromRenderableType;
            ToRenderableType = toRenderableType;
        }

        public bool Equals(TypeGroupKey other) =>
            FromRenderableType == other.FromRenderableType && ToRenderableType == other.ToRenderableType;

        public override bool Equals(object? obj) => obj is TypeGroupKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(FromRenderableType, ToRenderableType);
    }

    private readonly Dictionary<TypeGroupKey, int> _breakTypeGroupCounts = new();

    /// <summary>
    /// Every distinct (fromType, toType) break pair since the last <see cref="ResetBreakTally"/>,
    /// most-frequent first. Coarser than <see cref="GetBreakGroups"/> - use this for "what's
    /// alternating" at a glance, and <see cref="GetBreakGroups"/> when you need to know which
    /// specific texture/key is involved (issue #4575).
    /// </summary>
    public IReadOnlyList<BatchBreakTypeGroup> GetBreakGroupsByType()
    {
        List<BatchBreakTypeGroup> result = new(_breakTypeGroupCounts.Count);
        foreach (KeyValuePair<TypeGroupKey, int> pair in _breakTypeGroupCounts)
        {
            result.Add(new BatchBreakTypeGroup
            {
                FromRenderableType = pair.Key.FromRenderableType,
                ToRenderableType = pair.Key.ToRenderableType,
                Count = pair.Value,
            });
        }
        result.Sort((a, b) => b.Count.CompareTo(a.Count));
        return result;
    }

    /// <summary>
    /// Clears <see cref="MergeBlockedByOverlapCount"/>, <see cref="NoCandidateInWindowBreakCount"/>,
    /// and every tally behind <see cref="GetBreakGroups"/>/<see cref="GetBreakGroupsByType"/>.
    /// <see cref="Renderer"/> calls this at the same points it resets
    /// <see cref="RenderStateChangeStatistics"/>; call it yourself if you drive this orderer
    /// directly (issue #4575).
    /// </summary>
    public void ResetBreakTally()
    {
        MergeBlockedByOverlapCount = 0;
        NoCandidateInWindowBreakCount = 0;
        HardBoundaryTransitionCount = 0;
        _breakGroupCounts.Clear();
        _breakTypeGroupCounts.Clear();
    }

    private readonly struct BreakGroupKey : IEquatable<BreakGroupKey>
    {
        public readonly string FromBatchKey;
        public readonly object? FromSortKey;
        public readonly Type FromRenderableType;
        public readonly string ToBatchKey;
        public readonly object? ToSortKey;
        public readonly Type ToRenderableType;
        public readonly BreakReason Reason;

        public BreakGroupKey(string fromBatchKey, object? fromSortKey, Type fromRenderableType,
            string toBatchKey, object? toSortKey, Type toRenderableType, BreakReason reason)
        {
            FromBatchKey = fromBatchKey;
            FromSortKey = fromSortKey;
            FromRenderableType = fromRenderableType;
            ToBatchKey = toBatchKey;
            ToSortKey = toSortKey;
            ToRenderableType = toRenderableType;
            Reason = reason;
        }

        public bool Equals(BreakGroupKey other) =>
            FromBatchKey == other.FromBatchKey && Equals(FromSortKey, other.FromSortKey) &&
            FromRenderableType == other.FromRenderableType && ToBatchKey == other.ToBatchKey &&
            Equals(ToSortKey, other.ToSortKey) && ToRenderableType == other.ToRenderableType &&
            Reason == other.Reason;

        public override bool Equals(object? obj) => obj is BreakGroupKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            FromBatchKey, FromSortKey, FromRenderableType, ToBatchKey, ToSortKey, ToRenderableType, Reason);
    }

    private readonly Dictionary<BreakGroupKey, int> _breakGroupCounts = new();

    /// <summary>Placeholder "from" batch key for a <see cref="BreakReason.NoPredecessor"/> break.</summary>
    private const string NoPredecessorBatchKey = "(none)";

    /// <summary>Placeholder "from" renderable type for a <see cref="BreakReason.NoPredecessor"/> break.</summary>
    private static readonly Type NoPredecessorType = typeof(NoPredecessorMarker);

    /// <summary>
    /// Tallies one break into both <see cref="_breakGroupCounts"/> (identity-level) and
    /// <see cref="_breakTypeGroupCounts"/> (type-level rollup) - shared by every call site that
    /// records a break (<see cref="FlushWindow"/>'s per-item loop and
    /// <see cref="RecordHardBoundaryTransition"/>) so both dictionaries stay in sync.
    /// </summary>
    private void RecordBreak(
        string fromBatchKey, object? fromSortKey, Type fromRenderableType,
        string toBatchKey, object? toSortKey, Type toRenderableType, BreakReason reason)
    {
        BreakGroupKey groupKey = new(
            fromBatchKey, fromSortKey, fromRenderableType, toBatchKey, toSortKey, toRenderableType, reason);
        _breakGroupCounts[groupKey] = _breakGroupCounts.TryGetValue(groupKey, out int existing) ? existing + 1 : 1;

        TypeGroupKey typeKey = new(fromRenderableType, toRenderableType);
        _breakTypeGroupCounts[typeKey] = _breakTypeGroupCounts.TryGetValue(typeKey, out int existingTypeCount) ? existingTypeCount + 1 : 1;
    }

    /// <summary>
    /// Every distinct break this build produced, most-frequent first - "what's alternating," not
    /// just a count of how often something did. Recomputed from this build's tallies each call;
    /// cheap unless you're calling it every frame in a hot loop (issue #4575).
    /// </summary>
    public IReadOnlyList<BatchBreakGroup> GetBreakGroups()
    {
        List<BatchBreakGroup> result = new(_breakGroupCounts.Count);
        foreach (KeyValuePair<BreakGroupKey, int> pair in _breakGroupCounts)
        {
            result.Add(new BatchBreakGroup
            {
                FromBatchKey = pair.Key.FromBatchKey,
                FromSortKey = pair.Key.FromSortKey,
                FromRenderableType = pair.Key.FromRenderableType,
                ToBatchKey = pair.Key.ToBatchKey,
                ToSortKey = pair.Key.ToSortKey,
                ToRenderableType = pair.Key.ToRenderableType,
                Reason = pair.Key.Reason,
                Count = pair.Value,
            });
        }
        result.Sort((a, b) => b.Count.CompareTo(a.Count));
        return result;
    }

    private struct Entry
    {
        public IRenderableIpso Item;
        public System.Drawing.Rectangle Bounds;
        public string BatchKey;
        public object? BatchSortKey;

        /// <summary>
        /// True for a plain wrapper (empty <see cref="IRenderable.BatchKey"/>, not a render target,
        /// not a clip) — the shape a Gum component instance's root takes.
        /// <see cref="BatchOrchestrator.OnRenderable"/> treats an empty BatchKey as a complete
        /// no-op (no flush, no StartBatch/EndBatch, running key left alone) and
        /// <c>InvisibleRenderable.Render</c> submits no vertices, so emitting one costs nothing at
        /// the GPU level. That makes it both exempt from break accounting and a free choice for
        /// the topological sort — see the free-container tier in <see cref="FlushWindow"/>.
        /// Computed once per entry rather than per selection: <see cref="FlushWindow"/>'s tiebreak
        /// scans it on every pick.
        /// </summary>
        public bool IsFreeContainer;
    }

    // Scratch state pooled on the instance (#4200) so a build does not allocate after warm-up,
    // mirroring HierarchicalOrderer (#4190) and Renderer's own DrawCommand buffers. Safe to pool
    // here because BuildDrawList is never re-entrant on this instance: Renderer's render-target
    // bakes (the only other caller besides the main pass) run post-order in PreRender and fully
    // return before the next BuildDrawList call starts (see Renderer._bakeCommands' comment) --
    // so at most one BuildDrawList call is ever "in flight" on this instance at a time.

    // Reorder-window Entry lists. A window is rented when a new window starts (top-level, a
    // same-Y run, or a ClipsChildren subtree) and returned once FlushWindow has consumed it.
    // Nested clip windows are used in strict LIFO order matching the recursion, so a stack works.
    private readonly Stack<List<Entry>> _windowPool = new Stack<List<Entry>>();

    // FlushWindow's precedence-graph scratch, resized to fit the largest window seen so far.
    // FlushWindow calls never overlap (windows are flushed one at a time, depth-first), so a
    // single reusable set of buffers serves every call.
    private int[] _indegreeBuffer = Array.Empty<int>();
    private List<int>[] _successorsBuffer = Array.Empty<List<int>>();
    private bool[] _drawnBuffer = Array.Empty<bool>();
    private readonly List<int> _availableBuffer = new List<int>();

    // The "running" batch key/sort key/type, and whether the next comparison against it must be
    // treated as a forced break regardless of key equality. Unlike the per-window Entry/indegree
    // scratch above, these must be INSTANCE state rather than FlushWindow locals: a clip boundary
    // splits one BuildDrawList call into several independent FlushWindow calls (Renderer.cs and
    // ProcessRenderable's clip branch each start a fresh window), and entering/exiting a clip is a
    // real cost that must be attributed against whatever was running *before* that window split,
    // not lost because each FlushWindow call used to start fresh with a local set to null. Reset
    // once per BuildDrawList call (a fresh pass over the render list), not once per host frame -
    // that's what MergeBlockedByOverlapCount/etc do, via ResetBreakTally.
    private string? _runningBatchKey;
    private object? _runningSortKey;
    private Type? _runningRenderableType;
    private bool _forceNextTransition;

    /// <inheritdoc/>
    public void BuildDrawList(Layer layer, List<DrawCommand> destination, Camera? camera = null)
    {
        destination.Clear();
        _runningBatchKey = null;
        _runningSortKey = null;
        _runningRenderableType = null;
        _forceNextTransition = false;

        ClipBoundsSource clipBounds = camera != null
            ? new ClipBoundsSource(camera, layer)
            : default;

        ProcessLayerTopLevel(layer, clipBounds, destination);
    }

    /// <inheritdoc/>
    public void BuildDrawList(
        IList<IRenderableIpso> roots,
        List<DrawCommand> destination,
        ClipBoundsSource clipBounds = default)
    {
        destination.Clear();
        _runningBatchKey = null;
        _runningSortKey = null;
        _runningRenderableType = null;
        _forceNextTransition = false;
        ProcessWindow(roots, clipBounds, destination);
    }

    private List<Entry> RentWindow()
    {
        return _windowPool.Count > 0 ? _windowPool.Pop() : new List<Entry>();
    }

    private void ReturnWindow(List<Entry> window)
    {
        _windowPool.Push(window);
    }

    private void ProcessLayerTopLevel(
        Layer layer,
        ClipBoundsSource clipBounds,
        List<DrawCommand> destination)
    {
        ReadOnlyCollection<IRenderableIpso> top = layer.Renderables;
        int count = top.Count;
        if (count == 0)
        {
            return;
        }

        if (layer.SecondarySortOnY)
        {
            // The Layer's stable sort has already grouped same-Y top-level renderables; each
            // same-Y run is an independent reorder window so that callers relying on Y-order
            // (FRB legacy behavior) see no change in cross-Y ordering.
            int runStart = 0;
            while (runStart < count)
            {
                float runY = top[runStart].GetAbsoluteY();
                int runEnd = runStart + 1;
                while (runEnd < count && top[runEnd].GetAbsoluteY() == runY)
                {
                    runEnd++;
                }

                // Each same-Y run must reorder independently of every other run (see the doc
                // comment above) - reset the running key/forced-transition state so it doesn't
                // leak across the row boundary the way it's meant to leak across a clip boundary.
                _runningBatchKey = null;
                _runningSortKey = null;
                _runningRenderableType = null;
                _forceNextTransition = false;

                List<Entry> window = RentWindow();
                for (int i = runStart; i < runEnd; i++)
                {
                    ProcessRenderable(top[i], clipBounds, null, window, destination);
                }
                FlushWindow(window, destination);
                ReturnWindow(window);

                runStart = runEnd;
            }
        }
        else
        {
            ProcessWindow(top, clipBounds, destination);
        }
    }

    private void ProcessWindow(
        IList<IRenderableIpso> renderables,
        ClipBoundsSource clipBounds,
        List<DrawCommand> destination)
    {
        int count = renderables.Count;
        if (count == 0)
        {
            return;
        }

        List<Entry> window = RentWindow();
        for (int i = 0; i < count; i++)
        {
            ProcessRenderable(renderables[i], clipBounds, null, window, destination);
        }
        FlushWindow(window, destination);
        ReturnWindow(window);
    }

    private void ProcessRenderable(
        IRenderableIpso renderable,
        ClipBoundsSource clipBounds,
        System.Drawing.Rectangle? activeClip,
        List<Entry> currentWindow,
        List<DrawCommand> destination)
    {
        if (!renderable.Visible)
        {
            return;
        }

        // #2998 off-screen cull: skip a renderable (and its subtree) fully outside the active
        // clip. Gated on a resolvable mapping, mirroring HierarchicalOrderer -- see its rationale.
        if (clipBounds.CanResolveCullTestBounds
            && activeClip.HasValue
            && CameraScissorExtensions.CullOffscreenWhenClipped
            && CameraScissorExtensions.IsFullyOutside(
                clipBounds.GetCullTestBounds(renderable),
                activeClip.Value,
                CameraScissorExtensions.OffscreenCullMarginInPixels))
        {
            return;
        }

        bool clips = renderable.ClipsChildren;
        if (clips)
        {
            // The clip is a hard boundary: flush everything the parent window has accumulated
            // so it lands before BeginClip, then enter a fresh window for the clipped subtree
            // (the clip-bearing node itself draws inside its own clip, matching the legacy
            // walk in HierarchicalOrderer).
            FlushWindow(currentWindow, destination);

            // #4575 follow-up: entering a clip always forces a real flush
            // (Renderer.AdjustRenderStates restarts SpriteBatch on any clip change) -
            // unconditionally, regardless of whether this renderable's own BatchKey happens to
            // match whatever was running. Consume any already-pending forced transition (e.g.
            // from exiting a prior sibling clip) into this same recording instead of double-
            // counting it separately.
            _forceNextTransition = false;
            RecordHardBoundaryTransition(renderable.BatchKey, renderable.BatchSortKey, renderable.GetType());

            destination.Add(new DrawCommand(DrawCommandKind.BeginClip, renderable));

            List<Entry> innerWindow = RentWindow();
            AddEntry(renderable, innerWindow);

            // Entering a clipper narrows the active clip for descendants (intersect).
            System.Drawing.Rectangle? childClip = activeClip;
            if (clipBounds.CanResolveScissorRectangle)
            {
                System.Drawing.Rectangle thisClip = clipBounds.GetScissorRectangle(renderable);
                childClip = activeClip.HasValue ? System.Drawing.Rectangle.Intersect(activeClip.Value, thisClip) : thisClip;
            }

            if (Renderer.RenderUsingHierarchy && !renderable.IsRenderTarget)
            {
                ObservableCollection<IRenderableIpso> children = renderable.Children;
                if (children != null)
                {
                    int childCount = children.Count;
                    for (int i = 0; i < childCount; i++)
                    {
                        ProcessRenderable(children[i], clipBounds, childClip, innerWindow, destination);
                    }
                }
            }

            FlushWindow(innerWindow, destination);
            ReturnWindow(innerWindow);
            destination.Add(new DrawCommand(DrawCommandKind.EndClip, renderable));

            // Exiting a clip ALSO forces a flush (Renderer.Draw's didClipChange exit branch) -
            // whatever gets chosen next, in this window or a sibling one, must be treated as a
            // forced break too, regardless of what its own key turns out to be.
            _forceNextTransition = true;
        }
        else
        {
            AddEntry(renderable, currentWindow);

            if (Renderer.RenderUsingHierarchy && !renderable.IsRenderTarget)
            {
                ObservableCollection<IRenderableIpso> children = renderable.Children;
                if (children != null)
                {
                    int childCount = children.Count;
                    for (int i = 0; i < childCount; i++)
                    {
                        ProcessRenderable(children[i], clipBounds, activeClip, currentWindow, destination);
                    }
                }
            }
        }
    }

    private static void AddEntry(IRenderableIpso renderable, List<Entry> window)
    {
        string batchKey = renderable.BatchKey ?? string.Empty;
        window.Add(new Entry
        {
            Item = renderable,
            Bounds = GetEffectiveBounds(renderable),
            BatchKey = batchKey,
            BatchSortKey = renderable.BatchSortKey,
            IsFreeContainer = batchKey.Length == 0 && !renderable.IsRenderTarget && !renderable.ClipsChildren,
        });
    }

    /// <summary>
    /// Returns the renderable's absolute bounds, with a conservative fallback for cases
    /// where the computed bounds don't reflect the visible footprint. The common offender
    /// is a renderable with non-default <c>XOrigin/YOrigin/Rotation</c> (e.g. a sprite
    /// centered on its parent with <c>XOrigin=Center, XUnits=PixelsFromMiddle, Rotation=90</c>):
    /// the contained renderable's X/Y reflect a pre-rotation reference point that can sit
    /// outside the parent, even though the visible draw lands inside it. If the computed
    /// bounds don't intersect the parent's, fall back to the parent's bounds — it's a
    /// safe over-estimate that keeps the overlap test honest.
    /// </summary>
    private static System.Drawing.Rectangle GetEffectiveBounds(IRenderableIpso renderable)
    {
        System.Drawing.Rectangle bounds = renderable.GetAbsoluteBounds();
        IRenderableIpso? parent = renderable.Parent;
        if (parent != null)
        {
            System.Drawing.Rectangle parentBounds = parent.GetAbsoluteBounds();
            if (parentBounds.Width > 0 && parentBounds.Height > 0 && !bounds.IntersectsWith(parentBounds))
            {
                return parentBounds;
            }
        }
        return bounds;
    }

    private void EnsureGraphCapacity(int n)
    {
        if (_indegreeBuffer.Length >= n)
        {
            return;
        }

        int newSize = System.Math.Max(n, _indegreeBuffer.Length * 2);
        int oldSize = _successorsBuffer.Length;

        _indegreeBuffer = new int[newSize];
        _drawnBuffer = new bool[newSize];

        List<int>[] newSuccessors = new List<int>[newSize];
        Array.Copy(_successorsBuffer, newSuccessors, oldSize);
        for (int i = oldSize; i < newSize; i++)
        {
            newSuccessors[i] = new List<int>();
        }
        _successorsBuffer = newSuccessors;
    }

    private void FlushWindow(List<Entry> window, List<DrawCommand> destination)
    {
        int n = window.Count;
        if (n == 0)
        {
            return;
        }

        // Build the precedence graph: edge i -> j when i precedes j in DFS AND their bounds
        // intersect. Same-key pairs still need edges — alpha-blending order matters even
        // within a batch, and the topological sort that follows handles them as a tie.
        EnsureGraphCapacity(n);
        int[] indegree = _indegreeBuffer;
        List<int>[] successors = _successorsBuffer;
        bool[] drawn = _drawnBuffer;
        Array.Clear(indegree, 0, n);
        Array.Clear(drawn, 0, n);
        for (int i = 0; i < n; i++)
        {
            successors[i].Clear();
        }

        for (int i = 0; i < n; i++)
        {
            System.Drawing.Rectangle bi = window[i].Bounds;
            for (int j = i + 1; j < n; j++)
            {
                if (bi.IntersectsWith(window[j].Bounds))
                {
                    successors[i].Add(j);
                    indegree[j]++;
                }
            }
        }

        // Kahn's topological sort with a "stay on the current batch key" tiebreaker.
        // Among items with indegree 0, prefer one whose (BatchKey, BatchSortKey) matches the
        // last emitted exactly (keeps same-texture runs contiguous); failing that, one whose
        // BatchKey alone matches (keeps the coarser SpriteBatch/Apos.Shapes grouping
        // BatchOrchestrator relies on, even when BatchSortKey differs or is unset); failing that,
        // a free container (Entry.IsFreeContainer) — emitting one submits nothing and leaves the
        // running key alone, so it is strictly better than committing to a real key change, and
        // it unblocks the container's children while the running key may still be worth
        // continuing. Ties within a tier break by smallest DFS index for determinism. No match at
        // all → smallest DFS overall.
        //
        // The free-container tier is what makes same-key runs merge across sibling instances of
        // the same Gum component (#4579): each instance's root is a transparent wrapper whose
        // bounds encompass its children, so those children have a precedence edge from it and stay
        // unavailable until it is chosen. Without this tier a wrapper is only ever reached via the
        // last-resort smallest-DFS tier — by which point the first instance's non-matching items
        // have already been drained and the running key has moved on, so nothing merges across
        // instances no matter how many identical, non-overlapping ones exist.
        List<int> available = _availableBuffer;
        available.Clear();
        for (int i = 0; i < n; i++)
        {
            if (indegree[i] == 0)
            {
                available.Add(i);
            }
        }

        while (available.Count > 0)
        {
            int chosen = -1;
            for (int k = 0; k < available.Count; k++)
            {
                int idx = available[k];
                if (window[idx].BatchKey == _runningBatchKey && Equals(window[idx].BatchSortKey, _runningSortKey))
                {
                    if (chosen == -1 || idx < chosen)
                    {
                        chosen = idx;
                    }
                }
            }
            if (chosen == -1)
            {
                for (int k = 0; k < available.Count; k++)
                {
                    int idx = available[k];
                    if (window[idx].BatchKey == _runningBatchKey)
                    {
                        if (chosen == -1 || idx < chosen)
                        {
                            chosen = idx;
                        }
                    }
                }
            }
            if (chosen == -1)
            {
                for (int k = 0; k < available.Count; k++)
                {
                    int idx = available[k];
                    if (window[idx].IsFreeContainer)
                    {
                        if (chosen == -1 || idx < chosen)
                        {
                            chosen = idx;
                        }
                    }
                }
            }
            if (chosen == -1)
            {
                for (int k = 0; k < available.Count; k++)
                {
                    int idx = available[k];
                    if (chosen == -1 || idx < chosen)
                    {
                        chosen = idx;
                    }
                }
            }

            // #4575 diagnostics: classify a key change (BatchKey and/or BatchSortKey differs from
            // the last emitted item) as either overlap-blocked (a same-key item is still pending,
            // just not yet unblocked) or a genuine break (nothing pending shares the old key).
            // Skipped on the very first emission - there's no prior run to have broken yet.
            //
            // Also skipped for a free container (Entry.IsFreeContainer) - it is free at the real
            // GPU level, so counting it here would be a false positive.
            //
            // A render target or a pending forced transition (set when we just entered/exited a
            // clip, or just drew a render target) is UNCONDITIONALLY a break, regardless of
            // whether the key happens to match - HardBoundaryTransitionCount, never
            // MergeBlockedByOverlapCount/NoCandidateInWindowBreakCount, since no reordering could
            // ever have avoided it (issue #4575 follow-up).
            bool chosenIsFreeContainer = window[chosen].IsFreeContainer;
            bool forced = _forceNextTransition || window[chosen].Item.IsRenderTarget;

            // A free container is exempt from break accounting, so it must not CONSUME a pending
            // forced transition either - the real renderable after it is the one the GPU actually
            // flushes for, and the hard boundary belongs to it. Leaving the flag set keeps the
            // break groups reconciling against the frame's true draw-call count.
            if (!chosenIsFreeContainer)
            {
                _forceNextTransition = false;
            }

            if (!chosenIsFreeContainer)
            {
                Type toRenderableType = window[chosen].Item.GetType();

                if (_runningBatchKey == null)
                {
                    // Nothing preceded this item in the window - no reordering choice was even
                    // possible - but it is still a real draw call, so it gets its own break entry
                    // rather than being silently exempted (see BreakReason.NoPredecessor).
                    RecordBreak(
                        NoPredecessorBatchKey, null, NoPredecessorType,
                        window[chosen].BatchKey, window[chosen].BatchSortKey, toRenderableType,
                        BreakReason.NoPredecessor);
                }
                else
                {
                    bool exactMatch = window[chosen].BatchKey == _runningBatchKey &&
                        Equals(window[chosen].BatchSortKey, _runningSortKey);

                    if (forced)
                    {
                        HardBoundaryTransitionCount++;
                        RecordBreak(
                            _runningBatchKey, _runningSortKey, _runningRenderableType!,
                            window[chosen].BatchKey, window[chosen].BatchSortKey, toRenderableType,
                            BreakReason.HardBoundary);
                    }
                    else if (!exactMatch)
                    {
                        bool blockedCandidateExists = false;
                        for (int k = 0; k < n; k++)
                        {
                            if (!drawn[k] && k != chosen &&
                                window[k].BatchKey == _runningBatchKey && Equals(window[k].BatchSortKey, _runningSortKey))
                            {
                                blockedCandidateExists = true;
                                break;
                            }
                        }

                        BreakReason reason;
                        if (blockedCandidateExists)
                        {
                            MergeBlockedByOverlapCount++;
                            reason = BreakReason.BlockedByOverlap;
                        }
                        else
                        {
                            NoCandidateInWindowBreakCount++;
                            reason = BreakReason.NoCandidateInWindow;
                        }

                        RecordBreak(
                            _runningBatchKey, _runningSortKey, _runningRenderableType!,
                            window[chosen].BatchKey, window[chosen].BatchSortKey, toRenderableType, reason);
                    }
                }
            }

            destination.Add(new DrawCommand(DrawCommandKind.DrawRenderable, window[chosen].Item));
            drawn[chosen] = true;
            if (!chosenIsFreeContainer)
            {
                _runningBatchKey = window[chosen].BatchKey;
                _runningSortKey = window[chosen].BatchSortKey;
                _runningRenderableType = window[chosen].Item.GetType();
            }
            if (window[chosen].Item.IsRenderTarget)
            {
                // Drawing the target is only half the cost - DrawRenderTargetToScreen also
                // restores the normal effect afterward (its own BeginSpriteBatch with no
                // override), so whatever comes next is forced too, regardless of its own key.
                _forceNextTransition = true;
            }
            available.Remove(chosen);

            List<int> succ = successors[chosen];
            for (int k = 0; k < succ.Count; k++)
            {
                int s = succ[k];
                if (--indegree[s] == 0)
                {
                    available.Add(s);
                }
            }
        }

        window.Clear();
    }

    /// <summary>
    /// Unconditionally records a <see cref="BreakReason.HardBoundary"/> transition from whatever
    /// is currently running to <paramref name="toBatchKey"/>/<paramref name="toSortKey"/>/
    /// <paramref name="toRenderableType"/>, then makes that the new running state - used when
    /// entering a <see cref="IRenderableIpso.ClipsChildren"/> renderable, where the transition
    /// happens outside <see cref="FlushWindow"/>'s own per-item loop (issue #4575 follow-up).
    /// </summary>
    private void RecordHardBoundaryTransition(string toBatchKey, object? toSortKey, Type toRenderableType)
    {
        if (_runningBatchKey != null)
        {
            HardBoundaryTransitionCount++;
            RecordBreak(
                _runningBatchKey, _runningSortKey, _runningRenderableType!,
                toBatchKey, toSortKey, toRenderableType, BreakReason.HardBoundary);
        }
        else
        {
            // The clip is the very first thing in this window - nothing to force away from, but
            // still a real draw call (see BreakReason.NoPredecessor).
            RecordBreak(NoPredecessorBatchKey, null, NoPredecessorType, toBatchKey, toSortKey, toRenderableType, BreakReason.NoPredecessor);
        }

        _runningBatchKey = toBatchKey;
        _runningSortKey = toSortKey;
        _runningRenderableType = toRenderableType;
    }
}
