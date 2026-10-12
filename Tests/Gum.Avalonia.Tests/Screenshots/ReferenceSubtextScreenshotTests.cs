using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A Variables tab row driven by a variable reference, at every zoom level, for a PR's before/after images
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ReferenceSubtextScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ReferenceSubtextScreenshotTests
{
    [SkippableFact]
    public void ReferenceDrivenRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave leader = grid.Project.AddInstance(button, "Leader", "Text");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(leader);
        grid.TypeAndEnter("Width", "120");
        grid.Select(label);
        grid.TypeLinesAndApply("VariableReferences", "Width = Leader.Width");
        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "reference-driven-row");
    });
}
