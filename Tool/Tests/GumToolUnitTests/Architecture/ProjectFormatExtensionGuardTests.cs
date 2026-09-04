using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Shouldly;

namespace GumToolUnitTests.Architecture;

/// <summary>
/// A Gum project is either XML (.gumx) or JSON (.gumj), and every element, behavior, and animation
/// file in it follows the project's format. Code that composes an on-disk path from the bare XML
/// extension writes a file the project never loads back, so the edit looks saved and is lost.
///
/// That defect has appeared independently in the element save path, the behavior save path, the CLI
/// fixer, standard-element recreation, the tree view, drag/drop, gumx import, and the animation
/// sidecar. These are source-text scans that fail when a new site appears, because the compiler
/// cannot tell a correct extension from an incorrect one — both are strings.
///
/// Use the format-aware accessors instead: <c>ElementSave.GetFileExtension(bool)</c>,
/// <c>ElementReference.GetExtension(bool)</c>, <c>BehaviorReference.GetRelativeFilePath(bool)</c>,
/// <c>ElementAnimationsSave.GetFileNameSuffix(bool)</c>, or <c>IFileCommands.GetFullPathXmlFile</c>,
/// each driven by <c>GumProjectSave.IsJsonFormat(projectFileName)</c>.
///
/// Lower a baseline as sites are fixed; never raise one to make a new violation pass. Each remaining
/// site is enumerated below with why it is legitimate.
/// </summary>
public class ProjectFormatExtensionGuardTests
{
    [Fact]
    public void BareElementFileExtensionUses_DoesNotExceedBaseline()
    {
        // `element.FileExtension` is unconditionally the XML extension. The three remaining uses
        // are all deliberate:
        //   * GumProjectDependencyWalker — inside its own GetElementExtension(element, isJsonFormat)
        //     helper, which applies the x->j swap.
        //   * ConvertProjectToJsonService — reads the XML source it is converting FROM, by design.
        //   * ElementFilePathHelper — has no access to the project file name, so it cannot know the
        //     format; its callers only use the result via RemoveExtension() to reach a sibling file.
        //     Its doc comment carries the do-not-use-for-the-element-file warning.
        const int Baseline = 3;

        // ElementAnimationsSave.FileExtension is a different constant (the .ganx sidecar), guarded
        // by HardcodedAnimationSidecarSuffixes instead.
        var pattern = new Regex(@"(?<!ElementAnimationsSave)\.FileExtension\b");

        var violations = ProductionSourceFiles()
            .SelectMany(MatchingLines(pattern))
            .ToList();

        violations.Count.ShouldBeLessThanOrEqualTo(Baseline, Describe(violations));
    }

