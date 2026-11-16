using SkiaSharp;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>SPIKE #8116 textops (GUM_TEXTCACHE=1): consecutive plain texts (same matrix, one fill color, no halo) collect into
/// one pending batch, drawn as ONE multi-run SKTextBlob. The merged blob is shaped-text caching: it is kept per batch slot
/// (the n-th batch of the frame) with its items' blob identities and positions relative to the first item, and rebuilt only
/// when those change. A scrolling list moves the first item's origin, not the relative layout, so it keeps hitting.
/// Batches are capped at GUM_TEXTCACHE_CHUNK items (default 16) so Skia can still quick-reject off-clip chunks whole.
/// Flushed before any non-text draw, clip push/pop, render target, matrix or color change and at the end of the frame.</summary>
public static class TextBlobBatch
{
    public static readonly bool Enabled = Environment.GetEnvironmentVariable("GUM_TEXTCACHE") == "1";
    static readonly int Chunk = int.TryParse(Environment.GetEnvironmentVariable("GUM_TEXTCACHE_CHUNK"), out int c) && c > 0 ? c : 16;

    public struct Item { public SKTextBlob Blob; public ushort[] Glyphs; public SKPoint[] Positions; public SKTypeface Typeface; public float Size; public float X, Y; }

    sealed class Slot
    {
        public Item[] Items = Array.Empty<Item>();
        public int Count;
        public SKTextBlob? Merged;
    }

    public static bool Active;
    public static SKCanvas? Canvas;
    static Item[] _items = new Item[64];
    static int _count;
    static SKMatrix _matrix;
    static SKColor _color;
    static SKFontHinting _hinting; static SKFontEdging _edging; static bool _subpixel;
    static readonly List<Slot> _slots = new();
    static int _slotIndex;
    static readonly SKPaint _paint = new();
    static readonly SKTextBlobBuilder _builder = new();
    static readonly SKFont _font = new();
    static SKPoint[] _scratch = new SKPoint[256];
    public static long StatItems, StatBatches, StatHits, StatMisses, StatSingles, StatFrames;

    static TextBlobBatch()
    {
        string? path = Environment.GetEnvironmentVariable("GUM_BATCH_STATS");
        if (path != null && Enabled)
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                double f = System.Math.Max(1, StatFrames);
                System.IO.File.WriteAllText(path, $"textcache chunk {Chunk}: perFrame items {StatItems / f:F1} batches {StatBatches / f:F1} hits {StatHits / f:F2} misses {StatMisses / f:F2} singles {StatSingles / f:F1}\n");
            };
    }

    public static void BeginFrame(SKCanvas canvas) { Active = true; Canvas = canvas; _slotIndex = 0; }

    public static void EndFrame() { Flush(); Active = false; StatFrames++; }

    public static bool HasPending => _count > 0;

    /// <summary>Adds one plain text's runs. The caller has checked the text draws one segment with one color, no halo.</summary>
    public static void Add(SKMatrix m, SKColor color, SKFontHinting hinting, SKFontEdging edging, bool subpixel, in Item item)
    {
        if (_count > 0 && (m != _matrix || color != _color || hinting != _hinting || edging != _edging || subpixel != _subpixel || _count >= Chunk)) Flush();
        if (_count == 0) { _matrix = m; _color = color; _hinting = hinting; _edging = edging; _subpixel = subpixel; }
        if (_count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        _items[_count++] = item;
        StatItems++;
    }

    public static void Flush()
    {
        if (_count == 0) return;
        var canvas = Canvas!;
        bool same = canvas.TotalMatrix == _matrix;
        if (!same) { canvas.Save(); canvas.SetMatrix(_matrix); }
        _paint.Color = _color;
        StatBatches++;
        if (_count == 1)
        {
            StatSingles++;
            canvas.DrawText(_items[0].Blob, _items[0].X, _items[0].Y, _paint);
        }
        else
        {
            if (_slotIndex == _slots.Count) _slots.Add(new Slot());
            Slot slot = _slots[_slotIndex++];
            float x0 = _items[0].X, y0 = _items[0].Y;
            bool hit = slot.Merged != null && slot.Count == _count;
            for (int i = 0; hit && i < _count; i++)
            {
                ref Item a = ref _items[i]; ref Item b = ref slot.Items[i];
                hit = ReferenceEquals(a.Blob, b.Blob) && a.X - x0 == b.X && a.Y - y0 == b.Y;
            }
            if (hit) StatHits++;
            else
            {
                StatMisses++;
                slot.Merged?.Dispose();
                if (slot.Items.Length < _count) slot.Items = new Item[_count * 2];
                _font.Hinting = _hinting; _font.Edging = _edging; _font.Subpixel = _subpixel;
                for (int i = 0; i < _count; i++)
                {
                    ref Item r = ref _items[i];
                    float rx = r.X - x0, ry = r.Y - y0;
                    slot.Items[i] = r; slot.Items[i].X = rx; slot.Items[i].Y = ry;
                    _font.Typeface = r.Typeface; _font.Size = r.Size;
                    int n = r.Glyphs.Length;
                    if (_scratch.Length < n) _scratch = new SKPoint[n * 2];
                    for (int g = 0; g < n; g++) _scratch[g] = new SKPoint(r.Positions[g].X + rx, r.Positions[g].Y + ry);
                    SKPositionedRunBuffer buffer = _builder.AllocatePositionedRun(_font, n);
                    buffer.SetGlyphs(r.Glyphs.AsSpan());
                    buffer.SetPositions(_scratch.AsSpan(0, n));
                }
                slot.Count = _count;
                slot.Merged = _builder.Build();
            }
            if (slot.Merged != null) canvas.DrawText(slot.Merged, x0, y0, _paint);
        }
        for (int i = 0; i < _count; i++) _items[i] = default;
        _count = 0;
        if (!same) canvas.Restore();
    }
}
