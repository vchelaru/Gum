#nullable enable
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Diagnostics;

namespace GumSamples.Benchmark;

/// <summary>Spike: runs inside the real Gum draw pass (live GPU canvas), batches draws between two timestamps. Flush happens outside the timed region.</summary>
internal class PerDrawRenderable : InvisibleRenderable
{
    public Action<RenderingLibrary.SystemManagers, SKCanvas>? Work;
    public override void Render(ISystemManagers managers)
    {
        if (Work != null) { var w = Work; Work = null; var sm = (RenderingLibrary.SystemManagers)managers; w(sm, sm.Canvas); }
    }
}

internal static class PerDrawBench
{
    const int N = 100, Chunk = 2000, Chunks = 50;
    static readonly bool PrintChunks = Environment.GetEnvironmentVariable("PERDRAW_CHUNKS") == "1";
    static readonly string? Only = Environment.GetEnvironmentVariable("PERDRAW_ONLY");

    static double Measure(string name, SKCanvas canvas, Action<int> draw, int chunk = Chunk)
    {
        double[] us = new double[Chunks];
        for (int warm = 0; warm < 5; warm++) { for (int i = 0; i < chunk; i++) draw(i % N); canvas.Flush(); }
        long alloc0 = GC.GetAllocatedBytesForCurrentThread();
        int gc0 = GC.CollectionCount(0);
        for (int c = 0; c < Chunks; c++)
        {
            long t0 = Stopwatch.GetTimestamp();
            for (int i = 0; i < chunk; i++) draw(i % N);
            long t1 = Stopwatch.GetTimestamp();
            us[c] = (t1 - t0) * 1e6 / Stopwatch.Frequency / chunk;
            canvas.Flush();
        }
        long alloc = GC.GetAllocatedBytesForCurrentThread() - alloc0;
        int gcs = GC.CollectionCount(0) - gc0;
        if (PrintChunks) Console.WriteLine("   chunks: " + string.Join(" ", us.Select(u => u.ToString("F2"))));
        double[] sorted = (double[])us.Clone();
        Array.Sort(sorted);
        Console.WriteLine($"{name,-56} median {sorted[Chunks / 2]:F3}   min {sorted[0]:F3}   alloc {alloc / (double)(chunk * Chunks):F1} B/draw  gc0 {gcs}");
        return sorted[Chunks / 2];
    }

    // Interleaved A/B: base chunk, ablated chunk, base, ablated... so drift (clocks, driver, JIT) hits both alike.
    static void Ablate(string group, SKCanvas canvas, Action<int> draw, params (string Name, Action Set)[] items)
    {
        const int pairs = 80, chunk = 1000;
        PerfAblation.Reset();
        for (int warm = 0; warm < 20; warm++) { for (int i = 0; i < chunk; i++) draw(i % N); canvas.Flush(); }
        foreach (var (name, set) in items)
        {
            double[] a = new double[pairs], b = new double[pairs], d = new double[pairs];
            PerfAblation.Reset(); set();
            for (int i = 0; i < chunk; i++) draw(i % N); canvas.Flush();
            for (int p = 0; p < pairs; p++)
            {
                for (int side = 0; side < 2; side++)
                {
                    bool ablated = (p + side) % 2 == 1;
                    PerfAblation.Reset(); if (ablated) set();
                    long t0 = Stopwatch.GetTimestamp();
                    for (int i = 0; i < chunk; i++) draw(i % N);
                    long t1 = Stopwatch.GetTimestamp();
                    canvas.Flush();
                    double us = (t1 - t0) * 1e6 / Stopwatch.Frequency / chunk;
                    if (ablated) b[p] = us; else a[p] = us;
                }
                d[p] = a[p] - b[p];
            }
            PerfAblation.Reset();
            Array.Sort(a); Array.Sort(b); Array.Sort(d);
            Console.WriteLine($"{group,-10} -{name,-38} base {a[pairs / 2]:F3} ablated {b[pairs / 2]:F3}  saved(median paired) {d[pairs / 2]:F3}  [p25 {d[pairs / 4]:F3} p75 {d[3 * pairs / 4]:F3}]");
        }
    }

