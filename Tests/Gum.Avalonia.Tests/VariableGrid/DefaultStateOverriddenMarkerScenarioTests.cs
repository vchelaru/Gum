using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// With Default selected on a Forms control, the Variables tab marks a row the active Enabled or Disabled
/// category state also sets: a running game shows that state's value, not the Default one (#6857).
/// </summary>
public class DefaultStateOverriddenMarkerScenarioTests
{
    internal static (ComponentSave Button, InstanceSave Label, StateSave Enabled, StateSave Disabled) BuildButton(VariableGridHarness grid)
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");
        grid.Project.Project.Behaviors.Add(behavior);

        ComponentSave button = grid.Project.AddComponent("Button");
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behavior.Name });
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        StateSaveCategory category = grid.Project.AddCategory(button, "ButtonCategory");
        StateSave enabled = grid.Project.AddState(button, category, "Enabled");
        StateSave disabled = grid.Project.AddState(button, category, "Disabled");
        button.DefaultState!.SetValue("Label.Width", 50f, "float");
        enabled.SetValue("Label.Width", 200f, "float");
        disabled.SetValue("Label.Width", 300f, "float");
        foreach (string channel in new[] { "Red", "Green", "Blue" })
        {
            button.DefaultState!.SetValue("Label." + channel, 10, "int");
            enabled.SetValue("Label." + channel, 200, "int");
            disabled.SetValue("Label." + channel, 90, "int");
        }
        return (button, label, enabled, disabled);
    }

    [AvaloniaFact]
    public void DefaultSelected_RowTheEnabledStateAlsoSets_IsMarkedOverriddenByEnabled()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave button, InstanceSave label, _, _) = BuildButton(grid);
        grid.Select(button);

        grid.Select(label);

        grid.Member("Label.Width").DetailText.ShouldContain("Overridden by the Enabled state");
    }

    [AvaloniaFact]
    public void DefaultSelected_WhileIsEnabledFalse_RowIsMarkedOverriddenByDisabled()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave button, InstanceSave label, _, _) = BuildButton(grid);
        button.DefaultState!.SetValue("IsEnabled", false, "bool");
        grid.Select(button);

        grid.Select(label);

        grid.Member("Label.Width").DetailText.ShouldContain("Overridden by the Disabled state");
    }

    [AvaloniaFact]
    public void DefaultSelected_RowNoCategoryStateSets_HasNoMarker()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave button, InstanceSave label, _, _) = BuildButton(grid);
        grid.Select(button);

        grid.Select(label);

        (grid.Member("Label.Height").DetailText ?? "").ShouldNotContain("Overridden");
    }

    [AvaloniaFact]
    public void CategoryStateSelected_RowItSets_HasNoMarker()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave button, InstanceSave label, StateSave enabled, _) = BuildButton(grid);
        grid.Select(button);
        grid.Select(enabled);

        grid.Select(label);

        (grid.Member("Label.Width").DetailText ?? "").ShouldNotContain("Overridden");
    }

    [AvaloniaFact]
    public void DefaultSelected_ColorRowTheCategoryStateSetsByChannel_IsMarkedOverridden()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        (ComponentSave button, InstanceSave label, _, _) = BuildButton(grid);
        grid.Select(button);

        grid.Select(label);

        grid.Member("Color").DetailText.ShouldContain("Overridden by the Enabled state");
    }
}
