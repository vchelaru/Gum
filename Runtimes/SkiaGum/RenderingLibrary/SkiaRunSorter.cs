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

    struct Item { public IRenderableIpso R; public SKRect B; public int Next; public SKRect ChunkUnion; public int ChunkCount; public int NextChunk; }
    public static int Chunk = int.TryParse(Environment.GetEnvironmentVariable("GUM_SORT_CHUNK"), out int ch) ? ch : 8;
    struct Group { public int Key; public SKRect Union; public int First; public int Last; public int ChunkHead; }

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
        for (int i = 0; i < roots.Count; i++) ProcessRoot(roots[i]);
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

    // GUM_SORT_POS=chain uses GetAbsoluteX/Y per draw (the first version); default passes the parent's
    // absolute position down the walk. GUM_SORT_CHECK=1 counts mismatches between the two.
    static readonly bool ChainPos = Environment.GetEnvironmentVariable("GUM_SORT_POS") == "chain";
    static readonly bool CheckPos = Environment.GetEnvironmentVariable("GUM_SORT_CHECK") == "1";
    public static long StatPosMismatch;

    void ProcessRoot(IRenderableIpso r)
    {
        IRenderableIpso? p = r.Parent;
        bool rot = false;
        for (var q = p; q != null; q = q.Parent) if (q.Rotation != 0) { rot = true; break; }
        Process(r, p?.GetAbsoluteX() ?? 0, p?.GetAbsoluteY() ?? 0, rot);
    }

    void Process(IRenderableIpso r, float px, float py, bool rotated)
    {
        if (!r.Visible) return;
        float x = px + r.X, y = py + r.Y;
        bool rot = rotated || r.Rotation != 0;
        if (r.ClipsChildren)
        {
            FlushWindow();
            _dest.Add(new DrawCommand(DrawCommandKind.BeginClip, r));
            Add(r, x, y, rot);
            if (Renderer.RenderUsingHierarchy && !r.IsRenderTarget && r.Children != null)
                for (int i = 0; i < r.Children.Count; i++) Process(r.Children[i], x, y, rot);
            FlushWindow();
            _dest.Add(new DrawCommand(DrawCommandKind.EndClip, r));
            return;
        }
        Add(r, x, y, rot);
        if (Renderer.RenderUsingHierarchy && !r.IsRenderTarget && r.Children != null)
            for (int i = 0; i < r.Children.Count; i++) Process(r.Children[i], x, y, rot);
    }

    void Add(IRenderableIpso r, float x, float y, bool rotated)
    {
        IRenderableIpso inner = r is GraphicalUiElement e && e.RenderableComponent is IRenderableIpso c ? c : r;
        if (!r.IsRenderTarget && inner.GetType() == typeof(InvisibleRenderable)) return; // draws nothing

        if (ChainPos || CheckPos)
        {
            float cx = inner.GetAbsoluteX(), cy = inner.GetAbsoluteY();
            if (CheckPos && (MathF.Abs(cx - x) > 0.001f || MathF.Abs(cy - y) > 0.001f)) StatPosMismatch++;
            if (ChainPos) { x = cx; y = cy; }
        }

        int key;
        SKRect b;
        if (r.IsRenderTarget || rotated || !TryBounds(inner, x, y, out b))
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
        _items[idx] = new Item { R = r, B = b, Next = -1, ChunkUnion = b, ChunkCount = 1, NextChunk = -1 };
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
                    ref Item head = ref _items[grp.ChunkHead];
                    if (Chunk > 0 && head.ChunkCount < Chunk) { head.ChunkCount++; head.ChunkUnion = SKRect.Union(head.ChunkUnion, b); }
                    else { head.NextChunk = idx; grp.ChunkHead = idx; }
                    return;
                }
                if (grp.Key == Barrier || Overlaps(ref grp, b)) break;
            }
        }

        if (_groupCount == _groups.Length) Array.Resize(ref _groups, _groups.Length * 2);
        _groups[_groupCount++] = new Group { Key = key, Union = b, First = idx, Last = idx, ChunkHead = idx };
    }

    bool Overlaps(ref Group grp, SKRect b)
    {
        StatPairTests++;
        if (!Intersects(grp.Union, b)) return false;
        if (Chunk <= 0)
        {
            for (int i = grp.First; i >= 0; i = _items[i].Next)
            {
                StatPairTests++;
                if (Intersects(_items[i].B, b)) return true;
            }
            return false;
        }
        for (int h = grp.First; h >= 0; h = _items[h].NextChunk)
        {
            ref Item head = ref _items[h];
            StatPairTests++;
            if (!Intersects(head.ChunkUnion, b)) continue;
            int i = h;
            for (int k = 0; k < head.ChunkCount; k++, i = _items[i].Next)
            {
                StatPairTests++;
                if (Intersects(_items[i].B, b)) return true;
            }
        }
        return false;
    }

    // Strict: rects that only touch along an edge share no pixel coverage beyond antialiasing, which Gum's
    // snapped NineSlices and padded text bounds already cover.
    static bool Intersects(SKRect a, SKRect b) => a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;

    // World-space conservative text bounds by arithmetic: same rule as NineSliceBatcher.TryTextBounds under an
    // identity matrix, without the GetAbsoluteX/Y parent-chain walks and the MapRect P/Invoke.
    public static SKRect TextBounds(global::SkiaGum.Text t, float x, float y)
    {
        float w = t.EffectiveWidth, h = t.Height, ww = t.WrappedTextWidth, wh = t.WrappedTextHeight;
        float l, r, tp, bt;
        switch (t.HorizontalAlignment)
        {
            case HorizontalAlignment.Center: l = MathF.Min(x, x + (w - ww) / 2); r = MathF.Max(x + w, x + (w + ww) / 2); break;
            case HorizontalAlignment.Right: l = MathF.Min(x, x + w - ww); r = x + w; break;
            default: l = x; r = x + MathF.Max(w, ww); break;
        }
        switch (t.VerticalAlignment)
        {
            case VerticalAlignment.Center: tp = MathF.Min(y, y + (h - wh) / 2); bt = MathF.Max(y + h, y + (h + wh) / 2); break;
            case VerticalAlignment.Bottom: tp = MathF.Min(y, y + h - wh); bt = y + h; break;
            default: tp = y; bt = y + MathF.Max(h, wh); break;
        }
        const float pad = 4;
        return new SKRect(l - pad, tp - pad, r + pad, bt + pad);
    }

    static bool TryBounds(IRenderableIpso inner, float x, float y, out SKRect b)
    {
        b = default;
        switch (inner)
        {
            case global::SkiaGum.Text t:
                b = TextBounds(t, x, y);
                return true;
            case NineSlice:
            case Sprite:
                {
                    float w = inner.Width, h = inner.Height;
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
        return $"SORT step median {a[a.Count / 2] * toMs:F4} ms (frames {a.Count}); runSorter per frame: items {SkiaRunSorter.StatItems / f:F1} groups {SkiaRunSorter.StatGroups / f:F1} pairTests {SkiaRunSorter.StatPairTests / f:F1}; orderer pairTests {BatchKeyGroupedOrderer.Instance.PairTestCount}; posMismatch {SkiaRunSorter.StatPosMismatch / f:F1}";
    }
}
