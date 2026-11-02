using System;
using System.Collections.Generic;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Reference implementation of <see cref="BatchKeyGroupedOrderer"/>'s draw ordering as it was
/// before the precedence graph stopped testing every pair (#6179): an edge for every pair of
/// window entries whose bounds intersect, then Kahn's sort with the key-continuation tiers. Kept
/// deliberately naive and free of diagnostics so the differential test can compare the optimized
/// orderer against it. Like the orderer, it keeps whichever of its greedy order and the
/// depth-first order costs fewer key runs per window (#7051); its greedy picks stay independent.
/// Covers layers, SecondarySortOnY runs, clips and render targets; not the
/// off-screen cull, which needs a camera.
/// </summary>
internal sealed class LegacyPairwiseBatchOrderer
{
    private struct Entry
    {
        public IRenderableIpso Item;
        public System.Drawing.Rectangle Bounds;
        public string BatchKey;
        public object? BatchSortKey;
        public bool IsFreeContainer;
        public bool IsRenderTarget;
    }

    private string? _runningBatchKey;
    private object? _runningSortKey;

    // A clip exit or a drawn render target makes the next real draw a break whatever its key is.
    private bool _forceNextTransition;

    public List<DrawCommand> Build(Layer layer)
    {
        List<DrawCommand> destination = new List<DrawCommand>();
        _runningBatchKey = null;
        _runningSortKey = null;
        _forceNextTransition = false;

        IList<IRenderableIpso> top = layer.Renderables;
        if (layer.SecondarySortOnY)
        {
            int runStart = 0;
            while (runStart < top.Count)
            {
                float runY = top[runStart].GetAbsoluteY();
                int runEnd = runStart + 1;
                while (runEnd < top.Count && top[runEnd].GetAbsoluteY() == runY)
                {
                    runEnd++;
                }

                _runningBatchKey = null;
                _runningSortKey = null;
                _forceNextTransition = false;

                List<Entry> window = new List<Entry>();
                for (int i = runStart; i < runEnd; i++)
                {
                    Process(top[i], window, destination);
                }
                Flush(window, destination);
                runStart = runEnd;
            }
        }
        else
        {
            List<Entry> window = new List<Entry>();
            for (int i = 0; i < top.Count; i++)
            {
                Process(top[i], window, destination);
            }
            Flush(window, destination);
        }
        return destination;
    }

    private void Process(IRenderableIpso renderable, List<Entry> currentWindow, List<DrawCommand> destination)
    {
        if (!renderable.Visible)
        {
            return;
        }

        if (renderable.ClipsChildren)
        {
            Flush(currentWindow, destination);
            _runningBatchKey = renderable.BatchKey ?? string.Empty;
            _runningSortKey = renderable.BatchSortKey;
            _forceNextTransition = false;
            destination.Add(new DrawCommand(DrawCommandKind.BeginClip, renderable));

            List<Entry> inner = new List<Entry>();
            Add(renderable, inner);
            if (Renderer.RenderUsingHierarchy && !renderable.IsRenderTarget)
            {
                foreach (IRenderableIpso child in renderable.Children)
                {
                    Process(child, inner, destination);
                }
            }
            Flush(inner, destination);
            destination.Add(new DrawCommand(DrawCommandKind.EndClip, renderable));
            _forceNextTransition = true;
        }
        else
        {
            Add(renderable, currentWindow);
            if (Renderer.RenderUsingHierarchy && !renderable.IsRenderTarget)
            {
                foreach (IRenderableIpso child in renderable.Children)
                {
                    Process(child, currentWindow, destination);
                }
            }
        }
    }

    private static void Add(IRenderableIpso renderable, List<Entry> window)
    {
        string batchKey = renderable.BatchKey ?? string.Empty;
        System.Drawing.Rectangle bounds = renderable.GetAbsoluteBounds();
        IRenderableIpso? parent = renderable.Parent;
        if (parent != null)
        {
            System.Drawing.Rectangle parentBounds = parent.GetAbsoluteBounds();
            if (parentBounds.Width > 0 && parentBounds.Height > 0 && !bounds.IntersectsWith(parentBounds))
            {
                bounds = parentBounds;
            }
        }

        window.Add(new Entry
        {
            Item = renderable,
            Bounds = bounds,
            BatchKey = batchKey,
            BatchSortKey = renderable.BatchSortKey,
            IsFreeContainer = batchKey.Length == 0 && !renderable.IsRenderTarget && !renderable.ClipsChildren,
            IsRenderTarget = renderable.IsRenderTarget,
        });
    }

