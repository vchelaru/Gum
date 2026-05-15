using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;

namespace GumFormsPlugin.Services;

/// <summary>
/// Per-theme prerequisite metadata read from an optional <c>theme.txt</c> sitting in
/// the theme's content folder. Each theme can declare project-level settings it
/// needs (FontGenerator, extra Standard element references) and runtime NuGet
/// packages the user must add to their game project. A theme without a
/// <c>theme.txt</c> declares no prerequisites — that's the case for Standard.
/// </summary>
public sealed class ThemeRequirements
{
    public const string ThemeRequirementsFileName = "theme.txt";

    /// <summary>
    /// The font generator the theme expects. Null when the theme does not care.
    /// </summary>
    public FontGeneratorType? FontGenerator { get; init; }

    /// <summary>
    /// Standard element references that must be present in the project's gumx
    /// (e.g. <c>RoundedRectangle</c>, <c>ColoredCircle</c>).
    /// </summary>
    public IReadOnlyList<string> RequiredStandards { get; init; } = Array.Empty<string>();

    /// <summary>
    /// NuGet package families the user must add to their game project for the
    /// theme to render at runtime. Informational only — the tool cannot edit
    /// the user's .csproj.
    /// </summary>
    public IReadOnlyList<string> RuntimePackages { get; init; } = Array.Empty<string>();

    public static ThemeRequirements LoadFromThemeDirectory(string themeDirectory)
    {
        var path = Path.Combine(themeDirectory, ThemeRequirementsFileName);
        if (!File.Exists(path)) return new ThemeRequirements();
        return Parse(File.ReadAllText(path));
    }

    /// <summary>
    /// Parses the simple <c>key: value</c> format. Values that name a list are
    /// comma-separated. Lines starting with <c>#</c> are comments. Unknown
    /// keys are ignored (forward-compatibility with future themes).
    /// </summary>
    public static ThemeRequirements Parse(string text)
    {
        FontGeneratorType? fontGen = null;
        IReadOnlyList<string> standards = Array.Empty<string>();
        IReadOnlyList<string> packages = Array.Empty<string>();

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
                case "requiredstandards":
                    standards = SplitList(value);
                    break;
                case "runtimepackages":
                    packages = SplitList(value);
                    break;
            }
        }

        return new ThemeRequirements
        {
            FontGenerator = fontGen,
            RequiredStandards = standards,
            RuntimePackages = packages,
        };
    }

    private static IReadOnlyList<string> SplitList(string value) =>
        value.Split(',')
             .Select(s => s.Trim())
             .Where(s => s.Length > 0)
             .ToList();

    /// <summary>
    /// Compares the requirements against <paramref name="project"/> and returns
    /// the delta the user would need to accept before importing the theme.
    /// </summary>
    public ThemeRequirementsDiff Diff(GumProjectSave project)
    {
        var standardsToAdd = RequiredStandards
            .Where(name => !project.StandardElementReferences.Any(r =>
                string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        FontGeneratorType? fontGenChange =
            FontGenerator is { } targetGen && project.FontGenerator != targetGen
                ? targetGen
                : null;

        return new ThemeRequirementsDiff(fontGenChange, project.FontGenerator, standardsToAdd, RuntimePackages);
    }
}

/// <summary>
/// The set of project-level edits needed before a theme can be imported,
/// plus the informational runtime-package list to show the user afterwards.
/// </summary>
public sealed class ThemeRequirementsDiff
{
    public ThemeRequirementsDiff(
        FontGeneratorType? fontGeneratorChange,
        FontGeneratorType currentFontGenerator,
        IReadOnlyList<string> standardsToAdd,
        IReadOnlyList<string> runtimePackages)
    {
        FontGeneratorChange = fontGeneratorChange;
        CurrentFontGenerator = currentFontGenerator;
        StandardsToAdd = standardsToAdd;
        RuntimePackages = runtimePackages;
    }

    /// <summary>The new font generator to apply, or null if no change is needed.</summary>
    public FontGeneratorType? FontGeneratorChange { get; }

    /// <summary>The font generator the project currently has, for use in dialog text.</summary>
    public FontGeneratorType CurrentFontGenerator { get; }

    /// <summary>Names of Standard element references missing from the project.</summary>
    public IReadOnlyList<string> StandardsToAdd { get; }

    /// <summary>Runtime NuGet package families the user should add to their game project.</summary>
    public IReadOnlyList<string> RuntimePackages { get; }

    /// <summary>True when applying this diff will write something to the project's gumx.</summary>
    public bool HasGumxChanges => FontGeneratorChange.HasValue || StandardsToAdd.Count > 0;

    /// <summary>One human-readable line per gumx-level change.</summary>
    public IReadOnlyList<string> DescribeGumxChanges()
    {
        var lines = new List<string>();
        if (FontGeneratorChange is { } target)
        {
            lines.Add($"Switch font generator from {CurrentFontGenerator} to {target} " +
                      "(this re-rasterizes every font in your project).");
        }
        if (StandardsToAdd.Count > 0)
        {
            lines.Add("Add Standard element references: " + string.Join(", ", StandardsToAdd));
        }
        return lines;
    }

    public void Apply(GumProjectSave project)
    {
        if (FontGeneratorChange is { } target)
        {
            project.FontGenerator = target;
        }
        foreach (var name in StandardsToAdd)
        {
            project.StandardElementReferences.Add(new ElementReference
            {
                Name = name,
                ElementType = ElementType.Standard,
            });
        }
    }
}
