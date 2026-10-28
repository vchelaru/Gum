using Avalonia.Controls;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A NineSlice's Border Scale row, linked and broken apart, at every zoom level
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter BorderScaleVariablesScreenshotTests</c>, #6983).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class BorderScaleVariablesScreenshotTests
{
    [SkippableFact]
    public void BorderScaleLinked() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Select(AddNineSlice(grid));
        grid.Input.TypeInto(grid.View.FilterTextBox, "BorderScale");
        grid.Input.Window.Height = 140;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "6983-border-scale-linked-diamond");
    });

    [SkippableFact]
    public void BorderScaleBrokenApart() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Select(AddNineSlice(grid));
        grid.Input.TypeInto(grid.View.FilterTextBox, "BorderScale");

        BorderScaleDisplay display = grid.Editor<BorderScaleDisplay>("BorderScale");
        grid.Input.Click(display.GetVisualDescendants().OfType<Button>().Single());
        grid.Settle();
        display = grid.Editor<BorderScaleDisplay>("BorderScale");
        foreach ((int index, string text) in new[] { (0, "2"), (1, "0"), (3, "0.5") })
        {
            grid.Input.TypeAndEnter(grid.Editor<BorderScaleDisplay>("BorderScale").CornerTextBoxes[index], text);
            grid.Settle();
        }
        grid.Input.Window.Height = 200;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "6983-border-scale-broken-apart-diamond");
    });

    private static InstanceSave AddNineSlice(VariableGridHarness grid)
    {
        ComponentSave panel = grid.Project.AddComponent("Panel");
        return grid.Project.AddInstance(panel, "Frame", "NineSlice");
    }
}
