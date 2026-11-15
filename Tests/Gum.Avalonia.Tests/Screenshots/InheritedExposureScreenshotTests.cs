using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// An inner instance's rows on a component derived from the one that exposes them, as one contact sheet per
/// theme (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter InheritedExposureScreenshotTests</c>, #7650).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class InheritedExposureScreenshotTests
{
    [SkippableFact]
    public void ExposedByBase_SelectedOnDerived() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        grid.Project.Project.Behaviors.Add(behavior);

        ComponentSave button = grid.Project.AddComponent("Button");
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        ComponentSave baseRow = grid.Project.AddComponent("BaseRow");
        InstanceSave inner = grid.Project.AddInstance(baseRow, "ButtonInstance1", "Button");
        grid.Select(inner);
        StateSave baseDefault = baseRow.DefaultState!;
        baseDefault.Variables.Add(new VariableSave { Name = "ButtonInstance1.IsEnabled", Type = "bool", SetsValue = false, ExposedAsName = "IsEnabled" });
        baseDefault.Variables.Add(new VariableSave { Name = "ButtonInstance1.Width", Type = "float", SetsValue = false, ExposedAsName = "ButtonWidth" });

        ComponentSave derived = grid.Project.AddComponent("DerivedRow");
        derived.BaseType = "BaseRow";

        InstanceSave inherited = new InstanceSave { Name = "ButtonInstance1", BaseType = "Button", DefinedByBase = true, ParentContainer = derived };
        derived.Instances.Add(inherited);
        grid.Select(derived);
        grid.Select(inherited);
        grid.Input.TypeInto(grid.View.FilterTextBox, "e");

        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            grid.Input.Window.RequestedThemeVariant = theme;
            PrScreenshot.SaveContactSheet(grid.Input.Window, $"inherited-exposure-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    });
}
