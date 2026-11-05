using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab row of a Forms property (IsEnabled) that a component exposes from an inner instance, next
/// to an exposed plain variable, as one contact sheet per theme
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ExposedFormsPropertyScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ExposedFormsPropertyScreenshotTests
{
    [SkippableFact]
    public void ExposedIsEnabledRow() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        grid.Project.Project.Behaviors.Add(behavior);

        ComponentSave button = grid.Project.AddComponent("Button");
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        ComponentSave row = grid.Project.AddComponent("OptionsRow");
        InstanceSave inner = grid.Project.AddInstance(row, "ButtonInstance1", "Button");
        StateSave rowDefault = row.DefaultState;
        rowDefault.Variables.Add(new VariableSave { Name = "ButtonInstance1.IsEnabled", Type = "bool", SetsValue = false, ExposedAsName = "IsEnabled" });
        rowDefault.Variables.Add(new VariableSave { Name = "ButtonInstance1.Width", Type = "float", SetsValue = false, ExposedAsName = "ButtonWidth" });

        grid.Select(inner);
        grid.Input.TypeInto(grid.View.FilterTextBox, "e");

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            grid.Input.Window.RequestedThemeVariant = theme;
            PrScreenshot.SaveContactSheet(grid.Input.Window, $"exposed-isenabled-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    });
}
