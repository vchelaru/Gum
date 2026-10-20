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
/// orderer against it. Covers layers, SecondarySortOnY runs, clips and render targets; not the
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
    }

    private string? _runningBatchKey;
    private object? _runningSortKey;

    public List<DrawCommand> Build(Layer layer)
    {
        List<DrawCommand> destination = new List<DrawCommand>();
        _runningBatchKey = null;
        _runningSortKey = null;

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

        while (available.Count > 0)
        {
            int chosen = Pick(window, available, e => e.BatchKey == _runningBatchKey && Equals(e.BatchSortKey, _runningSortKey));
            if (chosen == -1)
            {
                chosen = Pick(window, available, e => e.BatchKey == _runningBatchKey);
            }
            if (chosen == -1)
            {
                chosen = Pick(window, available, e => e.IsFreeContainer);
            }
            if (chosen == -1)
            {
                chosen = Pick(window, available, _ => true);
            }

            destination.Add(new DrawCommand(DrawCommandKind.DrawRenderable, window[chosen].Item));
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

        window.Clear();
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
