using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Logic;

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
    /// The standard element whose presence proves the Skia shape bundle is already in the project.
    /// Svg is used because it is added on every project version (unlike the legacy ColoredCircle /
    /// RoundedRectangle, which are no longer added on V3+), making it a version-proof sentinel.
    /// </summary>
    private const string SkiaShapeBundleSentinel = "Svg";

    public static ThemeRequirements LoadFromThemeDirectory(string themeDirectory)
    {
        var path = Path.Combine(themeDirectory, ThemeRequirementsFileName);
        if (!File.Exists(path)) return new ThemeRequirements();
        return Parse(File.ReadAllText(path));
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

        return new ThemeRequirementsDiff(fontGenChange, project.FontGenerator, addSkiaShapes);
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
        bool addSkiaShapes)
    {
        FontGeneratorChange = fontGeneratorChange;
        CurrentFontGenerator = currentFontGenerator;
        AddSkiaShapes = addSkiaShapes;
    }

    /// <summary>The new font generator to apply, or null if no change is needed.</summary>
    public FontGeneratorType? FontGeneratorChange { get; }

    /// <summary>The font generator the project currently has, for use in dialog text.</summary>
    public FontGeneratorType CurrentFontGenerator { get; }

    /// <summary>True when the full Skia shape Standard bundle must be added.</summary>
    public bool AddSkiaShapes { get; }

    public bool HasChanges => FontGeneratorChange.HasValue || AddSkiaShapes;

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
        return lines;
    }

    public void Apply(GumProjectSave project, ISkiaShapeStandardsLogic skiaShapeStandards)
    {
        if (FontGeneratorChange is { } target)
        {
            project.FontGenerator = target;
        }
        if (AddSkiaShapes)
        {
            skiaShapeStandards.AddAllStandards();
        }
    }
}
