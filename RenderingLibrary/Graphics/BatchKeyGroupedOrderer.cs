using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace RenderingLibrary.Graphics;

/// <summary>
/// <see cref="IRenderableOrderer"/> that reorders DFS draws within layer- and clip-bounded
/// windows so that runs of same-<see cref="IRenderable.BatchKey"/> draws become contiguous.
/// Pixel-correct: any two draws whose <see cref="IPositionedSizedObjectExtensionMethods.GetAbsoluteBounds"/>
/// intersect maintain their original DFS-relative order, so the painter's algorithm result
/// is unchanged. Opt in by setting <c>Renderer.SiblingOrdering = BatchKeyGroupedOrderer.Instance</c>.
/// </summary>
/// <remarks>
/// The motivating case is a cross-batch-type scene (e.g. a list whose items contain both
/// <c>SpriteBatch</c>-using and <c>Apos.Shapes</c>-using renderables). In DFS order the
/// renderer flushes on every batch-type alternation; this orderer pulls each batch type
/// into one run, collapsing flushes from ~one-per-item to one-per-distinct-key.
///
/// First-pass scope: keys are the existing <see cref="IRenderable.BatchKey"/> string only.
/// Finer texture-level keying within <c>SpriteBatch</c> is a follow-up.
/// </remarks>
public sealed class BatchKeyGroupedOrderer : IRenderableOrderer
{
    /// <summary>Shared stateless instance.</summary>
    public static readonly BatchKeyGroupedOrderer Instance = new BatchKeyGroupedOrderer();

    private struct Entry
    {
        public IRenderableIpso Item;
        public Rectangle Bounds;
        public string BatchKey;
    }

    /// <inheritdoc/>
    public void BuildDrawList(Layer layer, List<DrawCommand> destination)
    {
        destination.Clear();
        ProcessLayerTopLevel(layer, destination);
    }

    private static void ProcessLayerTopLevel(Layer layer, List<DrawCommand> destination)
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

                List<Entry> window = new List<Entry>();
                for (int i = runStart; i < runEnd; i++)
                {
                    ProcessRenderable(top[i], window, destination);
                }
                FlushWindow(window, destination);

                runStart = runEnd;
            }
        }
        else
        {
            List<Entry> window = new List<Entry>();
            for (int i = 0; i < count; i++)
            {
                ProcessRenderable(top[i], window, destination);
            }
            FlushWindow(window, destination);
        }
    }

    private static void ProcessRenderable(
        IRenderableIpso renderable,
        List<Entry> currentWindow,
        List<DrawCommand> destination)
    {
        if (!renderable.Visible)
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

            List<Entry> innerWindow = new List<Entry>();
            AddEntry(renderable, innerWindow);

            if (Renderer.RenderUsingHierarchy && !renderable.IsRenderTarget)
            {
                ObservableCollection<IRenderableIpso> children = renderable.Children;
                if (children != null)
                {
                    int childCount = children.Count;
                    for (int i = 0; i < childCount; i++)
                    {
                        ProcessRenderable(children[i], innerWindow, destination);
                    }
                }
            }

            FlushWindow(innerWindow, destination);
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
                        ProcessRenderable(children[i], currentWindow, destination);
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
    private static Rectangle GetEffectiveBounds(IRenderableIpso renderable)
    {
        Rectangle bounds = renderable.GetAbsoluteBounds();
        IRenderableIpso? parent = renderable.Parent;
        if (parent != null)
        {
            Rectangle parentBounds = parent.GetAbsoluteBounds();
            if (parentBounds.Width > 0 && parentBounds.Height > 0 && !bounds.IntersectsWith(parentBounds))
            {
                return parentBounds;
            }
        }
        return bounds;
    }

    private static void FlushWindow(List<Entry> window, List<DrawCommand> destination)
    {
        int n = window.Count;
        if (n == 0)
        {
            return;
        }

        // Build the precedence graph: edge i -> j when i precedes j in DFS AND their bounds
        // intersect. Same-key pairs still need edges — alpha-blending order matters even
        // within a batch, and the topological sort that follows handles them as a tie.
        int[] indegree = new int[n];
        List<int>?[] successors = new List<int>?[n];
        for (int i = 0; i < n; i++)
        {
            Rectangle bi = window[i].Bounds;
            for (int j = i + 1; j < n; j++)
            {
                if (bi.IntersectsWith(window[j].Bounds))
                {
                    if (successors[i] == null)
                    {
                        successors[i] = new List<int>();
                    }
                    successors[i]!.Add(j);
                    indegree[j]++;
                }
            }
        }

        // Kahn's topological sort with a "stay on the current batch key" tiebreaker.
        // Among items with indegree 0, prefer one whose BatchKey matches the last emitted
        // (keeps batches contiguous); among matches, smallest DFS index for determinism.
        // No match → smallest DFS overall.
        List<int> available = new List<int>();
        for (int i = 0; i < n; i++)
        {
            if (indegree[i] == 0)
            {
                available.Add(i);
            }
        }

        string? currentBucket = null;
        while (available.Count > 0)
        {
            int chosen = -1;
            for (int k = 0; k < available.Count; k++)
            {
                int idx = available[k];
                if (window[idx].BatchKey == currentBucket)
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
                    if (chosen == -1 || idx < chosen)
                    {
                        chosen = idx;
                    }
                }
            }

            destination.Add(new DrawCommand(DrawCommandKind.DrawRenderable, window[chosen].Item));
            currentBucket = window[chosen].BatchKey;
            available.Remove(chosen);

            List<int>? succ = successors[chosen];
            if (succ != null)
            {
                for (int k = 0; k < succ.Count; k++)
                {
                    int s = succ[k];
                    if (--indegree[s] == 0)
                    {
                        available.Add(s);
                    }
                }
            }
        }

        window.Clear();
    }
}
