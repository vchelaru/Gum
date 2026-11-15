using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// Variables rows set by a category only in a later state, in both themes at every zoom level, for #7647's
/// before/after (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter SetByAnyStateScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class SetByAnyStateScreenshotTests
{
    [SkippableFact]
    public void VariableReferencesRow() => Capture("Variable References", "variable-references");

    [SkippableFact]
    public void PlainRow() => Capture("Width", "width");

    private static void Capture(string filter, string name) => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave slider, InstanceSave track, _) = SetByAnyStateScenarioTests.BuildSlider(grid.Project);
        grid.Select(slider);
        grid.Select(track);
        grid.Select(slider.GetDefaultStateOrThrow());
        grid.Input.TypeInto(grid.View.FilterTextBox, filter);
        grid.Input.Window.Height = 300;

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            grid.Input.Window.RequestedThemeVariant = theme;
            PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, $"{name}-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    });
}
