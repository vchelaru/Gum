using RenderingLibrary.Graphics;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Topten.RichTextKit;
namespace SkiaGum.Tests.Renderables;
public class ScratchDbg
{
    [Fact]
    public void Dbg()
    {
        var sb = new StringBuilder();
        global::SkiaGum.Text.DebugLog.Clear(); Text t = new() { FontName = "Arial", FontSize = 20, Width = 1000, LineHeightMultiplier = 1.5f, RawText = "a[FontScale=2]b[/FontScale]c\nabc[FontScale=0.5]d[/FontScale]\n[FontScale=3]e[/FontScale]f" };
        var runs = t.GetStyledRuns();
        foreach (var r in runs) sb.AppendLine($"run '{r.Text.Replace("\n","\n")}' size={r.Style.FontSize} lh={r.Style.LineHeight}");
        var b = t.GetTextBlock();
        int i = 0;
        foreach (var l in b.Lines)
        {
            sb.AppendLine($"line {i++} y={l.YCoord} h={l.Height} asc={l.MaxAscent} desc={l.MaxDescent} runs={l.Runs.Count}");
            foreach (var fr in l.Runs) sb.AppendLine($"   start={fr.Start} len={fr.Length} size={fr.Style.FontSize} lh={fr.Style.LineHeight} asc={fr.Ascent} desc={fr.Descent} half={fr.HalfLeading} typeface={fr.Typeface?.FamilyName}");
        }

        // typewriter cost
        foreach (float mult in new[] { 1.5f, 1f })
        {
            var sbm = new StringBuilder();
            for (int c = 0; c < 20; c++) sbm.Append($"[Color=Red]word{c} [/Color][Color=Blue]more{c} [/Color]");
            Text tw = new() { FontName = "Arial", FontSize = 20, Width = 300, LineHeightMultiplier = mult, RawText = sbm.ToString() };
            tw.GetPaintTextBlock(tw.GetCachedTextBlock());
            int total = tw.WrappedText.Sum(x => x.Length);
            GC.Collect(); GC.WaitForPendingFinalizers();
            long a0 = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            for (int f = 0; f < 500; f++)
            {
                tw.MaxLettersToShow = 1 + f % (total - 1);
                tw.GetPaintTextBlock(tw.GetCachedTextBlock());
            }
            sw.Stop();
            sb.AppendLine($"typewriter mult={mult}: {(GC.GetAllocatedBytesForCurrentThread() - a0) / 500.0:F0} bytes/frame, {sw.Elapsed.TotalMilliseconds * 1000 / 500:F1} us/frame");
        }
        throw new Exception("DBG|" + sb.ToString() + "|LOG|" + global::SkiaGum.Text.DebugLog.ToString());
    }
}
