using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab's Color Operation row for a selected Sprite, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter ColorOperationRowScreenshotTests -NoBefore</c>). The row
/// does not exist on the base, so there is no "before".
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ColorOperationRowScreenshotTests
{
    [SkippableFact]
    public void ColorOperationRow_ForASprite() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave card = grid.Project.AddComponent("Card");
        InstanceSave sprite = grid.Project.AddInstance(card, "Silhouette", "Sprite");
        grid.Select(sprite);
        grid.ViewModel.VariableFilterText = "ColorOperation";
        grid.Settle();

        PrScreenshot.SaveWindow(grid.Input.Window, "color-operation-row");
    });
}
