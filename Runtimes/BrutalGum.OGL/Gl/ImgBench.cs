using System;
using System.Collections.Generic;
using System.Diagnostics;
using RenderingLibrary.Graphics;
using SkiaSharp;
using E = RenderingLibrary.Graphics.ImageDrawCapture.Entry;
using K = RenderingLibrary.Graphics.ImageDrawCapture.DrawKind;

namespace Gum;

/// <summary>SPIKE #8116 img agent: GUM_IMGBENCH=1 captures every image draw of frame 300 (ImageDrawCapture), rebuilds it as
/// native-only SKPictures with ONE property changed per variant, and from then on plays each variant every frame onto the
/// back buffer after Gum's own frame, timing DrawPicture (Ganesh op creation) and gr.Flush separately.</summary>
public static class ImgBench
{
    public static readonly bool Enabled = Environment.GetEnvironmentVariable("GUM_IMGBENCH") != null;
    static int _frame;
    static readonly List<(string Name, SKPicture Pic, int Draws)> Variants = new();
    static readonly List<List<double>> Rec = new(), Flush = new();
    static SKImage? _rawImage;

    public static void Begin()
    {
        if (!Enabled) return;
        _frame++;
        if (_frame == 300) { ImageDrawCapture.Entries.Clear(); ImageDrawCapture.Active = true; }
    }

    public static void End(SKCanvas canvas, GRContext gr)
    {
        if (!Enabled) return;
        if (_frame == 300) { ImageDrawCapture.Active = false; Build(); return; }
        if (Variants.Count == 0) return;
        gr.Flush(false); // Gum's own frame stays out of the variant timings
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
        for (int i = 0; i < Variants.Count; i++)
        {
            double r = Median(Rec[i]), f = Median(Flush[i]);
            int d = Variants[i].Draws;
            Console.WriteLine($"IMGBENCH {Variants[i].Name,-14} draws {d,4}  record {r:F4} ms ({(d > 0 ? r * 1000 / d : 0):F2} us/draw)  flush {f:F4} ms ({(d > 0 ? f * 1000 / d : 0):F2} us/draw)");
        }
    }

