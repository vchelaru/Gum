using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

// SPIKE #8116 (GUM_TEXTBATCH=1 / GUM_RECTBATCH=1): one pending batch per kind, drawn later in a single Skia call.
// Same z-order rule as GUM_NSBATCH on spike/8116-skiaops: an op drawn while a batch is pending may go ahead of it only
// when its device bounds overlap nothing pending; otherwise (or when its bounds are unknown) the batch flushes first.
// Flushed on clip push/pop, render target, matrix change, paint change, overlap and the end of each top-level DrawList.
internal abstract class SpikePendingBatch
{
    public SKCanvas? Canvas;
    public bool Active;
    protected SKRect[] _bounds = new SKRect[256];
    protected int _count;
    protected SKMatrix _matrix;

    public long StatItems, StatBatches, StatBatchItems, StatFlushOverlap, StatFlushUnknown, StatFlushMatrix, StatFlushPaint, StatFlushClip, StatFlushEnd, StatFrames, StatFallback;

    public bool HasPending => _count > 0;

    protected void AddBounds(SKRect b)
    {
        if (_count == _bounds.Length) Array.Resize(ref _bounds, _bounds.Length * 2);
        _bounds[_count++] = b;
    }

    public bool Overlaps(SKRect b)
    {
        for (int i = 0; i < _count; i++)
            if (_bounds[i].IntersectsWith(b)) return true;
        return false;
    }

    // An op about to draw immediately: flush when it may touch a pending item.
    public void BeforeImmediate(IRenderableIpso inner, SKMatrix m)
    {
        if (_count == 0) return;
        if (!SpikeBounds.TryGet(inner, m, out SKRect b)) { StatFlushUnknown++; Flush(); return; }
        if (Overlaps(b)) { StatFlushOverlap++; Flush(); }
    }

    public void BeforeImmediate(SKRect deviceBounds)
    {
        if (_count == 0) return;
        if (Overlaps(deviceBounds)) { StatFlushOverlap++; Flush(); }
    }

    public void Flush()
    {
        if (_count == 0) return;
        StatBatches++;
        StatBatchItems += _count;
        var canvas = Canvas!;
        bool same = canvas.TotalMatrix == _matrix;
        if (!same) { canvas.Save(); canvas.SetMatrix(_matrix); }
        DrawPending(canvas);
        if (!same) canvas.Restore();
        _count = 0;
    }

    protected abstract void DrawPending(SKCanvas canvas);

    public string Stats(string name)
    {
        double f = System.Math.Max(1, StatFrames);
        return $"{name}: frames {StatFrames} perFrame items {StatItems / f:F1} fallback {StatFallback / f:F1} batches {StatBatches / f:F1} itemsInBatches {StatBatchItems / f:F1} " +
            $"flushOverlap {StatFlushOverlap / f:F1} flushUnknown {StatFlushUnknown / f:F1} flushMatrix {StatFlushMatrix / f:F1} flushPaint {StatFlushPaint / f:F1} flushClip {StatFlushClip / f:F1} flushEnd {StatFlushEnd / f:F1}\n";
    }
}

internal static class SpikeBatches
{
    public static readonly bool TextEnabled = Environment.GetEnvironmentVariable("GUM_TEXTBATCH") == "1";
    public static readonly bool RectEnabled = Environment.GetEnvironmentVariable("GUM_RECTBATCH") == "1";
    public static readonly bool AnyEnabled = TextEnabled || RectEnabled;
    public static readonly SpikeTextBatch Text = new();
    public static readonly SpikeRectBatch Rect = new();

    static SpikeBatches()
    {
        string? path = Environment.GetEnvironmentVariable("GUM_BATCH_STATS");
        if (path != null)
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                System.IO.File.WriteAllText(path, Text.Stats("text") + Rect.Stats("rect") + $"aaRectsPerFrame {RenderableShapeBase.SpikeAaRects / (double)System.Math.Max(1, Rect.StatFrames):F1}" + "\n");
    }

    public static bool Begin(SKCanvas canvas)
    {
        if (!AnyEnabled || Text.Active || Rect.Active) return false;
        if (TextEnabled) { Text.Active = true; Text.Canvas = canvas; }
        if (RectEnabled) { Rect.Active = true; Rect.Canvas = canvas; }
        return true;
    }

