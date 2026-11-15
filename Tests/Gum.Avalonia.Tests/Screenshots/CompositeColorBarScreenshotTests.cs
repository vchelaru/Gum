using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// DarkPro Slider's TrackBackground FillColor and StrokeColor rows with SliderCategory colored, both themes at every
/// zoom level (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter CompositeColorBarScreenshotTests</c>, #7661).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class CompositeColorBarScreenshotTests
{
    [SkippableFact]
    public void CompositeRowsColorBar() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        grid.Project.AddComponent("Other");
        ComponentSave slider = DarkProSliderSetByScenarioTests.LoadSlider(grid);
        DarkProSliderSetByScenarioTests.Show(grid, slider, slider.Instances.Single(i => i.Name == "TrackBackground"));
        grid.Input.TypeInto(grid.View.FilterTextBox, "Color");
        grid.Input.Window.Height = 330;
        grid.Settle();

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            grid.Input.Window.RequestedThemeVariant = theme;
            grid.Settle();
            PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, $"7661-composite-bar-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    });
}
