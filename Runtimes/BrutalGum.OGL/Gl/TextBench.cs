using System;
using System.Collections.Generic;
using System.Diagnostics;
using RenderingLibrary.Graphics;
using SkiaSharp;
using E = RenderingLibrary.Graphics.TextDrawCapture.Entry;

namespace Gum;

/// <summary>SPIKE #8116 textops agent: GUM_TEXTBENCH=1 captures every text-blob draw of frame 300 (TextDrawCapture), rebuilds
/// it as native-only SKPictures with ONE property changed per variant, and from then on plays each variant every frame onto
/// the back buffer after Gum's own frame, timing DrawPicture (Ganesh op creation) and gr.Flush separately. Every blob a
/// variant needs is built once at frame 300 (shaped-text caching), never per frame.</summary>
public static unsafe class TextBench
{
    public static readonly bool Enabled = Environment.GetEnvironmentVariable("GUM_TEXTBENCH") != null;
    static int _frame;
    static readonly List<(string Name, SKPicture Pic, int Draws)> Variants = new();
    static readonly List<List<double>> Rec = new(), Flush = new();
    static readonly List<string> PixelNotes = new();

    public static void Begin()
    {
        if (!Enabled) return;
        _frame++;
        if (_frame == 300) { TextDrawCapture.Entries.Clear(); TextDrawCapture.Active = true; }
    }

    public static void End(SKCanvas canvas, GRContext gr)
    {
        if (!Enabled) return;
        if (_frame == 300) { TextDrawCapture.Active = false; Build(gr); return; }
        if (Variants.Count == 0) return;
        gr.Flush(false);
        int n = Variants.Count;
        for (int k = 0; k < n; k++)
        {
            int i = (k + _frame) % n;
            canvas.Save();
            canvas.ResetMatrix();
            long a = Stopwatch.GetTimestamp();
            canvas.DrawPicture(Variants[i].Pic);
            long b = Stopwatch.GetTimestamp();
            gr.Flush(false);
            long c = Stopwatch.GetTimestamp();
            canvas.Restore();
            if (_frame > 400) { Rec[i].Add((b - a) * 1000.0 / Stopwatch.Frequency); Flush[i].Add((c - b) * 1000.0 / Stopwatch.Frequency); }
        }
    }

    static double Median(List<double> v) { var a = new List<double>(v); a.Sort(); return a.Count == 0 ? 0 : a[a.Count / 2]; }

    public static void Report()
    {
        if (!Enabled) return;
        foreach (var s in PixelNotes) Console.WriteLine(s);
        for (int i = 0; i < Variants.Count; i++)
        {
            double r = Median(Rec[i]), f = Median(Flush[i]);
            int d = Variants[i].Draws;
            Console.WriteLine($"TEXTBENCH {Variants[i].Name,-16} calls {d,4}  record {r:F4} ms ({(d > 0 ? r * 1000 / d : 0):F2} us/call)  flush {f:F4} ms ({(d > 0 ? f * 1000 / d : 0):F2} us/call)");
        }
    }

    // One recorded stream: per draw a clip (device space, when it changes) and a matrix (when it changes), then the draw.
    static SKPicture Make(Func<E, bool> include, Action<SKCanvas, E> draw, bool clip, Func<E, SKMatrix>? matrix, out int draws)
    {
        using var rec = new SKPictureRecorder();
        var c = rec.BeginRecording(new SKRect(0, 0, 4096, 4096));
        SKRectI? curClip = null; SKMatrix? curM = null; bool saved = false; draws = 0;
        foreach (var e in TextDrawCapture.Entries)
        {
            if (!include(e)) continue;
            if (clip && (curClip == null || curClip.Value != e.Clip))
            {
                if (saved) c.Restore();
                c.Save(); saved = true;
                c.ResetMatrix();
                c.ClipRect(new SKRect(e.Clip.Left, e.Clip.Top, e.Clip.Right, e.Clip.Bottom));
                curClip = e.Clip; curM = null;
            }
            var m = matrix?.Invoke(e) ?? e.Matrix;
            if (curM == null || !curM.Value.Equals(m)) { c.SetMatrix(m); curM = m; }
            draw(c, e); draws++;
        }
        if (saved) c.Restore();
        return rec.EndRecording();
    }

    static void Add(string name, Func<E, bool> include, Action<SKCanvas, E> draw, bool clip = true, Func<E, SKMatrix>? matrix = null)
    {
        var pic = Make(include, draw, clip, matrix, out int d);
        Variants.Add((name, pic, d)); Rec.Add(new()); Flush.Add(new());
    }

