using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A Variables tab row driven by a variable reference, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter ReferenceSubtextScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ReferenceSubtextScreenshotTests
{
    [SkippableFact]
    public void ReferenceDrivenRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(button);
        grid.TypeAndEnter("Width", "120");
        grid.Select(label);
        grid.TypeLinesAndApply("VariableReferences", "Width = Width + 10");
        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");

        PrScreenshot.SaveWindow(grid.Input.Window, "reference-driven-row");
    });
}
