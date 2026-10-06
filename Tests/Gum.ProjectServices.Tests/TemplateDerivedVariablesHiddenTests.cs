using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
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
    public static IEnumerable<object[]> TemplateProjects => new[]
    {
        "FormsTemplate",
        "FormsThemes/Bubblegum", "FormsThemes/DarkPro", "FormsThemes/ForestGlade", "FormsThemes/Hazard",
        "FormsThemes/Meadow", "FormsThemes/Neon", "FormsThemes/Retro95",
    }.Select(path => new object[] { path });

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
