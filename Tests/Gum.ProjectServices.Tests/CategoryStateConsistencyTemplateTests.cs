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
/// #7648: if any state of a category sets a variable, every state of that category sets it
/// (the other states with the Default state's value), so switching states never leaves a value
/// stuck from another state.
/// </summary>
public class CategoryStateConsistencyTemplateTests
{
    private static readonly string[] ThemeNames =
    {
        "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95"
    };

    // Gaps waiting on a maintainer decision (no Default or first-state value to copy); see #7648.
    private static readonly HashSet<string> KnownUnresolvedGaps = new()
    {
        "SizingCategory|ClipContainerInstance.MaxHeight",
        "ListBoxItemCategory|Background.VariableReferences",
        "MenuItemCategory|Background.VariableReferences",
        // Selected sets a literal dark text color; the Default reference would turn it white.
        "Controls/ListBoxItem|ListBoxItemCategory|TextInstance.VariableReferences",
    };

    public static IEnumerable<object[]> TemplateDirectories()
    {
        yield return new object[] { "FormsTemplate" };
        foreach (string theme in ThemeNames)
        {
            yield return new object[] { Path.Combine("FormsThemes", theme) };
        }
    }

    [Theory]
    [MemberData(nameof(TemplateDirectories))]
    public void EveryCategoryState_ShouldSetEveryVariableSetByAnyStateOfThatCategory(string templateDir)
    {
        StandardElementsManager.Self.Initialize();
        string projectPath = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", templateDir, "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue();

        List<string> gaps = new();
        foreach (ComponentSave component in result.Project!.Components)
        {
            foreach (StateSaveCategory category in component.Categories.Where(c => c.States.Count >= 2))
            {
                Dictionary<string, HashSet<string>> setBy = new();
                foreach (StateSave state in category.States)
                {
                    foreach (string name in GetSetNames(state))
                    {
                        if (!setBy.TryGetValue(name, out HashSet<string>? states))
                        {
                            setBy[name] = states = new HashSet<string>();
                        }
                        states.Add(state.Name);
                    }
                }

                foreach ((string name, HashSet<string> states) in setBy)
                {
                    if (states.Count == category.States.Count || (KnownUnresolvedGaps.Contains($"{category.Name}|{name}") || KnownUnresolvedGaps.Contains($"{component.Name}|{category.Name}|{name}")))
                    {
                        continue;
                    }
                    string missing = string.Join(",", category.States.Select(s => s.Name).Except(states));
                    gaps.Add($"{component.Name} {category.Name} {name}: not set in {missing}");
                }
            }
        }

        gaps.ShouldBeEmpty(string.Join(Environment.NewLine, gaps));
    }

    private static IEnumerable<string> GetSetNames(StateSave state)
    {
        foreach (VariableSave variable in state.Variables.Where(v => v.SetsValue && v.Value != null))
        {
            yield return variable.Name;
        }
        foreach (VariableListSave list in state.VariableLists.Where(l => l.ValueAsIList != null))
        {
            yield return list.Name;
        }
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
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
        throw new InvalidOperationException("Could not locate the repo root from " + AppContext.BaseDirectory);
    }
}
