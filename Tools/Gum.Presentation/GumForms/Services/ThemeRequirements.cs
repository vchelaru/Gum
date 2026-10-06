using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Gum.DataTypes;
using Gum.Logic;
using RenderingLibrary.Graphics.Fonts;

namespace GumFormsPlugin.Services;

/// <summary>
/// Per-theme prerequisite metadata read from an optional <c>theme.txt</c> sitting
/// in the theme's content folder. Each theme can declare project-level edits
/// it needs before the components/screens can be imported cleanly. A theme
/// without a <c>theme.txt</c> declares no prerequisites — Standard is one
/// such theme.
/// </summary>
/// <remarks>
/// The tool only describes and applies changes to the Gum project itself.
/// Runtime concerns (NuGet packages on the user's game project) are out of
/// scope: each runtime has different package names, and we'd rather say
/// nothing than say something wrong.
/// </remarks>
public sealed class ThemeRequirements
{
    public const string ThemeRequirementsFileName = "theme.txt";

    private const string ThemeProjectFileName = "GumProject.gumx";

    /// <summary>
    /// The font generator the theme expects. Null when the theme does not care.
    /// </summary>
    public FontGeneratorType? FontGenerator { get; init; }

    /// <summary>
    /// True when the theme uses any Skia-backed Standard (e.g. Svg, Canvas, Arc).
    /// When set, the apply step adds the Skia shape bundle via
    /// <see cref="ISkiaShapeStandardsLogic.AddAllStandards"/> — Arc, Canvas, Line,
    /// LottieAnimation, Svg, plus the legacy ColoredCircle / RoundedRectangle on
    /// pre-v3 projects only (V3+ projects use the plain Circle / Rectangle instead).
    /// </summary>
    public bool RequiresSkiaShapes { get; init; }

    /// <summary>
    /// The characters the theme's fonts need, from the theme project's own <c>FontRanges</c>. The
    /// theme draws some of its controls with glyphs outside Gum's default ranges (a CheckBox's
    /// check mark is U+2713), and those glyphs only exist in fonts generated with these ranges.
    /// </summary>
    public string? FontRanges { get; init; }

    /// <summary>
    /// The standard element whose presence proves the Skia shape bundle is already in the project.
    /// Svg is used because it is added on every project version (unlike the legacy ColoredCircle /
    /// RoundedRectangle, which are no longer added on V3+), making it a version-proof sentinel.
    /// </summary>
    private const string SkiaShapeBundleSentinel = "Svg";

    public static ThemeRequirements LoadFromThemeDirectory(string themeDirectory)
    {
        var path = Path.Combine(themeDirectory, ThemeRequirementsFileName);
        ThemeRequirements fromFile = File.Exists(path) ? Parse(File.ReadAllText(path)) : new ThemeRequirements();
        return new ThemeRequirements
        {
            FontGenerator = fromFile.FontGenerator,
            RequiresSkiaShapes = fromFile.RequiresSkiaShapes,
            FontRanges = ReadThemeProjectFontRanges(themeDirectory),
        };
    }

