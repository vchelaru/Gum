using Avalonia.Controls;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab's gradient unit rows for a radial-gradient Circle, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter GradientUnitsScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class GradientUnitsScreenshotTests
{
    [SkippableFact]
    public void GradientUnitRows_ForARadialCircle() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave card = grid.Project.AddComponent("Card");
        InstanceSave circle = grid.Project.AddInstance(card, "Ring", "Circle");
        grid.Select(circle);
        grid.Input.Click(grid.Editor<CheckBoxDisplay>("UseGradient").CheckBox);
        grid.Settle();
        grid.PickComboItem("GradientType", "Radial");
        grid.ViewModel.VariableFilterText = "Gradient";
        grid.Settle();
        grid.Input.Window.Height = 520;
        grid.Settle();

        PrScreenshot.SaveWindow(grid.Input.Window, "gradient-units");
    });
}
