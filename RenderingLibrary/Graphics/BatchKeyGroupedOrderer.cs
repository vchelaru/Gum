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

    private struct Entry
    {
        public IRenderableIpso Item;
        public System.Drawing.Rectangle Bounds;
        public string BatchKey;
        public object? BatchSortKey;
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
    private readonly List<int> _availableBuffer = new List<int>();

    /// <inheritdoc/>
    public void BuildDrawList(Layer layer, List<DrawCommand> destination, Camera? camera = null)
    {
        destination.Clear();

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
        window.Add(new Entry
        {
            Item = renderable,
            Bounds = GetEffectiveBounds(renderable),
            BatchKey = renderable.BatchKey ?? string.Empty,
            BatchSortKey = renderable.BatchSortKey,
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
        Array.Clear(indegree, 0, n);
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
        // BatchOrchestrator relies on, even when BatchSortKey differs or is unset). Ties within
        // a tier break by smallest DFS index for determinism. No match at all → smallest DFS
        // overall.
        List<int> available = _availableBuffer;
        available.Clear();
        for (int i = 0; i < n; i++)
        {
            if (indegree[i] == 0)
            {
                available.Add(i);
            }
        }

        string? currentBatchKey = null;
        object? currentSortKey = null;
        while (available.Count > 0)
        {
            int chosen = -1;
            for (int k = 0; k < available.Count; k++)
            {
                int idx = available[k];
                if (window[idx].BatchKey == currentBatchKey && Equals(window[idx].BatchSortKey, currentSortKey))
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
                    if (window[idx].BatchKey == currentBatchKey)
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

            destination.Add(new DrawCommand(DrawCommandKind.DrawRenderable, window[chosen].Item));
            currentBatchKey = window[chosen].BatchKey;
            currentSortKey = window[chosen].BatchSortKey;
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
}