    public static void End()
    {
        if (TextEnabled) { if (Text.HasPending) Text.StatFlushEnd++; Text.Flush(); Text.Active = false; Text.StatFrames++; }
        if (RectEnabled) { if (Rect.HasPending) Rect.StatFlushEnd++; Rect.Flush(); Rect.Active = false; Rect.StatFrames++; }
    }

    public static void FlushForClip()
    {
        if (TextEnabled && Text.HasPending) { Text.StatFlushClip++; Text.Flush(); }
        if (RectEnabled && Rect.HasPending) { Rect.StatFlushClip++; Rect.Flush(); }
    }

    public static void BeforeRenderOne(IRenderableIpso inner, SKCanvas canvas)
    {
        if (inner is InvisibleRenderable) return;
        if (TextEnabled && Text.HasPending && inner is not SkiaGum.Text) Text.BeforeImmediate(inner, canvas.TotalMatrix);
        if (RectEnabled && Rect.HasPending && inner is not SolidRectangle) Rect.BeforeImmediate(inner, canvas.TotalMatrix);
    }
}

internal static class SpikeBounds
{
    const float Pad = 4;

    // Conservative device bounds of an unrotated draw under a scale+translate matrix.
    public static bool TryGet(IRenderableIpso r, SKMatrix m, out SKRect dev)
    {
        dev = default;
        if (m.SkewX != 0 || m.SkewY != 0 || m.Persp0 != 0 || m.Persp1 != 0) return false;
        if (r is SkiaGum.Text t) return TryTextBounds(t, m, out dev);
        if (r is RenderableShapeBase s) { if (s.HasDropshadow) return false; }
        else if (r is not NineSlice && r is not Sprite) return false;
        if (r.GetAbsoluteRotation() != 0) return false;
        float x = r.GetAbsoluteLeft(), y = r.GetAbsoluteTop();
        dev = m.MapRect(new SKRect(x - Pad, y - Pad, x + r.Width + Pad, y + r.Height + Pad));
        return true;
    }

    public static bool TryTextBounds(SkiaGum.Text t, SKMatrix m, out SKRect dev)
    {
        dev = default;
        if (m.SkewX != 0 || m.SkewY != 0 || m.Persp0 != 0 || m.Persp1 != 0 || t.GetAbsoluteRotation() != 0 || t.HasDropshadow) return false;
        float x = t.GetAbsoluteX(), y = t.GetAbsoluteY();
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
        dev = m.MapRect(new SKRect(l - Pad, tp - Pad, r + Pad, bt + Pad));
        return true;
    }
}

internal sealed class SpikeTextBatch : SpikePendingBatch
{
    struct Run { public SKTypeface Typeface; public float Size; public ushort[] Glyphs; public SKPoint[] Positions; public float X, Y; }
    Run[] _runs = new Run[512];
    int _runCount;
    SKColor _color;
    SKFontHinting _hinting; SKFontEdging _edging; bool _subpixel;
    readonly SKTextBlobBuilder _builder = new();
    readonly SKFont _font = new();
    readonly SKPaint _paint = new();
    SKPoint[] _scratch = new SKPoint[256];

    public bool CanJoin(SKMatrix m, SKColor color, SKFontHinting hinting, SKFontEdging edging, bool subpixel)
    {
        if (_count == 0) return true;
        if (m != _matrix) { StatFlushMatrix++; return false; }
        if (color != _color || hinting != _hinting || edging != _edging || subpixel != _subpixel) { StatFlushPaint++; return false; }
        return true;
    }

    public void BeginItem(SKMatrix m, SKColor color, SKFontHinting hinting, SKFontEdging edging, bool subpixel, SKRect deviceBounds)
    {
        if (!CanJoin(m, color, hinting, edging, subpixel)) Flush();
        if (_count == 0) { _matrix = m; _color = color; _hinting = hinting; _edging = edging; _subpixel = subpixel; }
        AddBounds(deviceBounds);
        StatItems++;
    }

