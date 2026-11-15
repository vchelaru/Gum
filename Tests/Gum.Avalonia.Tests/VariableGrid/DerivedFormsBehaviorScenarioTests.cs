using Avalonia.Headless.XUnit;
using AvaloniaDataUi.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>Unchecking IsEnabled in the Variables tab on an instance of a component derived from a Forms control (#7651).</summary>
public class DerivedFormsBehaviorScenarioTests
{
    [AvaloniaFact]
    public void UncheckingIsEnabled_OnAnInstanceOfADerivedComponent_WritesTheDisabledCategoryState()
    {
        GumExpressionService.Initialize();
        using VariableGridHarness grid = new VariableGridHarness();
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");
        grid.Project.Project.Behaviors.Add(behavior);
        ComponentSave button = grid.Project.AddComponent("Button");
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        grid.Project.AddComponent("FancyButton", "Button");
        ScreenSave screen = grid.Project.AddScreen("MenuScreen");
        InstanceSave instance = grid.Project.AddInstance(screen, "FancyInstance", "FancyButton");
        grid.Select(screen);
        grid.Select(instance);

        grid.Editor<CheckBoxDisplay>("IsEnabled").CheckBox.IsChecked = false;
        grid.Settle();

        screen.DefaultState!.GetValue("FancyInstance.ButtonCategoryState").ShouldBe("Disabled");
    }
}
