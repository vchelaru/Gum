using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.Presentation.Tests.Architecture;

/// <summary>
/// A shader parameter that is saved as a variable (#7010) has one canonical variable name and one marker
/// string. Both are built only by <c>ShaderVariableNaming</c> and <c>ShaderParameterMarker</c> in
/// <c>RenderingLibrary/Graphics</c>, so the tool, codegen, the runtime and gumcli can never disagree
/// about a name. Code that spells the component suffix by hand, or writes a marker string it assembled
/// itself, is the second copy that drifts. These are source scans, because a name and a marker are both
/// just strings to the compiler.
///
/// Lower a baseline as sites are fixed; never raise one to make a new violation pass.
/// </summary>
public class ShaderVariableNamingGuardTests
{
    private static readonly string[] OwningFiles =
    {
        "/RenderingLibrary/Graphics/ShaderVariableNaming.cs",
        "/RenderingLibrary/Graphics/ShaderParameterMarker.cs",
    };

    private static readonly Regex ComponentSuffixPattern = new Regex(@"""__[XYZW]""|""XYZW""");
    private static readonly Regex MarkerAssignmentPattern = new Regex(@"\bShaderParameter\s*=(?!=)");
    private static readonly Regex AllowedMarkerValuePattern = new Regex(@"=\s*(null\b|[\w.]*\.Format\(\))");
    private static readonly Regex HandBuiltMarkerPattern = new Regex(@"""#""|#\{[^}]*\}/\{");

    [Fact]
    public void Patterns_FlagTheLinesTheyAreMeantTo()
    {
        // Without this, a guard whose pattern matched nothing would stay green forever.
        ComponentSuffixPattern.IsMatch("string name = stem + \"__X\";").ShouldBeTrue();
        ComponentSuffixPattern.IsMatch("const string axes = \"XYZW\";").ShouldBeTrue();
        ComponentSuffixPattern.IsMatch("string name = marker.Format();").ShouldBeFalse();

        MarkerAssignmentPattern.IsMatch("variable.ShaderParameter = parameter + \"#\" + index;").ShouldBeTrue();
        MarkerAssignmentPattern.IsMatch("if (variable.ShaderParameter == null)").ShouldBeFalse();
        AllowedMarkerValuePattern.IsMatch("variable.ShaderParameter = marker.Format();").ShouldBeTrue();
        AllowedMarkerValuePattern.IsMatch("variable.ShaderParameter = null;").ShouldBeTrue();
        AllowedMarkerValuePattern.IsMatch("variable.ShaderParameter = parameter + \"#\" + index;").ShouldBeFalse();

        HandBuiltMarkerPattern.IsMatch("string text = parameter + \"#\" + index + \"/\" + count;").ShouldBeTrue();
        HandBuiltMarkerPattern.IsMatch("string text = $\"{parameter}#{index}/{count}\";").ShouldBeTrue();
        HandBuiltMarkerPattern.IsMatch("string text = marker.Format();").ShouldBeFalse();

        // And a scan that found no files would pass too.
        ProductionSourceFiles().Count(IsOwningFile).ShouldBe(OwningFiles.Length);
    }

    [Fact]
    public void ComponentSuffixLiterals_OutsideTheNamingClasses_DoesNotExceedBaseline()
    {
        // The "__X".."__W" suffix and the "XYZW" letter table belong to ShaderVariableNaming. Anything
        // else that needs to know whether a name is a component calls IsComponentName or
        // TryParseVariableName.
        const int Baseline = 0;

        List<(string File, int LineNumber, string Line)> violations = ProductionSourceFiles()
            .Where(file => !IsOwningFile(file))
            .SelectMany(MatchingLines(ComponentSuffixPattern))
            .ToList();

        violations.Count.ShouldBeLessThanOrEqualTo(Baseline, Describe(violations));
    }

    [Fact]
    public void MarkerAssignments_OutsideTheSerializersAndTheNamingClasses_DoesNotExceedBaseline()
    {
        // A marker is written as `ShaderParameter = marker.Format()` (or cleared with null). Building
        // the "Param#i/n" text by hand anywhere else would be a second copy of the format.
        // VariableSave declares the property and VariableSaveJson copies it between the DTO and the
        // save class; both only move a string that already exists.
        const int Baseline = 0;

        string[] copyingFiles =
        {
            "/GumDataTypes/Variables/VariableSave.cs",
            "/GumDataTypes/Serialization/Json/VariableSaveJson.cs",
        };

        List<(string File, int LineNumber, string Line)> violations = ProductionSourceFiles()
            .Where(file => !IsOwningFile(file))
            .Where(file => !copyingFiles.Any(c => file.Replace('\\', '/').EndsWith(c, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(MatchingLines(MarkerAssignmentPattern))
            .Where(item => !AllowedMarkerValuePattern.IsMatch(item.Line))
            .ToList();

        violations.Count.ShouldBeLessThanOrEqualTo(Baseline, Describe(violations));
    }

    [Fact]
    public void HandBuiltMarkerText_OutsideTheNamingClasses_DoesNotExceedBaseline()
    {
        // The `#` between parameter and component index, written into a string next to a `/`.
        const int Baseline = 0;

        List<(string File, int LineNumber, string Line)> violations = ProductionSourceFiles()
            .Where(file => !IsOwningFile(file))
            .Where(file => File.ReadAllText(file).Contains("ShaderParameter"))
            .SelectMany(MatchingLines(HandBuiltMarkerPattern))
            .ToList();

        violations.Count.ShouldBeLessThanOrEqualTo(Baseline, Describe(violations));
    }

    private static bool IsOwningFile(string file) =>
        OwningFiles.Any(owner => file.Replace('\\', '/').EndsWith(owner, StringComparison.OrdinalIgnoreCase));

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
        IEnumerable<string> rendered = violations
            .Select(v => $"  {Path.GetRelativePath(repoRoot, v.File)}:{v.LineNumber}  {v.Line.Trim()}");
        return $"{violations.Count} site(s) found:{Environment.NewLine}{string.Join(Environment.NewLine, rendered)}";
    }

    /// <summary>
    /// Every production .cs file that could name a shader variable: the runtimes, the data types and
    /// the tool. Test projects and build output are excluded.
    /// </summary>
    private static IEnumerable<string> ProductionSourceFiles()
    {
        string repoRoot = FindRepoRoot();
        string[] roots =
        {
            "Gum", "GumDataTypes", "GumCommon", "GumRuntime", "RenderingLibrary", "MonoGameGum",
            "Runtimes", "Tools", "Tool", "AvaloniaDataUi", "DataUi.Core", "SkiaGum", "RaylibGum", "KniGum", "FnaGum",
        };

        return roots
            .Select(root => Path.Combine(repoRoot, root))
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
            })
            .Distinct();
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
