using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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

    [Fact]
    public void AllPhysicalComponentFiles_ShouldBeBubblegumNamespaced()
    {
        // FormsFileService.GetSourceDestinations copies every .gucx file it finds on disk
        // under the theme folder, regardless of whether that file is registered as a
        // ComponentReference in the theme's own GumProject.gumx. A file whose own <Name> tag
        // was left unrenamed during the Bubblegum/ namespacing pass still gets imported under
        // its stale name, colliding with (or duplicating) content from every other theme's
        // import.
        string bubblegumDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Bubblegum");

        List<string> componentFiles = Directory
            .GetFiles(Path.Combine(bubblegumDir, "Components"), "*.gucx", SearchOption.AllDirectories)
            .ToList();

        List<string> offenders = new();
        foreach (string file in componentFiles)
        {
            string? name = XDocument.Load(file).Root?.Element("Name")?.Value;
            if (name != null && !name.StartsWith("Bubblegum/", StringComparison.Ordinal))
            {
                offenders.Add($"{Path.GetFileName(file)}: <Name>{name}</Name>");
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void AllBehaviorFiles_ShouldBeSharedAndUnnamespaced()
    {
        // Unlike Components (which define a theme's look), Behaviors declare the generic
        // category-state/FormsProperty contract every theme's visuals plug into - the same
        // content every project and theme needs. They live flat at the project root
        // (Behaviors/*.behx, matching Standards) rather than namespaced per theme, so
        // importing a theme reuses/overwrites the shared behavior instead of adding a
        // redundant Bubblegum-specific copy alongside it.
        string behaviorsDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Bubblegum", "Behaviors");

        List<string> topLevelBehaviorFiles = Directory
            .GetFiles(behaviorsDir, "*.behx", SearchOption.TopDirectoryOnly)
            .ToList();
        List<string> allBehaviorFiles = Directory
            .GetFiles(behaviorsDir, "*.behx", SearchOption.AllDirectories)
            .ToList();

        allBehaviorFiles.ShouldBe(topLevelBehaviorFiles, ignoreOrder: true,
            customMessage: "Behavior files must live directly under Behaviors/, not in a per-theme subfolder.");

        List<string> offenders = new();
        foreach (string file in topLevelBehaviorFiles)
        {
            string? name = XDocument.Load(file).Root?.Element("Name")?.Value;
            if (name != null && name.StartsWith("Bubblegum/", StringComparison.Ordinal))
            {
                offenders.Add($"{Path.GetFileName(file)}: <Name>{name}</Name>");
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void StylesPalette_ShouldExactlyMatchCodeOnlyBubblegumColors()
    {
        // Components/Bubblegum/Styles.gucx exists so tool content can reference one set of
        // color/text tokens instead of hardcoding values. Its swatch names must match
        // Themes/Gum.Themes.Bubblegum.MonoGame/BubblegumStyling.cs's BubblegumColors/BubblegumText
        // properties exactly (not the generic Standard/FormsTemplate naming this once inherited),
        // and carry no swatch beyond what that class defines - otherwise the tool content silently
        // drifts from the theme it's supposed to mirror.
        string[] expectedColorSwatches =
        {
            "Accent", "AccentDark", "AccentHover", "AccentLight", "Background", "Border",
            "Disabled", "DisabledFill", "Muted", "Placeholder", "Surface1", "Text", "White"
        };
        string[] expectedTextStyles = { "Normal", "Strong" };

        GumProjectSave project = LoadBubblegum();
        ComponentSave styles = project.Components.Single(c => c.Name == "Bubblegum/Styles");

        List<string> actualColorSwatches = styles.Instances
            .Where(i => i.BaseType is "Rectangle")
            .Select(i => i.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        List<string> actualTextStyles = styles.Instances
            .Where(i => i.BaseType is "Text")
            .Select(i => i.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        actualColorSwatches.ShouldBe(expectedColorSwatches.OrderBy(n => n, StringComparer.Ordinal).ToList());
        actualTextStyles.ShouldBe(expectedTextStyles.OrderBy(n => n, StringComparer.Ordinal).ToList());
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
