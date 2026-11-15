using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A component derived from a Slider whose SliderCategory sets VariableReferences: the row's "Set by"
/// line and color bar come from the inherited category
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter InheritedCategorySetByScreenshotTests</c>, #7657).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class InheritedCategorySetByScreenshotTests
{
    [SkippableFact]
    public void Light() => PrScreenshot.Run(() => Capture(ThemeVariant.Light, "7657-inherited-set-by-light"));

    [SkippableFact]
    public void Dark() => PrScreenshot.Run(() => Capture(ThemeVariant.Dark, "7657-inherited-set-by-dark"));

    private static void Capture(ThemeVariant theme, string name)
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Project.AddComponent("Other");
        ComponentSave slider = DarkProSliderSetByScenarioTests.LoadSlider(grid);
        slider.DefaultState!.VariableLists.Add(new VariableListSave<string> { Name = "VariableReferences", Type = "string", Value = new List<string> { "X = 1" } });
        slider.Categories.Single().States.Single(s => s.Name == "Disabled").VariableLists.Add(
            new VariableListSave<string> { Name = "VariableReferences", Type = "string", Value = new List<string> { "X = 2" } });

        ComponentSave derived = grid.Project.AddComponent("RedSlider");
        derived.BaseType = slider.Name;
        derived.DefaultState!.VariableLists.Add(new VariableListSave<string> { Name = "VariableReferences", Type = "string", Value = new List<string> { "X = 1" } });
        // Selecting the same target again does not rebuild the grid, so leave the element first.
        grid.Select((ElementSave)grid.Project.Project.Components.First(c => c.Name == "Other"));
        grid.Select(derived);
        grid.Select(derived.DefaultState);

        grid.Input.Window.RequestedThemeVariant = theme;
        grid.Input.TypeInto(grid.View.FilterTextBox, "VariableReferences");
        grid.Input.Window.Height = 160;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, name);
    }
}
