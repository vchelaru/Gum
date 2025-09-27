using SharpVectors.Converters;
using SharpVectors.Dom;
using SharpVectors.Renderers.Wpf;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;


public static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Defaults
        string inputDir = @"C:\dev\Gum\Gum\Content\Svg\";
        string outputXaml = Path.Combine(@"C:\dev\Gum\Gum\Themes\", "GumIcons.xaml");
        bool recursive = false;
        bool asGeometry = true; // default: DrawingImage (multi-color)
        string keyPrefix = "";    // e.g., "Icon." if you want
        string enumOut = Path.Combine(@"C:\dev\Gum\Gum\Themes\", "GumIconKind.g.cs");
        string enumNamespace = "Gum.Themes";
        string enumName = "GumIconKind";

        // Very light argument parsing
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i":
                case "--in":
                    inputDir = RequireValue(args, ref i, "--in");
                    break;
                case "-o":
                case "--out":
                    outputXaml = RequireValue(args, ref i, "--out");
                    break;
                case "-r":
                case "--recursive":
                    recursive = true;
                    break;
                case "-g":
                case "--geometry":
                    asGeometry = true;
                    break;
                case "-p":
                case "--prefix":
                    keyPrefix = RequireValue(args, ref i, "--prefix");
                    break;
                case "-h":
                case "--help":
                    PrintHelp();
                    return 0;
                case "--enum-out": enumOut = RequireValue(args, ref i, "--enum-out"); break;
                case "--namespace": enumNamespace = RequireValue(args, ref i, "--namespace"); break;
                case "--enum-name": enumName = RequireValue(args, ref i, "--enum-name"); break;
                default:
                    // Treat a bare path as input dir (quality-of-life)
                    if (!args[i].StartsWith("-") && Directory.Exists(args[i]))
                    {
                        inputDir = args[i];
                    }
                    else
                    {
                        Console.Error.WriteLine($"Unknown option: {args[i]}");
                        PrintHelp();
                        return 1;
                    }
                    break;
            }
        }

        if (!Directory.Exists(inputDir))
        {
            Console.Error.WriteLine($"Input directory not found: {inputDir}");
            return 2;
        }

        var opts = new EnumerationOptions { RecurseSubdirectories = recursive, IgnoreInaccessible = true };
        var svgFiles = Directory.GetFiles(inputDir, "*.svg", opts).OrderBy(p => p).ToList();
        if (svgFiles.Count == 0)
        {
            Console.Error.WriteLine("No .svg files found.");
            return 3;
        }

        Console.WriteLine($"Found {svgFiles.Count} svg file(s). Converting {(asGeometry ? "to Geometry" : "to DrawingImage")}…");

        // SharpVectors reading setup
        var settings = new WpfDrawingSettings
        {
            TextAsGeometry = true, // avoid font dependency
            OptimizePath = true
        };
        var reader = new FileSvgReader(settings);

        var dict = new ResourceDictionary();

        int converted = 0;
        var items = new List<(string FileNameNoExt, string ResourceKey, string EnumToken)>();

        foreach (var filePath in svgFiles)
        {
            try
            {
                var drawing = reader.Read(filePath);
                if (drawing is null)
                {
                    Console.Error.WriteLine($"[WARN] Could not read: {filePath}");
                    continue;
                }

                var fileNameNoExt = Path.GetFileNameWithoutExtension(filePath);
                var key = MakeKey(keyPrefix, fileNameNoExt);         // resource key (XAML x:Key)
                var token = ToEnumToken(fileNameNoExt, items);        // enum member (PascalCase, unique)

                if (asGeometry)
                {
                    var geom = FlattenToGeometry(drawing);
                    geom.Freeze();
                    dict.Add(key, geom);
                }
                else
                {
                    var di = new DrawingImage(drawing);
                    di.Freeze();
                    dict.Add(key, di);
                }
                items.Add((fileNameNoExt, key, token));

                converted++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] {filePath}: {ex.Message}");
            }
        }
        WriteEnumAndMap(enumOut, enumNamespace, enumName, items);
        if (converted == 0)
        {
            Console.Error.WriteLine("No icons converted.");
            return 4;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputXaml))!);

        // Serialize ResourceDictionary to pretty XAML
        var xsettings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using (var fs = File.Create(outputXaml))
        using (var xw = XmlWriter.Create(fs, xsettings))
        {
            // Optional helpful comment
            xw.WriteComment("Auto-generated by gumicons. Keys are based on SVG filenames.");
            XamlWriter.Save(dict, xw);
        }

        


        Console.WriteLine($"Wrote {converted} resource(s) → {outputXaml}");
        Console.WriteLine(asGeometry
            ? "Use with: <Path Data=\"{StaticResource yourKey}\" Fill=\"{DynamicResource IconBrush}\"/>"
            : "Use with: <Image Source=\"{StaticResource yourKey}\" Width=\"16\" Height=\"16\"/>");

        return 0;
    }

    private static string ToEnumToken(string name, List<(string, string, string)> existing)
    {
        // Split on non-alphanumerics, PascalCase, ensure identifier
        var parts = Regex.Split(name, @"[^A-Za-z0-9]+").Where(p => p.Length > 0);
        var token = string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
        if (string.IsNullOrEmpty(token)) token = "Icon";
        if (char.IsDigit(token[0])) token = "_" + token;

        // Ensure uniqueness if two files collapse to same token
        var baseToken = token;
        int i = 2;
        while (existing.Any(e => e.Item3 == token))
            token = baseToken + i++;
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
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("[TypeConverter(typeof(GumIconKindConverter))]");
        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");
        sb.AppendLine("    None = 0,");
        foreach (var it in items)
            sb.AppendLine($"    {it.EnumToken}, // {it.FileNameNoExt}");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"public static class {enumName}Map");
        sb.AppendLine("{");
        sb.AppendLine($"    private static readonly string[] _keys = new string[] {{");
        sb.AppendLine("        null, // None");
        foreach (var it in items)
            sb.AppendLine($"        \"{it.ResourceKey}\",");
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
        sb.AppendLine();
        sb.AppendLine("    public override object? ConvertFrom(ITypeDescriptorContext? ctx, CultureInfo? culture, object value)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (value is string s)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Accept raw filename-ish (folder-star), or enum token (FolderStar)");
        sb.AppendLine("            var normalized = ToEnumTokenForConverter(s);");
        sb.AppendLine($"            if (Enum.TryParse(typeof({enumName}), normalized, ignoreCase: false, out var result))");
        sb.AppendLine("                return result!;");
        sb.AppendLine("        }");
        sb.AppendLine("        return base.ConvertFrom(ctx, culture, value);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static string ToEnumTokenForConverter(string name)");
        sb.AppendLine("    {");
        sb.AppendLine("        var parts = System.Text.RegularExpressions.Regex");
        sb.AppendLine("            .Split(name, @\"[^A-Za-z0-9]+\")");
        sb.AppendLine("            .Where(p => p.Length > 0)");
        sb.AppendLine("            .Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1));");
        sb.AppendLine("        var token = string.Concat(parts);");
        sb.AppendLine("        if (string.IsNullOrEmpty(token)) token = \"Icon\";");
        sb.AppendLine("        if (char.IsDigit(token[0])) token = \"_\" + token;");
        sb.AppendLine("        return token;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        File.WriteAllText(enumOut, sb.ToString(), new UTF8Encoding(false));
    }

    private static string RequireValue(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length) throw new ArgumentException($"Missing value for {name}");
        return args[++i];
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
gumicons - Convert SVGs in a folder to a XAML ResourceDictionary.

