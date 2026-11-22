using Avalonia.Controls;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A NineSlice's Custom Frame Texture Coordinate Width row, linked and broken apart, at every zoom level
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter FrameTextureCoordinateVariablesScreenshotTests</c>, #8354).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class FrameTextureCoordinateVariablesScreenshotTests
{
    [Fact]
    public void FrameTextureCoordinateLinked() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Select(AddNineSlice(grid));
        grid.Input.TypeInto(grid.View.FilterTextBox, "CustomFrameTextureCoordinate");
        grid.Input.Window.Height = 160;
        grid.Settle();

        PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "8354-frame-coordinate-linked");
    });

    [Fact]
    public void FrameTextureCoordinateBrokenApart() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Select(AddNineSlice(grid));

        foreach ((string filter, string rowName) in new[] { ("BorderScale", "BorderScale"), ("CustomFrameTextureCoordinate", "CustomFrameTextureCoordinateWidth") })
        {
            grid.Input.TypeInto(grid.View.FilterTextBox, filter);
            grid.Settle();
            grid.Input.Click(grid.Editor<NineSliceSidesDisplay>(rowName).GetVisualDescendants().OfType<Button>().Single());
            grid.Settle();
            foreach ((int index, string text) in new[] { (0, "6"), (1, "4"), (3, "2") })
            {
                grid.Input.TypeAndEnter(grid.Editor<NineSliceSidesDisplay>(rowName).CornerTextBoxes[index], text);
                grid.Settle();
            }
            grid.Input.Window.Height = 200;
            grid.Settle();

            // Border Scale is the existing per-side row, captured beside the new one to compare.
            PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, "8354-" + filter + "-broken-apart");
        }
    });

    private static InstanceSave AddNineSlice(VariableGridHarness grid)
    {
        ComponentSave panel = grid.Project.AddComponent("Panel");
        return grid.Project.AddInstance(panel, "Frame", "NineSlice");
    }
}