    static SKPicture Make(Func<E, bool> include, Action<SKCanvas, E> draw, bool clip, Func<E, SKMatrix>? matrix, out int draws)
    {
        using var rec = new SKPictureRecorder();
        var c = rec.BeginRecording(new SKRect(0, 0, 4096, 4096));
        SKRectI? curClip = null; SKMatrix? curM = null; bool saved = false; draws = 0;
        foreach (var e in ImageDrawCapture.Entries)
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

    static SKLattice Lat(E e, bool bounds = true) => new() { XDivs = e.XDivs, YDivs = e.YDivs, Bounds = bounds ? e.Bounds : null };

    static void Exact(SKCanvas c, E e, SKPaint? paint, SKImage? img = null)
    {
        var image = img ?? e.Image;
        switch (e.Kind)
        {
            case K.Lattice: c.DrawImageLattice(image, Lat(e), e.Dst, paint); break;
            case K.SpriteWhole: c.DrawImage(image, e.Dst, e.Sampling, paint); break;
            case K.Atlas:
                if (e.AtlasColors != null) c.DrawAtlas(image, e.AtlasSources!, e.AtlasTransforms!, e.AtlasColors, SKBlendMode.Modulate, e.Sampling, paint);
                else c.DrawAtlas(image, e.AtlasSources!, e.AtlasTransforms!, e.Sampling, paint);
                break;
            default: c.DrawImage(image, e.Src, e.Dst, e.Sampling, paint); break;
        }
    }

    static void Add(string name, Func<E, bool> include, Action<SKCanvas, E> draw, bool clip = true, Func<E, SKMatrix>? matrix = null)
    {
        var pic = Make(include, draw, clip, matrix, out int d);
        Variants.Add((name, pic, d)); Rec.Add(new()); Flush.Add(new());
    }

    // Lattices as device-space DrawVertices batches (one call per run of same image + clip), tint as vertex colors with Modulate.
    static void AddVerts(string name, bool tinted, bool onlyLattices)
    {
        using var rec = new SKPictureRecorder();
        var c = rec.BeginRecording(new SKRect(0, 0, 4096, 4096));
        var pos = new List<SKPoint>(); var tex = new List<SKPoint>(); var col = new List<SKColor>();
        SKImage? batchImage = null; SKRectI? curClip = null; SKMatrix? curM = null; bool saved = false; int calls = 0, draws = 0;
        void FlushBatch()
        {
            if (pos.Count == 0) return;
            c.SetMatrix(SKMatrix.Identity); curM = SKMatrix.Identity;
            using var v = SKVertices.CreateCopy(SKVertexMode.Triangles, pos.ToArray(), tex.ToArray(), col.ToArray());
            using var p = new SKPaint { Shader = batchImage!.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, new SKSamplingOptions(SKFilterMode.Nearest)) };
            c.DrawVertices(v, SKBlendMode.Modulate, p); calls++;
            pos.Clear(); tex.Clear(); col.Clear();
        }
        foreach (var e in ImageDrawCapture.Entries)
        {
            if (onlyLattices && e.Kind != K.Lattice) continue;
            bool batchable = e.Kind == K.Lattice && (tinted || e.Paint == null || e.Paint.ColorFilter == null);
            if (curClip == null || curClip.Value != e.Clip || (batchable && batchImage != null && batchImage.UniqueId != e.Image.UniqueId) || !batchable)
            {
                if (curClip == null || curClip.Value != e.Clip)
                {
                    FlushBatch();
                    if (saved) c.Restore();
                    c.Save(); saved = true; c.ResetMatrix();
                    c.ClipRect(new SKRect(e.Clip.Left, e.Clip.Top, e.Clip.Right, e.Clip.Bottom));
                    curClip = e.Clip; curM = null;
                }
                else FlushBatch();
            }
            draws++;
            if (!batchable)
            {
                if (curM == null || !curM.Value.Equals(e.Matrix)) { c.SetMatrix(e.Matrix); curM = e.Matrix; }
                Exact(c, e, e.Paint); calls++;
                continue;
            }
            batchImage = e.Image;
            var b = e.Bounds ?? new SKRectI(0, 0, e.Image.Width, e.Image.Height);
            float[] sx = { b.Left, e.XDivs![0], e.XDivs[1], b.Right }, sy = { b.Top, e.YDivs![0], e.YDivs[1], b.Bottom };
            float[] dx = { e.Dst.Left, e.Dst.Left + (sx[1] - sx[0]), e.Dst.Right - (sx[3] - sx[2]), e.Dst.Right };
            float[] dy = { e.Dst.Top, e.Dst.Top + (sy[1] - sy[0]), e.Dst.Bottom - (sy[3] - sy[2]), e.Dst.Bottom };
            var color = e.Paint?.ColorFilter != null ? e.Paint.Color : new SKColor(255, 255, 255, e.Paint?.Color.Alpha ?? 255);
            var m = e.Matrix;
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
            {
                SKPoint P(int i, int j) => m.MapPoint(dx[i], dy[j]);
                SKPoint T(int i, int j) => new(sx[i], sy[j]);
                pos.Add(P(x, y)); pos.Add(P(x + 1, y)); pos.Add(P(x + 1, y + 1)); pos.Add(P(x, y)); pos.Add(P(x + 1, y + 1)); pos.Add(P(x, y + 1));
                tex.Add(T(x, y)); tex.Add(T(x + 1, y)); tex.Add(T(x + 1, y + 1)); tex.Add(T(x, y)); tex.Add(T(x + 1, y + 1)); tex.Add(T(x, y + 1));
                for (int k = 0; k < 6; k++) col.Add(color);
            }
        }
        FlushBatch();
        if (saved) c.Restore();
        Variants.Add((name, rec.EndRecording(), draws)); Rec.Add(new()); Flush.Add(new());
        Console.WriteLine($"IMGBENCH {name}: {draws} draws as {calls} canvas calls");
    }

