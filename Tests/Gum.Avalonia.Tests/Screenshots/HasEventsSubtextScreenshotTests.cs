using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Has Events row with its cursor-only subtext, at every zoom level
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter HasEventsSubtextScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class HasEventsSubtextScreenshotTests
{
    [SkippableFact]
    public void HasEventsRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave box = grid.Project.AddInstance(button, "Box", "Container");
        grid.Select(box);
        grid.Input.TypeInto(grid.View.FilterTextBox, "Has Events");

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "has-events-row");
    });
}
