using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossRuntimeTests;

/// <summary>
/// Stacked NineSlices at fractional positions (#8072, ported from the SkiaGum seam harness of #8069). A ListBox
/// moved by a scrollbar sends fractional offsets, and adjacent items must show no seam. A runtime's test renders a
/// <see cref="Layout"/> of <see cref="TextureSpec"/> items into a <see cref="Frame"/> and <see cref="CountSeams"/>
/// reads the center column and row as runs of color and counts a gap (background inside the stack), a hairline (a
/// color the texture does not have), outline loss (an edge line thinner than the texture's) and overlap (an edge
/// line thicker, or a fill band shorter than the item, i.e. two items covering the same rows). The sweeps are the
/// same on every runtime: scroll steps of 0.0126123 and seeded random offsets, at scale 1 and 2, touching and spaced.
/// </summary>
public static class NineSliceSeamSweep
{
    public readonly record struct Rgb(byte R, byte G, byte B)
    {
        public override string ToString() => $"({R},{G},{B})";
    }

    public static readonly Rgb Background = new(255, 0, 255);
    public static readonly Rgb Outline = new(0xAA, 0xAA, 0xAA);
    public static readonly Rgb Fill = new(255, 255, 255);
    public static readonly Rgb SyntheticBorder = new(200, 40, 40);
    public static readonly Rgb SyntheticFill = new(40, 40, 200);

    /// <summary>One texture to stack: its name, the texel width of its edge line, its two colors, and for a texture
    /// cut from the embedded Forms sheet the source rectangle (left, top, width, height); null for a synthetic one.</summary>
    public sealed record TextureSpec(string Name, int Border, Rgb EdgeColor, Rgb InnerColor, int[]? Source)
    {
        public override string ToString() => Name;

        /// <summary>The color of a texel of the synthetic texture (3 x border square, edge ring then fill).</summary>
        public Rgb SyntheticTexel(int x, int y)
        {
            int size = Border * 3;
            bool edge = x < Border || y < Border || x >= size - Border || y >= size - Border;
            return edge ? EdgeColor : InnerColor;
        }
    }

    // Border 1 is the Forms "Bordered" tile: a one-texel gray line, with the nine-slice cut at 8 texels.
    public static readonly TextureSpec FormsBordered = new("FormsBordered", 1, Outline, Fill, new[] { 24, 48, 24, 24 });

    public static TextureSpec Synthetic(int border) => new("Synthetic" + border, border, SyntheticBorder, SyntheticFill, null);

    public static IEnumerable<TextureSpec> SweepSpecs() => new[] { FormsBordered, Synthetic(1), Synthetic(3), Synthetic(6) };

    /// <summary>The geometry of a stack, in world units. Items touch when spacing is 0.</summary>
    public sealed record Layout(float Left, float BaseTop, float Width, float ItemHeight, float Spacing, int Count, float Scroll, float Zoom)
    {
        public float ItemTop(int i) => BaseTop - Scroll + i * (ItemHeight + Spacing);

        public int PixelWidth => (int)MathF.Ceiling((Left + Width + 4) * Zoom);

        public int PixelHeight => (int)MathF.Ceiling((BaseTop + Count * (ItemHeight + Spacing) + 4) * Zoom);
    }

    /// <summary>Rendered pixels, row-major, top row first.</summary>
    public sealed class Frame
    {
        readonly Rgb[] _pixels;

        public Frame(int width, int height, Rgb[] pixels)
        {
            Width = width;
            Height = height;
            _pixels = pixels;
        }

        public int Width { get; }

        public int Height { get; }

        public Rgb this[int x, int y] => _pixels[(y * Width) + x];

