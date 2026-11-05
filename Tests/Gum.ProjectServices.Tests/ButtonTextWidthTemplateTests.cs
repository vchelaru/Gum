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
/// #7639: every button's text instance is RelativeToParent with a width of 0, or slightly
/// negative where the button reserves margins, so text wider than the button wraps instead of
/// overflowing. An unset width falls back to the Text default of 100, which makes the text 100
/// wider than its button.
/// </summary>
public class ButtonTextWidthTemplateTests
{
    // Smallest margin a button template reserves on each side pair (Retro95 and ForestGlade use -8).
    private const float MostNegativeAllowedWidth = -16;

    private static readonly string[] ThemeNames =
    {
        "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95"
    };

    public static IEnumerable<object[]> ButtonComponents()
    {
        foreach (string theme in ThemeNames)
        {
            string themeDir = Path.Combine("FormsThemes", theme);
            yield return new object[] { themeDir, $"{theme}/Button" };
            yield return new object[] { themeDir, $"{theme}/ToggleButton" };
        }

        string formsTemplate = "FormsTemplate";
        yield return new object[] { formsTemplate, "Controls/ButtonStandard" };
        yield return new object[] { formsTemplate, "Controls/ButtonConfirm" };
        yield return new object[] { formsTemplate, "Controls/ButtonDeny" };
        yield return new object[] { formsTemplate, "Controls/ToggleButton" };
    }

    [Theory]
    [MemberData(nameof(ButtonComponents))]
    public void TextInstance_ShouldBeRelativeToParentWithZeroOrSlightlyNegativeWidth(
        string templateDir, string componentName)
    {
        StandardElementsManager.Self.Initialize();
        string projectPath = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", templateDir, "GumProject.gumx");
        ProjectLoadResult result = new ProjectLoader().Load(projectPath);
        result.Success.ShouldBeTrue();

        ComponentSave component = result.Project!.Components.Single(c => c.Name == componentName);
        VariableSave? width = component.DefaultState.Variables.FirstOrDefault(v => v.Name == "TextInstance.Width");
        VariableSave? widthUnits = component.DefaultState.Variables.FirstOrDefault(v => v.Name == "TextInstance.WidthUnits");

        widthUnits.ShouldNotBeNull($"{componentName} must set TextInstance.WidthUnits");
        Convert.ToInt32(widthUnits!.Value).ShouldBe((int)DimensionUnitType.RelativeToParent, componentName);

        width.ShouldNotBeNull($"{componentName} must set TextInstance.Width; unset falls back to 100");
        float widthValue = Convert.ToSingle(width!.Value);
        widthValue.ShouldBeLessThanOrEqualTo(0, componentName);
        widthValue.ShouldBeGreaterThanOrEqualTo(MostNegativeAllowedWidth, componentName);
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
