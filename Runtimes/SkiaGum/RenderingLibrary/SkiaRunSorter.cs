using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>
/// SPIKE #8116 (sort3, GUM_SORT=2): Skia-only greedy run builder. One pass over each clip window in DFS order;
/// each draw joins the most recent group with its batch key unless a draw in a later group overlaps it, else it
/// opens a new group. Groups are emitted in order, so any two overlapping draws keep their DFS order. Containers
/// (InvisibleRenderable) are not emitted; anything whose drawn bounds are unknown is a barrier nothing moves past.
/// No allocation after warm-up.
/// </summary>
public sealed class SkiaRunSorter
{
    public static readonly SkiaRunSorter Instance = new();
    public static int MaxScan = int.TryParse(Environment.GetEnvironmentVariable("GUM_SORT_SCAN"), out int s) ? s : 8;
    public static long StatPairTests, StatGroups, StatItems, StatFrames;

    const int Barrier = -1;

    struct Item { public IRenderableIpso R; public SKRect B; public int Next; }
    struct Group { public int Key; public SKRect Union; public int First; public int Last; }

    Item[] _items = new Item[1024];
    int _itemCount;
    Group[] _groups = new Group[256];
    int _groupCount;

    // Tiny key table: (BatchKey string ref, sort key ref) -> id. Scenes have a handful of distinct keys.
    string?[] _keyNames = new string?[16];
    object?[] _keyObjs = new object?[16];
    int _keyCount;

    List<DrawCommand> _dest = null!;

    public void Build(IList<IRenderableIpso> roots, List<DrawCommand> destination)
    {
        destination.Clear();
        _dest = destination;
        _itemCount = 0;
        _groupCount = 0;
        for (int i = 0; i < roots.Count; i++) Process(roots[i]);
        FlushWindow();
        StatFrames++;
    }

    int KeyOf(string batchKey, object? sortKey)
    {
        for (int i = 0; i < _keyCount; i++)
            if (ReferenceEquals(_keyObjs[i], sortKey) && string.Equals(_keyNames[i], batchKey, StringComparison.Ordinal)) return i;
        if (_keyCount == _keyNames.Length) { Array.Resize(ref _keyNames, _keyCount * 2); Array.Resize(ref _keyObjs, _keyCount * 2); }
        _keyNames[_keyCount] = batchKey; _keyObjs[_keyCount] = sortKey;
        return _keyCount++;
    }

    void Process(IRenderableIpso r)
    {
        if (!r.Visible) return;
        if (r.ClipsChildren)
        {
            FlushWindow();
            _dest.Add(new DrawCommand(DrawCommandKind.BeginClip, r));
            Add(r);
            if (Renderer.RenderUsingHierarchy && !r.IsRenderTarget && r.Children != null)
                for (int i = 0; i < r.Children.Count; i++) Process(r.Children[i]);
            FlushWindow();
            _dest.Add(new DrawCommand(DrawCommandKind.EndClip, r));
            return;
        }
        Add(r);
        if (Renderer.RenderUsingHierarchy && !r.IsRenderTarget && r.Children != null)
            for (int i = 0; i < r.Children.Count; i++) Process(r.Children[i]);
    }

    void Add(IRenderableIpso r)
    {
        IRenderableIpso inner = r is GraphicalUiElement e && e.RenderableComponent is IRenderableIpso c ? c : r;
        if (!r.IsRenderTarget && inner.GetType() == typeof(InvisibleRenderable)) return; // draws nothing

        int key;
        SKRect b;
        if (r.IsRenderTarget || !TryBounds(inner, out b))
        {
            key = Barrier;
            b = default;
        }
        else
        {
            key = KeyOf(inner.BatchKey, inner.BatchSortKey);
        }

        if (_itemCount == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        int idx = _itemCount++;
        _items[idx] = new Item { R = r, B = b, Next = -1 };
        StatItems++;

        if (key != Barrier)
        {
            int stop = System.Math.Max(0, _groupCount - MaxScan);
            for (int g = _groupCount - 1; g >= stop; g--)
            {
                ref Group grp = ref _groups[g];
                if (grp.Key == key)
                {
                    _items[grp.Last].Next = idx;
                    grp.Last = idx;
                    grp.Union = SKRect.Union(grp.Union, b);
                    return;
                }
                if (grp.Key == Barrier || Overlaps(ref grp, b)) break;
            }
        }

        if (_groupCount == _groups.Length) Array.Resize(ref _groups, _groups.Length * 2);
        _groups[_groupCount++] = new Group { Key = key, Union = b, First = idx, Last = idx };
    }

    bool Overlaps(ref Group grp, SKRect b)
    {
        StatPairTests++;
        if (!Intersects(grp.Union, b)) return false;
        for (int i = grp.First; i >= 0; i = _items[i].Next)
        {
            StatPairTests++;
            if (Intersects(_items[i].B, b)) return true;
        }
        return false;
    }

    // Strict: rects that only touch along an edge share no pixel coverage beyond antialiasing, which Gum's
    // snapped NineSlices and padded text bounds already cover.
    static bool Intersects(SKRect a, SKRect b) => a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;

    static bool TryBounds(IRenderableIpso inner, out SKRect b)
    {
        b = default;
        switch (inner)
        {
            case global::SkiaGum.Text t:
                return NineSliceBatcher.TryTextBounds(t, SKMatrix.Identity, out b);
            case NineSlice:
            case Sprite:
                {
                    if (inner.GetAbsoluteRotation() != 0) return false;
                    float x = inner.GetAbsoluteX(), y = inner.GetAbsoluteY(), w = inner.Width, h = inner.Height;
                    const float pad = 1;
                    b = new SKRect(MathF.Min(x, x + w) - pad, MathF.Min(y, y + h) - pad, MathF.Max(x, x + w) + pad, MathF.Max(y, y + h) + pad);
                    return true;
                }
            default:
                return false; // shapes may carry strokes/shadows outside their bounds: barrier
        }
    }

    void FlushWindow()
    {
        for (int g = 0; g < _groupCount; g++)
            for (int i = _groups[g].First; i >= 0; i = _items[i].Next)
            {
                _dest.Add(new DrawCommand(DrawCommandKind.DrawRenderable, _items[i].R));
                _items[i].R = null!;
            }
        StatGroups += _groupCount;
        _groupCount = 0;
        _itemCount = 0;
    }
}

/// <summary>SPIKE #8116 (sort3): per-frame sort-step time, measured frames only (the last N reported).</summary>
public static class SortProbe
{
    static readonly List<long> Ticks = new();
    public static void Add(long ticks) => Ticks.Add(ticks);

    public static string Report(int lastFrames)
    {
        if (Ticks.Count == 0) return "";
        int from = System.Math.Max(0, Ticks.Count - lastFrames);
        var a = Ticks.GetRange(from, Ticks.Count - from);
        a.Sort();
        double toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        double f = System.Math.Max(1, SkiaRunSorter.StatFrames);
        return $"SORT step median {a[a.Count / 2] * toMs:F4} ms (frames {a.Count}); runSorter per frame: items {SkiaRunSorter.StatItems / f:F1} groups {SkiaRunSorter.StatGroups / f:F1} pairTests {SkiaRunSorter.StatPairTests / f:F1}; orderer pairTests {BatchKeyGroupedOrderer.Instance.PairTestCount}";
    }
}
