using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Behavior rows with a description (Raise Children Events Outside Of Bounds), one contact sheet per theme
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter RaiseChildrenEventsRowScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class RaiseChildrenEventsRowScreenshotTests
{
    [SkippableFact]
    public void BehaviorEventRows() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave box = grid.Project.AddInstance(button, "Box", "Container");
        grid.Select(box);
        grid.Input.TypeInto(grid.View.FilterTextBox, "Events");

        Window window = grid.Input.Window;
        window.Height = 330;
        ThemeVariant? original = window.RequestedThemeVariant;
        try
        {
            foreach ((string name, ThemeVariant theme) in new[] { ("dark", ThemeVariant.Dark), ("light", ThemeVariant.Light) })
            {
                window.RequestedThemeVariant = theme;
                PrScreenshot.SaveContactSheet(window, $"behavior-event-rows-{name}");
            }
        }
        finally
        {
            window.RequestedThemeVariant = original;
        }
    });
}
