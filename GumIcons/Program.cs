using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        string gumDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\Gum\"));
        string inputDir = Path.Combine(gumDirectory, @"Content\Svg\");
        string outputXaml = Path.Combine(gumDirectory, @"Themes\", "GumIcons.xaml");
        string enumOut = Path.Combine(gumDirectory, @"Themes\", "GumIconKind.g.cs");
        string enumNs = "Gum.Themes";
        string enumName = "GumIconKind";

        if (!Directory.Exists(inputDir))
        {
            Console.Error.WriteLine($"Input directory not found: {inputDir}");
            return 2;
        }

        var svgFiles = Directory.GetFiles(inputDir, "*.svg").OrderBy(p => p).ToList();
        if (svgFiles.Count == 0) { Console.Error.WriteLine("No .svg files found."); return 3; }

        var dict = new ResourceDictionary();
        var items = new List<(string FileNameNoExt, string ResourceKey, string EnumToken)>();

        var settings = new WpfDrawingSettings
        {
            TextAsGeometry = true,
            OptimizePath = true
        };

        int converted = 0;

        foreach (var filePath in svgFiles)
        {
            try
            {
                var xdoc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
                var viewBox = ParseViewBox(xdoc) ?? new Rect(0, 0, 32, 32);

                // Read SVG -> Drawing
                var reader = new FileSvgReader(settings);
                var drawing = reader.Read(filePath);
                if (drawing is null)
                {
                    Console.Error.WriteLine($"[WARN] Could not read: {filePath}");
                    continue;
                }

                var (primary, secondary) = FlattenTwoTone(drawing);

                // Hard-enforce the 2px live area (2..30) as a last safety
                var live = new RectangleGeometry(new Rect(viewBox.X + 2, viewBox.Y + 2, viewBox.Width - 4, viewBox.Height - 4));
                primary = Intersect(primary, live);
                secondary = Intersect(secondary, live);

                // Freeze
                primary.Freeze();
                secondary.Freeze();

                var name = Path.GetFileNameWithoutExtension(filePath);
                var key = MakeKey(name);
                var token = ToEnumToken(name, items);

                // Add resources. Primary is always present.
                dict.Add(key, primary);

                // Only add Secondary if it has content
                if (!IsEmpty(secondary))
                    dict.Add(key + ".Secondary", secondary);

                items.Add((name, key, token));
                converted++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] {filePath}: {ex.Message}");
            }
        }

        if (converted == 0) { Console.Error.WriteLine("No icons converted."); return 4; }

        // Write enum + key map (one enum per icon; secondary has no enum)
        WriteEnumAndMap(enumOut, enumNs, enumName, items);

        // Save ResourceDictionary (pretty XAML)
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputXaml))!);
        var xsettings = new System.Xml.XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using (var fs = File.Create(outputXaml))
        using (var xw = System.Xml.XmlWriter.Create(fs, xsettings))
        {
            xw.WriteComment("Auto-generated icon geometries. Keys are filenames. Two-tone: add .Secondary.");
            XamlWriter.Save(dict, xw);
        }

        Console.WriteLine($"Wrote {converted} icon(s) -> {outputXaml}");
        Console.WriteLine("Use with two stacked Paths:\n" +
            "<Path Data=\"{StaticResource Key}\" Fill=\"{DynamicResource IconBrush}\"/>\n" +
            "<Path Data=\"{StaticResource Key.Secondary}\" Fill=\"{DynamicResource IconBrush}\" Opacity=\"0.32\"/>");

        return 0;
    }

    // -------- Two-tone flattener (clip-aware, union per tone) --------------------

    private static (Geometry Primary, Geometry Secondary) FlattenTwoTone(Drawing drawing)
    {
        var primParts = new List<Geometry>();
        var toneParts = new List<Geometry>();
        Walk(drawing, Matrix.Identity, currentClipWorld: null, alphaChain: 1.0, primParts, toneParts);

        var primary = Union(primParts) ?? Geometry.Empty;
        var secondary = Union(toneParts) ?? Geometry.Empty;
        primary.Freeze();
        secondary.Freeze();

        return (primary, secondary);

        static void Walk(Drawing d, Matrix world, Geometry? currentClipWorld, double alphaChain,
                         List<Geometry> prim, List<Geometry> tone)
        {
            switch (d)
            {
                case DrawingGroup dg:
                    {
                        // Accumulate transform
                        var nextWorld = world;
                        if (dg.Transform is Transform t && !t.Value.IsIdentity)
                            nextWorld = Matrix.Multiply(nextWorld, t.Value);

                        Geometry? groupClipWorld = null;
                        if (dg.ClipGeometry is Geometry cg)
                        {
                            var c = cg.CloneCurrentValue();
                            var xf = CombineTransforms(c.Transform, new MatrixTransform(nextWorld));
                            if (xf is Transform xt && !xt.Value.IsIdentity) c.Transform = xt;
                            c.Freeze();
                            groupClipWorld = c;
                        }
                        // Combine with parent clip
                        var clipHere = CombineClips(currentClipWorld, groupClipWorld);

                        // Group opacity
                        var alphaHere = Clamp01(alphaChain * Clamp01(dg.Opacity));

                        foreach (var child in dg.Children)
                            Walk(child, nextWorld, clipHere, alphaHere, prim, tone);
                        break;
                    }

                case GeometryDrawing gd when gd.Geometry != null:
                    {
                        var baseGeom = gd.Geometry.CloneCurrentValue();
                        var xfWorld = CombineTransforms(baseGeom.Transform, new MatrixTransform(world));

                        // FILL
                        double fillA = alphaChain * BrushAlpha(gd.Brush);
                        if (fillA > 0)
                        {
                            var g = baseGeom.CloneCurrentValue();
                            if (xfWorld is Transform tf && !tf.Value.IsIdentity) g.Transform = tf;
                            g = ApplyClip(g, currentClipWorld);
                            if (!IsEmpty(g)) ((fillA >= 0.95) ? prim : tone).Add(g);
                        }

                        // STROKE (widened)
                        if (gd.Pen is Pen pen && pen.Thickness > 0)
                        {
                            double strokeA = alphaChain * BrushAlpha(pen.Brush);
                            if (strokeA > 0)
                            {
                                // 1) Transform the source path into world space
                                Geometry s = baseGeom.CloneCurrentValue();
                                if (xfWorld is Transform ts && !ts.Value.IsIdentity) s.Transform = ts;

                                // 2) Widen first (so hairlines and boundary-crossers survive)
                                Geometry w = s.GetWidenedPathGeometry(pen, 0.001, ToleranceType.Relative);

                                // 3) Then apply any active clip (group/user clip or live-area)
                                w = ApplyClip(w, currentClipWorld);

                                if (!IsEmpty(w)) ((strokeA >= 0.95) ? prim : tone).Add(w);
                            }
                        }
                        break;
                    }

                case GlyphRunDrawing gr when gr.GlyphRun != null:
                    {
                        var g = gr.GlyphRun.BuildGeometry();
                        if (!world.IsIdentity)
                            g.Transform = CombineTransforms(g.Transform, new MatrixTransform(world));
                        g = ApplyClip(g, currentClipWorld);

                        if (!IsEmpty(g)) prim.Add(g);
                        break;
                    }
            }
        }
    }

    // -------- Helpers ------------------------------------------------------------

    private static double BrushAlpha(Brush? brush)
    {
        if (brush is null) return 0.0;
        double a = Clamp01(brush.Opacity);
        if (brush is SolidColorBrush scb) a *= scb.Color.A / 255.0;
        return Clamp01(a);
    }

    private static Geometry? Union(List<Geometry> parts)
    {
        Geometry? acc = null;
        foreach (var g in parts)
            acc = acc is null ? g : Geometry.Combine(acc, g, GeometryCombineMode.Union, null, 0.001, ToleranceType.Relative);
        acc?.Freeze();
        return acc;
    }

    private static Geometry Intersect(Geometry a, Geometry b)
    {
        var c = Geometry.Combine(a, b, GeometryCombineMode.Intersect, null, 0.001, ToleranceType.Relative);
        c.Freeze();
        return c;
    }

    private static Geometry? CombineClips(Geometry? a, Geometry? b)
    {
        if (a is null) return b?.CloneCurrentValue();
        if (b is null) return a.CloneCurrentValue();
        var c = Geometry.Combine(a, b, GeometryCombineMode.Intersect, null, 0.001, ToleranceType.Relative);
        c.Freeze();
        return c;
    }

    private static Geometry ApplyClip(Geometry g, Geometry? clip)
    {
        if (clip is null) return g;
        var c = Geometry.Combine(g, clip, GeometryCombineMode.Intersect, null, 0.001, ToleranceType.Relative);
        c.Freeze();
        return c;
    }

    private static bool IsEmpty(Geometry g)
    {
        var b = g.Bounds;
        return b.IsEmpty || b.Width <= 0 || b.Height <= 0;
    }

    private static Transform? CombineTransforms(Transform? a, Transform? b)
    {
        bool aId = a is null || a.Value.IsIdentity;
        bool bId = b is null || b.Value.IsIdentity;
        if (aId && bId) return null;
        if (aId) return b;
        if (bId) return a;
        return new TransformGroup { Children = new TransformCollection { a!, b! } };
    }

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static Rect? ParseViewBox(XDocument xdoc)
    {
        var root = xdoc.Root;
        if (root is null) return null;
        var vbAttr = (string?)root.Attribute("viewBox");
        if (!string.IsNullOrWhiteSpace(vbAttr))
        {
            var p = vbAttr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 4 &&
                double.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                double.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) &&
                double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            {
                return new Rect(x, y, w, h);
            }
        }
        // Fallback to width/height
        double wv = TryNum((string?)root.Attribute("width")) ?? 32.0;
        double hv = TryNum((string?)root.Attribute("height")) ?? 32.0;
        return new Rect(0, 0, wv, hv);

        static double? TryNum(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim().TrimEnd(new[] { 'p', 'x', '%' });
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
        }
    }

    private static string MakeKey(string raw)
    {
        var safe = Regex.Replace(raw, @"[^A-Za-z0-9_.-]", "_");
        if (safe.Length == 0 || !char.IsLetter(safe[0])) safe = "_" + safe;
        return safe;
    }

    private static string ToEnumToken(string name, List<(string, string, string)> existing)
    {
        var parts = Regex.Split(name, @"[^A-Za-z0-9]+").Where(p => p.Length > 0);
        var token = string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
        if (string.IsNullOrEmpty(token)) token = "Icon";
        if (char.IsDigit(token[0])) token = "_" + token;

        var baseToken = token; int i = 2;
        while (existing.Any(e => e.Item3 == token)) token = baseToken + i++;
        return token;
    }

    private static void WriteEnumAndMap(string enumOut, string ns, string enumName,
        List<(string FileNameNoExt, string ResourceKey, string EnumToken)> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(enumOut))!);

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.ComponentModel;");
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("[TypeConverter(typeof(GumIconKindConverter))]");
        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");
        sb.AppendLine("    None = 0,");
        foreach (var it in items) sb.AppendLine($"    {it.EnumToken}, // {it.FileNameNoExt}");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"public static class {enumName}Map");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly string[] _keys = new string[] {");
        sb.AppendLine("        null, // None");
        foreach (var it in items) sb.AppendLine($"        \"{it.ResourceKey}\",");
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine($"    public static string? GetResourceKey({enumName} kind) =>");
        sb.AppendLine("        (int)kind >= 0 && (int)kind < _keys.Length ? _keys[(int)kind] : null;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// Lets you write Icon=\"folder-star\" or Icon=\"FolderStar\" in XAML");
        sb.AppendLine("public sealed class GumIconKindConverter : TypeConverter");
        sb.AppendLine("{");
        sb.AppendLine("    public override bool CanConvertFrom(ITypeDescriptorContext? ctx, Type t) =>");
        sb.AppendLine("        t == typeof(string) || base.CanConvertFrom(ctx, t);");
        sb.AppendLine("    public override object? ConvertFrom(ITypeDescriptorContext? ctx, CultureInfo? culture, object value)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (value is string s)");
        sb.AppendLine("        {");
        sb.AppendLine("            var normalized = ToEnumTokenForConverter(s);");
        sb.AppendLine($"            if (Enum.TryParse<{enumName}>(normalized, ignoreCase: false, out var result))");
        sb.AppendLine("                return result!;");
        sb.AppendLine("        }");
        sb.AppendLine("        return base.ConvertFrom(ctx, culture, value);");
        sb.AppendLine("    }");
        sb.AppendLine("    private static string ToEnumTokenForConverter(string name)");
        sb.AppendLine("    {");
        sb.AppendLine("        var parts = System.Text.RegularExpressions.Regex.Split(name, @\"[^A-Za-z0-9]+\").Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1));");
        sb.AppendLine("        var token = string.Concat(parts);");
        sb.AppendLine("        if (string.IsNullOrEmpty(token)) token = \"Icon\";");
        sb.AppendLine("        if (char.IsDigit(token[0])) token = \"_\" + token;");
        sb.AppendLine("        return token;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        File.WriteAllText(enumOut, sb.ToString(), new UTF8Encoding(false));
    }
}