    static SKTextBlob Rebuild(E e, SKFontEdging edging, SKFontHinting hinting, bool subpixel, float dx = 0, float dy = 0)
    {
        using var b = new SKTextBlobBuilder();
        using var f = new SKFont(e.Typeface, e.Size) { Edging = edging, Hinting = hinting, Subpixel = subpixel };
        var buf = b.AllocatePositionedRun(f, e.Glyphs.Length);
        buf.SetGlyphs(e.Glyphs);
        var p = new SKPoint[e.Positions.Length];
        for (int i = 0; i < p.Length; i++) p[i] = new SKPoint(e.Positions[i].X + dx, e.Positions[i].Y + dy);
        buf.SetPositions(p);
        return b.Build()!;
    }

    static bool IsTranslate(SKMatrix m) => m.ScaleX == 1 && m.ScaleY == 1 && m.SkewX == 0 && m.SkewY == 0 && m.Persp0 == 0 && m.Persp1 == 0 && m.Persp2 == 1;

    // Merged: consecutive entries with the same clip and paint become one multi-run blob in device space, one DrawText.
    static void AddMerged(string name, bool ignoreClip, bool byColorOnly, int maxPer = int.MaxValue, bool cull = false)
    {
        using var rec = new SKPictureRecorder();
        var c = rec.BeginRecording(new SKRect(0, 0, 4096, 4096));
        int calls = 0, draws = 0;
        var groups = new List<(SKRectI Clip, SKPaint Paint, List<E> Items)>();
        foreach (var e in TextDrawCapture.Entries)
        {
            if (cull && !Visible(e)) continue;
            draws++;
            var clip = ignoreClip ? new SKRectI(-100000, -100000, 100000, 100000) : e.Clip;
            int gi = -1;
            if (byColorOnly) { for (int g = 0; g < groups.Count; g++) if (groups[g].Clip == clip && SamePaint(groups[g].Paint, e.Paint)) { gi = g; break; } }
            else if (groups.Count > 0 && groups[^1].Clip == clip && SamePaint(groups[^1].Paint, e.Paint) && groups[^1].Items.Count < maxPer) gi = groups.Count - 1;
            if (gi < 0) { groups.Add((clip, e.Paint, new List<E>())); gi = groups.Count - 1; }
            groups[gi].Items.Add(e);
        }
        SKRectI? curClip = null; bool saved = false;
        c.ResetMatrix();
        foreach (var g in groups)
        {
            if (!ignoreClip && (curClip == null || curClip.Value != g.Clip))
            {
                if (saved) c.Restore();
                c.Save(); saved = true;
                c.ClipRect(new SKRect(g.Clip.Left, g.Clip.Top, g.Clip.Right, g.Clip.Bottom));
                curClip = g.Clip;
            }
            using var b = new SKTextBlobBuilder();
            foreach (var e in g.Items)
            {
                var origin = e.Matrix.MapPoint(e.X, e.Y);
                using var f = new SKFont(e.Typeface, e.Size) { Edging = e.Edging, Hinting = e.Hinting, Subpixel = e.Subpixel };
                var buf = b.AllocatePositionedRun(f, e.Glyphs.Length);
                buf.SetGlyphs(e.Glyphs);
                var p = new SKPoint[e.Positions.Length];
                for (int i = 0; i < p.Length; i++) p[i] = new SKPoint(e.Positions[i].X + origin.X, e.Positions[i].Y + origin.Y);
                buf.SetPositions(p);
            }
            var blob = b.Build()!;
            c.DrawText(blob, 0, 0, g.Paint); calls++;
        }
        if (saved) c.Restore();
        Variants.Add((name, rec.EndRecording(), calls)); Rec.Add(new()); Flush.Add(new());
        Console.WriteLine($"TEXTBENCH {name}: {draws} text draws as {calls} DrawText calls");
    }

    static SKRect DevBounds(E e) { var b = e.Blob.Bounds; b.Offset(e.X, e.Y); return e.Matrix.MapRect(b); }
    static bool Visible(E e) { var b = DevBounds(e); return b.IntersectsWith(new SKRect(e.Clip.Left, e.Clip.Top, e.Clip.Right, e.Clip.Bottom)); }

    static bool SamePaint(SKPaint a, SKPaint b) => a.Color == b.Color && a.Style == b.Style && a.StrokeWidth == b.StrokeWidth && a.IsAntialias == b.IsAntialias
        && a.Shader == null && b.Shader == null && a.MaskFilter == null && b.MaskFilter == null && a.ColorFilter == null && b.ColorFilter == null && a.BlendMode == b.BlendMode;

