using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;

namespace KniGumFromFile;

// TEMPORARY spike harness for #6156. Not committed.
public static class PremultiplyBench
{
    public static void Run(GraphicsDevice device, Action<string> log)
    {
        foreach (var name in new[] { "bench1024.png", "hytale.png", "MainSpriteSheet.png" })
        {
            byte[] bytes;
            using (var s = TitleContainer.OpenStream("Content/" + name))
            using (var ms = new MemoryStream()) { s.CopyTo(ms); bytes = ms.ToArray(); }

            var results = new Dictionary<string, List<double>>();
            void Add(string k, double v) { if (!results.TryGetValue(k, out var l)) results[k] = l = new List<double>(); l.Add(v); }
            const int reps = 6; // first is warmup
            for (int rep = 0; rep < reps; rep++)
            {
                var sw = new Stopwatch();
                double Lap() { var e = sw.Elapsed.TotalMilliseconds; sw.Restart(); return e; }

                // A: straight + edge bleed
                sw.Restart();
                var ta = Texture2D.FromStream(device, new MemoryStream(bytes));
                double aDecode = Lap();
                var pa = new Color[ta.Width * ta.Height];
                ta.GetData(pa); double aRead = Lap();
                TextureEdgeBleed.Bleed(pa, ta.Width, ta.Height); double aLoop = Lap();
                ta.SetData(pa); double aUp = Lap();
                ta.Dispose();

                // B: premultiplied, current code
                sw.Restart();
                var tb = Texture2D.FromStream(device, new MemoryStream(bytes));
                double bDecode = Lap();
                var pb = new Color[tb.Width * tb.Height];
                tb.GetData(pb); double bRead = Lap();
                TexturePremultiply.Premultiply(pb); double bLoop = Lap();
                tb.SetData(pb); double bUp = Lap();
                tb.Dispose();

                // C: stb decode + byte premultiply + one upload
                sw.Restart();
                var img = StbImageSharp.ImageResult.FromStream(new MemoryStream(bytes), StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                double cDecode = Lap();
                TexturePremultiply.Premultiply(img.Data); double cLoop = Lap();
                var tc = new Texture2D(device, img.Width, img.Height, false, SurfaceFormat.Color);
                tc.SetData(img.Data); double cUp = Lap();
                tc.Dispose();

                if (rep == 0) continue;
                Add("A.decode", aDecode); Add("A.read", aRead); Add("A.loop", aLoop); Add("A.upload", aUp); Add("A.total", aDecode + aRead + aLoop + aUp);
                Add("B.decode", bDecode); Add("B.read", bRead); Add("B.loop", bLoop); Add("B.upload", bUp); Add("B.total", bDecode + bRead + bLoop + bUp);
                Add("C.decode", cDecode); Add("C.read", 0); Add("C.loop", cLoop); Add("C.upload", cUp); Add("C.total", cDecode + cLoop + cUp);
            }
            foreach (var kv in results)
            {
                var sorted = kv.Value.OrderBy(v => v).ToList();
                double median = sorted[sorted.Count / 2];
                log($"PMBENCH {name} {kv.Key} median={median:F1} all=[{string.Join(",", kv.Value.Select(v => v.ToString("F1")))}]");
            }
        }
        log("PMBENCH DONE");
    }
}
