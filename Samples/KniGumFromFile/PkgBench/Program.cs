using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Gum.Bundle;

namespace PkgBench;

// SPIKE #6156 (not for merge).
// Usage: PkgBench <contentDir> <outDir> <realisticProjectDir>
public static class Program
{
    static readonly string[] Names = { "bench1024", "hytale", "MainSpriteSheet" };

    public static void Main(string[] args)
    {
        string content = args[0], outDir = args[1], project = args[2];
        Directory.CreateDirectory(outDir);
        foreach (var name in Names)
        {
            var png = File.ReadAllBytes(Path.Combine(content, name + ".png"));
            var raw = PkgBlobs.DecodePremultiplied(png, out int w, out int h);
            var blobs = new Dictionary<string, byte[]>
            {
                ["raw"] = raw,
                ["deflO"] = PkgBlobs.Deflate(raw, CompressionLevel.Optimal),
                ["deflF"] = PkgBlobs.Deflate(raw, CompressionLevel.Fastest),
                ["deflS"] = PkgBlobs.Deflate(raw, CompressionLevel.SmallestSize),
                ["upDeflO"] = PkgBlobs.Deflate(PkgBlobs.FilterUp(raw, w), CompressionLevel.Optimal),
                ["subDeflO"] = PkgBlobs.Deflate(PkgBlobs.FilterSub(raw), CompressionLevel.Optimal),
                ["br11"] = PkgBlobs.Brotli(raw, 11),
                ["br4"] = PkgBlobs.Brotli(raw, 4),
                ["upBr11"] = PkgBlobs.Brotli(PkgBlobs.FilterUp(raw, w), 11),
                ["pngBr4"] = PkgBlobs.Brotli(png, 4),
            };
            Console.WriteLine($"SIZE {name} {w}x{h} png={png.Length} " + string.Join(" ", blobs.Select(kv => $"{kv.Key}={kv.Value.Length}")));
            foreach (var kv in blobs)
            {
                if (kv.Key != "raw") File.WriteAllBytes(Path.Combine(outDir, $"{name}.{kv.Key}.bin"), kv.Value);
            }
            File.WriteAllText(Path.Combine(outDir, name + ".dim.txt"), $"{w} {h}");

            // Desktop timings (no GPU upload; the earlier spike measured desktop upload at ~0.3 ms).
            var t = new Dictionary<string, List<double>>();
            void Add(string k, double v) { if (!t.TryGetValue(k, out var l)) t[k] = l = new List<double>(); l.Add(v); }
            for (int rep = 0; rep < 8; rep++)
            {
                var sw = Stopwatch.StartNew();
                Inflate(blobs["deflO"], raw.Length); Add("deflO.inflate", sw.Elapsed.TotalMilliseconds);
                sw.Restart(); Inflate(blobs["deflF"], raw.Length); Add("deflF.inflate", sw.Elapsed.TotalMilliseconds);
                sw.Restart(); var u = Inflate(blobs["upDeflO"], raw.Length); UnfilterUp(u, w); Add("upDeflO.inflate+unfilter", sw.Elapsed.TotalMilliseconds);
                sw.Restart(); BrotliSharp(blobs["br11"], raw.Length); Add("br11.BrotliSharpLib", sw.Elapsed.TotalMilliseconds);
                sw.Restart(); var dst = new byte[raw.Length]; BrotliDecoder.TryDecompress(blobs["br11"], dst, out _); Add("br11.BCL", sw.Elapsed.TotalMilliseconds);
                sw.Restart(); var p = BrotliSharp(blobs["pngBr4"], png.Length); Add("pngBr4.BrotliSharpLib", sw.Elapsed.TotalMilliseconds);
                sw.Restart(); var d = StbImageSharp.ImageResult.FromMemory(p, StbImageSharp.ColorComponents.RedGreenBlueAlpha).Data; PkgBlobs.Premultiply(d); Add("png.stbDecode+premul", sw.Elapsed.TotalMilliseconds);
            }
            foreach (var kv in t) Console.WriteLine($"DESK {name} {kv.Key} median={kv.Value.Skip(1).OrderBy(v => v).ElementAt(3):F2}");
        }

        // Realistic project: every file in the folder, packed with the real GumBundleWriter (brotli(tar)).
        var files = Directory.GetFiles(project, "*", SearchOption.AllDirectories)
            .Select(f => (path: Path.GetRelativePath(project, f).Replace('\\', '/'), content: File.ReadAllBytes(f))).ToList();
        var pngs = files.Where(f => f.path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).ToList();
        long pngBytes = pngs.Sum(f => (long)f.content.Length);
        var rawFiles = files.Select(f => f.path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            ? (path: f.path + ".rgba", content: PkgBlobs.DecodePremultiplied(f.content, out _, out _)) : f).ToList();
        var rawOnly = rawFiles.Where(f => f.path.EndsWith(".rgba")).ToList();
        long rawBytes = rawOnly.Sum(f => (long)f.content.Length);
        Console.WriteLine($"PROJECT files={files.Count} pngs={pngs.Count} pngBytes={pngBytes} rawBytes={rawBytes}");
        Console.WriteLine($"PROJECT pkg.png.brotliTar={Pack(files)} pkg.raw.brotliTar={Pack(rawFiles)}");
        Console.WriteLine($"PROJECT zip.png.deflO={Zip(files, CompressionLevel.Optimal)} zip.raw.deflO={Zip(rawFiles, CompressionLevel.Optimal)} zip.raw.deflF={Zip(rawFiles, CompressionLevel.Fastest)}");
        Console.WriteLine($"PROJECT raw.perEntryBrotli11.sum={rawOnly.Sum(f => (long)PkgBlobs.Brotli(f.content, 11).Length)}");
    }

    static long Pack(List<(string path, byte[] content)> entries)
    {
        using var ms = new MemoryStream(); GumBundleWriter.Write(ms, entries); return ms.Length;
    }

    static long Zip(List<(string path, byte[] content)> entries, CompressionLevel level)
    {
        using var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (p, c) in entries) { using var s = z.CreateEntry(p, level).Open(); s.Write(c, 0, c.Length); }
        }
        return ms.Length;
    }

    public static byte[] Inflate(byte[] blob, int len)
    {
        var o = new byte[len];
        using var z = new DeflateStream(new MemoryStream(blob), CompressionMode.Decompress);
        int n = 0; while (n < len) { int r = z.Read(o, n, len - n); if (r <= 0) break; n += r; }
        return o;
    }

    public static void UnfilterUp(byte[] d, int w)
    {
        int stride = w * 4;
        for (int i = stride; i < d.Length; i++) d[i] = (byte)(d[i] + d[i - stride]);
    }

    static byte[] BrotliSharp(byte[] blob, int len)
    {
        var o = new byte[len];
        using var z = new BrotliSharpLib.BrotliStream(new MemoryStream(blob), CompressionMode.Decompress);
        int n = 0; while (n < len) { int r = z.Read(o, n, len - n); if (r <= 0) break; n += r; }
        return o;
    }
}
