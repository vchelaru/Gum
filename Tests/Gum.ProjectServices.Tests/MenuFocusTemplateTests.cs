using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Like WPF, a focused Menu shows focus only through its highlighted top-level item (#6706). Every
/// tool template's Menu has the MenuCategory the Menu control applies, and its focused states set
/// nothing the unfocused ones don't.
/// </summary>
public class MenuFocusTemplateTests
{
    public static IEnumerable<object[]> Menus()
    {
        yield return new object[] { "FormsTemplate", "Controls/Menu" };
        yield return new object[] { "FormsThemes/Bubblegum", "Bubblegum/Controls/Menu" };
        yield return new object[] { "FormsThemes/DarkPro", "DarkPro/Controls/Menu" };
        yield return new object[] { "FormsThemes/Editor", "Editor/Controls/Menu" };
        yield return new object[] { "FormsThemes/ForestGlade", "ForestGlade/Controls/Menu" };
        yield return new object[] { "FormsThemes/Hazard", "Hazard/Controls/Menu" };
        yield return new object[] { "FormsThemes/Meadow", "Meadow/Controls/Menu" };
        yield return new object[] { "FormsThemes/Neon", "Neon/Controls/Menu" };
        yield return new object[] { "FormsThemes/Retro95", "Retro95/Controls/Menu" };
    }

    [Theory]
    [MemberData(nameof(Menus))]
    public void Menu_FocusedStates_ShouldChangeNothingOnTheMenu(string templateFolder, string menuName)
    {
        StandardElementsManager.Self.Initialize();
        string projectPath = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates",
            templateFolder.Replace('/', Path.DirectorySeparatorChar), "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue();
        ComponentSave menu = result.Project!.Components.Single(c => c.Name == menuName);

        StateSaveCategory category = menu.Categories.SingleOrDefault(c => c.Name == "MenuCategory")
            .ShouldNotBeNull(menuName);
        category.States.Select(s => s.Name).ShouldBe(
            new[] { "Enabled", "Disabled", "Focused", "DisabledFocused" }, ignoreOrder: true, menuName);
        Describe(category, "Focused").ShouldBe(Describe(category, "Enabled"), menuName);
        Describe(category, "DisabledFocused").ShouldBe(Describe(category, "Disabled"), menuName);
    }

    private static string Describe(StateSaveCategory category, string stateName) =>
        string.Join(", ", category.States.Single(s => s.Name == stateName).Variables
            .OrderBy(v => v.Name, StringComparer.Ordinal)
            .Select(v => $"{v.Name}={v.Value}"));

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current)!;
        }
        throw new DirectoryNotFoundException("Could not find the repo root from " + AppContext.BaseDirectory);
    }
}
