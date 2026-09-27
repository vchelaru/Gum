using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    // Add Forms installs every element file on disk (FormsFileService.GetSourceDestinations) and
    // gumcli new/add-forms install every manifest line (FormsTemplateCreator), while the tool and
    // gumcli check only see what the .gumx references. An element in one set but not another is
    // installed without ever being checked or seen while the theme is edited (#5348).
    [Theory]
    [MemberData(nameof(TemplateFoldersAndSubfolders))]
    public void GumxReferences_ShouldMatchElementFilesOnDiskAndManifest(string templateFolder, string subfolder, string extension)
    {
        StandardElementsManager.Self.Initialize();
        string templateDir = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates",
            templateFolder.Replace('/', Path.DirectorySeparatorChar));
        string elementDir = Path.Combine(templateDir, subfolder);
        GumProjectSave project = new ProjectLoader().Load(Path.Combine(templateDir, "GumProject.gumx")).Project!;

        List<string> onDisk = Directory.GetFiles(elementDir, "*." + extension, SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(elementDir, file).Replace('\\', '/'))
            .Select(relative => relative.Substring(0, relative.Length - extension.Length - 1))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        List<string> inManifest = File.ReadAllLines(Path.Combine(templateDir, "manifest.txt"))
            .Select(line => line.Trim())
            .Where(line => line.StartsWith(subfolder + "/", StringComparison.Ordinal)
                && line.EndsWith("." + extension, StringComparison.Ordinal))
            .Select(line => line.Substring(subfolder.Length + 1, line.Length - subfolder.Length - extension.Length - 2))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        List<ElementReference> references = subfolder == "Screens" ? project.ScreenReferences : project.ComponentReferences;
        List<string> inGumx = references
            .Select(reference => reference.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        inGumx.ShouldBe(onDisk, $"{templateFolder}/{subfolder}: .gumx references vs files on disk");
        inManifest.ShouldBe(onDisk, $"{templateFolder}/{subfolder}: manifest.txt vs files on disk");
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
