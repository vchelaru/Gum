using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SkiaSharp;

namespace Gum;

/// <summary>SPIKE (#8116 skiaops): the busy scene's mix as raw SkiaSharp calls, no Gum. GUM_RAWMODE=1..7 picks how NineSlices
/// (and in 6/7 rects and texts) are expressed; GUM_RAWONLY=ns|text|rect draws only that kind. Run with --region empty.</summary>
public static class RawScene
{
    public static readonly int Mode = int.TryParse(Environment.GetEnvironmentVariable("GUM_RAWMODE"), out int m) ? m : 0;
    static readonly string? Only = Environment.GetEnvironmentVariable("GUM_RAWONLY");
    public static long RecordTicks;

    const int NsCount = 131, RectCount = 198, SpriteCount = 12, Cols = 22;
    static SKImage? _image;
    static SKTextBlob[] _blobs = null!;
    static ushort[][] _glyphs = null!;
    static SKPoint[][] _glyphPos = null!; // relative to the text origin
    static SKFont _font = null!;
    static SKPaint _imgPaint = null!, _textPaint = null!, _vertPaint = null!, _atlasPaint = null!, _rectVertPaint = null!;
    static SKPaint[] _rectPaints = null!;
    static readonly SKColor[] Palette = { new(60, 60, 70), new(80, 90, 110), new(50, 70, 60), new(90, 60, 60) };
    static readonly SKLattice Lattice = new() { XDivs = new[] { 8, 16 }, YDivs = new[] { 8, 16 } };
    static readonly SKSamplingOptions Linear = new(SKFilterMode.Linear);

    static SKRect Cell(int k) { float x = 4 + (k % Cols) * 58, y = 4 + (k / Cols) * 40; return new SKRect(x, y, x + 52, y + 36); }

