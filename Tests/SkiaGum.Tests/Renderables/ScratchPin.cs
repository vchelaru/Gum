using RenderingLibrary.Graphics;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

public class ScratchPin
{
    [Fact]
    public void Dump()
    {
        Text ctl = new() { FontName = "Arial", FontSize = 20, Width = 1000, RawText = "a\nb" };
        var cb = ctl.GetTextBlock();
        float h = cb.Lines[1].YCoord - cb.Lines[0].YCoord;
        string[] texts =
        {
            "Start [FontScale=0.75]The quick brown fox jumps over the lazy dog and again[/FontScale] end",
            "[FontSize=10]long wrapped text[/FontSize] Wmw",
            "a[FontScale=2]b[/FontScale]c",
            "abc[FontScale=0.5]d[/FontScale]",
            "The [FontScale=2]big[/FontScale] brown fox jumps over the lazy dog again and again",
            "[FontScale=3]Huge[/FontScale] line then normal text that wraps around the width",
            "small [FontScale=0.5]tiny words that wrap around a few lines here[/FontScale] after",
            "one\n[FontScale=2]two[/FontScale]\nthree four five six seven eight",
            "[FontSize=30]Large[/FontSize] and [FontSize=10]small[/FontSize] mixed in wrapped text example",
            "[Color=Red]red[/Color] [FontScale=1.5]bigger red words[/FontScale] [Color=Blue]blue[/Color] words that go on and on",
            "ab[FontScale=1]c[/FontScale]d wraps when there is enough text to wrap here",
            "[FontScale=0.75]small start[/FontScale] then base text that continues on for a while",
        };
        var combos = new System.Collections.Generic.List<(float b, float m, float w)> { (2, 1.5f, 100), (2, 1.5f, 200), (2, 3, 100), (2, 3, 200) };
        var sb = new StringBuilder();
        for (int t = 0; t < texts.Length; t++)
        {
            var list = new System.Collections.Generic.List<(float b, float m, float w)>(combos);
            if (t == 0) list.Add((1, 1, 120));
            if (t == 1) list.Add((2, 1, 200));
            foreach (var (b, m, w) in list)
            {
                Text text = new() { FontName = "Arial", FontSize = 20, Width = w, FontScale = b, LineHeightMultiplier = m, RawText = texts[t] };
                var block = text.GetTextBlock();
                var tops = new StringBuilder();
                foreach (var l in block.Lines) tops.Append((l.YCoord / h).ToString("0.000", CultureInfo.InvariantCulture)).Append(',');
                tops.Append((text.WrappedTextHeight / h).ToString("0.000", CultureInfo.InvariantCulture));
                sb.AppendLine($"    [InlineData(\"{texts[t].Replace("\n", "\n")}\", {b.ToString(CultureInfo.InvariantCulture)}f, {m.ToString(CultureInfo.InvariantCulture)}f, {w.ToString(CultureInfo.InvariantCulture)}f, \"{tops}\")]");
            }
        }
        foreach (float mult in new[] { 1.5f, 1f })
        {
            var sbm = new StringBuilder();
            for (int c = 0; c < 20; c++) sbm.Append($"[Color=Red]word{c} [/Color][Color=Blue]more{c} [/Color]");
            Text tw = new() { FontName = "Arial", FontSize = 20, Width = 300, LineHeightMultiplier = mult, RawText = sbm.ToString() };
            tw.GetPaintTextBlock(tw.GetCachedTextBlock());
            int total = 0; foreach (var wl in tw.WrappedText) total += wl.Length;
            for (int f = 0; f < 50; f++) { tw.MaxLettersToShow = 1 + f % (total - 1); tw.GetPaintTextBlock(tw.GetCachedTextBlock()); }
            GC.Collect(); GC.WaitForPendingFinalizers();
            long a0 = GC.GetAllocatedBytesForCurrentThread();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int f = 0; f < 500; f++) { tw.MaxLettersToShow = 1 + f % (total - 1); tw.GetPaintTextBlock(tw.GetCachedTextBlock()); }
            sw.Stop();
            sb.AppendLine($"// typewriter mult={mult}: {(GC.GetAllocatedBytesForCurrentThread() - a0) / 500.0:F0} bytes/frame, {sw.Elapsed.TotalMilliseconds * 1000 / 500:F1} us/frame");
        }
        throw new Exception("PINS|" + sb.ToString() + "|ENDPINS");
    }
}
// 1 19247
// 2 10994
// 3 24088