    private void Flush(List<Entry> window, List<DrawCommand> destination)
    {
        int n = window.Count;
        int[] indegree = new int[n];
        bool[] drawn = new bool[n];
        List<int>[] successors = new List<int>[n];
        for (int i = 0; i < n; i++)
        {
            successors[i] = new List<int>();
        }

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                if (window[i].Bounds.IntersectsWith(window[j].Bounds))
                {
                    successors[i].Add(j);
                    indegree[j]++;
                }
            }
        }

        List<int> available = new List<int>();
        for (int i = 0; i < n; i++)
        {
            if (indegree[i] == 0)
            {
                available.Add(i);
            }
        }

        // Pick the greedy order first without emitting it (the pick loop moves the running key as
        // it goes, so restore it afterwards), then keep whichever of it and the depth-first order
        // costs fewer key runs (#7051).
        string? startKey = _runningBatchKey;
        object? startSortKey = _runningSortKey;
        List<int> greedy = new List<int>(n);
        while (available.Count > 0)
        {
            int chosen = Pick(window, available, e => e.BatchKey == _runningBatchKey && Equals(e.BatchSortKey, _runningSortKey));
            if (chosen == -1)
            {
                chosen = Pick(window, available, e => e.IsFreeContainer);
            }
            if (chosen == -1)
            {
                chosen = Pick(window, available, e => e.BatchKey == _runningBatchKey);
            }
            if (chosen == -1)
            {
                chosen = Pick(window, available, _ => true);
            }

            greedy.Add(chosen);
            drawn[chosen] = true;
            if (!window[chosen].IsFreeContainer)
            {
                _runningBatchKey = window[chosen].BatchKey;
                _runningSortKey = window[chosen].BatchSortKey;
            }
            available.Remove(chosen);
            foreach (int s in successors[chosen])
            {
                if (--indegree[s] == 0)
                {
                    available.Add(s);
                }
            }
        }

        _runningBatchKey = startKey;
        _runningSortKey = startSortKey;

        List<int> depthFirst = new List<int>(n);
        for (int i = 0; i < n; i++)
        {
            depthFirst.Add(i);
        }
        List<int> order = CountBreaks(window, depthFirst) < CountBreaks(window, greedy) ? depthFirst : greedy;

        foreach (int idx in order)
        {
            destination.Add(new DrawCommand(DrawCommandKind.DrawRenderable, window[idx].Item));
            if (!window[idx].IsFreeContainer)
            {
                _forceNextTransition = false;
                _runningBatchKey = window[idx].BatchKey;
                _runningSortKey = window[idx].BatchSortKey;
            }
            if (window[idx].IsRenderTarget)
            {
                _forceNextTransition = true;
            }
        }

        window.Clear();
    }

    /// <summary>Key runs emitting <paramref name="order"/> from the current running state would cost.</summary>
    private int CountBreaks(List<Entry> window, List<int> order)
    {
        string? key = _runningBatchKey;
        object? sortKey = _runningSortKey;
        bool forcedPending = _forceNextTransition;
        int breaks = 0;
        foreach (int idx in order)
        {
            Entry e = window[idx];
            if (e.IsFreeContainer)
            {
                continue;
            }

            bool forced = forcedPending || e.IsRenderTarget;
            forcedPending = e.IsRenderTarget;
            if (forced || key == null || e.BatchKey != key || !Equals(e.BatchSortKey, sortKey))
            {
                breaks++;
            }
            key = e.BatchKey;
            sortKey = e.BatchSortKey;
        }
        return breaks;
    }

    private static int Pick(List<Entry> window, List<int> available, Func<Entry, bool> matches)
    {
        int chosen = -1;
        foreach (int idx in available)
        {
            if (matches(window[idx]) && (chosen == -1 || idx < chosen))
            {
                chosen = idx;
            }
        }
        return chosen;
    }
}
