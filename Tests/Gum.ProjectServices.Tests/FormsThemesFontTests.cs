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
/// targets, not extracted from embedded resources, so nothing else sweeps it for a stray system font
/// name (e.g. "Arial") that KernSmith can't resolve on BlazorGL/WASM.
/// </summary>
public class FormsThemesFontTests
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
            .SelectMany(e => e.DefaultState.Variables, (e, v) => (Element: e, Variable: v))
            .Where(x => x.Variable.IsFont && x.Variable.Value is string font
                && !font.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"{x.Element.Name}.{x.Variable.Name} = {x.Variable.Value}")
            .ToList();

        systemFontReferences.ShouldBeEmpty();
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