Usage:
  dotnet gumicons [options]
  dotnet gumicons <inputDir> [options]

Options:
  -i, --in <dir>        Input directory (default: .)
  -o, --out <file>      Output XAML file (default: ./Icons.xaml)
  -r, --recursive       Recurse subdirectories
  -g, --geometry        Output <Geometry> resources (single-color)
  -p, --prefix <text>   Prefix for keys (e.g., ""Icon."")
  -h, --help            Show help

Notes:
  • Default mode writes <DrawingImage> resources (preserves fills/strokes).
  • Geometry mode flattens shapes to a single GeometryGroup (loses per-part colors).
");
    }

    // Make a safe x:Key. Keep filename spirit; replace spaces and illegal chars with underscores.
    private static string MakeKey(string prefix, string filenameNoExt)
    {
        var safe = Regex.Replace(filenameNoExt, @"[^A-Za-z0-9_.-]", "_");
        // XAML is happier when keys start with a letter; prefix an underscore if not.
        if (!char.IsLetter(safe.FirstOrDefault())) safe = "_" + safe;
        return string.IsNullOrEmpty(prefix) ? safe : prefix + safe;
    }

    // Collapse a Drawing tree to a single Geometry (loses stroke width & per-part colors by design)
    private static Geometry FlattenToGeometry(Drawing drawing)
    {
        var root = new GeometryGroup();
        Walk(drawing, Matrix.Identity, root);
        root.Freeze();
        return root;

        static void Walk(Drawing d, Matrix accumulated, GeometryGroup outGroup)
        {
            switch (d)
            {
                case DrawingGroup dg:
                    var next = accumulated;
                    if (dg.Transform is Transform t)
                        next = Matrix.Multiply(next, t.Value);

                    foreach (var child in dg.Children)
                        Walk(child, next, outGroup);
                    break;

                case GeometryDrawing gd when gd.Geometry != null:
                    {
                        // Clone so we can mutate
                        Geometry g = gd.Geometry.CloneCurrentValue();

                        // Combine accumulated matrix with any transform already on the geometry
                        var combined = CombineTransforms(g.Transform, new MatrixTransform(accumulated));
                        if (combined is Transform ct && !ct.Value.IsIdentity)
                            g.Transform = ct;

                        // Add the fill geometry
                        outGroup.Children.Add(g);

                        // Optional: include stroke as widened outline (single-color workflows)
                        if (gd.Pen is Pen pen && pen.Thickness > 0)
                        {
                            // Widen AFTER setting the transform so stroke scales with it
                            var widened = g.GetWidenedPathGeometry(pen);
                            if (combined is Transform ct2 && !ct2.Value.IsIdentity)
                                widened.Transform = ct2; // safety if GetWidenedPathGeometry dropped it
                            outGroup.Children.Add(widened);
                        }
                        break;
                    }

                // If SharpVectors ever returns text as glyphs (rare with TextAsGeometry=true), handle it too:
                case GlyphRunDrawing gr when gr.GlyphRun != null:
                    {
                        var g = gr.GlyphRun.BuildGeometry();
                        var combined = new MatrixTransform(accumulated);
                        if (!combined.Value.IsIdentity)
                            g.Transform = CombineTransforms(g.Transform, combined);
                        outGroup.Children.Add(g);
                        break;
                    }

                    // Ignore ImageDrawing/VideoDrawing for icon scenarios
            }
        }

        static Transform? CombineTransforms(Transform? a, Transform? b)
        {
            bool aId = a is null || a.Value.IsIdentity;
            bool bId = b is null || b.Value.IsIdentity;

            if (aId && bId) return null;
            if (aId) return b;
            if (bId) return a;

            return new TransformGroup { Children = new TransformCollection { a, b! } };
        }
    }
}
