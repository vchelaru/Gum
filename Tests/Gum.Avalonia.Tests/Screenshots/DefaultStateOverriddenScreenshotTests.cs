using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// Variables tab rows the active category state overrides while a Button's Default is selected, at every
/// zoom level, for a PR's before/after images (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter DefaultStateOverriddenScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class DefaultStateOverriddenScreenshotTests
{
    [SkippableFact]
    public void OverriddenWidthRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave button, InstanceSave label, _, _) = DefaultStateOverriddenMarkerScenarioTests.BuildButton(grid);
        grid.Select(button);
        grid.Select(label);
        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "overridden-width-row");
    });

    [SkippableFact]
    public void OverriddenColorRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave button, InstanceSave label, _, _) = DefaultStateOverriddenMarkerScenarioTests.BuildButton(grid);
        button.DefaultState!.SetValue("IsEnabled", false, "bool");
        grid.Select(button);
        grid.Select(label);
        grid.Input.TypeInto(grid.View.FilterTextBox, "Color");

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "overridden-color-row");
    });
}
