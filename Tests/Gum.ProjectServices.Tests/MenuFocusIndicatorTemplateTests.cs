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
/// Every tool template's Menu shows the same focus bar as its code-only MenuVisual: a
/// MenuCategory with Enabled, Disabled, Focused and DisabledFocused, where only the focused
/// states show the FocusedIndicator bar under the menu (#6706).
/// </summary>
public class MenuFocusIndicatorTemplateTests
{
    public static IEnumerable<object[]> Menus()
    {
        yield return new object[] { "FormsTemplate", "Controls/Menu", 2f, 2f };
        yield return new object[] { "FormsThemes/Bubblegum", "Bubblegum/Controls/Menu", 3f, 2f };
        yield return new object[] { "FormsThemes/DarkPro", "DarkPro/Controls/Menu", 1f, 1f };
        yield return new object[] { "FormsThemes/Editor", "Editor/Controls/Menu", 2f, 2f };
        yield return new object[] { "FormsThemes/ForestGlade", "ForestGlade/Controls/Menu", 3f, 3f };
        yield return new object[] { "FormsThemes/Hazard", "Hazard/Controls/Menu", 1f, 1f };
        yield return new object[] { "FormsThemes/Meadow", "Meadow/Controls/Menu", 2.5f, 2f };
        yield return new object[] { "FormsThemes/Neon", "Neon/Controls/Menu", 1f, 4f };
        yield return new object[] { "FormsThemes/Retro95", "Retro95/Controls/Menu", 1f, 2f };
    }

    [Theory]
    [MemberData(nameof(Menus))]
    public void Menu_ShouldShowFocusedIndicatorOnlyInFocusedStates(
        string templateFolder, string menuName, float expectedHeight, float expectedY)
    {
        StandardElementsManager.Self.Initialize();
        string projectPath = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates",
            templateFolder.Replace('/', Path.DirectorySeparatorChar), "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue();
        ComponentSave menu = result.Project!.Components.Single(c => c.Name == menuName);

        menu.Instances.ShouldContain(i => i.Name == "FocusedIndicator", menuName);
        StateSave defaultState = menu.DefaultState!;
        ValueOf(defaultState, "FocusedIndicator.Visible").ShouldBe(false, menuName);
        ValueOf(defaultState, "FocusedIndicator.Height").ShouldBe(expectedHeight, menuName);
        ValueOf(defaultState, "FocusedIndicator.Y").ShouldBe(expectedY, menuName);

        StateSaveCategory category = menu.Categories.SingleOrDefault(c => c.Name == "MenuCategory")
            .ShouldNotBeNull(menuName);
        category.States.Select(s => s.Name).ShouldBe(
            new[] { "Enabled", "Disabled", "Focused", "DisabledFocused" }, ignoreOrder: true, menuName);
        ValueOf(category.States.Single(s => s.Name == "Enabled"), "FocusedIndicator.Visible").ShouldBe(false, menuName);
        ValueOf(category.States.Single(s => s.Name == "Disabled"), "FocusedIndicator.Visible").ShouldBe(false, menuName);
        ValueOf(category.States.Single(s => s.Name == "Focused"), "FocusedIndicator.Visible").ShouldBe(true, menuName);
        ValueOf(category.States.Single(s => s.Name == "DisabledFocused"), "FocusedIndicator.Visible").ShouldBe(true, menuName);
    }

    private static object? ValueOf(StateSave state, string variableName) =>
        state.Variables.FirstOrDefault(v => v.Name == variableName)?.Value;

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