    static void Init(GRContext gr)
    {
        using var bmp = new SKBitmap(24, 24);
        using (var c = new SKCanvas(bmp))
        {
            c.Clear(SKColors.DarkSlateGray);
            using var p = new SKPaint { Color = SKColors.LightGray }; c.DrawRect(4, 4, 16, 16, p);
            using var q = new SKPaint { Color = SKColors.OrangeRed }; c.DrawRect(0, 0, 3, 24, q); c.DrawRect(10, 0, 3, 3, q); // asymmetry so stretch errors show
        }
        _image = SKImage.FromBitmap(bmp).ToTextureImage(gr);
        _font = new SKFont(SKTypeface.Default, 16);
        int texts = NsCount + RectCount;
        _blobs = new SKTextBlob[texts]; _glyphs = new ushort[texts][]; _glyphPos = new SKPoint[texts][];
        for (int i = 0; i < texts; i++)
        {
            string s = "Item " + i;
            _blobs[i] = SKTextBlob.Create(s, _font)!;
            _glyphs[i] = _font.GetGlyphs(s);
            _glyphPos[i] = _font.GetGlyphPositions(_glyphs[i], SKPoint.Empty);
        }
        _imgPaint = new SKPaint();
        _atlasPaint = new SKPaint();
        _textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        _vertPaint = new SKPaint { Shader = _image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, Linear) };
        _rectVertPaint = new SKPaint { Color = SKColors.White };
        _rectPaints = Array.ConvertAll(Palette, c => new SKPaint { Color = c });
        Parity(gr);
    }

    public static void Draw(SKCanvas canvas, GRContext gr)
    {
        if (_image == null) Init(gr);
        long t0 = Stopwatch.GetTimestamp();
        bool ns = Only == null || Only.Contains("ns"), text = Only == null || Only.Contains("text"), rect = Only == null || Only.Contains("rect"), sprite = Only == null || Only.Contains("sprite");
        int nsMode = Mode is 6 ? 4 : Mode is 7 ? 5 : Mode;
        bool batchedNs = nsMode is 4 or 5, batchedRest = Mode is 6 or 7;

        if (ns && batchedNs) DrawNsBatch(canvas, nsMode, 0, NsCount);
        for (int i = 0; i < NsCount; i++)
        {
            if (ns && !batchedNs) DrawNs(canvas, nsMode, Cell(i));
            if (text && !batchedRest) DrawText(canvas, i);
        }
        if (batchedRest)
        {
            if (rect) DrawRectBatch(canvas);
            if (text) DrawTextBatch(canvas);
        }
        else
        {
            for (int j = 0; j < RectCount; j++)
            {
                int k = NsCount + j;
                if (rect) canvas.DrawRect(Cell(k), _rectPaints[j % _rectPaints.Length]);
                if (text) DrawText(canvas, k);
            }
        }
        if (sprite)
            for (int s = 0; s < SpriteCount; s++)
            {
                float x = 1200, y = 10 + s * 50;
                canvas.DrawImage(_image!, new SKRect(0, 0, 24, 24), new SKRect(x, y, x + 24, y + 24), Linear, _imgPaint);
            }
        RecordTicks = Stopwatch.GetTimestamp() - t0;
    }

    static void DrawText(SKCanvas canvas, int k) { var r = Cell(k); canvas.DrawText(_blobs[k], r.Left + 8, r.Top + 24, _textPaint); }

    static readonly float[] Src = { 0, 8, 16, 24 };
    static void DstEdges(SKRect d, Span<float> xs, Span<float> ys)
    {
        xs[0] = d.Left; xs[1] = d.Left + 8; xs[2] = d.Right - 8; xs[3] = d.Right;
        ys[0] = d.Top; ys[1] = d.Top + 8; ys[2] = d.Bottom - 8; ys[3] = d.Bottom;
    }

    static void DrawNs(SKCanvas canvas, int mode, SKRect dst)
    {
        switch (mode)
        {
            case 2: canvas.DrawImageNinePatch(_image!, new SKRectI(8, 8, 16, 16), dst, SKFilterMode.Linear, _imgPaint); break;
            case 3:
                Span<float> xs = stackalloc float[4], ys = stackalloc float[4];
                DstEdges(dst, xs, ys);
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                        canvas.DrawImage(_image!, new SKRect(Src[c], Src[r], Src[c + 1], Src[r + 1]), new SKRect(xs[c], ys[r], xs[c + 1], ys[r + 1]), Linear, _imgPaint);
                break;
            default: canvas.DrawImageLattice(_image!, Lattice, dst, SKFilterMode.Linear, _imgPaint); break;
        }
    }

    static SKRect[]? _atlasSrc; static SKRotationScaleMatrix[]? _atlasXf;
    static SKPoint[]? _nsPos, _nsTex; static ushort[]? _nsIdx;

    static void DrawNsBatch(SKCanvas canvas, int mode, int from, int count, Func<int, SKRect>? cellOf = null)
    {
        cellOf ??= Cell;
        Span<float> xs = stackalloc float[4], ys = stackalloc float[4];
        if (mode == 4)
        {
            // RSXform is rotation + UNIFORM scale: stretched cells cannot be expressed. Proxy: each cell scaled uniformly by its
            // destination height (wrong width on horizontal stretch). Timing proxy only; parity reports the difference.
            int n = count * 9;
            if (_atlasSrc == null || _atlasSrc.Length != n) { _atlasSrc = new SKRect[n]; _atlasXf = new SKRotationScaleMatrix[n]; }
            int w = 0;
            for (int i = 0; i < count; i++)
            {
                DstEdges(cellOf(from + i), xs, ys);
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++, w++)
                    {
                        _atlasSrc[w] = new SKRect(Src[c], Src[r], Src[c + 1], Src[r + 1]);
                        float scale = (ys[r + 1] - ys[r]) / 8f;
                        _atlasXf![w] = new SKRotationScaleMatrix(scale, 0, xs[c], ys[r]);
                    }
            }
            canvas.DrawAtlas(_image!, _atlasSrc, _atlasXf!, Linear, _atlasPaint);
        }
        else
        {
            int nv = count * 16, ni = count * 54;
            if (_nsPos == null || _nsPos.Length != nv) { _nsPos = new SKPoint[nv]; _nsTex = new SKPoint[nv]; _nsIdx = new ushort[ni]; }
            int ii = 0;
            for (int i = 0; i < count; i++)
            {
                DstEdges(cellOf(from + i), xs, ys);
                int b = i * 16;
                for (int r = 0; r < 4; r++)
                    for (int c = 0; c < 4; c++) { _nsPos[b + r * 4 + c] = new SKPoint(xs[c], ys[r]); _nsTex![b + r * 4 + c] = new SKPoint(Src[c], Src[r]); }
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                    {
                        ushort v = (ushort)(b + r * 4 + c);
                        _nsIdx![ii++] = v; _nsIdx[ii++] = (ushort)(v + 1); _nsIdx[ii++] = (ushort)(v + 4);
                        _nsIdx[ii++] = (ushort)(v + 1); _nsIdx[ii++] = (ushort)(v + 5); _nsIdx[ii++] = (ushort)(v + 4);
                    }
            }
            using var verts = SKVertices.CreateCopy(SKVertexMode.Triangles, _nsPos, _nsTex, null, _nsIdx);
            canvas.DrawVertices(verts, SKBlendMode.Modulate, _vertPaint);
        }
    }

    static SKPoint[]? _rPos; static SKColor[]? _rCol; static ushort[]? _rIdx;
    static void DrawRectBatch(SKCanvas canvas)
    {
        int nv = RectCount * 4;
        if (_rPos == null) { _rPos = new SKPoint[nv]; _rCol = new SKColor[nv]; _rIdx = new ushort[RectCount * 6]; }
        for (int j = 0; j < RectCount; j++)
        {
            var r = Cell(NsCount + j); var col = Palette[j % Palette.Length]; int b = j * 4;
            _rPos[b] = new SKPoint(r.Left, r.Top); _rPos[b + 1] = new SKPoint(r.Right, r.Top); _rPos[b + 2] = new SKPoint(r.Left, r.Bottom); _rPos[b + 3] = new SKPoint(r.Right, r.Bottom);
            _rCol![b] = _rCol[b + 1] = _rCol[b + 2] = _rCol[b + 3] = col;
            int x = j * 6; _rIdx![x] = (ushort)b; _rIdx[x + 1] = (ushort)(b + 1); _rIdx[x + 2] = (ushort)(b + 2); _rIdx[x + 3] = (ushort)(b + 1); _rIdx[x + 4] = (ushort)(b + 3); _rIdx[x + 5] = (ushort)(b + 2);
        }
        using var verts = SKVertices.CreateCopy(SKVertexMode.Triangles, _rPos, null, _rCol, _rIdx);
        canvas.DrawVertices(verts, SKBlendMode.Dst, _rectVertPaint);
    }

    static readonly SKTextBlobBuilder _builder = new();
    static SKPoint[] _posScratch = new SKPoint[64];
    static void DrawTextBatch(SKCanvas canvas)
    {
        int texts = NsCount + RectCount;
        for (int k = 0; k < texts; k++)
        {
            var r = Cell(k); var rel = _glyphPos[k];
            if (_posScratch.Length < rel.Length) _posScratch = new SKPoint[rel.Length];
            for (int g = 0; g < rel.Length; g++) _posScratch[g] = new SKPoint(rel[g].X + r.Left + 8, rel[g].Y + r.Top + 24);
            _builder.AddPositionedRun(_glyphs[k], _font, _posScratch.AsSpan(0, rel.Length));
        }
        using var blob = _builder.Build();
        if (blob != null) canvas.DrawText(blob, 0, 0, _textPaint);
    }

    // ---- pixel parity: each NineSlice variant vs lattice, offscreen GPU surface, one readback per variant ----
    static void Parity(GRContext gr)
    {
        var dsts = new[] { new SKRect(10, 10, 62, 46), new SKRect(80.5f, 10.25f, 180.5f, 40.25f), new SKRect(10, 60, 30, 140), new SKRect(60.3f, 70.7f, 250.3f, 130.7f) };
        var info = new SKImageInfo(260, 150, SKColorType.Rgba8888, SKAlphaType.Premul);
        byte[] Render(int mode)
        {
            using var surf = SKSurface.Create(gr, false, info)!;
            var c = surf.Canvas; c.Clear(SKColors.Black);
            if (mode is 4 or 5) DrawNsBatch(c, mode, 0, dsts.Length, i => dsts[i]);
            else foreach (var d in dsts) DrawNs(c, mode, d);
            var bytes = new byte[info.BytesSize];
            unsafe { fixed (byte* p = bytes) surf.ReadPixels(info, (IntPtr)p, info.RowBytes, 0, 0); }
            return bytes;
        }
        var baseBytes = Render(1);
        var lines = new List<string>();
        foreach (int mode in new[] { 2, 3, 4, 5 })
        {
            var b = Render(mode);
            int diffPx = 0, maxD = 0;
            for (int i = 0; i < b.Length; i += 4)
            {
                int d = 0; for (int ch = 0; ch < 4; ch++) d = Math.Max(d, Math.Abs(b[i + ch] - baseBytes[i + ch]));
                if (d > 0) diffPx++; maxD = Math.Max(maxD, d);
            }
            lines.Add($"mode {mode} vs lattice: {diffPx} of {b.Length / 4} px differ, max channel diff {maxD}");
        }
        string? outPath = Environment.GetEnvironmentVariable("GUM_RAWPARITY");
        if (outPath != null) File.WriteAllLines(outPath, lines);
    }
}
