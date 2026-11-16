using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Gum.Wireframe;
using SkiaSharp;

namespace RenderingLibrary.Graphics;

/// <summary>
/// SPIKE #8116: GUM_IMGPROBE=&lt;frame&gt; logs every image op of that one frame (Sprite, NineSlice fallback, DrawAtlas
/// flush) with its owner chain and why it did not batch, to GUM_IMGPROBE_OUT (default imgprobe.log). Not for merge.
/// </summary>
internal static class ImgProbe
{
    static readonly int Target = int.TryParse(Environment.GetEnvironmentVariable("GUM_IMGPROBE"), out int f) ? f : -1;
    static readonly string Out = Environment.GetEnvironmentVariable("GUM_IMGPROBE_OUT") ?? "imgprobe.log";
    static readonly List<string> Lines = new();
    static int _frame;
    static int _seq;

    public static bool Active => _frame == Target;

    public static void FrameStart()
    {
        if (Target < 0) return;
        if (Active) System.IO.File.WriteAllLines(Out, Lines);
        _frame++;
        _seq = 0;
    }

    public static void Log(string s)
    {
        if (Active) Lines.Add($"{_seq++:D4} {s}");
    }

    public static string Img(SKImage? image, object? key) =>
        image == null ? "null" : $"{image.Width}x{image.Height}#{(key == null ? 0 : RuntimeHelpers.GetHashCode(key)) % 100000}";

    public static string Owner(IRenderableIpso r)
    {
        var parts = new List<string>();
        IRenderableIpso? p = r;
        for (int i = 0; i < 4 && p != null; i++)
        {
            string n = string.IsNullOrEmpty(p.Name) ? "?" : p.Name;
            string forms = p is InteractiveGue g && g.FormsControlAsObject != null ? "[" + g.FormsControlAsObject.GetType().Name + "]" : "";
            parts.Add($"{p.GetType().Name}:{n}{forms}");
            p = p.Parent;
        }
        return string.Join(" < ", parts);
    }
}