        /// <summary>Writes the frame as a PNG, each pixel magnified to a square so a one-pixel line can be seen.</summary>
        public void SavePng(string path, int magnify = 6)
        {
            int w = Width * magnify;
            int h = Height * magnify;
            byte[] raw = new byte[h * ((w * 3) + 1)];
            int o = 0;
            for (int y = 0; y < h; y++)
            {
                raw[o++] = 0;
                for (int x = 0; x < w; x++)
                {
                    Rgb p = this[x / magnify, y / magnify];
                    raw[o++] = p.R;
                    raw[o++] = p.G;
                    raw[o++] = p.B;
                }
            }
            using var compressed = new System.IO.MemoryStream();
            using (var z = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
            {
                z.Write(raw, 0, raw.Length);
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            using var file = System.IO.File.Create(path);
            file.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            var header = new byte[13];
            WriteInt(header, 0, w);
            WriteInt(header, 4, h);
            header[8] = 8;
            header[9] = 2;
            WriteChunk(file, "IHDR", header);
            WriteChunk(file, "IDAT", compressed.ToArray());
            WriteChunk(file, "IEND", Array.Empty<byte>());
        }

        static void WriteInt(byte[] b, int at, int v)
        {
            b[at] = (byte)(v >> 24);
            b[at + 1] = (byte)(v >> 16);
            b[at + 2] = (byte)(v >> 8);
            b[at + 3] = (byte)v;
        }

        static void WriteChunk(System.IO.Stream s, string type, byte[] data)
        {
            var length = new byte[4];
            WriteInt(length, 0, data.Length);
            s.Write(length);
            byte[] body = new byte[4 + data.Length];
            for (int i = 0; i < 4; i++)
            {
                body[i] = (byte)type[i];
            }
            Array.Copy(data, 0, body, 4, data.Length);
            s.Write(body);
            uint crc = 0xFFFFFFFF;
            foreach (byte b in body)
            {
                crc ^= b;
                for (int k = 0; k < 8; k++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
                }
            }
            var crcBytes = new byte[4];
            WriteInt(crcBytes, 0, (int)~crc);
            s.Write(crcBytes);
        }
    }

    public sealed class SeamCounts
    {
        public int Frames, Gap, Hairline, OutlineLoss, Overlap;
        public string? FirstExample;
        public int Total => Gap + Hairline + OutlineLoss + Overlap;

        public override string ToString() =>
            $"{Frames} frames: gap {Gap}, hairline {Hairline}, outline loss {OutlineLoss}, overlap {Overlap}"
            + (FirstExample == null ? "" : $"; first: {FirstExample}");
    }

    // A tenth of each sweep locally, all of it on GitHub Actions or with GUM_FULL_SWEEPS=1 (the same rule as
    // MonoGameGum.Tests' SweepScale), so a local run stays a few seconds.
    static readonly int Stride =
        Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" || Environment.GetEnvironmentVariable("GUM_FULL_SWEEPS") == "1"
            ? 1 : 10;

    // A pixel whose center is within this of an edge is left out of "inside": float noise decides which side it falls on.
    const float Epsilon = 0.02f;

    static (int First, int Last) PixelsInside(float low, float high) =>
        ((int)MathF.Ceiling(low - 0.5f + Epsilon), (int)MathF.Floor(high - 0.5f - Epsilon));

    // Runs of equal color along the line, from the first pixel that is not background to the last.
    static List<(Rgb Color, int Length)> RunsAlong(Frame frame, int fixedCoordinate, bool vertical)
    {
        int length = vertical ? frame.Height : frame.Width;
        Rgb Get(int i) => vertical ? frame[fixedCoordinate, i] : frame[i, fixedCoordinate];
        int start = 0;
        while (start < length && Get(start) == Background)
        {
            start++;
        }
        int end = length - 1;
        while (end > start && Get(end) == Background)
        {
            end--;
        }
        var runs = new List<(Rgb, int)>();
        for (int i = start; i <= end; i++)
        {
            Rgb c = Get(i);
            if (runs.Count > 0 && runs[^1].Item1 == c)
            {
                runs[^1] = (c, runs[^1].Item2 + 1);
            }
            else
            {
                runs.Add((c, 1));
            }
        }
        return runs;
    }

    public static void CountSeams(Frame frame, TextureSpec spec, Layout layout, SeamCounts counts, string label)
    {
        counts.Frames++;
        float zoom = layout.Zoom;
        int band = (int)MathF.Round(spec.Border * zoom);
        int spacing = (int)MathF.Round(layout.Spacing * zoom);
        (int firstX, int lastX) = PixelsInside(layout.Left * zoom, (layout.Left + layout.Width) * zoom);
        int centerX = (firstX + lastX) / 2;
        float topEdge = layout.ItemTop(0) * zoom;
        int centerY = (int)MathF.Round(topEdge + layout.ItemHeight * zoom / 2);

        void Add(ref int counter, string what)
        {
            counter++;
            counts.FirstExample ??= $"{label}: {what}";
        }

        // Vertical: edge line, fill, edge line (touching items: one line of twice the width), and so on, with the
        // spacing as background between spaced items.
        var runs = RunsAlong(frame, centerX, vertical: true);
        int items = 0;
        for (int r = 0; r < runs.Count; r++)
        {
            (Rgb color, int length) = runs[r];
            bool isEdge = color == spec.EdgeColor;
            bool isFill = color == spec.InnerColor;
            bool isBackground = color == Background;
            if (!isEdge && !isFill && !isBackground)
            {
                Add(ref counts.Hairline, $"color {color} for {length} rows");
            }
            else if (isBackground)
            {
                // Only legal between spaced items, and only as wide as the spacing (give or take a rounding pixel).
                if (spacing == 0 || Math.Abs(length - spacing) > 1)
                {
                    Add(ref counts.Gap, $"{length} background rows inside the stack (spacing {spacing})");
                }
            }
            else if (isEdge)
            {
                bool first = r == 0;
                bool last = r == runs.Count - 1;
                bool touchingJoin = spacing == 0 && !first && !last;
                int expected = touchingJoin ? 2 * band : band;
                if (length < expected)
                {
                    Add(ref counts.OutlineLoss, $"edge line {length} rows, expected {expected}");
                }
                else if (length > expected)
                {
                    Add(ref counts.Overlap, $"edge line {length} rows, expected {expected}");
                }
            }
            else
            {
                items++;
                int expectedFill = (int)MathF.Round(layout.ItemHeight * zoom) - 2 * band;
                if (length < expectedFill - 1)
                {
                    Add(ref counts.Overlap, $"fill {length} rows, expected {expectedFill}");
                }
                else if (length > expectedFill + 1)
                {
                    Add(ref counts.Gap, $"fill {length} rows, expected {expectedFill}");
                }
            }
        }
        if (items != layout.Count)
        {
            Add(ref counts.Gap, $"{items} items found, expected {layout.Count}");
        }

        // Horizontal: edge line, fill, edge line.
        var across = RunsAlong(frame, centerY, vertical: false);
        foreach ((Rgb color, int length) in across)
        {
            if (color != spec.EdgeColor && color != spec.InnerColor)
            {
                Add(ref counts.Hairline, $"color {color} for {length} columns across");
            }
        }
        if (across.Count == 3)
        {
            if (across[0].Length < band || across[2].Length < band)
            {
                Add(ref counts.OutlineLoss, $"side line {across[0].Length}/{across[2].Length} columns, expected {band}");
            }
            else if (across[0].Length > band || across[2].Length > band)
            {
                Add(ref counts.Overlap, $"side line {across[0].Length}/{across[2].Length} columns, expected {band}");
            }
        }
        else
        {
            Add(ref counts.OutlineLoss, $"{across.Count} runs across the item, expected 3");
        }
    }

    /// <summary>
    /// Wraps <paramref name="render"/> so that, when GUM_SEAM_DUMP names a folder, the first frame of each spec and up to
    /// three frames with a seam are written there as magnified PNGs. Without the variable it only passes frames through.
    /// </summary>
    public static Func<TextureSpec, Layout, Frame> Dumping(string runtime, string caseName, Func<TextureSpec, Layout, Frame> render)
    {
        string? folder = Environment.GetEnvironmentVariable("GUM_SEAM_DUMP");
        int seams = 0;
        var sampled = new HashSet<string>();
        return (spec, layout) =>
        {
            Frame frame = render(spec, layout);
            if (!string.IsNullOrEmpty(folder))
            {
                string stem = System.IO.Path.Combine(folder, $"{runtime}-{caseName}-{spec.Name}");
                if (sampled.Add(spec.Name))
                {
                    frame.SavePng($"{stem}-sample.png");
                }
                if (seams < 3)
                {
                    var one = new SeamCounts();
                    CountSeams(frame, spec, layout, one, "");
                    if (one.Total > 0)
                    {
                        seams++;
                        frame.SavePng($"{stem}-seam{seams}-scroll{layout.Scroll:F4}.png");
                    }
                }
            }
            return frame;
        };
    }

    /// <summary>
    /// Scroll positions k * 0.0126123 (about 160 of them across 2 px) so edges land on every fraction, for each
    /// sweep spec and an item height of 30.698242 (a Forms line height) and 20.5.
    /// </summary>
    public static SeamCounts SweepSteps(Func<TextureSpec, Layout, Frame> render, float zoom, float spacing)
    {
        var counts = new SeamCounts();
        foreach (TextureSpec spec in SweepSpecs())
        {
            foreach (float height in new[] { 30.698242f, 20.5f })
            {
                for (int k = 0; k <= 158; k += Stride)
                {
                    var layout = new Layout(8.35f, 8.698242f, 48.5f, height, spacing, 4, k * 0.0126123f, zoom);
                    CountSeams(render(spec, layout), spec, layout, counts, $"{spec.Name} height {height} scroll {layout.Scroll:F4}");
                }
            }
        }
        return counts;
    }

    /// <summary>800 frames at random base positions, scrolls, offsets and sizes from a fixed seed.</summary>
    public static SeamCounts SweepRandom(Func<TextureSpec, Layout, Frame> render, float zoom, float spacing)
    {
        var random = new Random(8072 + (int)zoom * 10 + (int)spacing);
        var counts = new SeamCounts();
        TextureSpec[] specs = SweepSpecs().ToArray();
        for (int i = 0; i < 800 / Stride; i++)
        {
            TextureSpec spec = specs[i % specs.Length];
            float baseTop = 8.698242f + random.Next(0, 160) * 0.0126123f;
            float scroll = (float)(random.NextDouble() * 2);
            float left = 4f + (float)random.NextDouble() * 6f;
            float height = i % 3 == 0 ? 30.698242f : 18f + (float)random.NextDouble() * 16f;
            var layout = new Layout(left, baseTop, 40f + (float)random.NextDouble() * 20f, height, spacing, 4, scroll, zoom);
            CountSeams(render(spec, layout), spec, layout, counts,
                $"{spec.Name} left {left:F4} base {baseTop:F4} scroll {scroll:F4} height {height:F4}");
        }
        return counts;
    }
}