    static void Build()
    {
        var entries = ImageDrawCapture.Entries;
        var images = new HashSet<uint>();
        var kinds = new Dictionary<string, int>();
        foreach (var e in entries)
        {
            images.Add(e.Image.UniqueId);
            var p = e.Paint;
            string key = $"{e.Kind} img {e.Image.Width}x{e.Image.Height} cs={(e.Image.ColorSpace == null ? "null" : e.Image.ColorSpace.IsSrgb ? "srgb" : "other")} lazy={e.Image.IsLazyGenerated} tex={e.Image.IsTextureBacked} bounds={(e.Bounds.HasValue ? "sub" : "whole")} " +
                $"m=({e.Matrix.ScaleX},{e.Matrix.ScaleY},fracT={(e.Matrix.TransX % 1 != 0 || e.Matrix.TransY % 1 != 0)}) dstFrac={(e.Dst.Left % 1 != 0 || e.Dst.Top % 1 != 0 || e.Dst.Width % 1 != 0 || e.Dst.Height % 1 != 0)} " +
                $"paint={(p == null ? "null" : $"c={p.Color} cf={p.ColorFilter != null} sh={p.Shader != null} blend={p.BlendMode} aa={p.IsAntialias} if={p.ImageFilter != null} mf={p.MaskFilter != null} style={p.Style}")} " +
                $"samp={e.Sampling.Filter}/{e.Sampling.Mipmap} clipFull={(e.Clip.Width >= 1280 && e.Clip.Height >= 720)}";
            kinds[key] = kinds.TryGetValue(key, out int n) ? n + 1 : 1;
        }
        Console.WriteLine($"IMGCAP {entries.Count} image draws, {images.Count} distinct image ids");
        foreach (var kv in kinds) Console.WriteLine($"IMGCAP {kv.Value,4} x {kv.Key}");

        using (var bmp = new SKBitmap(24, 24))
        {
            using (var cc = new SKCanvas(bmp)) { cc.Clear(SKColors.DarkSlateGray); using var pp = new SKPaint { Color = SKColors.LightGray }; cc.DrawRect(4, 4, 16, 16, pp); }
            _rawImage = SKImage.FromBitmap(bmp);
        }
        var plain = new Dictionary<E, SKPaint>();
        foreach (var e in entries) plain[e] = new SKPaint { Color = e.Paint?.Color ?? SKColors.White };
        SKImage? first = entries.Find(x => x.Kind == K.Lattice)?.Image;
        var rawLattice = new SKLattice { XDivs = new[] { 8, 16 }, YDivs = new[] { 8, 16 } };
        bool All(E e) => true;
        bool IsLat(E e) => e.Kind == K.Lattice;

        Add("empty", e => false, (c, e) => { });
        Add("exact", All, (c, e) => Exact(c, e, e.Paint));
        Add("lattice-only", IsLat, (c, e) => Exact(c, e, e.Paint));
        Add("rect-only", e => e.Kind is K.SpriteRect or K.SpriteWhole or K.NineSliceSection, (c, e) => Exact(c, e, e.Paint));
        Add("atlas-only", e => e.Kind == K.Atlas, (c, e) => Exact(c, e, e.Paint));
        Add("nullpaint", All, (c, e) => Exact(c, e, null));
        Add("plainpaint", All, (c, e) => Exact(c, e, plain[e]));
        Add("noclip", All, (c, e) => Exact(c, e, e.Paint), clip: false);
        Add("lat-nobounds", All, (c, e) => { if (IsLat(e)) c.DrawImageLattice(e.Image, Lat(e, false), e.Dst, e.Paint); else Exact(c, e, e.Paint); });
        Add("lat->rect", All, (c, e) =>
        {
            if (IsLat(e)) { var b = e.Bounds ?? new SKRectI(0, 0, e.Image.Width, e.Image.Height); c.DrawImage(e.Image, new SKRect(b.Left, b.Top, b.Right, b.Bottom), e.Dst, new SKSamplingOptions(SKFilterMode.Nearest), e.Paint); }
            else Exact(c, e, e.Paint);
        });
        Add("lat-linear", All, (c, e) => { if (IsLat(e)) c.DrawImageLattice(e.Image, Lat(e), e.Dst, SKFilterMode.Linear, e.Paint); else Exact(c, e, e.Paint); });
        Add("oneimage", All, (c, e) => Exact(c, e, e.Paint, e.Kind == K.Atlas ? null : first));
        Add("int-matrix", All, (c, e) => Exact(c, e, e.Paint), matrix: e => { var m = e.Matrix; m.TransX = MathF.Round(m.TransX); m.TransY = MathF.Round(m.TransY); return m; });
        Add("ident-matrix", All, (c, e) => Exact(c, e, e.Paint), matrix: e => SKMatrix.Identity);
        Add("raw-lattice", All, (c, e) => { if (IsLat(e)) c.DrawImageLattice(_rawImage, rawLattice, e.Dst, null); else Exact(c, e, e.Paint); });
        Add("lat->9rects", IsLat, (c, e) =>
        {
            var b = e.Bounds ?? new SKRectI(0, 0, e.Image.Width, e.Image.Height);
            float[] sx = { b.Left, e.XDivs![0], e.XDivs[1], b.Right }, sy = { b.Top, e.YDivs![0], e.YDivs[1], b.Bottom };
            float[] dx = { e.Dst.Left, e.Dst.Left + (sx[1] - sx[0]), e.Dst.Right - (sx[3] - sx[2]), e.Dst.Right };
            float[] dy = { e.Dst.Top, e.Dst.Top + (sy[1] - sy[0]), e.Dst.Bottom - (sy[3] - sy[2]), e.Dst.Bottom };
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                c.DrawImage(e.Image, new SKRect(sx[x], sy[y], sx[x + 1], sy[y + 1]), new SKRect(dx[x], dy[y], dx[x + 1], dy[y + 1]), new SKSamplingOptions(SKFilterMode.Nearest), e.Paint);
        });
        // Reference ops at the same places: what any Ganesh op costs, what text costs, what SkCanvas+playback alone cost.
        var solid = new SKPaint { Color = SKColors.CadetBlue };
        Add("ref-solidrect", All, (c, e) => c.DrawRect(e.Kind == K.Atlas ? SKRect.Create(0, 0, 8, 8) : e.Dst, solid));
        var font = new SKFont(SKTypeface.Default, 16);
        var blob = SKTextBlob.Create("42", font)!;
        var white = new SKPaint { Color = SKColors.White, IsAntialias = true };
        Add("ref-textblob", All, (c, e) => c.DrawText(blob, e.Dst.Left + 4, e.Dst.Top + 14, white));
        Add("ref-rejected", All, (c, e) => { if (IsLat(e)) c.DrawImageLattice(e.Image, Lat(e), SKRect.Create(-5000, -5000, e.Dst.Width, e.Dst.Height), e.Paint); else c.DrawImage(e.Image, SKRect.Create(-5000, -5000, 10, 10), e.Sampling, e.Paint); });
        Add("rect-nocf", e => e.Kind is K.SpriteRect or K.SpriteWhole or K.NineSliceSection, (c, e) => Exact(c, e, plain[e]));
        Add("lat->rect-nocf", IsLat, (c, e) => { var b = e.Bounds ?? new SKRectI(0, 0, e.Image.Width, e.Image.Height); c.DrawImage(e.Image, new SKRect(b.Left, b.Top, b.Right, b.Bottom), e.Dst, new SKSamplingOptions(SKFilterMode.Nearest), plain[e]); });
        Add("lattice-nocf", IsLat, (c, e) => Exact(c, e, plain[e]));
        AddVerts("lat->verts", tinted: true, onlyLattices: false);
        AddVerts("lat->verts-wht", tinted: false, onlyLattices: false);
        AddVerts("latonly->verts", tinted: true, onlyLattices: true);
        Console.WriteLine($"IMGBENCH built {Variants.Count} variants");
    }
}