    [Fact]
    public void BareXmlExtensionConstantsInPathComposition_DoesNotExceedBaseline()
    {
        // Lines that name one of the XML extension constants. A match is excluded when the JSON
        // counterpart appears within two lines — that is already both-format handling (a file-dialog
        // filter, a recognized-extension set, a multi-line || chain). The files that *define* these
        // accessors are excluded too; they are the seam everything else should route through.
        //
        // The remaining sites read from a known-XML source or create a brand-new project, which is
        // always .gumx:
        //   * GumxSourceService — the import-from-gumx source is fetched over HTTP and is XML-only
        //     by design today; a .gumj source URL is an unimplemented feature, not a data-loss path.
        //   * ProjectLoader — its silent-drop scan looks for XML element names that XmlSerializer
        //     drops without error. JSON has no such failure mode, so the scan is XML-only.
        //   * FormsThemeBehaviorStagingService — writes into a flat postbuild staging directory,
        //     not into a Gum project.
        const int Baseline = 8;

        var pattern = new Regex(
            @"(GumProjectSave\.(Screen|Component|Standard|Project)Extension|BehaviorReference\.Extension)\b");
        var jsonAware = new Regex(@"JsonExtension|IsJsonFormat|GetFileExtension|GetExtension\(");

        // These files declare the extension accessors themselves.
        string[] accessorDefinitions =
        {
            "/GumDataTypes/ComponentSave.cs",
            "/GumDataTypes/ScreenSave.cs",
            "/GumDataTypes/StandardElementSave.cs",
            "/GumDataTypes/ElementReference.cs",
        };

        var violations = ProductionSourceFiles()
            .Where(f => !accessorDefinitions.Any(d => f.Replace('\\', '/').EndsWith(d, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(MatchingLinesWithContext(pattern, jsonAware, contextLines: 2))
            .ToList();

        violations.Count.ShouldBeLessThanOrEqualTo(Baseline, Describe(violations));
    }

    [Fact]
    public void HardcodedAnimationSidecarSuffixes_DoesNotExceedBaseline()
    {
        // The "Animations.ganx"/"Animations.ganj" suffix must come from
        // ElementAnimationsSave.GetFileNameSuffix(bool) so the suffix and the serializer can never
        // disagree — an XML payload inside a .ganj breaks the runtime's animation loader, and the
        // reverse leaves the Animations tab showing nothing.
        const int Baseline = 0;

        var pattern = new Regex(@"""Animations\.gan[xj]""");

        var violations = ProductionSourceFiles()
            .SelectMany(MatchingLines(pattern))
            .ToList();

        violations.Count.ShouldBeLessThanOrEqualTo(Baseline, Describe(violations));
    }

    [Fact]
    public void XmlSerializerUsesOnAnimationFiles_DoesNotExceedBaseline()
    {
        // Reading or writing an ElementAnimationsSave must go through ElementAnimationsSave
        // .Load/.Save, which pick the serializer off the file's own extension. A direct
        // FileManager.XmlSerialize/XmlDeserialize call reintroduces the bug where the path says
        // .ganj and the bytes say XML.
        //
        // Two remaining uses, both already holding a known-format payload rather than a path:
        //   * GumAnimationLoader's .ganx branch — the file was selected by glob and is read from a
        //     stream, so the runtime has no FilePath to dispatch on.
        //   * GumxImportService — deserializes a known-XML source sidecar out of fetched bytes,
        //     then re-saves it through ElementAnimationsSave.Save into the destination's format.
        // ElementAnimationsSave itself is excluded: it is the dispatching implementation.
        const int Baseline = 2;

        var pattern = new Regex(@"Xml(Serialize|Deserialize)\w*<?\s*\(?\s*[^)]*ElementAnimationsSave");

        var violations = ProductionSourceFiles()
            .Where(f => !f.Replace('\\', '/').EndsWith("/GumDataTypes/SaveClasses/ElementAnimationsSave.cs",
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(MatchingLines(pattern))
            .ToList();

        violations.Count.ShouldBeLessThanOrEqualTo(Baseline, Describe(violations));
    }

    /// <summary>
    /// Like <see cref="MatchingLines"/>, but drops a match when <paramref name="exempt"/> matches
    /// any line within <paramref name="contextLines"/> of it. Format handling is often spread over a
    /// multi-line <c>||</c> chain or an adjacent <c>.Concat(...)</c>, where the XML and JSON halves
    /// sit on different lines and a per-line check would flag correct code.
    /// </summary>
    private static Func<string, IEnumerable<(string File, int LineNumber, string Line)>> MatchingLinesWithContext(
        Regex pattern, Regex exempt, int contextLines) =>
        file =>
        {
            string[] lines = File.ReadAllLines(file);
            return MatchingLines(pattern)(file).Where(item =>
            {
                int index = item.LineNumber - 1;
                int start = System.Math.Max(0, index - contextLines);
                int end = System.Math.Min(lines.Length - 1, index + contextLines);
                for (int i = start; i <= end; i++)
                {
                    if (exempt.IsMatch(lines[i]))
                    {
                        return false;
                    }
                }
                return true;
            });
        };

    private static Func<string, IEnumerable<(string File, int LineNumber, string Line)>> MatchingLines(Regex pattern) =>
        file => File.ReadAllLines(file)
            .Select((line, index) => (File: file, LineNumber: index + 1, Line: line))
            .Where(item =>
            {
                string trimmed = item.Line.TrimStart();
                bool isComment = trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("/*");
                return !isComment && pattern.IsMatch(item.Line);
            });

    private static string Describe(IReadOnlyCollection<(string File, int LineNumber, string Line)> violations)
    {
        string repoRoot = FindRepoRoot();
        var rendered = violations
            .Select(v => $"  {Path.GetRelativePath(repoRoot, v.File)}:{v.LineNumber}  {v.Line.Trim()}");
        return $"{violations.Count} site(s) found:{Environment.NewLine}{string.Join(Environment.NewLine, rendered)}";
    }

    /// <summary>
    /// Every production .cs file in the projects that compose or serialize Gum project files. Test
    /// projects, obj/bin output, and generated files are excluded — a test may legitimately assert
    /// against a literal .gucx path.
    /// </summary>
    private static IEnumerable<string> ProductionSourceFiles()
    {
        string repoRoot = FindRepoRoot();
        string[] roots =
        {
            Path.Combine(repoRoot, "Gum"),
            Path.Combine(repoRoot, "GumDataTypes"),
            Path.Combine(repoRoot, "GumCommon"),
            Path.Combine(repoRoot, "Tools"),
            Path.Combine(repoRoot, "Tool"),
        };

        return roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(file =>
            {
                string normalized = file.Replace('\\', '/');
                return !normalized.Contains("/obj/")
                    && !normalized.Contains("/bin/")
                    && !normalized.Contains("/Tests/")
                    && !normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
                    && !normalized.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
            });
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Gum")) && File.Exists(Path.Combine(current, "GumFull.sln")))
            {
                return current;
            }
            current = Path.GetFullPath(Path.Combine(current, ".."));
        }
        throw new DirectoryNotFoundException("Could not locate the repo root from " + AppContext.BaseDirectory);
    }
}
