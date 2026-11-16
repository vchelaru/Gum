using SkiaSharp;
using System;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>SPIKE #8116 textops agent: records every text-blob draw of one frame with its canvas state and the glyph data
/// the blob was built from, so a host can rebuild the same native op stream with one property changed (GUM_TEXTBENCH).</summary>
public static class TextDrawCapture
{
    public static readonly bool Enabled = Environment.GetEnvironmentVariable("GUM_TEXTBENCH") != null;

    public sealed class Entry
    {
        public SKTextBlob Blob = null!;
        public float X, Y;
        public SKPaint Paint = null!;
        public SKMatrix Matrix;
        public SKRectI Clip;
        public object Owner = null!;
        public ushort[] Glyphs = null!;
        public SKPoint[] Positions = null!;
        public SKTypeface Typeface = null!;
        public float Size;
        public SKFontEdging Edging;
        public SKFontHinting Hinting;
        public bool Subpixel;
    }

    public static bool Active;
    public static readonly List<Entry> Entries = new();

    public static void Add(SKCanvas canvas, SKTextBlob blob, float x, float y, SKPaint paint, object owner,
        ushort[] glyphs, SKPoint[] positions, SKTypeface typeface, float size, SKFontEdging edging, SKFontHinting hinting, bool subpixel)
    {
        Entries.Add(new Entry
        {
            Blob = blob, X = x, Y = y, Paint = paint.Clone(), Matrix = canvas.TotalMatrix, Clip = canvas.DeviceClipBounds, Owner = owner,
            Glyphs = glyphs, Positions = positions, Typeface = typeface, Size = size, Edging = edging, Hinting = hinting, Subpixel = subpixel,
        });
    }
}
