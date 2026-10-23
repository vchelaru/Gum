using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace KniGumFromFile;

// SPIKE #6156 (not for merge). D: browser-native decode (JS, see wwwroot/js/benchd.js).
// E: KNI content pipeline .xnb (raw and Brotli-compressed) through ContentManager.
public static class WebLoadBench
{
    static readonly string[] Names = { "bench1024", "hytale", "MainSpriteSheet" };
    const int Reps = 6; // first is warmup

    static byte[] Fetch(string path)
    {
        using var s = TitleContainer.OpenStream(path);
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    static void LogMedian(Action<string> log, string name, string key, List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        log($"WEBBENCH {name} {key} median={sorted[sorted.Count / 2]:F1} all=[{string.Join(",", values.Select(v => v.ToString("F1")))}]");
    }

    public static void RunE(Game game, Action<string> log)
    {
        foreach (var name in Names)
        {
            var r = new Dictionary<string, List<double>>();
            void Add(string k, double v) { if (!r.TryGetValue(k, out var l)) r[k] = l = new List<double>(); l.Add(v); }
            for (int rep = 0; rep < Reps; rep++)
            {
                var sw = Stopwatch.StartNew();
                var png = Fetch("Content/" + name + ".png");
                double pngFetch = sw.Elapsed.TotalMilliseconds;

                sw.Restart();
                var raw = Fetch("Content/xnbraw/" + name + ".xnb");
                double rawFetch = sw.Elapsed.TotalMilliseconds;

                sw.Restart();
                var br = Fetch("Content/xnbbr/" + name + ".xnb");
                double brFetch = sw.Elapsed.TotalMilliseconds;

                var cmRaw = new ContentManager(game.Services, "Content/xnbraw");
                sw.Restart();
                var tRaw = cmRaw.Load<Texture2D>(name);
                double rawLoad = sw.Elapsed.TotalMilliseconds;
                cmRaw.Unload();

                var cmBr = new ContentManager(game.Services, "Content/xnbbr");
                sw.Restart();
                var tBr = cmBr.Load<Texture2D>(name);
                double brLoad = sw.Elapsed.TotalMilliseconds;
                cmBr.Unload();

                if (rep == 0)
                {
                    log($"WEBBENCH {name} bytes png={png.Length} xnbraw={raw.Length} xnbbr={br.Length} size={tBr.Width}x{tBr.Height}");
                    continue;
                }
                Add("png.fetch", pngFetch);
                Add("xnbraw.fetch", rawFetch);
                Add("xnbraw.load", rawLoad);
                Add("xnbraw.loadMinusFetch", rawLoad - rawFetch);
                Add("xnbbr.fetch", brFetch);
                Add("xnbbr.load", brLoad);
                Add("xnbbr.loadMinusFetch", brLoad - brFetch);
            }
            foreach (var kv in r) LogMedian(log, name, kv.Key, kv.Value);
        }
        log("WEBBENCH E DONE");
    }

    public static async Task RunD(IJSRuntime js, Game game, Action<string> log)
    {
        foreach (var name in Names)
        {
            var bytes = Fetch("Content/" + name + ".png");
            var transfer = new List<double>();
            for (int i = 0; i < Reps; i++)
            {
                var sw = Stopwatch.StartNew();
                await js.InvokeAsync<int>("gumBenchNop", bytes);
                if (i > 0) transfer.Add(sw.Elapsed.TotalMilliseconds);
            }
            LogMedian(log, name, "D.transferToJs", transfer);
            var json = await js.InvokeAsync<string>("gumBenchD", bytes, Reps);
            log($"WEBBENCH {name} D {json}");
        }

        var prec = Fetch("Content/precision.png");
        log($"WEBBENCH precision {await js.InvokeAsync<string>("gumBenchDPixels", prec, 8, 1)}");

        // Wrap proof: create the KNI texture, then let JS upload the browser-decoded bitmap into it.
        var hytale = Fetch("Content/hytale.png");
        var kniTexture = new Texture2D(game.GraphicsDevice, 960, 960, false, SurfaceFormat.Color);
        log($"WEBBENCH intoKni {await js.InvokeAsync<string>("gumBenchDIntoKni", hytale)}");
        var pixels = new Color[960 * 960];
        kniTexture.GetData(pixels);
        var nonZero = pixels.Count(p => p.PackedValue != 0);
        log($"WEBBENCH intoKni GetData nonZeroTexels={nonZero} of {pixels.Length} sample={pixels[480 * 960 + 480]}");
        kniTexture.Dispose();
        log("WEBBENCH D DONE");
    }
}