    // Read straight from the theme's .gumx rather than loading the whole project: only this one
    // value is needed, and it is a plain top-level element. The Add Forms dialog reads this to
    // describe a theme, so a theme project that can't be read means no requirement, not a crash.
    private static string? ReadThemeProjectFontRanges(string themeDirectory)
    {
        string projectPath = Path.Combine(themeDirectory, ThemeProjectFileName);
        if (!File.Exists(projectPath)) return null;
        string? ranges;
        try
        {
            ranges = XDocument.Load(projectPath).Root?.Element("FontRanges")?.Value;
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
        return string.IsNullOrWhiteSpace(ranges) ? null : ranges.Trim();
    }

    /// <summary>
    /// Parses the simple <c>key: value</c> format. Lines starting with <c>#</c>
    /// are comments. Unknown keys are ignored (forward-compatibility with
    /// future themes).
    /// </summary>
    public static ThemeRequirements Parse(string text)
    {
        FontGeneratorType? fontGen = null;
        bool requiresSkiaShapes = false;

        foreach (var rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            var colonIndex = line.IndexOf(':');
            if (colonIndex < 0) continue;

            var key = line.Substring(0, colonIndex).Trim();
            var value = line.Substring(colonIndex + 1).Trim();

            switch (key.ToLowerInvariant())
            {
                case "fontgenerator":
                    if (Enum.TryParse<FontGeneratorType>(value, ignoreCase: true, out var fg))
                        fontGen = fg;
                    break;
                case "requiresskiashapes":
                    if (bool.TryParse(value, out var b))
                        requiresSkiaShapes = b;
                    break;
            }
        }

        return new ThemeRequirements
        {
            FontGenerator = fontGen,
            RequiresSkiaShapes = requiresSkiaShapes,
        };
    }

    /// <summary>
    /// Compares the requirements against <paramref name="project"/> and returns
    /// the changes the import would need to apply. <c>Svg</c>'s presence is the
    /// proxy for "this project already has Skia shapes" — the apply step is
    /// idempotent so a more thorough check isn't necessary.
    /// </summary>
    public ThemeRequirementsDiff Diff(GumProjectSave project)
    {
        FontGeneratorType? fontGenChange =
            FontGenerator is { } targetGen && project.FontGenerator != targetGen
                ? targetGen
                : null;

        bool addSkiaShapes = RequiresSkiaShapes &&
            !project.StandardElementReferences.Any(r =>
                string.Equals(r.Name, SkiaShapeBundleSentinel, StringComparison.OrdinalIgnoreCase));

        return new ThemeRequirementsDiff(fontGenChange, project.FontGenerator, addSkiaShapes,
            GetWidenedFontRanges(project.FontRanges));
    }

    // The project's ranges plus any of the theme's characters they lack, or null when they already
    // cover every one. Only ever adds: a project's own extra characters are kept.
    private string? GetWidenedFontRanges(string? projectRanges)
    {
        if (FontRanges == null) return null;

        HashSet<int> projectCharacters = new HashSet<int>(BmfcSave.ParseCharRanges(projectRanges ?? string.Empty));
        List<int> missing = BmfcSave.ParseCharRanges(FontRanges).Where(c => !projectCharacters.Contains(c)).ToList();
        if (missing.Count == 0) return null;

        projectCharacters.UnionWith(missing);
        return FormatRanges(projectCharacters);
    }

    private static string FormatRanges(IEnumerable<int> characters)
    {
        List<string> parts = new List<string>();
        List<int> sorted = characters.OrderBy(c => c).ToList();
        int index = 0;
        while (index < sorted.Count)
        {
            int start = sorted[index];
            int end = start;
            while (index + 1 < sorted.Count && sorted[index + 1] == end + 1)
            {
                index++;
                end = sorted[index];
            }
            parts.Add(start == end ? start.ToString() : $"{start}-{end}");
            index++;
        }
        return string.Join(",", parts);
    }
}

/// <summary>
/// The set of project-level edits an import needs to apply before copying
/// the theme's content files.
/// </summary>
public sealed class ThemeRequirementsDiff
{
    public ThemeRequirementsDiff(
        FontGeneratorType? fontGeneratorChange,
        FontGeneratorType currentFontGenerator,
        bool addSkiaShapes,
        string? fontRangesChange = null)
    {
        FontGeneratorChange = fontGeneratorChange;
        CurrentFontGenerator = currentFontGenerator;
        AddSkiaShapes = addSkiaShapes;
        FontRangesChange = fontRangesChange;
    }

    /// <summary>The new font generator to apply, or null if no change is needed.</summary>
    public FontGeneratorType? FontGeneratorChange { get; }

    /// <summary>The font generator the project currently has, for use in dialog text.</summary>
    public FontGeneratorType CurrentFontGenerator { get; }

    /// <summary>True when the full Skia shape Standard bundle must be added.</summary>
    public bool AddSkiaShapes { get; }

    /// <summary>The project's new font ranges, widened to cover the theme's characters, or null if no change is needed.</summary>
    public string? FontRangesChange { get; }

    public bool HasChanges => FontGeneratorChange.HasValue || AddSkiaShapes || FontRangesChange != null;

    /// <summary>One human-readable bullet per change. Empty when <see cref="HasChanges"/> is false.</summary>
    public IReadOnlyList<string> DescribeChanges()
    {
        var lines = new List<string>();
        if (FontGeneratorChange is { } target)
        {
            lines.Add($"Switch font generator from {CurrentFontGenerator} to {target} " +
                      "(re-rasterizes every font in your project).");
        }
        if (AddSkiaShapes)
        {
            lines.Add("Add Skia shape Standards (Arc, Canvas, Line, LottieAnimation, Svg).");
        }
        if (FontRangesChange != null)
        {
            lines.Add("Add the characters this theme draws (such as the check mark) to the project's font ranges " +
                      "(regenerates every font in your project).");
        }
        return lines;
    }

    public void Apply(GumProjectSave project, ISkiaShapeStandardsLogic skiaShapeStandards)
    {
        if (FontGeneratorChange is { } target)
        {
            project.FontGenerator = target;
        }
        if (FontRangesChange != null)
        {
            project.FontRanges = FontRangesChange;
        }
        if (AddSkiaShapes)
        {
            skiaShapeStandards.AddAllStandards();
        }
    }
}
