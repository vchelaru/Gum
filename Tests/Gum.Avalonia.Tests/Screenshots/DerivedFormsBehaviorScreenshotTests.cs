using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab of a component derived from a Forms control (its base carries the behavior), and of an
/// instance of it with IsEnabled turned off, in both themes at every zoom level
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter DerivedFormsBehaviorScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class DerivedFormsBehaviorScreenshotTests
{
    [SkippableFact]
    public void DerivedComponentAndItsInstance() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "ToolTip", Value = null });
        grid.Project.Project.Behaviors.Add(behavior);

        ComponentSave button = grid.Project.AddComponent("Button");
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        ComponentSave fancy = grid.Project.AddComponent("FancyButton", "Button");
        ScreenSave screen = grid.Project.AddScreen("MenuScreen");
        InstanceSave instance = grid.Project.AddInstance(screen, "FancyInstance", "FancyButton");
        screen.DefaultState!.Variables.Add(new VariableSave { Name = "FancyInstance.IsEnabled", Type = "bool", Value = false, SetsValue = true });

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            string themeName = theme.Key.ToString()!.ToLowerInvariant();
            grid.Input.Window.RequestedThemeVariant = theme;

            grid.Select(fancy);
            PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, $"7651-derived-component-{themeName}");

            // Selecting the same target again would not rebuild the grid, so move through another element first.
            grid.Select(button);
            grid.Select(screen);
            grid.Select(instance);
            grid.Input.TypeInto(grid.View.FilterTextBox, "Enabled");
            PrScreenshot.SaveWindowAtZoomLevels(grid.Input.Window, $"7651-derived-instance-{themeName}");
            grid.Select(button);
        }
    });
}
