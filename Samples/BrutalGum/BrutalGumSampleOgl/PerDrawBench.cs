#nullable enable
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
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

    static void Measure(string name, SKCanvas canvas, Action<int> draw)
    {
        double[] us = new double[Chunks];
        for (int warm = 0; warm < 5; warm++) { for (int i = 0; i < Chunk; i++) draw(i % N); canvas.Flush(); }
        long alloc0 = GC.GetAllocatedBytesForCurrentThread();
        for (int c = 0; c < Chunks; c++)
        {
            long t0 = Stopwatch.GetTimestamp();
            for (int i = 0; i < Chunk; i++) draw(i % N);
            long t1 = Stopwatch.GetTimestamp();
            us[c] = (t1 - t0) * 1e6 / Stopwatch.Frequency / Chunk;
            canvas.Flush();
        }
        long alloc = GC.GetAllocatedBytesForCurrentThread() - alloc0;
        Array.Sort(us);
        Console.WriteLine($"{name,-52} median {us[Chunks / 2]:F3} us/draw   min {us[0]:F3}   alloc {alloc / (double)(Chunk * Chunks):F1} B/draw");
    }

    public static void Run(RenderingLibrary.SystemManagers sm, SKCanvas canvas, string unused)
    {
        Console.WriteLine("--- per-draw bench (record cost, flush excluded), N=" + N + " objects, " + Chunk * Chunks + " draws each");
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
        canvas.Flush();

        Measure("Gum TextRuntime.Render", canvas, i => texts[i].Render(sm));
        Measure("Gum NineSliceRuntime.Render (lattice)", canvas, i => nines[i].Render(sm));
        Measure("Gum SpriteRuntime.Render (no batch scope)", canvas, i => sprites[i].Render(sm));
        Measure("Gum ColoredRectangleRuntime.Render", canvas, i => rects[i].Render(sm));

        var ns = (SkiaGum.Renderables.NineSlice)nines[0].RenderableComponent!;
        SKImage img = ns.Image!;
        using SKPaint paint = new SKPaint();
        int[] xd = { 8, img.Width - 8 }, yd = { 8, img.Height - 8 };
        var lattice = new SKLattice { XDivs = xd, YDivs = yd };
        var dest = new SKRect(0, 0, 56, 36);
        Measure("RAW Save/Translate/Scale/DrawImageLattice/Restore", canvas, i =>
        {
            canvas.Save(); canvas.Translate(10 + (i % 10) * 60, 10 + (i / 10) * 40); canvas.Scale(1f);
            canvas.DrawImageLattice(img, lattice, dest, paint); canvas.Restore();
        });
        Measure("RAW DrawImageLattice only", canvas, i =>
            canvas.DrawImageLattice(img, lattice, new SKRect(10 + (i % 10) * 60, 10 + (i / 10) * 40, 66 + (i % 10) * 60, 46 + (i / 10) * 40), paint));
        SKSamplingOptions nearest = new SKSamplingOptions(SKFilterMode.Nearest);
        Measure("RAW DrawImageRect", canvas, i =>
            canvas.DrawImage(img, new SKRect(0, 0, img.Width, img.Height), new SKRect(10 + (i % 10) * 60, 10 + (i / 10) * 40, 66 + (i % 10) * 60, 46 + (i / 10) * 40), nearest, paint));
        using SKPaint rp = new SKPaint { Color = SKColors.Red };
        Measure("RAW DrawRect", canvas, i => canvas.DrawRect(new SKRect(10 + (i % 10) * 60, 10 + (i / 10) * 40, 66 + (i % 10) * 60, 46 + (i / 10) * 40), rp));

        using SKFont font = new SKFont(SKTypeface.Default, 18);
        using SKPaint tp = new SKPaint { Color = SKColors.White, IsAntialias = true };
        var blobs = new SKTextBlob[N];
        var pics = new SKPicture[N];
        for (int i = 0; i < N; i++)
        {
            blobs[i] = SKTextBlob.Create(i.ToString(), font);
            using var rec = new SKPictureRecorder();
            var rc = rec.BeginRecording(new SKRect(-1000, -1000, 5000, 5000));
            rc.DrawText(blobs[i], 0, 18, tp);
            pics[i] = rec.EndRecording();
        }
        Measure("RAW DrawTextBlob (preshaped)", canvas, i => canvas.DrawText(blobs[i], 10 + (i % 10) * 60, 10 + (i / 10) * 40, tp));
        Measure("RAW Save/SetMatrix/DrawPicture/Restore", canvas, i =>
        {
            canvas.Save(); canvas.SetMatrix(SKMatrix.CreateTranslation(10 + (i % 10) * 60, 10 + (i / 10) * 40)); canvas.DrawPicture(pics[i]); canvas.Restore();
        });
        Measure("RAW Save+Restore only", canvas, i => { canvas.Save(); canvas.Restore(); });
        Console.WriteLine("--- done");
    }
}
