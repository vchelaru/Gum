using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Templates/FormsThemes/&lt;theme&gt; is a separate template tree from Templates/FormsTemplate
/// (the gumcli/StandardElementsManager path fixed by #4674) - it's copied by GumFormsPlugin's MSBuild
/// targets, not extracted from embedded resources, so nothing else sweeps it: for a stray system font
/// name (e.g. "Arial") that KernSmith can't resolve on BlazorGL/WASM, or for error-check errors that
/// every project a user imports the theme into would inherit.
/// </summary>
public class FormsThemesTemplateTests
{
    public static IEnumerable<object[]> ThemeNames => new[]
    {
        "Bubblegum", "DarkPro", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95"
    }.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Load_ShouldNotReferenceASystemFontNameAnywhere(string themeName)
    {
        StandardElementsManager.Self.Initialize();

        string themeDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", themeName);
        ProjectLoadResult result = new ProjectLoader().Load(Path.Combine(themeDir, "GumProject.gumx"));

        result.Success.ShouldBeTrue();
        result.LoadErrors.ShouldBeEmpty();

        IEnumerable<ElementSave> allElements = result.Project!.StandardElements
            .Cast<ElementSave>()
            .Concat(result.Project.Components)
            .Concat(result.Project.Screens);

        List<string> systemFontReferences = allElements
            .SelectMany(e => e.DefaultState!.Variables, (e, v) => (Element: e, Variable: v))
            .Where(x => x.Variable.IsFont && x.Variable.Value is string font
                && !font.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"{x.Element.Name}.{x.Variable.Name} = {x.Variable.Value}")
            .ToList();

        systemFontReferences.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Load_ShouldHaveNoErrorCheckErrors(string themeName)
    {
        StandardElementsManager.Self.Initialize();
        string themeDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", themeName);
        ProjectLoadResult result = new ProjectLoader().Load(Path.Combine(themeDir, "GumProject.gumx"));
        result.Success.ShouldBeTrue();

        IReadOnlyList<ErrorResult> errors = new HeadlessErrorChecker(new DefaultTypeResolver()).GetAllErrors(result.Project!);

        errors.Where(error => error.Severity == ErrorSeverity.Error)
            .Select(error => $"{error.ElementName}: {error.Message}")
            .ShouldBeEmpty();
    }

    private static readonly string[] AllTemplateFolders =
    {
        "FormsTemplate",
        "FormsThemes/Bubblegum", "FormsThemes/DarkPro", "FormsThemes/ForestGlade", "FormsThemes/Hazard",
        "FormsThemes/Meadow", "FormsThemes/Neon", "FormsThemes/Retro95"
    };

    public static IEnumerable<object[]> TemplateFolders => AllTemplateFolders.Select(folder => new object[] { folder });

    public static IEnumerable<object[]> TemplateFoldersAndSubfolders =>
        AllTemplateFolders.SelectMany(folder => new[]
        {
            new object[] { folder, "Components", GumProjectSave.ComponentExtension },
            new object[] { folder, "Screens", GumProjectSave.ScreenExtension },
        });

    // Add Forms installs every element file on disk (FormsFileService.GetSourceDestinations), while
    // the tool and gumcli check only see what the .gumx references. An element on disk but not in
    // the .gumx is installed without ever being checked or seen while the theme is edited (#5348).
    [Theory]
    [MemberData(nameof(TemplateFoldersAndSubfolders))]
    public void GumxReferences_ShouldMatchElementFilesOnDisk(string templateFolder, string subfolder, string extension)
    {
        StandardElementsManager.Self.Initialize();
        string templateDir = GetTemplateDir(templateFolder);
        GumProjectSave project = new ProjectLoader().Load(Path.Combine(templateDir, "GumProject.gumx")).Project!;

        List<ElementReference> references = subfolder == "Screens" ? project.ScreenReferences : project.ComponentReferences;
        List<string> inGumx = references
            .Select(reference => reference.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        inGumx.ShouldBe(GetElementNamesOnDisk(templateDir, subfolder, extension),
            $"{templateFolder}/{subfolder}: .gumx references vs files on disk");
    }

    // gumcli new/add-forms extract the base template from embedded resources by its manifest
    // (FormsTemplateCreator), so an element missing from the manifest never reaches a new project.
    // Themes have no manifest: Add Forms copies their folders directly.
    [Theory]
    [InlineData("Components", GumProjectSave.ComponentExtension)]
    [InlineData("Screens", GumProjectSave.ScreenExtension)]
    public void FormsTemplateManifest_ShouldMatchElementFilesOnDisk(string subfolder, string extension)
    {
        string templateDir = GetTemplateDir("FormsTemplate");

        List<string> inManifest = File.ReadAllLines(Path.Combine(templateDir, "manifest.txt"))
            .Select(line => line.Trim())
            .Where(line => line.StartsWith(subfolder + "/", StringComparison.Ordinal)
                && line.EndsWith("." + extension, StringComparison.Ordinal))
            .Select(line => line.Substring(subfolder.Length + 1, line.Length - subfolder.Length - extension.Length - 2))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        inManifest.ShouldBe(GetElementNamesOnDisk(templateDir, subfolder, extension),
            $"FormsTemplate/{subfolder}: manifest.txt vs files on disk");
    }

    // GumFormsPlugin copies each template folder, FontCache included, into the release's
    // Content/FormsThemes. A cached font no element needs ships for nothing, and the Standard
    // theme's leftovers were rendered from Arial and Wasco Sans, which may not be redistributed (#5430).
    [Theory]
    [MemberData(nameof(TemplateFolders))]
    public void FontCache_ShouldOnlyContainFontsTheTemplateUses(string templateFolder)
    {
        StandardElementsManager.Self.Initialize();
        string templateDir = GetTemplateDir(templateFolder);
        GumProjectSave project = new ProjectLoader().Load(Path.Combine(templateDir, "GumProject.gumx")).Project!;
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            IEnumerable<ElementSave> allElements = project.StandardElements
                .Cast<ElementSave>()
                .Concat(project.Components)
                .Concat(project.Screens);
            HashSet<string> required = new FontReferenceCollector(instance => ObjectFinder.Self.GetElementSave(instance))
                .Collect(project, allElements)
                .Keys
                .Select(Path.GetFileName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

            string fontCacheDir = Path.Combine(templateDir, "FontCache");
            List<string> unused = Directory.Exists(fontCacheDir)
                ? Directory.GetFiles(fontCacheDir, "*.fnt")
                    .Select(Path.GetFileName)
                    .Where(fileName => !required.Contains(fileName!))
                    .OrderBy(fileName => fileName, StringComparer.Ordinal)
                    .ToList()!
                : new List<string>();

            unused.ShouldBeEmpty();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    // GumFormsPlugin stages every file in a template folder into the release, so anything that is not
    // project content (an executable, a stray tool config with a personal path) ships to users (#5441).
    // FontCache holds generated fonts (.bmfc/.fnt/.png); everywhere else only project content is allowed.
    private static readonly HashSet<string> AllowedContentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gumx", ".gumfcs", ".gucx", ".gusx", ".gutx", ".behx", ".ganx", ".codsj",
        ".png", ".ttf", ".txt", ".gitignore"
    };

    private static readonly HashSet<string> AllowedFontCacheExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmfc", ".fnt", ".png"
    };

    [Theory]
    [MemberData(nameof(TemplateFolders))]
    public void TemplateFiles_ShouldOnlyBeProjectContent(string templateFolder)
    {
        string templateDir = GetTemplateDir(templateFolder);
        string fontCacheDir = Path.Combine(templateDir, "FontCache") + Path.DirectorySeparatorChar;

        List<string> unexpected = Directory.GetFiles(templateDir, "*", SearchOption.AllDirectories)
            .Where(file =>
            {
                HashSet<string> allowed = file.StartsWith(fontCacheDir, StringComparison.Ordinal)
                    ? AllowedFontCacheExtensions
                    : AllowedContentExtensions;
                return !allowed.Contains(Path.GetExtension(file));
            })
            .Select(file => Path.GetRelativePath(templateDir, file).Replace('\\', '/'))
            .ToList();

        unexpected.ShouldBeEmpty($"{templateFolder}: files that are not project content");
    }

    private static string GetTemplateDir(string templateFolder) =>
        Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates",
            templateFolder.Replace('/', Path.DirectorySeparatorChar));

    private static List<string> GetElementNamesOnDisk(string templateDir, string subfolder, string extension)
    {
        string elementDir = Path.Combine(templateDir, subfolder);
        return Directory.GetFiles(elementDir, "*." + extension, SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(elementDir, file).Replace('\\', '/'))
            .Select(relative => relative.Substring(0, relative.Length - extension.Length - 1))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    // Same check as "gumcli check-references": a reference whose scalar isn't materialized renders
    // one way in the tool (which applies references) and another at runtime (which reads the scalar).
    [Theory]
    [MemberData(nameof(TemplateFolders))]
    public void Load_ShouldHaveNoUnpropagatedVariableReferences(string templateFolder)
    {
        StandardElementsManager.Self.Initialize();
        string templateDir = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates",
            templateFolder.Replace('/', Path.DirectorySeparatorChar));
        GumProjectSave project = new ProjectLoader().Load(Path.Combine(templateDir, "GumProject.gumx")).Project!;
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            new ReferencePropagationService().Detect(project).Elements
                .Select(entry => entry.Element.Name)
                .ShouldBeEmpty();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string templatesDir = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(templatesDir))
            {
                return current;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate repo root from " + AppContext.BaseDirectory);
    }
}