    static void Build(GRContext gr)
    {
        var entries = TextDrawCapture.Entries;
        var owners = new Dictionary<object, int>();
        var kinds = new Dictionary<string, int>();
        int glyphs = 0;
        foreach (var e in entries)
        {
            owners[e.Owner] = owners.TryGetValue(e.Owner, out int n) ? n + 1 : 1;
            glyphs += e.Glyphs.Length;
            var m = e.Matrix; var p = e.Paint;
            var o = m.MapPoint(e.X, e.Y);
            string key = $"tf={e.Typeface.FamilyName} size={e.Size} edging={e.Edging} hint={e.Hinting} subpx={e.Subpixel} m=({m.ScaleX},{m.ScaleY},transOnly={IsTranslate(m)}) originFrac={(o.X % 1 != 0 || o.Y % 1 != 0)} " +
                $"posFrac={Array.Exists(e.Positions, q => q.X % 1 != 0 || q.Y % 1 != 0)} paint=(aa={p.IsAntialias} style={p.Style} sw={p.StrokeWidth} blend={p.BlendMode} sh={p.Shader != null} mf={p.MaskFilter != null} cf={p.ColorFilter != null} a={p.Color.Alpha}) clipFull={(e.Clip.Width >= 1280 && e.Clip.Height >= 720)}";
            kinds[key] = kinds.TryGetValue(key, out int k) ? k + 1 : 1;
        }
        var perOwner = new Dictionary<int, int>();
        foreach (var v in owners.Values) perOwner[v] = perOwner.TryGetValue(v, out int k) ? k + 1 : 1;
        var colors = new HashSet<SKColor>(); foreach (var e in entries) colors.Add(e.Paint.Color);
        var blobs = new HashSet<uint>(); foreach (var e in entries) blobs.Add(e.Blob.UniqueId);
        Console.WriteLine($"TEXTCAP {entries.Count} blob draws, {owners.Count} text blocks, {glyphs} glyphs, {colors.Count} colors, {blobs.Count} distinct blob ids");
        foreach (var kv in perOwner) Console.WriteLine($"TEXTCAP {kv.Value} draws per block: {kv.Key} blocks");
        foreach (var kv in kinds) Console.WriteLine($"TEXTCAP {kv.Value,4} x {kv.Key}");

        bool All(E e) => true;
        var rebuilt = new Dictionary<E, SKTextBlob>();
        foreach (var e in entries) rebuilt[e] = Rebuild(e, e.Edging, e.Hinting, e.Subpixel);
        SKTextBlob Var(E e, Dictionary<E, SKTextBlob> d) => d[e];
        Dictionary<E, SKTextBlob> With(SKFontEdging? ed, SKFontHinting? hi, bool? sp)
        {
            var d = new Dictionary<E, SKTextBlob>();
            foreach (var e in entries) d[e] = Rebuild(e, ed ?? e.Edging, hi ?? e.Hinting, sp ?? e.Subpixel);
            return d;
        }

        Add("empty", e => false, (c, e) => { });
        Add("exact", All, (c, e) => c.DrawText(e.Blob, e.X, e.Y, e.Paint));
        Add("rebuilt", All, (c, e) => c.DrawText(rebuilt[e], e.X, e.Y, e.Paint));
        Add("noclip", All, (c, e) => c.DrawText(e.Blob, e.X, e.Y, e.Paint), clip: false);
        // Device-space origin with an identity matrix (same pixels when the matrix is a pure translation).
        Add("ident-matrix", All, (c, e) => { var o = e.Matrix.MapPoint(e.X, e.Y); c.DrawText(e.Blob, o.X, o.Y, e.Paint); }, matrix: e => SKMatrix.Identity);
        Add("int-origin", All, (c, e) => { var o = e.Matrix.MapPoint(e.X, e.Y); c.DrawText(e.Blob, MathF.Round(o.X), MathF.Round(o.Y), e.Paint); }, matrix: e => SKMatrix.Identity);
        Add("frac-origin+.37", All, (c, e) => { var o = e.Matrix.MapPoint(e.X, e.Y); c.DrawText(e.Blob, o.X + 0.37f, o.Y + 0.37f, e.Paint); }, matrix: e => SKMatrix.Identity);
        var first = entries.Count > 0 ? entries[0].Blob : null;
        Add("oneblob", All, (c, e) => c.DrawText(first!, e.X, e.Y, e.Paint));
        var shared = new SKPaint { Color = SKColors.White, IsAntialias = true };
        Add("onepaint-white", All, (c, e) => c.DrawText(e.Blob, e.X, e.Y, shared));
        Add("paint-noaa", All, (c, e) => { var p = e.Paint.Clone(); p.IsAntialias = false; c.DrawText(e.Blob, e.X, e.Y, p); });
        var alias = With(SKFontEdging.Alias, null, null);
        var aa = With(SKFontEdging.Antialias, null, null);
        var lcd = With(SKFontEdging.SubpixelAntialias, null, null);
        var nosub = With(null, null, false);
        var sub = With(null, null, true);
        var hNone = With(null, SKFontHinting.None, null);
        var hFull = With(null, SKFontHinting.Full, null);
        Add("edging-alias", All, (c, e) => c.DrawText(alias[e], e.X, e.Y, e.Paint));
        Add("edging-aa", All, (c, e) => c.DrawText(aa[e], e.X, e.Y, e.Paint));
        Add("edging-lcd", All, (c, e) => c.DrawText(lcd[e], e.X, e.Y, e.Paint));
        Add("subpixel-off", All, (c, e) => c.DrawText(nosub[e], e.X, e.Y, e.Paint));
        Add("subpixel-on", All, (c, e) => c.DrawText(sub[e], e.X, e.Y, e.Paint));
        Add("hint-none", All, (c, e) => c.DrawText(hNone[e], e.X, e.Y, e.Paint));
        Add("hint-full", All, (c, e) => c.DrawText(hFull[e], e.X, e.Y, e.Paint));
        var solid = new SKPaint { Color = SKColors.CadetBlue };
        Add("ref-solidrect", All, (c, e) => c.DrawRect(e.X, e.Y - 10, 20, 10, solid));
        Add("ref-rejected", All, (c, e) => c.DrawText(e.Blob, -5000, -5000, e.Paint));
        AddMerged("merged-seq", ignoreClip: false, byColorOnly: false);
        AddMerged("merged-color", ignoreClip: false, byColorOnly: true);
        AddMerged("merged-noclip", ignoreClip: true, byColorOnly: true);
        AddMerged("merged-chunk1", false, false, 1);
        AddMerged("merged-chunk4", false, false, 4);
        AddMerged("merged-chunk16", false, false, 16);
        AddMerged("merged-chunk64", false, false, 64);
        AddMerged("merged-seq-cull", false, false, int.MaxValue, cull: true);
        int vis = 0, inside = 0; foreach (var e in entries) { if (Visible(e)) vis++; var b = DevBounds(e); if (new SKRect(e.Clip.Left, e.Clip.Top, e.Clip.Right, e.Clip.Bottom).Contains(b)) inside++; }
        { var seen = new Dictionary<string,int>(); foreach (var e in entries) if (!Visible(e)) { var b = DevBounds(e); string k = $"clip={e.Clip} y={(b.Bottom < e.Clip.Top ? "above" : b.Top > e.Clip.Bottom ? "below" : "side")}"; seen[k] = seen.TryGetValue(k, out int q) ? q + 1 : 1; } foreach (var kv in seen) Console.WriteLine($"TEXTCAP invisible {kv.Value} x {kv.Key}"); }
        Console.WriteLine($"TEXTCAP visible (bounds meet clip) {vis} of {entries.Count}, fully inside clip {inside}");
        Console.WriteLine($"TEXTBENCH built {Variants.Count} variants");

        // Pixels of each variant against exact, on a GPU surface like the back buffer.
        var info = new SKImageInfo(1280, 720, SKColorType.Rgba8888, SKAlphaType.Premul);
        byte[] Render(SKPicture p)
        {
            using var s = SKSurface.Create(gr, false, info)!;
            s.Canvas.Clear(new SKColor(30, 40, 60)); s.Canvas.DrawPicture(p); s.Flush();
            var px = new byte[1280 * 720 * 4];
            fixed (byte* q = px) s.ReadPixels(info, (IntPtr)q, 1280 * 4, 0, 0);
            return px;
        }
        int exactIdx = Variants.FindIndex(v => v.Name == "exact");
        var refPx = Render(Variants[exactIdx].Pic);
        foreach (var v in Variants)
        {
            var px = Render(v.Pic);
            int diff = 0, maxd = 0;
            for (int i = 0; i < px.Length; i += 4)
            {
                int d = Math.Max(Math.Max(Math.Abs(px[i] - refPx[i]), Math.Abs(px[i + 1] - refPx[i + 1])), Math.Max(Math.Abs(px[i + 2] - refPx[i + 2]), Math.Abs(px[i + 3] - refPx[i + 3])));
                if (d > 0) { diff++; if (d > maxd) maxd = d; }
            }
            PixelNotes.Add($"TEXTPIX {v.Name,-16} {diff} px differ from exact (max channel {maxd})");
        }
    }
}
