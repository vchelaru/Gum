using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab's toggle-strip rows as the panel is dragged narrow, at every zoom level, for a PR's
/// before/after images (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ToggleStripRowScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ToggleStripRowScreenshotTests
{
    [SkippableTheory]
    [InlineData(400)]
    [InlineData(300)]
    [InlineData(240)]
    [InlineData(180)]
    public void UnitsAndOriginRows_WithThePanelDraggedNarrow(int width) => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Container");
        grid.Select(label);
        grid.Input.Window.Width = width;
        grid.Input.Window.Height = 620;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, $"toggle-strip-rows-width-{width}");
    });

    [SkippableFact]
    public void VariableReferencesRow_LabelOnOneLine() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Container");
        grid.Select(label);
        grid.Input.TypeInto(grid.View.FilterTextBox, "References");
        grid.Input.Window.Width = 300;
        grid.Input.Window.Height = 300;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "variable-references-row");
    });
}
