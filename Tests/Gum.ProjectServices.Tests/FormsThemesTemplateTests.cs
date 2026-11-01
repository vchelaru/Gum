using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
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
        "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95"
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
        "FormsThemes/Bubblegum", "FormsThemes/DarkPro", "FormsThemes/Editor", "FormsThemes/ForestGlade", "FormsThemes/Hazard",
        "FormsThemes/Meadow", "FormsThemes/Neon", "FormsThemes/Retro95"
    };

    // The Standard template ships no FontCache; its text resolves through the runtime font creator.
    public static IEnumerable<object[]> ThemeFolders =>
        AllTemplateFolders.Where(folder => folder != "FormsTemplate").Select(folder => new object[] { folder });

    public static IEnumerable<object[]> TemplateFolders => AllTemplateFolders.Select(folder => new object[] { folder });

    public static IEnumerable<object[]> TemplateFoldersAndSubfolders =>
        AllTemplateFolders.SelectMany(folder => new[]
        {
            new object[] { folder, "Components", GumProjectSave.ComponentExtension },
            new object[] { folder, "Screens", GumProjectSave.ScreenExtension },
        });

    // Add Forms imports what the theme's .gumx references. An element on disk but not in the .gumx
    // is never imported, and is never checked or seen while the theme is edited (#5348).
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

    // A new project never ships sample screens beyond the demo screen: Add Forms, gumcli new and the
    // New Project dialog all copy these folders (#7244).
    [Theory]
    [MemberData(nameof(TemplateFolders))]
    public void Screens_ShouldBeOnlyTheDemoScreen(string templateFolder)
    {
        StandardElementsManager.Self.Initialize();
        string templateDir = GetTemplateDir(templateFolder);
        GumProjectSave project = new ProjectLoader().Load(Path.Combine(templateDir, "GumProject.gumx")).Project!;
        string screensDir = Path.Combine(templateDir, "Screens");

        List<string> names = project.ScreenReferences.Select(reference => reference.Name)
            .Concat(Directory.GetFiles(screensDir, "*", SearchOption.AllDirectories)
                .Select(file => Path.ChangeExtension(Path.GetRelativePath(screensDir, file), null).Replace((char)92, '/')))
            .Concat((templateFolder == "FormsTemplate" ? File.ReadAllLines(Path.Combine(templateDir, "manifest.txt")) : Array.Empty<string>())
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("Screens/", StringComparison.Ordinal))
                .Select(line => Path.ChangeExtension(line.Substring("Screens/".Length), null)))
            .ToList();

        names.ShouldNotBeEmpty();
        names.Where(name => !DemoScreenRule.IsDemoScreenName(name)).ShouldBeEmpty($"{templateFolder} lists or ships a screen that is not the demo screen");
    }

    // Slider and ScrollBar thumbs get their own components (copies of the button) so restyling a thumb
    // does not restyle every button (#6165), and restyling one control's thumb does not restyle the
    // other's (#6808). DarkPro and Neon, and every theme's SliderThumb except Editor's, are already
    // styled, so they are not button copies.
    [Theory]
    [InlineData("FormsTemplate", "SliderThumb")]
    [InlineData("FormsTemplate", "ScrollBarThumb")]
    [InlineData("FormsThemes/Editor", "SliderThumb")]
    [InlineData("FormsThemes/Editor", "ScrollBarThumb")]
    [InlineData("FormsThemes/Bubblegum", "ScrollBarThumb")]
    [InlineData("FormsThemes/ForestGlade", "ScrollBarThumb")]
    [InlineData("FormsThemes/Hazard", "ScrollBarThumb")]
    [InlineData("FormsThemes/Meadow", "ScrollBarThumb")]
    [InlineData("FormsThemes/Retro95", "ScrollBarThumb")]
    public void Load_ShouldHaveThumbComponentCopiedFromTheButton(string templateFolder, string thumbName)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = LoadTemplate(templateFolder);

        string prefix = GetPrefix(templateFolder);
        string buttonName = prefix + (prefix.Length == 0 ? "Controls/ButtonStandard" : "Controls/Button");

        ComponentSave button = project.Components.Single(c => c.Name == buttonName);
        ComponentSave thumb = project.Components.Single(c => c.Name == $"{prefix}Controls/{thumbName}");

        thumb.BaseType.ShouldBe(button.BaseType);
        thumb.Instances.Select(i => (i.Name, i.BaseType))
            .ShouldBe(button.Instances.Select(i => (i.Name, i.BaseType)));
        thumb.Behaviors.Select(b => b.BehaviorName)
            .ShouldBe(button.Behaviors.Select(b => b.BehaviorName));
        thumb.DefaultState!.Variables.Count.ShouldBe(button.DefaultState!.Variables.Count);
    }

    // Each control's thumb is its own component, so a game can style the slider thumb and the scroll
    // bar thumb differently without adding a component (#6808).
    [Theory]
    [MemberData(nameof(TemplateFolders))]
    public void ThumbInstance_ShouldUseItsControlsOwnThumbComponent(string templateFolder)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = LoadTemplate(templateFolder);
        string prefix = GetPrefix(templateFolder);

        foreach (string controlName in new[] { "Slider", "ScrollBar" })
        {
            ComponentSave control = project.Components.Single(c => c.Name == $"{prefix}Controls/{controlName}");

            control.Instances.Single(i => i.Name == "ThumbInstance").BaseType
                .ShouldBe($"{prefix}Controls/{controlName}Thumb");
        }
    }

    // The shared Thumb component is gone from what Add Forms ships. Projects that already have one keep
    // it, since their own Slider and ScrollBar still reference it.
    [Theory]
    [MemberData(nameof(TemplateFolders))]
    public void Load_ShouldNotHaveSharedThumbComponent(string templateFolder)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = LoadTemplate(templateFolder);

        project.Components.Select(c => c.Name).ShouldNotContain(GetPrefix(templateFolder) + "Controls/Thumb");
    }

    private static GumProjectSave LoadTemplate(string templateFolder) =>
        new ProjectLoader().Load(Path.Combine(GetTemplateDir(templateFolder), "GumProject.gumx")).Project!;

    private static string GetPrefix(string templateFolder) =>
        templateFolder.StartsWith("FormsThemes/", StringComparison.Ordinal)
            ? templateFolder.Substring("FormsThemes/".Length) + "/"
            : string.Empty;

    // The scroll bar's up and down arrows get their own ScrollBarButton component (a copy of ButtonIcon)
    // so restyling them does not restyle every icon button (#6165). Only the template, Editor and
    // Retro95 have arrows; the other themes' scroll bars have none.
    [Theory]
    [InlineData("FormsTemplate", "")]
    [InlineData("FormsThemes/Editor", "Editor/")]
    [InlineData("FormsThemes/Retro95", "Retro95/")]
    public void ScrollBarArrows_ShouldUseScrollBarButtonCopiedFromButtonIcon(string templateFolder, string prefix)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new ProjectLoader()
            .Load(Path.Combine(GetTemplateDir(templateFolder), "GumProject.gumx")).Project!;

        ComponentSave buttonIcon = project.Components.Single(c => c.Name == prefix + "Controls/ButtonIcon");
        ComponentSave scrollBarButton = project.Components.Single(c => c.Name == prefix + "Controls/ScrollBarButton");
        ComponentSave scrollBar = project.Components.Single(c => c.Name == prefix + "Controls/ScrollBar");

        scrollBarButton.BaseType.ShouldBe(buttonIcon.BaseType);
        scrollBarButton.Instances.Select(i => (i.Name, i.BaseType))
            .ShouldBe(buttonIcon.Instances.Select(i => (i.Name, i.BaseType)));
        scrollBarButton.Behaviors.Select(b => b.BehaviorName)
            .ShouldBe(buttonIcon.Behaviors.Select(b => b.BehaviorName));
        scrollBarButton.DefaultState!.Variables.Count.ShouldBe(buttonIcon.DefaultState!.Variables.Count);

        scrollBar.Instances.Single(i => i.Name == "UpButtonInstance").BaseType
            .ShouldBe(prefix + "Controls/ScrollBarButton");
        scrollBar.Instances.Single(i => i.Name == "DownButtonInstance").BaseType
            .ShouldBe(prefix + "Controls/ScrollBarButton");
    }

    // The submenu indicator is a fixed-size Icon showing Arrow2, not a '>' in a font (#6164, #6173). Only
    // the template and Editor are covered: the other themes' Icon elements have no ColorCategory (or
    // no Icon at all), so the arrow cannot follow the per-state text color there yet.
    [Theory]
    [InlineData("FormsTemplate", "")]
    [InlineData("FormsThemes/Editor", "Editor/")]
    public void SubmenuIndicatorInstance_ShouldBeAnArrow2Icon(string templateFolder, string prefix)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new ProjectLoader()
            .Load(Path.Combine(GetTemplateDir(templateFolder), "GumProject.gumx")).Project!;

        ComponentSave menuItem = project.Components.Single(c => c.Name == prefix + "Controls/MenuItem");
        StateSave defaultState = menuItem.DefaultState!;

        menuItem.Instances.Single(i => i.Name == "SubmenuIndicatorInstance").BaseType
            .ShouldBe(prefix + "Elements/Icon");
        defaultState.GetValue("SubmenuIndicatorInstance.IconCategoryState").ShouldBe("Arrow2");
        defaultState.GetValue("SubmenuIndicatorInstance.Width").ShouldBe(16f);
        defaultState.GetValue("SubmenuIndicatorInstance.Height").ShouldBe(16f);
        defaultState.GetValue("SubmenuIndicatorInstance.WidthUnits").ShouldBe(DimensionUnitType.Absolute);
        defaultState.GetValue("SubmenuIndicatorInstance.HeightUnits").ShouldBe(DimensionUnitType.Absolute);
        defaultState.GetValue("SubmenuIndicatorInstance.Text").ShouldBeNull();

        foreach (StateSave state in menuItem.Categories.Single(c => c.Name == "MenuItemCategory").States)
        {
            state.GetValue("SubmenuIndicatorInstance.ColorCategoryState")
                .ShouldNotBeNull($"{prefix}MenuItem state {state.Name}");
        }
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

    // The release loads a template without a runtime font creator, so a font missing from FontCache
    // silently falls back to the default font, and an icon glyph such as the ComboBox arrow draws as
    // '?' (#7387). Every font an element uses has a cached .fnt, which was baked with the project's
    // FontRanges (the glyph block the icons live in).
    [Theory]
    [MemberData(nameof(ThemeFolders))]
    public void FontCache_ShouldContainEveryFontTheTemplateUses(string templateFolder)
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
            string fontCacheDir = Path.Combine(templateDir, "FontCache");

            List<string> missing = new FontReferenceCollector(instance => ObjectFinder.Self.GetElementSave(instance))
                .Collect(project, allElements)
                .Keys
                .Select(Path.GetFileName)
                .Where(fileName => !File.Exists(Path.Combine(fontCacheDir, fileName!)))
                .OrderBy(fileName => fileName, StringComparer.Ordinal)
                .ToList()!;

            missing.ShouldBeEmpty();
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    // A cached font's .bmfc ships with the template, so its font file is relative to the cache, never
    // the path of the machine that generated it, and it names no font family (#6722).
    [Theory]
    [MemberData(nameof(TemplateFolders))]
    public void FontCacheBmfc_ShouldReferenceFontFileRelatively(string templateFolder)
    {
        string fontCacheDir = Path.Combine(GetTemplateDir(templateFolder), "FontCache");
        if (!Directory.Exists(fontCacheDir))
        {
            return;
        }

        List<string> wrong = new();
        foreach (string bmfc in Directory.GetFiles(fontCacheDir, "*.bmfc"))
        {
            foreach (string line in File.ReadAllLines(bmfc))
            {
                bool rootedFontFile = line.StartsWith("fontFile=") && Path.IsPathRooted(line.Substring("fontFile=".Length));
                bool namedFont = line.StartsWith("fontName=") && line.Length > "fontName=".Length;
                if (rootedFontFile || namedFont)
                {
                    wrong.Add($"{Path.GetFileName(bmfc)}: {line}");
                }
            }
        }

        wrong.ShouldBeEmpty();
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
