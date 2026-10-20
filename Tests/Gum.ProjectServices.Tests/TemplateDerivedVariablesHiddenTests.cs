using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A visual variable that a behavior derives from a Forms property (the left side of a
/// ToolOnlyVariableReference, e.g. LineModeCategoryState from TextWrapping) must be hidden from
/// instances on every template component using that behavior. The runtime reads the Forms
/// property, so an instance that sets the visual variable directly looks right in the tool and
/// behaves differently in the game.
/// </summary>
public class TemplateDerivedVariablesHiddenTests
{
    /// <summary>The Standard template and every theme folder, so a new theme is enforced without editing this list.</summary>
    public static IEnumerable<object[]> TemplateProjects
    {
        get
        {
            string templates = FindTemplatesDirectory();
            List<string> paths = new List<string> { "FormsTemplate" };
            paths.AddRange(Directory.GetDirectories(Path.Combine(templates, "FormsThemes"))
                .Select(directory => "FormsThemes/" + Path.GetFileName(directory))
                .OrderBy(path => path, StringComparer.Ordinal));
            return paths.Select(path => new object[] { path });
        }
    }

    /// <summary>
    /// Categories on a Forms control that an instance is meant to set: a ListBox's Fixed/FitChildren
    /// sizing and a Label's text styles. Every other category on a Forms control is driven by the
    /// control itself at runtime.
    /// </summary>
    private static readonly HashSet<string> InstanceAuthoredCategories = new HashSet<string>(StringComparer.Ordinal)
    {
        "SizingCategory",
        "TextCategory",
    };

    [Theory]
    [MemberData(nameof(TemplateProjects))]
    public void DerivedVariables_ShouldBeHiddenFromInstances(string templateRelativePath)
    {
        StandardElementsManager.Self.Initialize();
        string projectPath = Path.Combine(FindTemplatesDirectory(), templateRelativePath, "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue(result.ErrorMessage);
        GumProjectSave project = result.Project!;

        List<string> unhidden = new List<string>();
        foreach (ComponentSave component in project.Components)
        {
            foreach (ElementBehaviorReference reference in component.Behaviors)
            {
                BehaviorSave? behavior = project.Behaviors.FirstOrDefault(item => item.Name == reference.BehaviorName);
                if (behavior == null)
                {
                    continue;
                }
                foreach (string assignment in behavior.ToolOnlyVariableReferences)
                {
                    string derivedVariable = assignment.Split('=')[0].Trim();
                    if (!component.VariablesHiddenFromInstances.Contains(derivedVariable))
                    {
                        unhidden.Add($"{component.Name}.{derivedVariable} (from {behavior.Name})");
                    }
                }
            }
        }

        unhidden.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(TemplateProjects))]
    public void ControlDrivenStateCategories_ShouldBeHiddenFromInstances(string templateRelativePath)
    {
        StandardElementsManager.Self.Initialize();
        string projectPath = Path.Combine(FindTemplatesDirectory(), templateRelativePath, "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue(result.ErrorMessage);
        GumProjectSave project = result.Project!;

        List<string> unhidden = new List<string>();
        foreach (ComponentSave component in project.Components.Where(item => item.Behaviors.Count > 0))
        {
            foreach (StateSaveCategory category in component.Categories)
            {
                string stateVariable = category.Name + "State";
                if (!InstanceAuthoredCategories.Contains(category.Name)
                    && !component.VariablesHiddenFromInstances.Contains(stateVariable))
                {
                    unhidden.Add($"{component.Name}.{stateVariable}");
                }
            }
        }

        unhidden.ShouldBeEmpty();
    }

    private static string FindTemplatesDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate the templates directory from " + AppContext.BaseDirectory);
    }
}
