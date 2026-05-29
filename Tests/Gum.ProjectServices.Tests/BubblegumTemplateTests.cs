using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Validates the bundled Bubblegum theme template after its migration off the retired
/// ColoredRectangle / RoundedRectangle / ColoredCircle standards (PR #2975). Guards against a
/// regression where a future edit reintroduces a retired standard or leaves a dangling color
/// reference.
/// </summary>
public class BubblegumTemplateTests
{
    private static readonly string[] RetiredStandards = { "ColoredRectangle", "RoundedRectangle", "ColoredCircle" };
    private static readonly string[] LegacyColorChannels = { "Red", "Green", "Blue", "Alpha" };

    private static GumProjectSave LoadBubblegum()
    {
        StandardElementsManager.Self.Initialize();

        string bubblegumDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Bubblegum");
        ProjectLoadResult result = new ProjectLoader().Load(Path.Combine(bubblegumDir, "GumProject.gumx"));

        result.Success.ShouldBeTrue();
        result.LoadErrors.ShouldBeEmpty();
        return result.Project!;
    }

    [Fact]
    public void Load_CircleInstances_ShouldNotCarryRadiusVariable()
    {
        // Circle sizes via Width/Height since #2947 — "Radius" was dropped from the standard.
        // PR #2975 left redundant "Radius = Width/2" writes on the radio/slider-thumb circles;
        // they self-heal on load but should not linger in the committed template.
        GumProjectSave project = LoadBubblegum();

        List<string> offenders = new();
        foreach (ElementSave element in project.AllElements)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableSave variable in state.Variables)
                {
                    if (variable.Name != null && GetLastNameSegment(variable.Name) == "Radius")
                    {
                        offenders.Add($"{element.Name} / {state.Name}: {variable.Name}");
                    }
                }
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Load_ShouldNotReferenceRetiredStandardFiles()
    {
        string standardsDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Bubblegum", "Standards");

        foreach (string retired in RetiredStandards)
        {
            File.Exists(Path.Combine(standardsDir, retired + ".gutx"))
                .ShouldBeFalse($"{retired}.gutx should have been removed");
        }
    }

    [Fact]
    public void Load_ShouldNotUseRetiredBaseTypes()
    {
        GumProjectSave project = LoadBubblegum();

        List<string> offenders = new();
        foreach (ElementSave element in project.AllElements)
        {
            foreach (InstanceSave instance in element.Instances)
            {
                if (RetiredStandards.Contains(instance.BaseType))
                {
                    offenders.Add($"{element.Name}.{instance.Name} -> {instance.BaseType}");
                }
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Load_StylesColorReferences_ShouldTargetFillChannels()
    {
        GumProjectSave project = LoadBubblegum();

        List<string> dangling = new();
        foreach (ElementSave element in project.AllElements)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableListSave variableList in state.VariableLists)
                {
                    if (variableList.GetRootName() != "VariableReferences")
                    {
                        continue;
                    }

                    foreach (string referenceString in variableList.ValueAsIList.Cast<string>())
                    {
                        int equalsIndex = referenceString.IndexOf('=');
                        if (equalsIndex < 0)
                        {
                            continue;
                        }

                        string right = referenceString.Substring(equalsIndex + 1).Trim();
                        if (!right.Contains("Styles."))
                        {
                            continue;
                        }

                        string channel = right.Substring(right.LastIndexOf('.') + 1);
                        if (LegacyColorChannels.Contains(channel))
                        {
                            dangling.Add($"{element.Name} / {state.Name}: {referenceString}");
                        }
                    }
                }
            }
        }

        dangling.ShouldBeEmpty();
    }

    private static string GetLastNameSegment(string variableName)
    {
        int lastDotIndex = variableName.LastIndexOf('.');
        return lastDotIndex < 0 ? variableName : variableName.Substring(lastDotIndex + 1);
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