    public void AddRun(SKTypeface typeface, float size, ushort[] glyphs, SKPoint[] positions, float x, float y)
    {
        if (_runCount == _runs.Length) Array.Resize(ref _runs, _runs.Length * 2);
        _runs[_runCount++] = new Run { Typeface = typeface, Size = size, Glyphs = glyphs, Positions = positions, X = x, Y = y };
    }

    protected override void DrawPending(SKCanvas canvas)
    {
        _font.Hinting = _hinting; _font.Edging = _edging; _font.Subpixel = _subpixel;
        for (int i = 0; i < _runCount; i++)
        {
            ref Run r = ref _runs[i];
            _font.Typeface = r.Typeface;
            _font.Size = r.Size;
            int n = r.Glyphs.Length;
            if (_scratch.Length < n) _scratch = new SKPoint[n * 2];
            for (int g = 0; g < n; g++) _scratch[g] = new SKPoint(r.Positions[g].X + r.X, r.Positions[g].Y + r.Y);
            SKPositionedRunBuffer buffer = _builder.AllocatePositionedRun(_font, n);
            buffer.SetGlyphs(r.Glyphs.AsSpan());
            buffer.SetPositions(_scratch.AsSpan(0, n));
            r = default;
        }
        _runCount = 0;
        using SKTextBlob? blob = _builder.Build();
        if (blob == null) return;
        _paint.Color = _color;
        canvas.DrawText(blob, 0, 0, _paint);
    }
}

internal sealed class SpikeRectBatch : SpikePendingBatch
{
    SKPoint[] _pos = new SKPoint[1024];
    SKColor[] _col = new SKColor[1024];
    ushort[] _idx = new ushort[1536];
    readonly SKPaint _paint = new() { Color = SKColors.White, IsAntialias = false };
    readonly Dictionary<int, (SKPoint[] P, SKColor[] C, ushort[] I)> _exact = new();

    public void Add(SKMatrix m, SKRect deviceRect, SKColor color)
    {
        if (_count > 0 && m != _matrix) { StatFlushMatrix++; Flush(); }
        if (_count == 16000) Flush();
        if (_count == 0) _matrix = m;
        int v = _count * 4, ii = _count * 6;
        if (v + 4 > _pos.Length) { Array.Resize(ref _pos, _pos.Length * 2); Array.Resize(ref _col, _col.Length * 2); Array.Resize(ref _idx, _idx.Length * 2); }
        _pos[v] = new SKPoint(deviceRect.Left, deviceRect.Top);
        _pos[v + 1] = new SKPoint(deviceRect.Right, deviceRect.Top);
        _pos[v + 2] = new SKPoint(deviceRect.Left, deviceRect.Bottom);
        _pos[v + 3] = new SKPoint(deviceRect.Right, deviceRect.Bottom);
        _col[v] = _col[v + 1] = _col[v + 2] = _col[v + 3] = color;
        _idx[ii] = (ushort)v; _idx[ii + 1] = (ushort)(v + 1); _idx[ii + 2] = (ushort)(v + 2);
        _idx[ii + 3] = (ushort)(v + 1); _idx[ii + 4] = (ushort)(v + 3); _idx[ii + 5] = (ushort)(v + 2);
        // Overlap among rects is fine (drawn in order); bounds are padded for the immediate-op test.
        AddBounds(new SKRect(deviceRect.Left - 1, deviceRect.Top - 1, deviceRect.Right + 1, deviceRect.Bottom + 1));
        StatItems++;
    }

    protected override void DrawPending(SKCanvas canvas)
    {
        int n = _count;
        if (!_exact.TryGetValue(n, out var a))
        {
            a = (new SKPoint[n * 4], new SKColor[n * 4], new ushort[n * 6]);
            _exact[n] = a;
        }
        Array.Copy(_pos, a.P, n * 4); Array.Copy(_col, a.C, n * 4); Array.Copy(_idx, a.I, n * 6);
        // Positions are already device space; draw under identity.
        canvas.Save();
        canvas.ResetMatrix();
        using (var verts = SKVertices.CreateCopy(SKVertexMode.Triangles, a.P, null, a.C, a.I))
            canvas.DrawVertices(verts, SKBlendMode.Modulate, _paint);
        canvas.Restore();
    }
}
