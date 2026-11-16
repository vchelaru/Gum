using SkiaSharp;
using System.Collections.Generic;

namespace RenderingLibrary.Graphics;

/// <summary>SPIKE #8116 img agent: records every image draw of one frame with its canvas state, so a host can rebuild
/// the same native op stream with one property changed (GUM_IMGBENCH in BrutalGum.OGL's FlushProbe).</summary>
public static class ImageDrawCapture
{
    public enum DrawKind { Lattice, NineSliceSection, SpriteRect, SpriteWhole, Atlas }

    public sealed class Entry
    {
        public DrawKind Kind;
        public SKImage Image = null!;
        public int[]? XDivs, YDivs;
        public SKRectI? Bounds;
        public SKRect Src, Dst;
        public SKSamplingOptions Sampling;
        public SKPaint? Paint;
        public SKMatrix Matrix;
        public SKRectI Clip;
        public SKRect[]? AtlasSources;
        public SKRotationScaleMatrix[]? AtlasTransforms;
        public SKColor[]? AtlasColors;
    }

    public static bool Active;
    public static readonly List<Entry> Entries = new();

    public static void Add(DrawKind kind, SKCanvas canvas, SKImage image, SKRect src, SKRect dst, SKSamplingOptions sampling, SKPaint? paint,
        int[]? xDivs = null, int[]? yDivs = null, SKRectI? bounds = null)
    {
        Entries.Add(new Entry
        {
            Kind = kind, Image = image, Src = src, Dst = dst, Sampling = sampling, Paint = paint?.Clone(),
            XDivs = (int[]?)xDivs?.Clone(), YDivs = (int[]?)yDivs?.Clone(), Bounds = bounds,
            Matrix = canvas.TotalMatrix, Clip = canvas.DeviceClipBounds,
        });
    }

    public static void AddAtlas(SKCanvas canvas, SKImage image, SKRect[] sources, SKRotationScaleMatrix[] transforms, SKColor[]? colors,
        SKSamplingOptions sampling, SKPaint? paint)
    {
        Entries.Add(new Entry
        {
            Kind = DrawKind.Atlas, Image = image, Sampling = sampling, Paint = paint?.Clone(),
            AtlasSources = (SKRect[])sources.Clone(), AtlasTransforms = (SKRotationScaleMatrix[])transforms.Clone(),
            AtlasColors = (SKColor[]?)colors?.Clone(), Matrix = canvas.TotalMatrix, Clip = canvas.DeviceClipBounds,
        });
    }
}