    static SKPaint GumPaint(object renderable)
    {
        var f = typeof(RenderableShapeBase).GetField("_cachedPaint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (SKPaint)f.GetValue(renderable)!;
    }

    public static void Run(RenderingLibrary.SystemManagers sm, SKCanvas canvas, string unused)
    {
        Console.WriteLine("--- per-draw bench (record cost, flush excluded), N=" + N + " objects, " + Chunk * Chunks + " draws each");
        string? aff = Environment.GetEnvironmentVariable("PERDRAW_AFFINITY");
        if (aff != null)
        {
            var proc = Process.GetCurrentProcess();
            if (aff != "0") proc.ProcessorAffinity = (IntPtr)Convert.ToInt64(aff, 16);
            if (Environment.GetEnvironmentVariable("PERDRAW_NOPRIO") != "1")
            {
                proc.PriorityClass = ProcessPriorityClass.High;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }
            Console.WriteLine("affinity " + aff + " high priority");
        }
        var texts = new List<TextRuntime>();
        var nines = new List<NineSliceRuntime>();
        var sprites = new List<SpriteRuntime>();
        var rects = new List<ColoredRectangleRuntime>();
        for (int i = 0; i < N; i++)
        {
            float x = 10 + (i % 10) * 60, y = 10 + (i / 10) * 40;
            texts.Add(new TextRuntime { Text = i.ToString(), X = x, Y = y });
            nines.Add(new NineSliceRuntime { SourceFileName = "Frame.png", X = x, Y = y, Width = 56, Height = 36 });
            sprites.Add(new SpriteRuntime { SourceFileName = "Frame.png", X = x, Y = y, Width = 56, Height = 36 });
            rects.Add(new ColoredRectangleRuntime { X = x, Y = y, Width = 56, Height = 36 });
        }
        foreach (var t in texts) t.Render(sm);
        foreach (var n in nines) n.Render(sm);
        foreach (var s in sprites) s.Render(sm);
        canvas.Flush();

        bool Want(string k) => Only == null || Only.Contains(k);

        if (Want("bimodal"))
        {
            Console.WriteLine("--- bimodal: NineSlice x3, chunk sizes, one object");
            Measure("NineSlice #1", canvas, i => nines[i].Render(sm));
            Measure("NineSlice #2", canvas, i => nines[i].Render(sm));
            Measure("NineSlice #3", canvas, i => nines[i].Render(sm));
            Measure("NineSlice chunk=200", canvas, i => nines[i].Render(sm), 200);
            Measure("NineSlice chunk=20000", canvas, i => nines[i].Render(sm), 20000);
            Measure("NineSlice same object", canvas, i => nines[0].Render(sm));
        }

        if (Want("text"))
        {
            Ablate("Text", canvas, i => texts[i].Render(sm),
                ("blockCheck", () => PerfAblation.TextSkipBlockCheck = true),
                ("rotation", () => PerfAblation.TextSkipRotation = true),
                ("snap", () => PerfAblation.TextSkipSnap = true),
                ("matrixChain", () => PerfAblation.TextSkipMatrixChain = true),
                ("renderPaint", () => PerfAblation.TextSkipRenderPaint = true),
                ("clipper", () => PerfAblation.TextSkipClipper = true),
                ("stats", () => PerfAblation.TextSkipStats = true),
                ("pictureReplay(paint block directly)", () => PerfAblation.TextBypassPicture = true),
                ("pictureReplay(direct blob)", () => PerfAblation.TextDirectBlob = true),
                ("ALL+blob", () =>
                {
                    PerfAblation.TextSkipBlockCheck = PerfAblation.TextSkipRotation = PerfAblation.TextSkipSnap = PerfAblation.TextSkipMatrixChain =
                    PerfAblation.TextSkipRenderPaint = PerfAblation.TextSkipClipper = PerfAblation.TextSkipStats = PerfAblation.TextDirectBlob = true;
                }));
        }

        if (Want("nine"))
        {
            Ablate("NineSlice", canvas, i => nines[i].Render(sm),
                ("renderOffset", () => PerfAblation.NsSkipRenderOffset = true),
                ("snap", () => PerfAblation.NsSkipSnap = true),
                ("borderCompute", () => PerfAblation.NsSkipBorderCompute = true),
                ("latticeChecks", () => PerfAblation.NsSkipLatticeChecks = true),
                ("basePath", () => PerfAblation.ShapeDirectDrawBound = true),
                ("ALL", () =>
                {
                    PerfAblation.NsSkipRenderOffset = PerfAblation.NsSkipSnap = PerfAblation.NsSkipBorderCompute = PerfAblation.NsSkipLatticeChecks =
                    PerfAblation.ShapeDirectDrawBound = true;
                }));
        }

        if (Want("sprite"))
        {
            Ablate("Sprite", canvas, i => sprites[i].Render(sm),
                ("renderOffset", () => PerfAblation.SpriteSkipRenderOffset = true),
                ("sampledSource", () => PerfAblation.SpriteSkipSampledSource = true),
                ("basePath", () => PerfAblation.ShapeDirectDrawBound = true),
                ("ALL", () =>
                {
                    PerfAblation.SpriteSkipRenderOffset = PerfAblation.SpriteSkipSampledSource = PerfAblation.ShapeDirectDrawBound = true;
                }));
        }

        if (Want("base"))
        {
            for (int rep = 0; rep < 3; rep++)
            {
                Measure("Gum Text", canvas, i => texts[i].Render(sm));
                Measure("Gum NineSlice", canvas, i => nines[i].Render(sm));
                Measure("Gum Sprite", canvas, i => sprites[i].Render(sm));
                Measure("Gum Rect", canvas, i => rects[i].Render(sm));
            }
        }

        if (Want("rect"))
        {
            Ablate("Rect", canvas, i => rects[i].Render(sm),
                ("basePath", () => PerfAblation.ShapeDirectDrawBound = true));
        }

        if (Want("paired"))
        {
            var ns0 = (NineSlice)nines[0].RenderableComponent!;
            SKImage im = ns0.Image!;
            using SKPaint plainNoAA = new SKPaint { IsAntialias = false };
            SKPaint gn = GumPaint(ns0);
            SKPaint gs = GumPaint(sprites[0].RenderableComponent!);
            var lat = new SKLattice { XDivs = new[] { 8, im.Width - 8 }, YDivs = new[] { 8, im.Height - 8 } };
            var dst = new SKRect(0, 0, 56, 36);
            SKSamplingOptions nn = new SKSamplingOptions(SKFilterMode.Nearest);
            Ablate("RAWlattice", canvas, i =>
            {
                canvas.Save(); canvas.Translate(10 + (i % 10) * 60, 10 + (i / 10) * 40); canvas.Scale(1f);
                canvas.DrawImageLattice(im, lat, dst, PerfAblation.Alt ? plainNoAA : gn); canvas.Restore();
            }, ("whiteTintFilter", () => PerfAblation.Alt = true));
            Ablate("RAWimage", canvas, i =>
                canvas.DrawImage(im, new SKRect(10 + (i % 10) * 60, 10 + (i / 10) * 40, 66 + (i % 10) * 60, 46 + (i / 10) * 40), nn, PerfAblation.Alt ? plainNoAA : gs),
                ("whiteTintFilter", () => PerfAblation.Alt = true));
            Ablate("Nine-vs-RAWlattice", canvas, i =>
            {
                if (PerfAblation.Alt) { canvas.Save(); canvas.Translate(10 + (i % 10) * 60, 10 + (i / 10) * 40); canvas.Scale(1f); canvas.DrawImageLattice(im, lat, dst, gn); canvas.Restore(); }
                else nines[i].Render(sm);
            }, ("gumOverhead", () => PerfAblation.Alt = true));
            Ablate("Sprite-vs-RAWimage", canvas, i =>
            {
                if (PerfAblation.Alt) canvas.DrawImage(im, new SKRect(10 + (i % 10) * 60, 10 + (i / 10) * 40, 66 + (i % 10) * 60, 46 + (i / 10) * 40), nn, gs);
                else sprites[i].Render(sm);
            }, ("gumOverhead", () => PerfAblation.Alt = true));
            using SKFont f18 = new SKFont(SKTypeface.Default, 18);
            using SKPaint tp2 = new SKPaint { Color = SKColors.White, IsAntialias = true };
            var bl = new SKTextBlob[N];
            for (int i = 0; i < N; i++) bl[i] = SKTextBlob.Create(i.ToString(), f18);
            Ablate("Text-vs-RAWblob", canvas, i =>
            {
                if (PerfAblation.Alt) { canvas.Save(); canvas.SetMatrix(SKMatrix.CreateTranslation(10 + (i % 10) * 60, 10 + (i / 10) * 40)); canvas.DrawText(bl[i], 0, 18, tp2); canvas.Restore(); }
                else texts[i].Render(sm);
            }, ("gumOverhead", () => PerfAblation.Alt = true));
        }

        if (Want("raw"))
        {
            var ns = (NineSlice)nines[0].RenderableComponent!;
            SKImage img = ns.Image!;
            using SKPaint paint = new SKPaint();
            SKPaint gumNinePaint = GumPaint(ns);
            SKPaint gumSpritePaint = GumPaint(sprites[0].RenderableComponent!);
            Console.WriteLine($"Gum nine paint: AA={gumNinePaint.IsAntialias} filter={(gumNinePaint.ColorFilter != null)}; sprite paint filter={(gumSpritePaint.ColorFilter != null)}");
            int[] xd = { 8, img.Width - 8 }, yd = { 8, img.Height - 8 };
            var lattice = new SKLattice { XDivs = xd, YDivs = yd };
            var dest = new SKRect(0, 0, 56, 36);
            Measure("RAW Save/Translate/Scale/Lattice/Restore", canvas, i =>
            {
                canvas.Save(); canvas.Translate(10 + (i % 10) * 60, 10 + (i / 10) * 40); canvas.Scale(1f);
                canvas.DrawImageLattice(img, lattice, dest, paint); canvas.Restore();
            });
            Measure("RAW same, Gum's paint", canvas, i =>
            {
                canvas.Save(); canvas.Translate(10 + (i % 10) * 60, 10 + (i / 10) * 40); canvas.Scale(1f);
                canvas.DrawImageLattice(img, lattice, dest, gumNinePaint); canvas.Restore();
            });
            Measure("RAW canvas.TotalMatrix only", canvas, i => { var m = canvas.TotalMatrix; });
            Measure("RAW canvas.DeviceClipBounds only", canvas, i => { var m = canvas.DeviceClipBounds; });
            SKSamplingOptions nearest = new SKSamplingOptions(SKFilterMode.Nearest);
            Measure("RAW DrawImage(rect) plain paint", canvas, i =>
                canvas.DrawImage(img, new SKRect(10 + (i % 10) * 60, 10 + (i / 10) * 40, 66 + (i % 10) * 60, 46 + (i / 10) * 40), nearest, paint));
            Measure("RAW DrawImage(rect) Gum sprite paint", canvas, i =>
                canvas.DrawImage(img, new SKRect(10 + (i % 10) * 60, 10 + (i / 10) * 40, 66 + (i % 10) * 60, 46 + (i / 10) * 40), nearest, gumSpritePaint));
            using SKPaint rp = new SKPaint { Color = SKColors.Red };
            Measure("RAW DrawRect", canvas, i => canvas.DrawRect(new SKRect(10 + (i % 10) * 60, 10 + (i / 10) * 40, 66 + (i % 10) * 60, 46 + (i / 10) * 40), rp));
            using SKFont font = new SKFont(SKTypeface.Default, 18);
            using SKPaint tp = new SKPaint { Color = SKColors.White, IsAntialias = true };
            var blobs = new SKTextBlob[N];
            for (int i = 0; i < N; i++) blobs[i] = SKTextBlob.Create(i.ToString(), font);
            Measure("RAW DrawTextBlob (preshaped)", canvas, i => canvas.DrawText(blobs[i], 10 + (i % 10) * 60, 10 + (i / 10) * 40, tp));
            Measure("RAW Save/SetMatrix/DrawTextBlob/Restore", canvas, i =>
            {
                canvas.Save(); canvas.SetMatrix(SKMatrix.CreateTranslation(10 + (i % 10) * 60, 10 + (i / 10) * 40)); canvas.DrawText(blobs[i], 0, 18, tp); canvas.Restore();
            });
        }

        if (Want("pic"))
        {
            Console.WriteLine("--- picture replay vs paint-direct, where it was meant to win");
            string para = "Gum is a UI framework for games. " + string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog, again and again. ", 8));
            var cases = new (string Name, Func<TextRuntime> Make)[]
            {
                ("plain short '42'", () => new TextRuntime { Text = "42" }),
                ("BBCode 4 colors", () => new TextRuntime { Text = "[Color=Red]Health[/Color] [Color=Lime]Mana[/Color] [Color=Yellow]Gold[/Color] [Color=Cyan]XP[/Color]" }),
                ("outline 2", () => new TextRuntime { Text = "Outlined label", OutlineThickness = 2 }),
                ("drop shadow", () => new TextRuntime { Text = "Shadowed label", HasDropshadow = true, DropshadowOffsetX = 2, DropshadowOffsetY = 2, DropshadowBlurX = 2, DropshadowBlurY = 2 }),
                ("long paragraph wrap 400", () => { var t = new TextRuntime { Text = para }; t.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute; t.Width = 400; return t; }),
            };
            foreach (var (name, make) in cases)
            {
                var list = new List<TextRuntime>();
                for (int i = 0; i < N; i++) { var t = make(); t.X = 10 + (i % 10) * 60; t.Y = 10 + (i / 10) * 40; list.Add(t); }
                foreach (var t in list) { t.Render(sm); t.Render(sm); }
                canvas.Flush();
                PerfAblation.Reset();
                Measure($"{name}: picture replay (shipping)", canvas, i => list[i].Render(sm), 500);
                PerfAblation.TextBypassPicture = true;
                Measure($"{name}: paint block directly", canvas, i => list[i].Render(sm), 500);
                PerfAblation.Reset();
            }
        }
        Console.WriteLine("--- done");
    }
}
