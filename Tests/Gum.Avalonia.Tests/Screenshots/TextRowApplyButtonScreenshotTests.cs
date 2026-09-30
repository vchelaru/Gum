using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab's Text row with an unapplied edit, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter TextRowApplyButtonScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class TextRowApplyButtonScreenshotTests
{
    [SkippableFact]
    public void TextRow_WithAnUnappliedEdit() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);
        grid.ViewModel.VariableFilterText = "Text";
        grid.Settle();

        grid.TextField("Text").Text = "Press start\nto play";
        grid.Settle();

        PrScreenshot.SaveWindow(grid.Input.Window, "text-row-unapplied-edit");
    });
}
